using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    public interface IDataSeeder
    {
        Task UpdateOfficerCredentialsAsync();
        Task EnsureSystemAdminExistsAsync();
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
                _logger.LogInformation("Starting officer credentials update...");

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
                var updateCount = 0;

                // Update all officers
                foreach (var (role, email) in officerCredentials)
                {
                    var officer = await _context.Officers
                        .FirstOrDefaultAsync(o => o.Role == role);

                    if (officer != null)
                    {
                        // Update existing officer - ALWAYS update email and password
                        officer.Email = email;
                        officer.PasswordHash = hashedPassword;
                        officer.UpdatedBy = "System";
                        officer.UpdatedDate = DateTime.UtcNow;
                        updateCount++;
                        
                        _logger.LogInformation(
                            "Updated officer: {Role} → Email: {Email}, Password: Test@123",
                            role, email
                        );
                    }
                    else
                    {
                        _logger.LogWarning("Officer with role {Role} not found. Creating new officer...", role);
                        
                        // Create new officer if doesn't exist
                        var newOfficer = new Officer
                        {
                            Name = GetOfficerName(role),
                            Email = email,
                            PasswordHash = hashedPassword,
                            Role = role,
                            EmployeeId = GenerateEmployeeId(role),
                            IsActive = true,
                            CreatedBy = "System",
                            CreatedDate = DateTime.UtcNow,
                            UpdatedBy = "System",
                            UpdatedDate = DateTime.UtcNow
                        };
                        
                        _context.Officers.Add(newOfficer);
                        updateCount++;
                        
                        _logger.LogInformation(
                            "Created new officer: {Role} → Email: {Email}, Password: Test@123",
                            role, email
                        );
                    }
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "Officer credentials update completed. {Count} officers updated/created. Password: Test@123",
                    updateCount
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during officer credentials update");
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
                OfficerRole.Clerk => "CLK",
                OfficerRole.JuniorArchitect => "JA",
                OfficerRole.AssistantArchitect => "AA",
                OfficerRole.JuniorLicenceEngineer => "JLE",
                OfficerRole.AssistantLicenceEngineer => "ALE",
                OfficerRole.JuniorStructuralEngineer => "JSE",
                OfficerRole.AssistantStructuralEngineer => "ASE",
                OfficerRole.JuniorSupervisor1 => "JS1",
                OfficerRole.AssistantSupervisor1 => "AS1",
                OfficerRole.JuniorSupervisor2 => "JS2",
                OfficerRole.AssistantSupervisor2 => "AS2",
                OfficerRole.ExecutiveEngineer => "EE",
                OfficerRole.CityEngineer => "CE",
                _ => "OFF"
            };

            var timestamp = DateTime.UtcNow.Ticks % 10000;
            return $"PMC-{roleCode}-{timestamp:D4}";
        }
    }
}

