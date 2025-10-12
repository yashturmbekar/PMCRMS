using Microsoft.AspNetCore.Mvc;
using PMCRMS.API.Services;

namespace PMCRMS.API.Controllers
{
    /// <summary>
    /// Public API for certificate and document downloads
    /// Uses OTP-based authentication for secure access
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly DocumentDownloadService _downloadService;
        private readonly ILogger<DownloadController> _logger;

        public DownloadController(
            DocumentDownloadService downloadService,
            ILogger<DownloadController> logger)
        {
            _downloadService = downloadService;
            _logger = logger;
        }

        /// <summary>
        /// Request OTP for document download access
        /// PUBLIC ENDPOINT - No authentication required
        /// </summary>
        /// <param name="request">Application number and email</param>
        /// <returns>Success status and OTP sent confirmation</returns>
        [HttpPost("RequestAccess")]
        public async Task<IActionResult> RequestAccess([FromBody] DownloadAccessRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ApplicationNumber) || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Application number and email are required."
                });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, message) = await _downloadService.RequestAccessAsync(
                request.ApplicationNumber,
                request.Email,
                ipAddress);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = message,
                    otpSent = true
                });
            }

            return BadRequest(new
            {
                success = false,
                message = message
            });
        }

        /// <summary>
        /// Verify OTP and receive download token
        /// PUBLIC ENDPOINT - No authentication required
        /// </summary>
        /// <param name="request">Application number and OTP</param>
        /// <returns>Download token if OTP is valid</returns>
        [HttpPost("VerifyOTP")]
        public async Task<IActionResult> VerifyOTP([FromBody] OtpVerifyRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ApplicationNumber) || string.IsNullOrWhiteSpace(request.Otp))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Application number and OTP are required."
                });
            }

            var (success, message, downloadToken, applicantName) = await _downloadService.VerifyOtpAsync(
                request.ApplicationNumber,
                request.Otp);

            if (success && downloadToken != null)
            {
                return Ok(new
                {
                    success = true,
                    message = message,
                    downloadToken = downloadToken,
                    applicantName = applicantName,
                    certificateAvailable = true
                });
            }

            return BadRequest(new
            {
                success = false,
                message = message
            });
        }

        /// <summary>
        /// Download certificate PDF
        /// PUBLIC ENDPOINT - Requires valid download token
        /// </summary>
        /// <param name="token">Download token from OTP verification</param>
        /// <returns>Certificate PDF file</returns>
        [HttpGet("Certificate/{token}")]
        public async Task<IActionResult> DownloadCertificate(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Download token is required." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var (fileBytes, fileName, errorMessage) = await _downloadService.GetCertificateAsync(
                token,
                ipAddress,
                userAgent);

            if (fileBytes == null || string.IsNullOrEmpty(fileName))
            {
                return BadRequest(new { message = errorMessage });
            }

            return File(fileBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Download recommendation form PDF
        /// PUBLIC ENDPOINT - Requires valid download token
        /// </summary>
        /// <param name="token">Download token from OTP verification</param>
        /// <returns>Recommendation form PDF file</returns>
        [HttpGet("RecommendationForm/{token}")]
        public async Task<IActionResult> DownloadRecommendationForm(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Download token is required." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var (fileBytes, fileName, errorMessage) = await _downloadService.GetRecommendationFormAsync(
                token,
                ipAddress,
                userAgent);

            if (fileBytes == null || string.IsNullOrEmpty(fileName))
            {
                return BadRequest(new { message = errorMessage });
            }

            return File(fileBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Download payment challan PDF
        /// PUBLIC ENDPOINT - Requires valid download token
        /// </summary>
        /// <param name="token">Download token from OTP verification</param>
        /// <returns>Payment challan PDF file</returns>
        [HttpGet("Challan/{token}")]
        public async Task<IActionResult> DownloadChallan(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Download token is required." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var (fileBytes, fileName, errorMessage) = await _downloadService.GetChallanAsync(
                token,
                ipAddress,
                userAgent);

            if (fileBytes == null || string.IsNullOrEmpty(fileName))
            {
                return BadRequest(new { message = errorMessage });
            }

            return File(fileBytes, "application/pdf", fileName);
        }

        // DTOs for request/response
        public class DownloadAccessRequest
        {
            public string ApplicationNumber { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class OtpVerifyRequest
        {
            public string ApplicationNumber { get; set; } = string.Empty;
            public string Otp { get; set; } = string.Empty;
        }
    }
}
