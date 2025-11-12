using Graphene_Group_Project.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

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
            b.Entity<Patient>()
                .HasIndex(p => p.ExternalUserId)
                .IsUnique()
#if !SQLITE
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
