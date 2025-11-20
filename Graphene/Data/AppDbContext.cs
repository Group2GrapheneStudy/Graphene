using Graphene_Group_Project.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Graphene_Group_Project.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<DataFile> DataFiles => Set<DataFile>();
        public DbSet<PressureFrame> PressureFrames => Set<PressureFrame>();
        public DbSet<Alert> Alerts => Set<Alert>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Primary Keys
            b.Entity<Patient>().HasKey(p => p.PatientId);
            b.Entity<DataFile>().HasKey(f => f.FileId);
            b.Entity<PressureFrame>().HasKey(f => f.FrameId);
            b.Entity<Alert>().HasKey(a => a.AlertId);

            // Relations

            // Patient 1..* DataFiles
            b.Entity<DataFile>()
                .HasOne(f => f.Patient)
                .WithMany(p => p.Files)
                .HasForeignKey(f => f.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient 1..* PressureFrames
            b.Entity<PressureFrame>()
                .HasOne(f => f.Patient)
                .WithMany(p => p.Frames)
                .HasForeignKey(f => f.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            // DataFile 1..* PressureFrames
            b.Entity<PressureFrame>()
                .HasOne(f => f.File)
                .WithMany(df => df.Frames)
                .HasForeignKey(f => f.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient 1..* Alerts
            b.Entity<Alert>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Alerts)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Alert -> PressureFrame
            b.Entity<Alert>()
                .HasOne(a => a.Frame)
                .WithMany()
                .HasForeignKey(a => a.FrameId)
                .OnDelete(DeleteBehavior.NoAction);

            // Decimal for PressureFrame
            b.Entity<PressureFrame>()
                .Property(f => f.ContactAreaPct)
                .HasColumnType("decimal(18,2)");

            // Indexes
            b.Entity<Patient>()
                .HasIndex(p => p.ExternalUserId)
                .IsUnique()
#if !SQLITEx
                .HasFilter("[ExternalUserId] IS NOT NULL");
#endif

            b.Entity<DataFile>()
                .HasIndex(f => new { f.PatientId, f.FilePath })
                .IsUnique();

            b.Entity<PressureFrame>()
                .HasIndex(f => new { f.PatientId, f.CapturedUtc });

            b.Entity<PressureFrame>()
                .HasIndex(f => new { f.PatientId, f.FileId, f.FrameIndex })
                .IsUnique();

            b.Entity<Alert>()
                .HasIndex(a => new { a.PatientId, a.TriggeredUtc });

            b.Entity<Alert>()
                .HasIndex(a => new { a.Status, a.Severity });
        }
    }
}
