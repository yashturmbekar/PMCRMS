using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace PMCRMS.API.Services
{
    /// <summary>
    /// Service for managing custom font registration for PDF generation
    /// Ensures Marathi/Devanagari fonts are available across all PDFs
    /// </summary>
    public static class FontService
    {
        private static bool _fontsRegistered = false;
        private static readonly object _lock = new object();
        private static ILogger? _logger;

        /// <summary>
        /// Register custom fonts for Marathi/Devanagari support
        /// Call this once during application startup
        /// </summary>
        public static void RegisterFonts(IWebHostEnvironment environment, ILogger logger)
        {
            lock (_lock)
            {
                if (_fontsRegistered)
                {
                    logger.LogInformation("✅ Fonts already registered");
                    return;
                }

                _logger = logger;

                try
                {
                    var fontsPath = Path.Combine(environment.WebRootPath, "Fonts");
                    
                    if (!Directory.Exists(fontsPath))
                    {
                        logger.LogWarning("⚠️ Fonts directory not found at {FontsPath}. Creating directory.", fontsPath);
                        Directory.CreateDirectory(fontsPath);
                    }

                    // Register Devanagari fonts for Marathi support
                    RegisterFont(fontsPath, "Nirmala.ttf", "Nirmala UI");
                    RegisterFont(fontsPath, "NirmalaB.ttf", "Nirmala UI Bold");
                    RegisterFont(fontsPath, "Mangal.ttf", "Mangal");
                    RegisterFont(fontsPath, "NotoSansDevanagari-Regular.ttf", "Noto Sans Devanagari");
                    RegisterFont(fontsPath, "NotoSansDevanagari-Bold.ttf", "Noto Sans Devanagari Bold");

                    _fontsRegistered = true;
                    logger.LogInformation("✅ Custom fonts registered successfully for PDF generation");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Failed to register custom fonts. Marathi text may not render correctly.");
                    // Don't throw - fallback to system fonts
                }
            }
        }

        private static void RegisterFont(string fontsPath, string fileName, string fontName)
        {
            try
            {
                var fontPath = Path.Combine(fontsPath, fileName);
                
                if (File.Exists(fontPath))
                {
                    // Read font file as stream and register with QuestPDF
                    using (var fontStream = File.OpenRead(fontPath))
                    {
                        FontManager.RegisterFont(fontStream);
                    }
                    
                    _logger?.LogInformation("✅ Registered font: {FontName} from {FileName}", fontName, fileName);
                }
                else
                {
                    _logger?.LogWarning("⚠️ Font file not found: {FontPath}", fontPath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "⚠️ Failed to register font: {FileName}", fileName);
            }
        }

        /// <summary>
        /// Get the primary Marathi font name
        /// </summary>
        public static string MarathiFontFamily => "Nirmala UI";

        /// <summary>
        /// Get the fallback Marathi font name
        /// </summary>
        public static string MarathiFontFallback => "Mangal";

        /// <summary>
        /// Get the English font name
        /// </summary>
        public static string EnglishFontFamily => "Arial";
    }
}
