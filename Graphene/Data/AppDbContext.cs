using Graphene_Group_Project.Data.Entities;
using Graphene_Group_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graphene_Group_Project.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<DataFile> DataFiles { get; set; }
        public DbSet<PressureFrame> PressureFrames { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }

        // NEW: persisted patient–clinician messages
        public DbSet<PatientMessage> PatientMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // PRIMARY KEYS
            b.Entity<Patient>().HasKey(p => p.PatientId);
            b.Entity<DataFile>().HasKey(f => f.FileId);
            b.Entity<PressureFrame>().HasKey(f => f.FrameId);
            b.Entity<Alert>().HasKey(a => a.AlertId);
            b.Entity<UserAccount>().HasKey(u => u.Id);
            b.Entity<PatientMessage>().HasKey(m => m.Id);

            // RELATIONSHIPS

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

            // Alert -> PressureFrame (optional)
            b.Entity<Alert>()
                .HasOne(a => a.Frame)
                .WithMany()
                .HasForeignKey(a => a.FrameId)
                .OnDelete(DeleteBehavior.NoAction);

            // NEW: Patient 1..* PatientMessages
            b.Entity<PatientMessage>()
                .HasOne(m => m.Patient)
                .WithMany(p => p.Messages)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // NEW: optional link PatientMessage -> PressureFrame
            b.Entity<PatientMessage>()
                .HasOne(m => m.Frame)
                .WithMany()
                .HasForeignKey(m => m.FrameId)
                .OnDelete(DeleteBehavior.NoAction);

            // Decimal precision for ContactAreaPct
            b.Entity<PressureFrame>()
                .Property(f => f.ContactAreaPct)
                .HasColumnType("decimal(18,2)");

            // INDEXES

            b.Entity<Patient>()
                .HasIndex(p => p.ExternalUserId)
                .IsUnique();

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

            // Optional: index messages by patient/time for faster retrieval
            b.Entity<PatientMessage>()
                .HasIndex(m => new { m.PatientId, m.CreatedUtc });
        }
    }
}
