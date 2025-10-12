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
        /// Seeds default auto-assignment rules for all position types and workflow stages
        /// Creates complete end-to-end workflow auto-assignment from submission to final approval
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

                _logger.LogInformation("No auto-assignment rules found. Creating complete workflow rules...");

                var rules = new List<AutoAssignmentRule>();

                // ============================================================================
                // STAGE 1: APPLICATION SUBMISSION → JUNIOR ENGINEER ASSIGNMENT
                // ============================================================================
                
                // Architect - Initial Assignment to Junior Architect
                rules.Add(new AutoAssignmentRule
                {
                    PositionType = PositionType.Architect,
                    TargetOfficerRole = OfficerRole.JuniorArchitect,
                    Strategy = AssignmentStrategy.WorkloadBased,
                    Priority = 1,
                    MaxWorkloadPerOfficer = 50,
                    IsActive = true,
                    AutoAssignOnSubmission = true,
                    SendNotification = true,
                    Description = "STAGE 1: Auto-assign Architect applications to Junior Architects on submission",
                    EscalationTimeHours = 72,
                    EscalationRole = OfficerRole.AssistantArchitect,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.UtcNow
                });

                // Structural Engineer - Initial Assignment to Junior Structural Engineer
                rules.Add(new AutoAssignmentRule
                {
                    PositionType = PositionType.StructuralEngineer,
                    TargetOfficerRole = OfficerRole.JuniorStructuralEngineer,
                    Strategy = AssignmentStrategy.WorkloadBased,
                    Priority = 1,
                    MaxWorkloadPerOfficer = 50,
                    IsActive = true,
                    AutoAssignOnSubmission = true,
                    SendNotification = true,
                    Description = "STAGE 1: Auto-assign Structural Engineer applications to Junior Structural Engineers on submission",
                    EscalationTimeHours = 72,
                    EscalationRole = OfficerRole.AssistantStructuralEngineer,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.UtcNow
                });

                // Licence Engineer - Initial Assignment to Junior Licence Engineer
                rules.Add(new AutoAssignmentRule
                {
                    PositionType = PositionType.LicenceEngineer,
                    TargetOfficerRole = OfficerRole.JuniorLicenceEngineer,
                    Strategy = AssignmentStrategy.WorkloadBased,
                    Priority = 1,
                    MaxWorkloadPerOfficer = 50,
                    IsActive = true,
                    AutoAssignOnSubmission = true,
                    SendNotification = true,
                    Description = "STAGE 1: Auto-assign Licence Engineer applications to Junior Licence Engineers on submission",
                    EscalationTimeHours = 72,
                    EscalationRole = OfficerRole.AssistantLicenceEngineer,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.UtcNow
                });

                // Supervisor1 - Initial Assignment to Junior Supervisor1
                rules.Add(new AutoAssignmentRule
                {
                    PositionType = PositionType.Supervisor1,
                    TargetOfficerRole = OfficerRole.JuniorSupervisor1,
                    Strategy = AssignmentStrategy.WorkloadBased,
                    Priority = 1,
                    MaxWorkloadPerOfficer = 50,
                    IsActive = true,
                    AutoAssignOnSubmission = true,
                    SendNotification = true,
                    Description = "STAGE 1: Auto-assign Supervisor1 applications to Junior Supervisor1 on submission",
                    EscalationTimeHours = 72,
                    EscalationRole = OfficerRole.AssistantSupervisor1,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.UtcNow
                });

                // Supervisor2 - Initial Assignment to Junior Supervisor2
                rules.Add(new AutoAssignmentRule
                {
                    PositionType = PositionType.Supervisor2,
                    TargetOfficerRole = OfficerRole.JuniorSupervisor2,
                    Strategy = AssignmentStrategy.WorkloadBased,
                    Priority = 1,
                    MaxWorkloadPerOfficer = 50,
                    IsActive = true,
                    AutoAssignOnSubmission = true,
                    SendNotification = true,
                    Description = "STAGE 1: Auto-assign Supervisor2 applications to Junior Supervisor2 on submission",
                    EscalationTimeHours = 72,
                    EscalationRole = OfficerRole.AssistantSupervisor2,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.UtcNow
                });

                // ============================================================================
                // STAGE 2: ASSISTANT ENGINEER APPROVAL (After JE Digital Signature)
                // ============================================================================
                
                foreach (var positionType in new[] { 
                    PositionType.Architect, 
                    PositionType.StructuralEngineer, 
                    PositionType.LicenceEngineer, 
                    PositionType.Supervisor1, 
                    PositionType.Supervisor2 
                })
                {
                    var assistantRole = positionType switch
                    {
                        PositionType.Architect => OfficerRole.AssistantArchitect,
                        PositionType.StructuralEngineer => OfficerRole.AssistantStructuralEngineer,
                        PositionType.LicenceEngineer => OfficerRole.AssistantLicenceEngineer,
                        PositionType.Supervisor1 => OfficerRole.AssistantSupervisor1,
                        PositionType.Supervisor2 => OfficerRole.AssistantSupervisor2,
                        _ => OfficerRole.AssistantStructuralEngineer
                    };

                    rules.Add(new AutoAssignmentRule
                    {
                        PositionType = positionType,
                        TargetOfficerRole = assistantRole,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 2,
                        MaxWorkloadPerOfficer = 50,
                        IsActive = true,
                        AutoAssignOnSubmission = false, // Triggered after JE stage
                        SendNotification = true,
                        Description = $"STAGE 2: Auto-assign {positionType} to {assistantRole} after JE digital signature",
                        EscalationTimeHours = 48,
                        EscalationRole = OfficerRole.ExecutiveEngineer,
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    });
                }

                // ============================================================================
                // STAGE 3: EXECUTIVE ENGINEER APPROVAL - STAGE 1 (After AE Approval)
                // ============================================================================
                
                foreach (var positionType in new[] { 
                    PositionType.Architect, 
                    PositionType.StructuralEngineer, 
                    PositionType.LicenceEngineer, 
                    PositionType.Supervisor1, 
                    PositionType.Supervisor2 
                })
                {
                    rules.Add(new AutoAssignmentRule
                    {
                        PositionType = positionType,
                        TargetOfficerRole = OfficerRole.ExecutiveEngineer,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 3,
                        MaxWorkloadPerOfficer = 100,
                        IsActive = true,
                        AutoAssignOnSubmission = false, // Triggered after AE stage
                        SendNotification = true,
                        Description = $"STAGE 3: Auto-assign {positionType} to Executive Engineer (Stage 1) after AE approval",
                        EscalationTimeHours = 48,
                        EscalationRole = OfficerRole.CityEngineer,
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    });
                }

                // ============================================================================
                // STAGE 4: CITY ENGINEER APPROVAL (After EE Stage 1)
                // Routes to payment after approval
                // ============================================================================
                
                foreach (var positionType in new[] { 
                    PositionType.Architect, 
                    PositionType.StructuralEngineer, 
                    PositionType.LicenceEngineer, 
                    PositionType.Supervisor1, 
                    PositionType.Supervisor2 
                })
                {
                    rules.Add(new AutoAssignmentRule
                    {
                        PositionType = positionType,
                        TargetOfficerRole = OfficerRole.CityEngineer,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 4,
                        MaxWorkloadPerOfficer = 100,
                        IsActive = true,
                        AutoAssignOnSubmission = false, // Triggered after EE Stage 1
                        SendNotification = true,
                        Description = $"STAGE 4: Auto-assign {positionType} to City Engineer after EE approval (routes to payment)",
                        EscalationTimeHours = 72,
                        EscalationRole = null, // City Engineer is top level
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    });
                }

                // ============================================================================
                // STAGE 5: CLERK PROCESSING (After Payment by User)
                // ============================================================================
                
                foreach (var positionType in new[] { 
                    PositionType.Architect, 
                    PositionType.StructuralEngineer, 
                    PositionType.LicenceEngineer, 
                    PositionType.Supervisor1, 
                    PositionType.Supervisor2 
                })
                {
                    rules.Add(new AutoAssignmentRule
                    {
                        PositionType = positionType,
                        TargetOfficerRole = OfficerRole.Clerk,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 5,
                        MaxWorkloadPerOfficer = 200, // Clerks can handle more volume
                        IsActive = true,
                        AutoAssignOnSubmission = false, // Triggered after payment
                        SendNotification = true,
                        Description = $"STAGE 5: Auto-assign {positionType} to Clerk after payment completion",
                        EscalationTimeHours = 24,
                        EscalationRole = OfficerRole.ExecutiveEngineer,
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    });
                }

                // ============================================================================
                // STAGE 6: EXECUTIVE ENGINEER - DIGITAL SIGNATURE (After Clerk)
                // ============================================================================
                
                foreach (var positionType in new[] { 
                    PositionType.Architect, 
                    PositionType.StructuralEngineer, 
                    PositionType.LicenceEngineer, 
                    PositionType.Supervisor1, 
                    PositionType.Supervisor2 
                })
                {
                    rules.Add(new AutoAssignmentRule
                    {
                        PositionType = positionType,
                        TargetOfficerRole = OfficerRole.ExecutiveEngineer,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 6,
                        MaxWorkloadPerOfficer = 100,
                        IsActive = true,
                        AutoAssignOnSubmission = false, // Triggered after clerk processing
                        SendNotification = true,
                        Description = $"STAGE 6: Auto-assign {positionType} to Executive Engineer for digital signature",
                        EscalationTimeHours = 48,
                        EscalationRole = OfficerRole.CityEngineer,
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    });
                }

                // ============================================================================
                // STAGE 7: CITY ENGINEER - FINAL DIGITAL SIGNATURE (Final Stage)
                // ============================================================================
                
                foreach (var positionType in new[] { 
                    PositionType.Architect, 
                    PositionType.StructuralEngineer, 
                    PositionType.LicenceEngineer, 
                    PositionType.Supervisor1, 
                    PositionType.Supervisor2 
                })
                {
                    rules.Add(new AutoAssignmentRule
                    {
                        PositionType = positionType,
                        TargetOfficerRole = OfficerRole.CityEngineer,
                        Strategy = AssignmentStrategy.WorkloadBased,
                        Priority = 7,
                        MaxWorkloadPerOfficer = 100,
                        IsActive = true,
                        AutoAssignOnSubmission = false, // Triggered after EE signature
                        SendNotification = true,
                        Description = $"STAGE 7: Auto-assign {positionType} to City Engineer for final digital signature",
                        EscalationTimeHours = 72,
                        EscalationRole = null, // Final stage - no escalation
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    });
                }

                _context.AutoAssignmentRules.AddRange(rules);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Complete workflow auto-assignment rules created! Total rules: {Count}", 
                    rules.Count
                );

                _logger.LogInformation("📋 WORKFLOW SUMMARY:");
                _logger.LogInformation("  Stage 1: Application Submission → Junior Engineer (5 rules)");
                _logger.LogInformation("  Stage 2: JE Digital Signature → Assistant Engineer (5 rules)");
                _logger.LogInformation("  Stage 3: AE Approval → Executive Engineer Stage 1 (5 rules)");
                _logger.LogInformation("  Stage 4: EE Approval → City Engineer (routes to payment) (5 rules)");
                _logger.LogInformation("  Stage 5: Payment Complete → Clerk (5 rules)");
                _logger.LogInformation("  Stage 6: Clerk Processing → Executive Engineer Signature (5 rules)");
                _logger.LogInformation("  Stage 7: EE Signature → City Engineer Final Signature (5 rules)");
                _logger.LogInformation("  ────────────────────────────────────────");
                _logger.LogInformation("  Total: {Count} auto-assignment rules", rules.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error seeding auto-assignment rules");
                throw;
            }
        }
    }
}

