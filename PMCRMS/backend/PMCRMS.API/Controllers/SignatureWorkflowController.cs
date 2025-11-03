using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.Data;
using PMCRMS.API.Models;
using PMCRMS.API.Services;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SignatureWorkflowController : ControllerBase
    {
        private readonly ISignatureWorkflowService _signatureService;
        private readonly IHsmService _hsmService;
        private readonly PMCRMSDbContext _context;
        private readonly ILogger<SignatureWorkflowController> _logger;

        public SignatureWorkflowController(
            ISignatureWorkflowService signatureService,
            IHsmService hsmService,
            PMCRMSDbContext context,
            ILogger<SignatureWorkflowController> logger)
        {
            _signatureService = signatureService;
            _hsmService = hsmService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Generate OTP for digital signature
        /// POST /api/SignatureWorkflow/generate-otp
        /// </summary>
        [HttpPost("generate-otp")]
        public async Task<IActionResult> GenerateOtp([FromBody] GenerateOtpRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Generating OTP for application {ApplicationId}, officer {OfficerId}",
                    request.ApplicationId, request.OfficerId);

                // Generate OTP using HSM service
                var result = await _hsmService.GenerateOtpForOfficerAsync(
                    request.ApplicationId,
                    request.OfficerId,
                    _context
                );

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        title = "OTP Sent",
                        rawResponse = result.RawResponse
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage,
                    title = "OTP Generation Failed",
                    rawResponse = result.RawResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error while generating OTP",
                    title = "Error"
                });
            }
        }

        /// <summary>
        /// Junior Engineer signs recommendation form (First signature)
        /// POST /api/SignatureWorkflow/sign-je
        /// </summary>
        [HttpPost("sign-je")]
        [Authorize(Roles = "JuniorArchitect,JuniorLicenceEngineer,JuniorStructuralEngineer,JuniorSupervisor1,JuniorSupervisor2,Admin")]
        public async Task<IActionResult> SignAsJuniorEngineer([FromBody] SignDocumentRequest request)
        {
            try
            {
                var result = await _signatureService.SignAsJuniorEngineerAsync(
                    request.ApplicationId,
                    request.OfficerId,
                    request.Otp
                );

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        title = "Signature Successful",
                        nextStatus = result.NextStatus?.ToString(),
                        signedDocumentId = result.SignedDocumentId // SEDocument ID in database
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage,
                    title = "Signature Failed",
                    rawResponse = result.RawHsmResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during JE signature");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error during signature",
                    title = "Error"
                });
            }
        }

        /// <summary>
        /// Assistant Engineer signs recommendation form (Second signature)
        /// POST /api/SignatureWorkflow/sign-ae
        /// </summary>
        [HttpPost("sign-ae")]
        [Authorize(Roles = "AssistantArchitect,AssistantLicenceEngineer,AssistantStructuralEngineer,AssistantSupervisor1,AssistantSupervisor2,Admin")]
        public async Task<IActionResult> SignAsAssistantEngineer([FromBody] SignDocumentRequest request)
        {
            try
            {
                var result = await _signatureService.SignAsAssistantEngineerAsync(
                    request.ApplicationId,
                    request.OfficerId,
                    request.Otp
                );

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        title = "Signature Successful",
                        nextStatus = result.NextStatus?.ToString(),
                        signedDocumentId = result.SignedDocumentId
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage,
                    title = "Signature Failed",
                    rawResponse = result.RawHsmResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AE signature");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error during signature",
                    title = "Error"
                });
            }
        }

        /// <summary>
        /// Executive Engineer signs recommendation form (Third signature)
        /// POST /api/SignatureWorkflow/sign-ee
        /// </summary>
        [HttpPost("sign-ee")]
        [Authorize(Roles = "ExecutiveEngineer")]
        public async Task<IActionResult> SignAsExecutiveEngineer([FromBody] SignDocumentRequest request)
        {
            try
            {
                var result = await _signatureService.SignAsExecutiveEngineerAsync(
                    request.ApplicationId,
                    request.OfficerId,
                    request.Otp
                );

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        title = "Signature Successful",
                        nextStatus = result.NextStatus?.ToString(),
                        signedDocumentId = result.SignedDocumentId
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage,
                    title = "Signature Failed",
                    rawResponse = result.RawHsmResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during EE signature");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error during signature",
                    title = "Error"
                });
            }
        }

        /// <summary>
        /// City Engineer signs recommendation form (Fourth and final signature)
        /// POST /api/SignatureWorkflow/sign-ce
        /// </summary>
        [HttpPost("sign-ce")]
        [Authorize(Roles = "CityEngineer")]
        public async Task<IActionResult> SignAsCityEngineer([FromBody] SignDocumentRequest request)
        {
            try
            {
                var result = await _signatureService.SignAsCityEngineerAsync(
                    request.ApplicationId,
                    request.OfficerId,
                    request.Otp
                );

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        title = "Final Signature Successful",
                        nextStatus = result.NextStatus?.ToString(),
                        signedDocumentId = result.SignedDocumentId,
                        note = "All signatures completed. Ready for certificate generation."
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage,
                    title = "Signature Failed",
                    rawResponse = result.RawHsmResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CE signature");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error during signature",
                    title = "Error"
                });
            }
        }

        /// <summary>
        /// Get signature status for an application
        /// GET /api/SignatureWorkflow/status/{applicationId}
        /// </summary>
        [HttpGet("status/{applicationId}")]
        public async Task<IActionResult> GetSignatureStatus(int applicationId)
        {
            try
            {
                var result = await _signatureService.GetSignatureStatusAsync(applicationId);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            applicationId = result.ApplicationId,
                            currentStatus = result.CurrentStatus.ToString(),
                            signatures = new
                            {
                                juniorEngineer = result.JuniorEngineerSigned,
                                assistantEngineer = result.AssistantEngineerSigned,
                                executiveEngineer = result.ExecutiveEngineerSigned,
                                cityEngineer = result.CityEngineerSigned
                            },
                            allComplete = result.AllSignaturesComplete,
                            nextSigner = result.NextSigner
                        }
                    });
                }

                return NotFound(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signature status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Get signature coordinates for a specific role
        /// GET /api/SignatureWorkflow/coordinates/{role}
        /// </summary>
        [HttpGet("coordinates/{role}")]
        public IActionResult GetSignatureCoordinates(string role)
        {
            try
            {
                OfficerRole officerRole = role.ToLower() switch
                {
                    "je" or "juniorengineer" or "juniorstructuralengineer" => OfficerRole.JuniorStructuralEngineer,
                    "ae" or "assistantengineer" or "assistantstructuralengineer" => OfficerRole.AssistantStructuralEngineer,
                    "ee" or "executiveengineer" => OfficerRole.ExecutiveEngineer,
                    "ce" or "cityengineer" => OfficerRole.CityEngineer,
                    _ => OfficerRole.JuniorStructuralEngineer
                };

                var coordinates = _signatureService.GetSignatureCoordinates(officerRole, 0);

                return Ok(new
                {
                    success = true,
                    role = officerRole.ToString(),
                    coordinates = coordinates,
                    description = $"Signature coordinates for {officerRole}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coordinates");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }
    }

    #region Request DTOs

    public class GenerateOtpRequest
    {
        public int ApplicationId { get; set; }
        public int OfficerId { get; set; }
    }

    public class SignDocumentRequest
    {
        public int ApplicationId { get; set; }
        public int OfficerId { get; set; }
        public string Otp { get; set; } = string.Empty;
    }

    #endregion
}
