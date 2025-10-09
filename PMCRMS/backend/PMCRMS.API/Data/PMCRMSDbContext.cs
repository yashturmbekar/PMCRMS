using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Models;

namespace PMCRMS.API.Data
{
    public class PMCRMSDbContext : DbContext
    {
        public PMCRMSDbContext(DbContextOptions<PMCRMSDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }
        public DbSet<ApplicationStatus> ApplicationStatuses { get; set; }
        public DbSet<ApplicationComment> ApplicationComments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }
        
        // Position Application entities
        public DbSet<PositionApplication> PositionApplications { get; set; }
        public DbSet<SEAddress> SEAddresses { get; set; }
        public DbSet<SEQualification> SEQualifications { get; set; }
        public DbSet<SEExperience> SEExperiences { get; set; }
        public DbSet<SEDocument> SEDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.PhoneNumber).IsUnique();
                entity.Property(e => e.Role).HasConversion<int>();
            });

            // Configure Application entity
            modelBuilder.Entity<Application>(entity =>
            {
                entity.HasIndex(e => e.ApplicationNumber).IsUnique();
                entity.Property(e => e.Type).HasConversion<int>();
                entity.Property(e => e.CurrentStatus).HasConversion<int>();
                
                entity.HasOne(e => e.Applicant)
                    .WithMany(e => e.Applications)
                    .HasForeignKey(e => e.ApplicantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ApplicationDocument entity
            modelBuilder.Entity<ApplicationDocument>(entity =>
            {
                entity.Property(e => e.Type).HasConversion<int>();
                
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.Documents)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.VerifiedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.VerifiedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure ApplicationStatus entity
            modelBuilder.Entity<ApplicationStatus>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<int>();
                
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.StatusHistory)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany(e => e.StatusUpdates)
                    .HasForeignKey(e => e.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ApplicationComment entity
            modelBuilder.Entity<ApplicationComment>(entity =>
            {
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.Comments)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.CommentedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CommentedBy)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.ParentComment)
                    .WithMany(e => e.Replies)
                    .HasForeignKey(e => e.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Payment entity
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(e => e.Status).HasConversion<int>();
                entity.Property(e => e.Method).HasConversion<int>();
                entity.HasIndex(e => e.PaymentId).IsUnique();
                entity.HasIndex(e => e.TransactionId).IsUnique();
                
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.Payments)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.ProcessedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ProcessedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure OtpVerification entity
            modelBuilder.Entity<OtpVerification>(entity =>
            {
                entity.HasIndex(e => new { e.Identifier, e.Purpose, e.IsActive });
            });

            // Configure PositionApplication entity
            modelBuilder.Entity<PositionApplication>(entity =>
            {
                entity.HasIndex(e => e.ApplicationNumber).IsUnique();
                entity.Property(e => e.PositionType).HasConversion<int>();
                entity.Property(e => e.Gender).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure SEAddress entity
            modelBuilder.Entity<SEAddress>(entity =>
            {
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.Addresses)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SEQualification entity
            modelBuilder.Entity<SEQualification>(entity =>
            {
                entity.Property(e => e.Specialization).HasConversion<int>();
                
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.Qualifications)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SEExperience entity
            modelBuilder.Entity<SEExperience>(entity =>
            {
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.Experiences)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SEDocument entity
            modelBuilder.Entity<SEDocument>(entity =>
            {
                entity.Property(e => e.DocumentType).HasConversion<int>();
                
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.Documents)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.VerifiedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.VerifiedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "System Administrator",
                    Email = "admin@pmcrms.gov.in",
                    PhoneNumber = "9999999999",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "System"
                }
            );

            // Seed sample officers
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 2,
                    Name = "Junior Engineer",
                    Email = "je@pmcrms.gov.in",
                    PhoneNumber = "9999999998",
                    Role = UserRole.JuniorEngineer,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "System"
                },
                new User
                {
                    Id = 3,
                    Name = "Assistant Engineer",
                    Email = "ae@pmcrms.gov.in",
                    PhoneNumber = "9999999997",
                    Role = UserRole.AssistantEngineer,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "System"
                },
                new User
                {
                    Id = 4,
                    Name = "Executive Engineer",
                    Email = "ee@pmcrms.gov.in",
                    PhoneNumber = "9999999996",
                    Role = UserRole.ExecutiveEngineer,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "System"
                },
                new User
                {
                    Id = 5,
                    Name = "City Engineer",
                    Email = "ce@pmcrms.gov.in",
                    PhoneNumber = "9999999995",
                    Role = UserRole.CityEngineer,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "System"
                },
                new User
                {
                    Id = 6,
                    Name = "Clerk",
                    Email = "clerk@pmcrms.gov.in",
                    PhoneNumber = "9999999994",
                    Role = UserRole.Clerk,
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "System"
                }
            );
        }

        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedDate = DateTime.UtcNow;
                        break;
                }
            }
        }
    }
}