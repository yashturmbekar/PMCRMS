using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.Services;
using PMCRMS.API.ViewModels;

namespace PMCRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PdfController : ControllerBase
    {
        private readonly PdfService _pdfService;
        private readonly ILogger<PdfController> _logger;

        public PdfController(PdfService pdfService, ILogger<PdfController> logger)
        {
            _pdfService = pdfService;
            _logger = logger;
        }

        /// <summary>
        /// Generates PDF for the specified application
        /// </summary>
        /// <param name="request">PDF generation request containing application ID</param>
        /// <returns>PDF generation response with file content or error message</returns>
        [HttpPost("generate")]
        public async Task<ActionResult<PdfGenerationResponse>> GenerateApplicationPdf([FromBody] PdfGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Generating PDF for application ID: {ApplicationId}", request.ApplicationId);

                if (request.ApplicationId <= 0)
                {
                    return BadRequest(new PdfGenerationResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid application ID"
                    });
                }

                var result = await _pdfService.GenerateApplicationPdfAsync(request.ApplicationId);

                if (!result.IsSuccess)
                {
                    _logger.LogError("Failed to generate PDF for application ID: {ApplicationId}. Error: {Error}",
                        request.ApplicationId, result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Successfully generated PDF for application ID: {ApplicationId}", request.ApplicationId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for application ID: {ApplicationId}", request.ApplicationId);
                return StatusCode(500, new PdfGenerationResponse
                {
                    IsSuccess = false,
                    Message = "An internal error occurred while generating the PDF"
                });
            }
        }

        /// <summary>
        /// Downloads the generated PDF file
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>PDF file for download</returns>
        [HttpGet("download/{applicationId}")]
        public async Task<IActionResult> DownloadPdf(int applicationId)
        {
            try
            {
                _logger.LogInformation("Downloading PDF for application ID: {ApplicationId}", applicationId);

                var result = await _pdfService.GenerateApplicationPdfAsync(applicationId);

                if (!result.IsSuccess)
                {
                    _logger.LogError("Failed to generate PDF for download. Application ID: {ApplicationId}. Error: {Error}",
                        applicationId, result.Message);
                    return NotFound(result.Message);
                }

                if (result.FileContent == null)
                {
                    _logger.LogError("PDF content is null for application ID: {ApplicationId}", applicationId);
                    return StatusCode(500, "Failed to retrieve PDF content");
                }

                var fileName = result.FileName ?? $"Application_{applicationId}.pdf";

                _logger.LogInformation("Successfully retrieved PDF for download. Application ID: {ApplicationId}", applicationId);

                return File(result.FileContent, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading PDF for application ID: {ApplicationId}", applicationId);
                return StatusCode(500, "An internal error occurred while downloading the PDF");
            }
        }

        /// <summary>
        /// Generates and immediately downloads PDF for the specified application
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>PDF file for immediate download</returns>
        [HttpGet("generate-and-download/{applicationId}")]
        public async Task<IActionResult> GenerateAndDownloadPdf(int applicationId)
        {
            try
            {
                _logger.LogInformation("Generating and downloading PDF for application ID: {ApplicationId}", applicationId);

                if (applicationId <= 0)
                {
                    return BadRequest("Invalid application ID");
                }

                var result = await _pdfService.GenerateApplicationPdfAsync(applicationId);

                if (!result.IsSuccess)
                {
                    _logger.LogError("Failed to generate PDF for application ID: {ApplicationId}. Error: {Error}",
                        applicationId, result.Message);
                    return BadRequest(result.Message);
                }

                if (result.FileContent == null)
                {
                    _logger.LogError("PDF content is null for application ID: {ApplicationId}", applicationId);
                    return StatusCode(500, "Failed to retrieve PDF content");
                }

                var fileName = result.FileName ?? $"Application_{applicationId}.pdf";

                _logger.LogInformation("Successfully generated and retrieved PDF for application ID: {ApplicationId}", applicationId);

                return File(result.FileContent, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating and downloading PDF for application ID: {ApplicationId}", applicationId);
                return StatusCode(500, "An internal error occurred while processing the PDF");
            }
        }
    }
}
