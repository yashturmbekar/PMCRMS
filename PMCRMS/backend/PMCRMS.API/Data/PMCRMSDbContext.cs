using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Models;

namespace PMCRMS.API.Data
{
    public class PMCRMSDbContext : DbContext
    {
        public PMCRMSDbContext(DbContextOptions<PMCRMSDbContext> options) : base(options)
        {
        }

        // Authentication & Authorization
        public DbSet<SystemAdmin> SystemAdmins { get; set; }
        public DbSet<Officer> Officers { get; set; }
        public DbSet<User> Users { get; set; } // Regular applicants/citizens
        
        // Core Application Management
        public DbSet<Application> Applications { get; set; }
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }
        public DbSet<ApplicationStatus> ApplicationStatuses { get; set; }
        public DbSet<ApplicationComment> ApplicationComments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OfficerInvitation> OfficerInvitations { get; set; }
        public DbSet<FormConfiguration> FormConfigurations { get; set; }
        public DbSet<FormFeeHistory> FormFeeHistories { get; set; }
        
        // Position Application entities
        public DbSet<PositionApplication> PositionApplications { get; set; }
        public DbSet<SEAddress> SEAddresses { get; set; }
        public DbSet<SEQualification> SEQualifications { get; set; }
        public DbSet<SEExperience> SEExperiences { get; set; }
        public DbSet<SEDocument> SEDocuments { get; set; }

