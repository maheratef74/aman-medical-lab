using DrMohamedWeb.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DrMohamedWeb.Infrastructure.Data
{
    public class AmanDbContext : DbContext
    {
        public AmanDbContext(DbContextOptions<AmanDbContext> options) : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientVisit> PatientVisits { get; set; }
        public DbSet<TestResult> TestResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Entity Relationships
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Visits)
                .WithOne(v => v.Patient)
                .HasForeignKey(v => v.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PatientVisit>()
                .HasMany(v => v.TestResults)
                .WithOne(t => t.Visit)
                .HasForeignKey(t => t.VisitId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
