using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMCRMS.API.Data;
using PMCRMS.API.DTOs;
using PMCRMS.API.Models;
using System.Text.Json;

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
                    CustomFields = !string.IsNullOrEmpty(f.CustomFields) 
                        ? JsonSerializer.Deserialize<List<CustomFieldDto>>(f.CustomFields) 
                        : new List<CustomFieldDto>(),
                    RequiredDocuments = !string.IsNullOrEmpty(f.RequiredDocuments)
                        ? JsonSerializer.Deserialize<List<string>>(f.RequiredDocuments)
                        : new List<string>()
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
                    CustomFields = !string.IsNullOrEmpty(form.CustomFields)
                        ? JsonSerializer.Deserialize<List<CustomFieldDto>>(form.CustomFields)
                        : new List<CustomFieldDto>(),
                    RequiredDocuments = !string.IsNullOrEmpty(form.RequiredDocuments)
                        ? JsonSerializer.Deserialize<List<string>>(form.RequiredDocuments)
                        : new List<string>()
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
            [FromBody] CreateFormConfigurationRequest request)
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
                    CustomFields = request.CustomFields,
                    RequiredDocuments = request.RequiredDocuments
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
            [FromBody] UpdateFormConfigurationRequest request)
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
}
