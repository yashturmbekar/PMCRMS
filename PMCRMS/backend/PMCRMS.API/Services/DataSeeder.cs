using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    public interface IDataSeeder
    {
        Task SeedOfficerPasswordsAsync();
        Task EnsureSystemAdminExistsAsync();
        Task SeedAllOfficersForTestingAsync();
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
        /// Creates default admin if none exists: admin@gmail.com / admin@123
        /// Also fixes any admin accounts without password hashes
        /// </summary>
        public async Task EnsureSystemAdminExistsAsync()
        {
            try
            {
                _logger.LogInformation("Checking if System Admin exists...");

                // Check if admin@gmail.com exists
                var defaultAdmin = await _context.SystemAdmins
                    .FirstOrDefaultAsync(a => a.Email == "admin@gmail.com");

                if (defaultAdmin != null)
                {
                    // Admin exists, but check if password is set
                    if (string.IsNullOrEmpty(defaultAdmin.PasswordHash))
                    {
                        _logger.LogWarning("System Admin exists but has no password. Setting password...");
                        defaultAdmin.PasswordHash = _passwordHasher.HashPassword("admin@123");
                        defaultAdmin.UpdatedBy = "System";
                        defaultAdmin.UpdatedDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Password set successfully for admin@gmail.com");
                        _logger.LogWarning(
                            "SECURITY NOTICE: Default admin password is 'admin@123'. Please change it immediately after first login."
                        );
                    }
                    else
                    {
                        _logger.LogInformation("System Admin already exists with password set. Skipping.");
                    }
                    return;
                }

                _logger.LogInformation("No System Admin found. Creating default admin account...");

                // Create default System Admin
                defaultAdmin = new SystemAdmin
                {
                    Name = "System Administrator",
                    Email = "admin@gmail.com",
                    PasswordHash = _passwordHasher.HashPassword("admin@123"),
                    EmployeeId = "ADMIN001",
                    IsSuperAdmin = true,
                    Department = "Administration",
                    Designation = "System Administrator",
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedBy = "System",
                    UpdatedDate = DateTime.UtcNow
                };

                _context.SystemAdmins.Add(defaultAdmin);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "System Admin created successfully. Email: {Email}, EmployeeId: {EmployeeId}",
                    defaultAdmin.Email, defaultAdmin.EmployeeId
                );
                _logger.LogWarning(
                    "SECURITY NOTICE: Default admin password is 'admin@123'. Please change it immediately after first login."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while ensuring System Admin exists");
                throw;
            }
        }

        /// <summary>
        /// Seeds default passwords for officer accounts
        /// Password format: "admin@123" for admin, "pmcrms@[role]123" for others
        /// </summary>
        public async Task SeedOfficerPasswordsAsync()
        {
            try
            {
                _logger.LogInformation("Starting officer password seeding...");

                // Get all officer accounts (non-Applicant users) without passwords
                var officers = await _context.Users
                    .Where(u => u.Role != UserRole.User && string.IsNullOrEmpty(u.PasswordHash))
                    .ToListAsync();

                if (!officers.Any())
                {
                    _logger.LogInformation("No officers found that need password seeding");
                    return;
                }

                var updateCount = 0;

                foreach (var officer in officers)
                {
                    // Generate default password based on role
                    var defaultPassword = GenerateDefaultPassword(officer.Role);
                    officer.PasswordHash = _passwordHasher.HashPassword(defaultPassword);
                    officer.EmployeeId = GenerateEmployeeId(officer.Role, officer.Id);
                    officer.UpdatedBy = "System";
                    officer.UpdatedDate = DateTime.UtcNow;

                    updateCount++;
                    _logger.LogInformation(
                        "Seeded password for {Role} (ID: {Id}, Email: {Email}). Default password: {Password}",
                        officer.Role, officer.Id, officer.Email, defaultPassword
                    );
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "Password seeding completed successfully. {Count} officers updated.",
                    updateCount
                );
                _logger.LogWarning(
                    "SECURITY NOTICE: Default passwords have been set. Please ask all officers to change their passwords on first login."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password seeding");
                throw;
            }
        }

        private string GenerateDefaultPassword(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "admin@123",
                UserRole.Clerk => "pmcrms@clerk123",
                UserRole.JuniorArchitect => "pmcrms@ja123",
                UserRole.AssistantArchitect => "pmcrms@aa123",
                UserRole.JuniorLicenceEngineer => "pmcrms@jle123",
                UserRole.AssistantLicenceEngineer => "pmcrms@ale123",
                UserRole.JuniorStructuralEngineer => "pmcrms@jse123",
                UserRole.AssistantStructuralEngineer => "pmcrms@ase123",
                UserRole.JuniorSupervisor1 => "pmcrms@js1_123",
                UserRole.AssistantSupervisor1 => "pmcrms@as1_123",
                UserRole.JuniorSupervisor2 => "pmcrms@js2_123",
                UserRole.AssistantSupervisor2 => "pmcrms@as2_123",
                UserRole.ExecutiveEngineer => "pmcrms@ee123",
                UserRole.CityEngineer => "pmcrms@ce123",
                _ => "pmcrms@officer123"
            };
        }

        private string GenerateEmployeeId(UserRole role, int userId)
        {
            var roleCode = role switch
            {
                UserRole.Admin => "ADM",
                UserRole.Clerk => "CLK",
                UserRole.JuniorArchitect => "JA",
                UserRole.AssistantArchitect => "AA",
                UserRole.JuniorLicenceEngineer => "JLE",
                UserRole.AssistantLicenceEngineer => "ALE",
                UserRole.JuniorStructuralEngineer => "JSE",
                UserRole.AssistantStructuralEngineer => "ASE",
                UserRole.JuniorSupervisor1 => "JS1",
                UserRole.AssistantSupervisor1 => "AS1",
                UserRole.JuniorSupervisor2 => "JS2",
                UserRole.AssistantSupervisor2 => "AS2",
                UserRole.ExecutiveEngineer => "EE",
                UserRole.CityEngineer => "CE",
                _ => "OFF"
            };

            return $"PMC-{roleCode}-{userId:D4}";
        }

        /// <summary>
        /// Seeds all 13 officer roles for testing purposes
        /// This is a ONE-TIME operation for testing only
        /// </summary>
        public async Task SeedAllOfficersForTestingAsync()
        {
            try
            {
                _logger.LogInformation("Starting test officer seeding for all 13 roles...");

                var officerRoles = new[]
                {
                    UserRole.JuniorArchitect,
                    UserRole.AssistantArchitect,
                    UserRole.JuniorLicenceEngineer,
                    UserRole.AssistantLicenceEngineer,
                    UserRole.JuniorStructuralEngineer,
                    UserRole.AssistantStructuralEngineer,
                    UserRole.JuniorSupervisor1,
                    UserRole.AssistantSupervisor1,
                    UserRole.JuniorSupervisor2,
                    UserRole.AssistantSupervisor2,
                    UserRole.ExecutiveEngineer,
                    UserRole.CityEngineer,
                    UserRole.Clerk
                };

                var createdCount = 0;

                foreach (var role in officerRoles)
                {
                    // Check if officer with this role already exists
                    var existingOfficer = await _context.Users
                        .FirstOrDefaultAsync(u => u.Role == role);

                    if (existingOfficer != null)
                    {
                        _logger.LogInformation("Officer with role {Role} already exists. Skipping.", role);
                        continue;
                    }

                    // Create test officer
                    var roleName = role.ToString();
                    var email = $"{roleName.ToLower()}@test.com";
                    var defaultPassword = GenerateDefaultPassword(role);

                    var officer = new User
                    {
                        Name = $"Test {FormatRoleName(roleName)}",
                        Email = email,
                        PasswordHash = _passwordHasher.HashPassword(defaultPassword),
                        Role = role,
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "System",
                        UpdatedDate = DateTime.UtcNow
                    };

                    _context.Users.Add(officer);
                    await _context.SaveChangesAsync();

                    // Generate and update employee ID after saving to get the ID
                    officer.EmployeeId = GenerateEmployeeId(role, officer.Id);
                    await _context.SaveChangesAsync();

                    createdCount++;
                    _logger.LogInformation(
                        "Created test officer: {Role} | Email: {Email} | Password: {Password} | EmployeeId: {EmployeeId}",
                        role, email, defaultPassword, officer.EmployeeId
                    );
                }

                _logger.LogInformation(
                    "Test officer seeding completed. {Count} officers created out of 13 roles.",
                    createdCount
                );

                if (createdCount > 0)
                {
                    _logger.LogWarning(
                        "SECURITY NOTICE: Test officers created with default passwords. These are for TESTING ONLY."
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during test officer seeding");
                throw;
            }
        }

        private string FormatRoleName(string roleName)
        {
            // Add spaces before capital letters and numbers
            return System.Text.RegularExpressions.Regex.Replace(roleName, "([A-Z0-9])", " $1").Trim();
        }
    }
}
