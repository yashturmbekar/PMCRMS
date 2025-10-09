using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.DTOs
{
    public class SendOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [MaxLength(15)]
        public string? PhoneNumber { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Purpose { get; set; } = string.Empty; // LOGIN, REGISTRATION, CERTIFICATE_DOWNLOAD
    }

    public class VerifyOtpRequest
    {
        [Required]
        public string Identifier { get; set; } = string.Empty; // Email or Phone
        
        [Required]
        [MaxLength(10)]
        public string OtpCode { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Purpose { get; set; } = string.Empty;
    }

    public class OfficerLoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;
        
        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string OtpCode { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Address { get; set; }
        public string? EmployeeId { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
    }

    public class ApiResponse : ApiResponse<object>
    {
    }
}