using System.ComponentModel.DataAnnotations;

namespace PMCRMS.API.Models
{
    /// <summary>
    /// System-wide settings and assets like logos, certificates, etc.
    /// </summary>
    public class SystemSettings : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? SettingValue { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        // For storing binary data like logos, watermarks, etc.
        public byte[]? BinaryData { get; set; }

        [MaxLength(100)]
        public string? ContentType { get; set; }

        public bool IsActive { get; set; } = true;

        // Common settings keys:
        // - PMC_LOGO: Official PMC logo for certificates
        // - CERTIFICATE_WATERMARK: Watermark for certificates
        // - SIGNATURE_CE: City Engineer signature
        // - SIGNATURE_EE: Executive Engineer signature
    }
}
