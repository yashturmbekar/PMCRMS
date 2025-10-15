using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.Models;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Only authenticated users can access
    public class SystemSettingsController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<SystemSettingsController> _logger;

        public SystemSettingsController(
            PMCRMSDbContext context,
            ILogger<SystemSettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Upload PMC Logo for certificates
        /// </summary>
        [HttpPost("upload-pmc-logo")]
        public async Task<IActionResult> UploadPMCLogo(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded" });
                }

                // Validate file type
                var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest(new { message = "Only PNG and JPG images are allowed" });
                }

                // Validate file size (max 2MB)
                if (file.Length > 2 * 1024 * 1024)
                {
                    return BadRequest(new { message = "File size must be less than 2MB" });
                }

                // Read file content
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // Check if PMC_LOGO setting already exists
                var existingSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "PMC_LOGO");

                if (existingSetting != null)
                {
                    // Update existing
                    existingSetting.BinaryData = fileBytes;
                    existingSetting.ContentType = file.ContentType;
                    existingSetting.UpdatedDate = DateTime.UtcNow;
                    existingSetting.UpdatedBy = User.Identity?.Name ?? "System";
                    
                    _logger.LogInformation("Updated PMC Logo in database (Size: {Size} bytes)", fileBytes.Length);
                }
                else
                {
                    // Create new
                    var newSetting = new SystemSettings
                    {
                        SettingKey = "PMC_LOGO",
                        Description = "Official PMC Logo for certificates",
                        BinaryData = fileBytes,
                        ContentType = file.ContentType,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = User.Identity?.Name ?? "System"
                    };

                    _context.SystemSettings.Add(newSetting);
                    _logger.LogInformation("Added PMC Logo to database (Size: {Size} bytes)", fileBytes.Length);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "PMC Logo uploaded successfully",
                    size = fileBytes.Length,
                    contentType = file.ContentType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading PMC logo");
                return StatusCode(500, new { message = "Error uploading logo", error = ex.Message });
            }
        }

        /// <summary>
        /// Get PMC Logo
        /// </summary>
        [HttpGet("pmc-logo")]
        [AllowAnonymous] // Allow anyone to view the logo
        public async Task<IActionResult> GetPMCLogo()
        {
            try
            {
                var logoSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "PMC_LOGO" && s.IsActive);

                if (logoSetting?.BinaryData == null)
                {
                    return NotFound(new { message = "PMC Logo not found in database" });
                }

                return File(logoSetting.BinaryData, logoSetting.ContentType ?? "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PMC logo");
                return StatusCode(500, new { message = "Error retrieving logo" });
            }
        }

        /// <summary>
        /// Delete PMC Logo
        /// </summary>
        [HttpDelete("pmc-logo")]
        public async Task<IActionResult> DeletePMCLogo()
        {
            try
            {
                var logoSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "PMC_LOGO");

                if (logoSetting == null)
                {
                    return NotFound(new { message = "PMC Logo not found" });
                }

                _context.SystemSettings.Remove(logoSetting);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted PMC Logo from database");
                return Ok(new { message = "PMC Logo deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PMC logo");
                return StatusCode(500, new { message = "Error deleting logo" });
            }
        }

        /// <summary>
        /// Get all system settings
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllSettings()
        {
            try
            {
                var settings = await _context.SystemSettings
                    .Where(s => s.IsActive)
                    .Select(s => new
                    {
                        s.Id,
                        s.SettingKey,
                        s.SettingValue,
                        s.Description,
                        s.ContentType,
                        HasBinaryData = s.BinaryData != null && s.BinaryData.Length > 0,
                        BinaryDataSize = s.BinaryData != null ? s.BinaryData.Length : 0,
                        s.CreatedDate,
                        s.UpdatedDate
                    })
                    .ToListAsync();

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system settings");
                return StatusCode(500, new { message = "Error retrieving settings" });
            }
        }
    }
}
