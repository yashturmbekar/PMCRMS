# PowerShell Script to Copy Marathi/Devanagari Fonts for PDF Generation
# Run this script on your Windows machine to copy required fonts to the project

$fontsDestination = ".\wwwroot\Fonts"
$windowsFonts = "C:\Windows\Fonts"

# Create destination folder if it doesn't exist
if (!(Test-Path $fontsDestination)) {
    New-Item -ItemType Directory -Path $fontsDestination -Force
    Write-Host "[OK] Created Fonts directory: $fontsDestination" -ForegroundColor Green
}

# List of fonts to copy
$fontsToCopy = @(
    @{Source = "nirmala.ttf"; Dest = "Nirmala.ttf"; Name = "Nirmala UI Regular"},
    @{Source = "nirmalab.ttf"; Dest = "NirmalaB.ttf"; Name = "Nirmala UI Bold"},
    @{Source = "mangal.ttf"; Dest = "Mangal.ttf"; Name = "Mangal"}
)

Write-Host "`nCopying Marathi/Devanagari fonts..." -ForegroundColor Cyan

foreach ($font in $fontsToCopy) {
    $sourcePath = Join-Path $windowsFonts $font.Source
    $destPath = Join-Path $fontsDestination $font.Dest
    
    if (Test-Path $sourcePath) {
        try {
            Copy-Item -Path $sourcePath -Destination $destPath -Force
            $fileSize = (Get-Item $destPath).Length / 1KB
            $fileSizeRounded = [math]::Round($fileSize, 2)
            Write-Host "  [OK] Copied: $($font.Name) ($fileSizeRounded KB)" -ForegroundColor Green
        }
        catch {
            Write-Host "  [ERROR] Failed to copy: $($font.Name) - $_" -ForegroundColor Red
        }
    }
    else {
        Write-Host "  [WARN] Font not found: $($font.Name) at $sourcePath" -ForegroundColor Yellow
    }
}

Write-Host "`nFonts copied successfully!" -ForegroundColor Green
Write-Host "   Location: $fontsDestination" -ForegroundColor Cyan

# List copied fonts
Write-Host "`nCopied font files:" -ForegroundColor Cyan
Get-ChildItem -Path $fontsDestination -Filter "*.ttf" | ForEach-Object {
    $sizeKB = [math]::Round($_.Length / 1KB, 2)
    Write-Host "   - $($_.Name) ($sizeKB KB)" -ForegroundColor Gray
}

Write-Host "`n[OK] Font copying complete!" -ForegroundColor Green
Write-Host "   You can now build and deploy your application with Marathi font support." -ForegroundColor Cyan
Write-Host "`nAlternative: If fonts are missing, download 'Noto Sans Devanagari' from Google Fonts" -ForegroundColor Yellow
Write-Host "   https://fonts.google.com/noto/specimen/Noto+Sans+Devanagari" -ForegroundColor Yellow
