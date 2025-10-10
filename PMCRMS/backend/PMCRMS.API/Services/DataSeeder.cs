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
        /// Password format: "admin@123" for admin, "pmcrms@[role]123" for others
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
    }
}
