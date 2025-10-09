using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Services
{
    public interface IDataSeeder
    {
        Task SeedOfficerPasswordsAsync();
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
        /// Seeds default passwords for officer accounts
        /// Password format: "pmcrms@[role]123"  e.g., "pmcrms@admin123"
        /// </summary>
        public async Task SeedOfficerPasswordsAsync()
        {
            try
            {
                _logger.LogInformation("Starting officer password seeding...");

                // Get all officer accounts (non-Applicant users) without passwords
                var officers = await _context.Users
                    .Where(u => u.Role != UserRole.Applicant && string.IsNullOrEmpty(u.PasswordHash))
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
                UserRole.Admin => "pmcrms@admin123",
                UserRole.JuniorEngineer => "pmcrms@je123",
                UserRole.AssistantEngineer => "pmcrms@ae123",
                UserRole.ExecutiveEngineer => "pmcrms@ee123",
                UserRole.CityEngineer => "pmcrms@ce123",
                UserRole.Clerk => "pmcrms@clerk123",
                _ => "pmcrms@officer123"
            };
        }

        private string GenerateEmployeeId(UserRole role, int userId)
        {
            var roleCode = role switch
            {
                UserRole.Admin => "ADM",
                UserRole.JuniorEngineer => "JE",
                UserRole.AssistantEngineer => "AE",
                UserRole.ExecutiveEngineer => "EE",
                UserRole.CityEngineer => "CE",
                UserRole.Clerk => "CLK",
                _ => "OFF"
            };

            return $"PMC-{roleCode}-{userId:D4}";
        }
    }
}
