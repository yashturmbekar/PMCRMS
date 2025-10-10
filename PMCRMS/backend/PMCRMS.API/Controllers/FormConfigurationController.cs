using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FormConfigurationController : ControllerBase
    {
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<FormConfigurationController> _logger;

        public FormConfigurationController(PMCRMSDbContext context, ILogger<FormConfigurationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<FormConfigurationDto>>>> GetFormConfigurations(
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.FormConfigurations.AsQueryable();

                if (isActive.HasValue)
                {
                    query = query.Where(f => f.IsActive == isActive.Value);
                }

                var forms = await query.OrderBy(f => f.FormType).ToListAsync();

                var formDtos = forms.Select(f => new FormConfigurationDto
                {
                    Id = f.Id,
                    FormName = f.FormName,
                    FormType = f.FormType.ToString(),
                    Description = f.Description,
                    BaseFee = f.BaseFee,
                    ProcessingFee = f.ProcessingFee,
                    LateFee = f.LateFee,
                    TotalFee = f.BaseFee + f.ProcessingFee,
                    IsActive = f.IsActive,
                    AllowOnlineSubmission = f.AllowOnlineSubmission,
                    ProcessingDays = f.ProcessingDays,
                    MaxFileSizeMB = f.MaxFileSizeMB,
                    MaxFilesAllowed = f.MaxFilesAllowed,
                    CustomFields = f.CustomFields,
                    RequiredDocuments = f.RequiredDocuments
                }).ToList();

                return Ok(new ApiResponse<List<FormConfigurationDto>>
                {
                    Success = true,
                    Data = formDtos,
                    Message = "Form configurations retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching form configurations");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to fetch form configurations",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<FormConfigurationDto>>> GetFormConfiguration(int id)
        {
            try
            {
                var form = await _context.FormConfigurations.FindAsync(id);
                if (form == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Form configuration not found"
                    });
                }

                var formDto = new FormConfigurationDto
                {
                    Id = form.Id,
                    FormName = form.FormName,
                    FormType = form.FormType.ToString(),
                    Description = form.Description,
                    BaseFee = form.BaseFee,
                    ProcessingFee = form.ProcessingFee,
                    LateFee = form.LateFee,
                    TotalFee = form.BaseFee + form.ProcessingFee,
                    IsActive = form.IsActive,
                    AllowOnlineSubmission = form.AllowOnlineSubmission,
                    ProcessingDays = form.ProcessingDays,
                    MaxFileSizeMB = form.MaxFileSizeMB,
                    MaxFilesAllowed = form.MaxFilesAllowed,
                    CustomFields = form.CustomFields,
                    RequiredDocuments = form.RequiredDocuments
                };

                return Ok(new ApiResponse<FormConfigurationDto>
                {
                    Success = true,
                    Data = formDto,
                    Message = "Form configuration retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching form configuration");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to fetch form configuration",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<FormConfigurationDto>>> CreateFormConfiguration(
            [FromBody] FormConfigCreateRequest request)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} creating form configuration", adminId);

                // Check if form type already exists
                var existing = await _context.FormConfigurations
                    .FirstOrDefaultAsync(f => f.FormType == request.FormType);
                
                if (existing != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "A form configuration for this type already exists"
                    });
                }

                var formConfig = new FormConfiguration
                {
                    FormName = request.FormName,
                    FormType = request.FormType,
                    Description = request.Description,
                    BaseFee = request.BaseFee,
                    ProcessingFee = request.ProcessingFee,
                    LateFee = request.LateFee,
                    IsActive = true,
                    AllowOnlineSubmission = true,
                    ProcessingDays = request.ProcessingDays,
                    MaxFileSizeMB = request.MaxFileSizeMB,
                    MaxFilesAllowed = request.MaxFilesAllowed,
                    CustomFields = request.CustomFields != null ? JsonSerializer.Serialize(request.CustomFields) : null,
                    RequiredDocuments = request.RequiredDocuments != null ? JsonSerializer.Serialize(request.RequiredDocuments) : null,
                    CreatedBy = User.FindFirst("email")?.Value ?? "Admin"
                };

                _context.FormConfigurations.Add(formConfig);
                await _context.SaveChangesAsync();

                var responseDto = new FormConfigurationDto
                {
                    Id = formConfig.Id,
                    FormName = formConfig.FormName,
                    FormType = formConfig.FormType.ToString(),
                    Description = formConfig.Description,
                    BaseFee = formConfig.BaseFee,
                    ProcessingFee = formConfig.ProcessingFee,
                    LateFee = formConfig.LateFee,
                    TotalFee = formConfig.BaseFee + formConfig.ProcessingFee,
                    IsActive = formConfig.IsActive,
                    AllowOnlineSubmission = formConfig.AllowOnlineSubmission,
                    ProcessingDays = formConfig.ProcessingDays,
                    MaxFileSizeMB = formConfig.MaxFileSizeMB,
                    MaxFilesAllowed = formConfig.MaxFilesAllowed,
                    CustomFields = formConfig.CustomFields,
                    RequiredDocuments = formConfig.RequiredDocuments
                };

                return Ok(new ApiResponse<FormConfigurationDto>
                {
                    Success = true,
                    Data = responseDto,
                    Message = "Form configuration created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating form configuration");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to create form configuration",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> UpdateFormConfiguration(
            int id,
            [FromBody] FormConfigUpdateRequest request)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} updating form configuration {FormId}", adminId, id);

                var formConfig = await _context.FormConfigurations.FindAsync(id);
                if (formConfig == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Form configuration not found"
                    });
                }

                // Track fee changes for history
                bool feeChanged = false;
                var oldBaseFee = formConfig.BaseFee;
                var oldProcessingFee = formConfig.ProcessingFee;

                // Update fields
                if (!string.IsNullOrEmpty(request.FormName))
                    formConfig.FormName = request.FormName;

                if (!string.IsNullOrEmpty(request.Description))
                    formConfig.Description = request.Description;

                if (request.BaseFee.HasValue && request.BaseFee.Value != formConfig.BaseFee)
                {
                    formConfig.BaseFee = request.BaseFee.Value;
                    feeChanged = true;
                }

                if (request.ProcessingFee.HasValue && request.ProcessingFee.Value != formConfig.ProcessingFee)
                {
                    formConfig.ProcessingFee = request.ProcessingFee.Value;
                    feeChanged = true;
                }

                if (request.LateFee.HasValue)
                    formConfig.LateFee = request.LateFee.Value;

                if (request.IsActive.HasValue)
                    formConfig.IsActive = request.IsActive.Value;

                if (request.AllowOnlineSubmission.HasValue)
                    formConfig.AllowOnlineSubmission = request.AllowOnlineSubmission.Value;

                if (request.ProcessingDays.HasValue)
                    formConfig.ProcessingDays = request.ProcessingDays.Value;

                if (request.MaxFileSizeMB.HasValue)
                    formConfig.MaxFileSizeMB = request.MaxFileSizeMB.Value;

                if (request.MaxFilesAllowed.HasValue)
                    formConfig.MaxFilesAllowed = request.MaxFilesAllowed.Value;

                if (request.CustomFields != null)
                    formConfig.CustomFields = JsonSerializer.Serialize(request.CustomFields);

                if (request.RequiredDocuments != null)
                    formConfig.RequiredDocuments = JsonSerializer.Serialize(request.RequiredDocuments);

                formConfig.UpdatedDate = DateTime.UtcNow;
                formConfig.UpdatedBy = User.FindFirst("email")?.Value;

                // Create fee history entry if fees changed
                if (feeChanged)
                {
                    var feeHistory = new FormFeeHistory
                    {
                        FormConfigurationId = formConfig.Id,
                        OldBaseFee = oldBaseFee,
                        NewBaseFee = formConfig.BaseFee,
                        OldProcessingFee = oldProcessingFee,
                        NewProcessingFee = formConfig.ProcessingFee,
                        EffectiveFrom = DateTime.UtcNow,
                        ChangedByUserId = adminId,
                        ChangeReason = request.ChangeReason ?? "Fee structure updated",
                        CreatedBy = User.FindFirst("email")?.Value ?? "Admin"
                    };

                    _context.FormFeeHistories.Add(feeHistory);
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Form configuration updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating form configuration");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to update form configuration",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse>> DeleteFormConfiguration(int id)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
                _logger.LogInformation("Admin {AdminId} deleting form configuration {FormId}", adminId, id);

                var formConfig = await _context.FormConfigurations.FindAsync(id);
                if (formConfig == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Form configuration not found"
                    });
                }

                // Instead of deleting, we deactivate
                formConfig.IsActive = false;
                formConfig.UpdatedDate = DateTime.UtcNow;
                formConfig.UpdatedBy = User.FindFirst("email")?.Value;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Form configuration deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting form configuration");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Failed to delete form configuration",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }

    // Local DTOs for FormConfigurationController to avoid conflicts with AdminController DTOs
    public class FormConfigCreateRequest
    {
        [Required]
        [MaxLength(100)]
        public string FormName { get; set; } = string.Empty;

        [Required]
        public FormType FormType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal BaseFee { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ProcessingFee { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal LateFee { get; set; } = 0;

        public bool AllowOnlineSubmission { get; set; } = true;

        [Range(1, 365)]
        public int ProcessingDays { get; set; } = 30;

        [Range(1, 100)]
        public int? MaxFileSizeMB { get; set; } = 5;

        [Range(1, 50)]
        public int? MaxFilesAllowed { get; set; } = 10;

        public List<CustomFieldDto>? CustomFields { get; set; }

        public List<string>? RequiredDocuments { get; set; }
    }

    public class FormConfigUpdateRequest
    {
        [MaxLength(100)]
        public string? FormName { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? BaseFee { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ProcessingFee { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? LateFee { get; set; }

        public bool? IsActive { get; set; }

        public bool? AllowOnlineSubmission { get; set; }

        [Range(1, 365)]
        public int? ProcessingDays { get; set; }

        [Range(1, 100)]
        public int? MaxFileSizeMB { get; set; }

        [Range(1, 50)]
        public int? MaxFilesAllowed { get; set; }

        public List<CustomFieldDto>? CustomFields { get; set; }

        public List<string>? RequiredDocuments { get; set; }

        [MaxLength(500)]
        public string? ChangeReason { get; set; }
    }
}
