using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    public interface IDataSeeder
    {
        Task UpdateOfficerCredentialsAsync();
        Task EnsureSystemAdminExistsAsync();
        Task SeedAutoAssignmentRulesAsync();
    }

    public class DataSeeder : IDataSeeder
    {
        private readonly PMCRMSDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(
            PMCRMSDbContext context,
            IPasswordHasher passwordHasher,
            ILogger<DataSeeder> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        /// <summary>
        /// Ensures a System Admin account exists in the SystemAdmins table
        /// Creates default admin if none exists: pmc@mailinator.com / Test@123
        /// </summary>
        public async Task EnsureSystemAdminExistsAsync()
        {
            try
            {
                _logger.LogInformation("Checking if System Admin exists...");

                // Check if pmc@mailinator.com exists
                var defaultAdmin = await _context.SystemAdmins
                    .FirstOrDefaultAsync(a => a.Email == "pmc@mailinator.com");

                if (defaultAdmin != null)
                {
                    // Admin exists - ALWAYS update password to Test@123
                    _logger.LogWarning("System Admin exists. Updating password to Test@123...");
                    defaultAdmin.PasswordHash = _passwordHasher.HashPassword("Test@123");
                    defaultAdmin.UpdatedBy = "System";
                    defaultAdmin.UpdatedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Password updated successfully for pmc@mailinator.com");
                    return;
                }

                _logger.LogInformation("No System Admin found. Creating default admin account...");

                // Create default System Admin
                defaultAdmin = new SystemAdmin
                {
                    Name = "PMC Administrator",
                    Email = "pmc@mailinator.com",
                    PasswordHash = _passwordHasher.HashPassword("Test@123"),
                    EmployeeId = "ADMIN001",
                    IsSuperAdmin = true,
                    Department = "Administration",
                    Designation = "System Administrator",
                    PhoneNumber = "9999999999",
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.UtcNow
                };

                _context.SystemAdmins.Add(defaultAdmin);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "System Admin created successfully. Email: {Email}, Password: Test@123",
                    defaultAdmin.Email
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while ensuring System Admin exists");
                throw;
            }
        }

        /// <summary>
        /// Updates existing officer credentials with predefined test credentials
        /// Email and password are updated for all officer roles
        /// Password: Test@123 for all roles
        /// </summary>
        public async Task UpdateOfficerCredentialsAsync()
        {
            try
            {
                _logger.LogInformation("Starting officer credentials update/creation...");

                // Define officer credentials mapping
                var officerCredentials = new Dictionary<OfficerRole, string>
                {
                    { OfficerRole.Clerk, "raghava.reddy@invimatic.com" },
                    { OfficerRole.JuniorStructuralEngineer, "jrengg-stru@mailinator.com" },
                    { OfficerRole.AssistantStructuralEngineer, "assiengg-stru@mailinator.com" },
                    { OfficerRole.JuniorLicenceEngineer, "jrengg-lice@mailinator.com" },
                    { OfficerRole.AssistantLicenceEngineer, "assiengg-lice@mailinator.com" },
                    { OfficerRole.JuniorArchitect, "jrengg-arch@mailinator.com" },
                    { OfficerRole.AssistantArchitect, "assiengg-arch@mailinator.com" },
                    { OfficerRole.JuniorSupervisor1, "jrengg-super1@mailinator.com" },
                    { OfficerRole.AssistantSupervisor1, "aassiengg-super1@mailinator.com" },
                    { OfficerRole.JuniorSupervisor2, "jrengg-super2@mailinator.com" },
                    { OfficerRole.AssistantSupervisor2, "assiengg-super2@mailinator.com" },
                    { OfficerRole.ExecutiveEngineer, "exeengg@mailinator.com" },
                    { OfficerRole.CityEngineer, "cityengg@mailinator.com" },
                };

                var password = "Test@123";
                var hashedPassword = _passwordHasher.HashPassword(password);
                var createdCount = 0;
                var updatedCount = 0;

                // Load all existing officers into memory for efficient checking
                var existingOfficers = await _context.Officers.ToListAsync();
                _logger.LogInformation("Found {Count} existing officers in database", existingOfficers.Count);

                // Update or create all officers
                foreach (var (role, email) in officerCredentials)
                {
                    var officer = existingOfficers.FirstOrDefault(o => o.Role == role);

                    if (officer != null)
                    {
                        // Update existing officer - ALWAYS update email and password
                        officer.Email = email;
                        officer.PasswordHash = hashedPassword;
                        officer.UpdatedBy = "System";
                        officer.UpdatedDate = DateTime.UtcNow;
                        updatedCount++;
                        
                        _logger.LogInformation(
                            "✓ Updated officer: {Role} → Email: {Email}",
                            role, email
                        );
                    }
                    else
                    {
                        _logger.LogInformation("✓ Creating new officer: {Role} → Email: {Email}", role, email);
                        
                        // Create new officer if doesn't exist
                        var newOfficer = new Officer
                        {
                            Name = GetOfficerName(role),
                            Email = email,
                            PasswordHash = hashedPassword,
                            Role = role,
                            EmployeeId = GenerateEmployeeId(role),
                            IsActive = true,
                            PhoneNumber = "9999999999", // Default phone number
                            CreatedBy = "System",
                            CreatedDate = DateTime.UtcNow,
                            UpdatedBy = "System",
                            UpdatedDate = DateTime.UtcNow
                        };
                        
                        _context.Officers.Add(newOfficer);
                        createdCount++;
                    }
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "✅ Officer seeding completed! Created: {Created}, Updated: {Updated}, Total: {Total}, Password: Test@123",
                    createdCount, updatedCount, createdCount + updatedCount
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during officer credentials update/creation");
                throw;
            }
        }

        private string GetOfficerName(OfficerRole role)
        {
            return role switch
            {
                OfficerRole.Clerk => "Raghava Reddy",
                OfficerRole.JuniorStructuralEngineer => "Junior Structural Engineer",
                OfficerRole.AssistantStructuralEngineer => "Assistant Structural Engineer",
                OfficerRole.JuniorLicenceEngineer => "Junior Licence Engineer",
                OfficerRole.AssistantLicenceEngineer => "Assistant Licence Engineer",
                OfficerRole.JuniorArchitect => "Junior Architect",
                OfficerRole.AssistantArchitect => "Assistant Architect",
                OfficerRole.JuniorSupervisor1 => "Junior Supervisor1",
                OfficerRole.AssistantSupervisor1 => "Assistant Supervisor1",
                OfficerRole.JuniorSupervisor2 => "Junior Supervisor2",
                OfficerRole.AssistantSupervisor2 => "Assistant Supervisor2",
                OfficerRole.ExecutiveEngineer => "Executive Engineer",
                OfficerRole.CityEngineer => "City Engineer",
                _ => "Officer"
            };
        }

        private string GenerateEmployeeId(OfficerRole role)
        {
            var roleCode = role switch
            {
                OfficerRole.Clerk => "CLK001",
                OfficerRole.JuniorArchitect => "JA001",
                OfficerRole.AssistantArchitect => "AA001",
                OfficerRole.JuniorLicenceEngineer => "JLE001",
                OfficerRole.AssistantLicenceEngineer => "ALE001",
                OfficerRole.JuniorStructuralEngineer => "JSE001",
                OfficerRole.AssistantStructuralEngineer => "ASE001",
                OfficerRole.JuniorSupervisor1 => "JS1001",
                OfficerRole.AssistantSupervisor1 => "AS1001",
                OfficerRole.JuniorSupervisor2 => "JS2001",
                OfficerRole.AssistantSupervisor2 => "AS2001",
                OfficerRole.ExecutiveEngineer => "EE001",
                OfficerRole.CityEngineer => "CE001",
                _ => "OFF001"
            };

            return $"PMC-{roleCode}";
        }

        /// <summary>
        /// Seeds default auto-assignment rules for all position types
        /// Creates workload-based assignment rules with auto-assign on submission
        /// </summary>
        public async Task SeedAutoAssignmentRulesAsync()
        {
            try
            {
                _logger.LogInformation("Checking auto-assignment rules...");

                // Check if any rules already exist
                var existingRulesCount = await _context.AutoAssignmentRules.CountAsync();
                
                if (existingRulesCount > 0)
                {
                    _logger.LogInformation("Auto-assignment rules already exist ({Count} rules). Skipping seed.", existingRulesCount);
                    return;
                }

                _logger.LogInformation("No auto-assignment rules found. Creating default rules...");

                var rules = new List<AutoAssignmentRule>
                {
                    // Architect - WorkloadBased
                    new AutoAssignmentRule
                    {
                        PositionType = PositionType.Architect,
                        TargetOfficerRole = OfficerRole.JuniorArchitect,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 1,
                        MaxWorkloadPerOfficer = 50,
                        IsActive = true,
                        AutoAssignOnSubmission = true,
                        SendNotification = true,
                        Description = "Auto-assign Architect applications to Junior Architects based on workload",
                        EscalationTimeHours = 72,
                        EscalationRole = OfficerRole.AssistantArchitect,
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    },

                    // Structural Engineer - WorkloadBased
                    new AutoAssignmentRule
                    {
                        PositionType = PositionType.StructuralEngineer,
                        TargetOfficerRole = OfficerRole.JuniorStructuralEngineer,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 1,
                        MaxWorkloadPerOfficer = 50,
                        IsActive = true,
                        AutoAssignOnSubmission = true,
                        SendNotification = true,
                        Description = "Auto-assign Structural Engineer applications to Junior Structural Engineers based on workload",
                        EscalationTimeHours = 72,
                        EscalationRole = OfficerRole.AssistantStructuralEngineer,
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    },

                    // Licence Engineer - WorkloadBased
                    new AutoAssignmentRule
                    {
                        PositionType = PositionType.LicenceEngineer,
                        TargetOfficerRole = OfficerRole.JuniorLicenceEngineer,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 1,
                        MaxWorkloadPerOfficer = 50,
                        IsActive = true,
                        AutoAssignOnSubmission = true,
                        SendNotification = true,
                        Description = "Auto-assign Licence Engineer applications to Junior Licence Engineers based on workload",
                        EscalationTimeHours = 72,
                        EscalationRole = OfficerRole.AssistantLicenceEngineer,
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    },

                    // Supervisor1 - WorkloadBased
                    new AutoAssignmentRule
                    {
                        PositionType = PositionType.Supervisor1,
                        TargetOfficerRole = OfficerRole.JuniorSupervisor1,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 1,
                        MaxWorkloadPerOfficer = 50,
                        IsActive = true,
                        AutoAssignOnSubmission = true,
                        SendNotification = true,
                        Description = "Auto-assign Supervisor1 applications to Junior Supervisor1 based on workload",
                        EscalationTimeHours = 72,
                        EscalationRole = OfficerRole.AssistantSupervisor1,
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    },

                    // Supervisor2 - WorkloadBased
                    new AutoAssignmentRule
                    {
                        PositionType = PositionType.Supervisor2,
                        TargetOfficerRole = OfficerRole.JuniorSupervisor2,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 1,
                        MaxWorkloadPerOfficer = 50,
                        IsActive = true,
                        AutoAssignOnSubmission = true,
                        SendNotification = true,
                        Description = "Auto-assign Supervisor2 applications to Junior Supervisor2 based on workload",
                        EscalationTimeHours = 72,
                        EscalationRole = OfficerRole.AssistantSupervisor2,
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    }
                };

                _context.AutoAssignmentRules.AddRange(rules);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Auto-assignment rules created successfully! Total rules: {Count}", 
                    rules.Count
                );

                foreach (var rule in rules)
                {
                    _logger.LogInformation(
                        "  ✓ {PositionType} → {Role} (Strategy: {Strategy}, Max Workload: {MaxWorkload})",
                        rule.PositionType,
                        rule.TargetOfficerRole,
                        rule.Strategy,
                        rule.MaxWorkloadPerOfficer
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error seeding auto-assignment rules");
                throw;
            }
        }
    }
}