        // Junior Engineer Workflow entities
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<DocumentVerification> DocumentVerifications { get; set; }
        public DbSet<DigitalSignature> DigitalSignatures { get; set; }
        public DbSet<AutoAssignmentRule> AutoAssignmentRules { get; set; }
        public DbSet<AssignmentHistory> AssignmentHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure SystemAdmin entity
            modelBuilder.Entity<SystemAdmin>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.EmployeeId).IsUnique();
            });

            // Configure Officer entity
            modelBuilder.Entity<Officer>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.EmployeeId).IsUnique();
                entity.Property(e => e.Role).HasConversion<int>();
            });

            // Configure User entity (Applicants + LEGACY Officers/Admins during migration)
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.PhoneNumber)
                    .IsUnique()
                    .HasFilter("\"PhoneNumber\" IS NOT NULL");
                entity.HasIndex(e => e.EmployeeId)
                    .IsUnique()
                    .HasFilter("\"EmployeeId\" IS NOT NULL");
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
                    
                entity.HasOne(e => e.VerifiedByOfficer)
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
                    
                entity.HasOne(e => e.UpdatedByOfficer)
                    .WithMany(e => e.StatusUpdates)
                    .HasForeignKey(e => e.UpdatedByOfficerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // TEMPORARY: Backward compatibility
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
                    
                entity.HasOne(e => e.CommentedByOfficer)
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
                    
                entity.HasOne(e => e.ProcessedByOfficer)
                    .WithMany()
                    .HasForeignKey(e => e.ProcessedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure OtpVerification entity
            modelBuilder.Entity<OtpVerification>(entity =>
            {
                entity.HasIndex(e => new { e.Identifier, e.Purpose, e.IsActive });
            });

            // Configure Notification entity
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(e => e.Priority).HasConversion<int>();
                entity.HasIndex(e => new { e.UserId, e.IsRead });
                entity.HasIndex(e => e.CreatedDate);
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Application)
                    .WithMany()
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure PositionApplication entity
            modelBuilder.Entity<PositionApplication>(entity =>
            {
                entity.HasIndex(e => e.ApplicationNumber).IsUnique();
                entity.HasIndex(e => e.AssignedJuniorEngineerId);
                entity.HasIndex(e => new { e.Status, e.AssignedJuniorEngineerId });
                entity.Property(e => e.PositionType).HasConversion<int>();
                entity.Property(e => e.Gender).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.AssignedJuniorEngineer)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedJuniorEngineerId)
                    .OnDelete(DeleteBehavior.SetNull);
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
                    
                entity.HasOne(e => e.VerifiedByOfficer)
                    .WithMany()
                    .HasForeignKey(e => e.VerifiedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure OfficerInvitation entity
            modelBuilder.Entity<OfficerInvitation>(entity =>
            {
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.EmployeeId).IsUnique();
                entity.Property(e => e.Role).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                
                entity.HasOne(e => e.InvitedByAdmin)
                    .WithMany()
                    .HasForeignKey(e => e.InvitedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.Officer)
                    .WithOne(e => e.Invitation)
                    .HasForeignKey<OfficerInvitation>(e => e.OfficerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Appointment entity
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasIndex(e => e.ApplicationId);
                entity.HasIndex(e => e.ScheduledByOfficerId);
                entity.HasIndex(e => new { e.Status, e.ReviewDate });
                entity.Property(e => e.Status).HasConversion<int>();
                
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.Appointments)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.ScheduledByOfficer)
                    .WithMany()
                    .HasForeignKey(e => e.ScheduledByOfficerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.RescheduledToAppointment)
                    .WithOne(e => e.RescheduledFromAppointment)
                    .HasForeignKey<Appointment>(e => e.RescheduledToAppointmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure DocumentVerification entity
            modelBuilder.Entity<DocumentVerification>(entity =>
            {
                entity.HasIndex(e => e.ApplicationId);
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.VerifiedByOfficerId);
                entity.HasIndex(e => new { e.Status, e.VerifiedDate });
                entity.Property(e => e.Status).HasConversion<int>();
                
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.DocumentVerifications)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.Document)
                    .WithMany()
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.VerifiedByOfficer)
                    .WithMany()
                    .HasForeignKey(e => e.VerifiedByOfficerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure DigitalSignature entity
            modelBuilder.Entity<DigitalSignature>(entity =>
            {
                entity.HasIndex(e => e.ApplicationId);
                entity.HasIndex(e => e.SignedByOfficerId);
                entity.HasIndex(e => e.HsmTransactionId);
                entity.HasIndex(e => new { e.Status, e.SignedDate });
                entity.Property(e => e.Type).HasConversion<int>();
                entity.Property(e => e.Status).HasConversion<int>();
                
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.DigitalSignatures)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.SignedByOfficer)
                    .WithMany()
                    .HasForeignKey(e => e.SignedByOfficerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure AutoAssignmentRule entity
            modelBuilder.Entity<AutoAssignmentRule>(entity =>
            {
                entity.HasIndex(e => new { e.PositionType, e.IsActive });
                entity.HasIndex(e => new { e.TargetOfficerRole, e.IsActive });
                entity.HasIndex(e => e.Priority);
                entity.Property(e => e.PositionType).HasConversion<int>();
                entity.Property(e => e.TargetOfficerRole).HasConversion<int>();
                entity.Property(e => e.Strategy).HasConversion<int>();
                entity.Property(e => e.EscalationRole).HasConversion<int>();
            });

            // Configure AssignmentHistory entity
            modelBuilder.Entity<AssignmentHistory>(entity =>
            {
                entity.HasIndex(e => e.ApplicationId);
                entity.HasIndex(e => e.AssignedToOfficerId);
                entity.HasIndex(e => e.PreviousOfficerId);
                entity.HasIndex(e => new { e.IsActive, e.AssignedDate });
                entity.Property(e => e.Action).HasConversion<int>();
                entity.Property(e => e.StrategyUsed).HasConversion<int>();
                entity.Property(e => e.ApplicationStatusAtAssignment).HasConversion<int>();
                
                entity.HasOne(e => e.Application)
                    .WithMany(e => e.AssignmentHistories)
                    .HasForeignKey(e => e.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.PreviousOfficer)
                    .WithMany()
                    .HasForeignKey(e => e.PreviousOfficerId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(e => e.AssignedToOfficer)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedToOfficerId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(e => e.AutoAssignmentRule)
                    .WithMany(e => e.AssignmentHistories)
                    .HasForeignKey(e => e.AutoAssignmentRuleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure FormConfiguration entity
            modelBuilder.Entity<FormConfiguration>(entity =>
            {
                entity.HasIndex(e => e.FormType).IsUnique();
                entity.Property(e => e.FormType).HasConversion<int>();
            });

            // Configure FormFeeHistory entity
            modelBuilder.Entity<FormFeeHistory>(entity =>
            {
                entity.HasOne(e => e.FormConfiguration)
                    .WithMany(e => e.FeeHistory)
                    .HasForeignKey(e => e.FormConfigurationId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(e => e.ChangedByAdmin)
                    .WithMany()
                    .HasForeignKey(e => e.ChangedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                // TEMPORARY: Backward compatibility
                entity.HasOne(e => e.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ChangedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default system administrator
            modelBuilder.Entity<SystemAdmin>().HasData(
                new SystemAdmin
                {
                    Id = 1,
                    Name = "System Administrator",
                    Email = "admin@gmail.com",
                    PhoneNumber = "9999999999",
                    EmployeeId = "ADMIN001",
                    IsActive = true,
                    IsSuperAdmin = true,
                    Department = "Administration",
                    Designation = "System Administrator",
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "System"
                }
            );

            // Seed default form configurations
            modelBuilder.Entity<FormConfiguration>().HasData(
                new FormConfiguration
                {
                    Id = 1,
                    FormName = "Building Permit Application",
                    FormType = FormType.BuildingPermit,
                    Description = "Application for new building construction or major renovation",
                    BaseFee = 5000m,
                    ProcessingFee = 1000m,
                    LateFee = 500m,
                    IsActive = true,
                    AllowOnlineSubmission = true,
                    ProcessingDays = 30,
                    MaxFileSizeMB = 10,
                    MaxFilesAllowed = 15,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "System"
                },
                new FormConfiguration
                {
                    Id = 2,
                    FormName = "Structural Engineer License",
                    FormType = FormType.StructuralEngineerLicense,
                    Description = "Application for Structural Engineer registration",
                    BaseFee = 2500m,
                    ProcessingFee = 500m,
                    LateFee = 250m,
                    IsActive = true,
                    AllowOnlineSubmission = true,
                    ProcessingDays = 15,
                    MaxFileSizeMB = 5,
                    MaxFilesAllowed = 10,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "System"
                },
                new FormConfiguration
                {
                    Id = 3,
                    FormName = "Architect License",
                    FormType = FormType.ArchitectLicense,
                    Description = "Application for Architect registration",
                    BaseFee = 2500m,
                    ProcessingFee = 500m,
                    LateFee = 250m,
                    IsActive = true,
                    AllowOnlineSubmission = true,
                    ProcessingDays = 15,
                    MaxFileSizeMB = 5,
                    MaxFilesAllowed = 10,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "System"
                },
                new FormConfiguration
                {
                    Id = 4,
                    FormName = "Occupancy Certificate",
                    FormType = FormType.OccupancyCertificate,
                    Description = "Application for Occupancy Certificate",
                    BaseFee = 3000m,
                    ProcessingFee = 750m,
                    LateFee = 300m,
                    IsActive = true,
                    AllowOnlineSubmission = true,
                    ProcessingDays = 20,
                    MaxFileSizeMB = 10,
                    MaxFilesAllowed = 12,
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
