# Test Script - Verify Marathi Font Support

Write-Host "`n=== PMCRMS Marathi Font Support Verification ===" -ForegroundColor Cyan
Write-Host "This script verifies that Marathi fonts are properly configured.`n" -ForegroundColor Gray

$success = $true

# 1. Check Fonts Directory
Write-Host "[1/4] Checking Fonts directory..." -ForegroundColor Yellow
$fontsPath = ".\wwwroot\Fonts"
if (Test-Path $fontsPath) {
    Write-Host "  [OK] Fonts directory exists" -ForegroundColor Green
    
    # Check individual font files
    $requiredFonts = @("Nirmala.ttf", "NirmalaB.ttf")
    foreach ($font in $requiredFonts) {
        $fontPath = Join-Path $fontsPath $font
        if (Test-Path $fontPath) {
            $size = [math]::Round((Get-Item $fontPath).Length / 1KB, 2)
            Write-Host "  [OK] $font found ($size KB)" -ForegroundColor Green
        } else {
            Write-Host "  [ERROR] $font NOT FOUND" -ForegroundColor Red
            Write-Host "         Run: .\copy-fonts.ps1" -ForegroundColor Yellow
            $success = $false
        }
    }
} else {
    Write-Host "  [ERROR] Fonts directory not found at $fontsPath" -ForegroundColor Red
    Write-Host "         Run: .\copy-fonts.ps1" -ForegroundColor Yellow
    $success = $false
}

# 2. Check FontService.cs exists
Write-Host "`n[2/4] Checking FontService..." -ForegroundColor Yellow
$fontServicePath = ".\Services\FontService.cs"
if (Test-Path $fontServicePath) {
    Write-Host "  [OK] FontService.cs exists" -ForegroundColor Green
    
    # Check if it contains RegisterFonts method
    $content = Get-Content $fontServicePath -Raw
    if ($content -match "RegisterFonts") {
        Write-Host "  [OK] RegisterFonts method found" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] RegisterFonts method not found" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [ERROR] FontService.cs not found" -ForegroundColor Red
    $success = $false
}

# 3. Check Program.cs for font registration
Write-Host "`n[3/4] Checking Program.cs..." -ForegroundColor Yellow
$programPath = ".\Program.cs"
if (Test-Path $programPath) {
    $programContent = Get-Content $programPath -Raw
    if ($programContent -match "FontService\.RegisterFonts") {
        Write-Host "  [OK] FontService.RegisterFonts() called in startup" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] FontService.RegisterFonts() NOT called in Program.cs" -ForegroundColor Red
        $success = $false
    }
} else {
    Write-Host "  [ERROR] Program.cs not found" -ForegroundColor Red
    $success = $false
}

# 4. Build project
Write-Host "`n[4/4] Building project..." -ForegroundColor Yellow
$buildOutput = dotnet build 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Build successful" -ForegroundColor Green
} else {
    Write-Host "  [ERROR] Build failed" -ForegroundColor Red
    Write-Host "  Output: $buildOutput" -ForegroundColor Gray
    $success = $false
}

# Summary
Write-Host "`n=== Verification Summary ===" -ForegroundColor Cyan
if ($success) {
    Write-Host "[SUCCESS] All checks passed! Your application is ready." -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "  1. Test locally: dotnet run" -ForegroundColor Gray
    Write-Host "  2. Generate a test PDF (certificate/challan)" -ForegroundColor Gray
    Write-Host "  3. Verify Marathi text displays correctly" -ForegroundColor Gray
    Write-Host "  4. Deploy to server: git add . && git commit -m 'Fix: Marathi fonts' && git push" -ForegroundColor Gray
} else {
    Write-Host "[FAILED] Some checks failed. Please fix the issues above." -ForegroundColor Red
}

Write-Host ""
