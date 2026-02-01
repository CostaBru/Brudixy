# Package Content Verification Script
# This script builds, packs, and verifies the Brudixy NuGet package contains all required files

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Brudixy Package Content Verification" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$packageVersion = "1.0.0-verify"
$outputPath = ".\artifacts\package-verify"
$extractPath = "$outputPath\extracted"

# Step 1: Clean
Write-Host "[1/6] Cleaning previous outputs..." -ForegroundColor Yellow
if (Test-Path $outputPath) {
    Remove-Item -Recurse -Force $outputPath
}
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

# Step 2: Build in Release mode
Write-Host "[2/6] Building projects in Release mode..." -ForegroundColor Yellow
Write-Host "  Building Brudixy.Interfaces..." -ForegroundColor Cyan
dotnet build .\Brudixy.Interfaces\Brudixy.Interfaces.csproj -c Release --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Brudixy.Interfaces build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "  Building Brudixy.Core..." -ForegroundColor Cyan
dotnet build .\Brudixy.Core\Brudixy.Core.csproj -c Release --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Brudixy.Core build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "  Building Brudixy..." -ForegroundColor Cyan
dotnet build .\Brudixy\Brudixy.csproj -c Release --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Brudixy build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ All projects built successfully" -ForegroundColor Green
Write-Host ""

# Step 3: Check XML files were generated
Write-Host "[3/6] Verifying XML documentation files..." -ForegroundColor Yellow

$xmlFiles = @(
    ".\Brudixy\bin\Release\net8.0\Brudixy.xml",
    ".\Brudixy.Core\bin\Release\net8.0\Brudixy.Core.xml",
    ".\Brudixy.Interfaces\bin\Release\net8.0\Brudixy.Interfaces.xml"
)

$missingXml = @()
foreach ($xml in $xmlFiles) {
    if (Test-Path $xml) {
        Write-Host "  ✓ $xml" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $xml (MISSING)" -ForegroundColor Red
        $missingXml += $xml
    }
}

if ($missingXml.Count -gt 0) {
    Write-Host ""
    Write-Host "❌ XML documentation files are missing!" -ForegroundColor Red
    Write-Host "   Make sure GenerateDocumentationFile is enabled in all projects." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ All XML documentation files generated" -ForegroundColor Green
Write-Host ""

# Step 4: Pack the NuGet package
Write-Host "[4/6] Creating NuGet package..." -ForegroundColor Yellow
dotnet pack .\Brudixy\Brudixy.csproj -c Release -o $outputPath --no-build -p:PackageVersion=$packageVersion --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Pack failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Package created" -ForegroundColor Green
Write-Host ""

# Step 5: Extract and inspect package
Write-Host "[5/6] Inspecting package contents..." -ForegroundColor Yellow

$nupkgFile = Get-ChildItem "$outputPath\Brudixy.$packageVersion.nupkg" -ErrorAction SilentlyContinue

if (-not $nupkgFile) {
    Write-Host "❌ Package file not found!" -ForegroundColor Red
    exit 1
}

Write-Host "  Package: $($nupkgFile.Name)" -ForegroundColor Cyan
Write-Host "  Size: $([math]::Round($nupkgFile.Length / 1KB, 2)) KB" -ForegroundColor Cyan
Write-Host ""

# Extract package
if (Test-Path $extractPath) {
    Remove-Item -Recurse -Force $extractPath
}
Expand-Archive -Path $nupkgFile.FullName -DestinationPath $extractPath -Force

# Check lib folder contents
Write-Host "  Contents of lib/net8.0/:" -ForegroundColor Cyan
$libPath = "$extractPath\lib\net8.0"

if (-not (Test-Path $libPath)) {
    Write-Host "❌ lib/net8.0/ folder not found in package!" -ForegroundColor Red
    exit 1
}

$libFiles = Get-ChildItem $libPath -File | Sort-Object Name

if ($libFiles.Count -eq 0) {
    Write-Host "❌ No files found in lib/net8.0/!" -ForegroundColor Red
    exit 1
}

$dllFiles = @()
$xmlFiles = @()
$pdbFiles = @()

foreach ($file in $libFiles) {
    Write-Host "    $($file.Name)" -ForegroundColor White
    
    switch ($file.Extension) {
        ".dll" { $dllFiles += $file.Name }
        ".xml" { $xmlFiles += $file.Name }
        ".pdb" { $pdbFiles += $file.Name }
    }
}

Write-Host ""

# Step 6: Verify required files
Write-Host "[6/6] Verifying required files..." -ForegroundColor Yellow

$requiredDlls = @("Brudixy.dll", "Brudixy.Core.dll", "Brudixy.Interfaces.dll")
$requiredXmls = @("Brudixy.xml", "Brudixy.Core.xml", "Brudixy.Interfaces.xml")

Write-Host ""
Write-Host "DLL Files:" -ForegroundColor Cyan
$missingDlls = @()
foreach ($dll in $requiredDlls) {
    if ($dllFiles -contains $dll) {
        Write-Host "  ✅ $dll" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $dll (MISSING)" -ForegroundColor Red
        $missingDlls += $dll
    }
}

Write-Host ""
Write-Host "XML Documentation Files:" -ForegroundColor Cyan
$missingXmls = @()
foreach ($xml in $requiredXmls) {
    if ($xmlFiles -contains $xml) {
        Write-Host "  ✅ $xml" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  $xml (MISSING)" -ForegroundColor Yellow
        $missingXmls += $xml
    }
}

Write-Host ""
Write-Host "PDB Symbol Files:" -ForegroundColor Cyan
if ($pdbFiles.Count -gt 0) {
    foreach ($pdb in $pdbFiles) {
        Write-Host "  ✅ $pdb" -ForegroundColor Green
    }
} else {
    Write-Host "  ℹ️  No PDB files (they may be in separate symbol package)" -ForegroundColor Gray
}

# Check for symbol package
$snupkgFile = Get-ChildItem "$outputPath\Brudixy.$packageVersion.snupkg" -ErrorAction SilentlyContinue
if ($snupkgFile) {
    Write-Host ""
    Write-Host "Symbol Package:" -ForegroundColor Cyan
    Write-Host "  ✅ $($snupkgFile.Name) ($([math]::Round($snupkgFile.Length / 1KB, 2)) KB)" -ForegroundColor Green
}

# Final verdict
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan

if ($missingDlls.Count -gt 0) {
    Write-Host "❌ FAILED: Missing required DLL files!" -ForegroundColor Red
    exit 1
}

if ($missingXmls.Count -gt 0) {
    Write-Host "⚠️  WARNING: Missing XML documentation files!" -ForegroundColor Yellow
    Write-Host "   IntelliSense may not work properly for consumers." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   This usually means GenerateDocumentationFile is not enabled." -ForegroundColor Yellow
} else {
    Write-Host "✅ ALL CHECKS PASSED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Package contains:" -ForegroundColor Green
    Write-Host "  • All 3 required DLL files" -ForegroundColor Green
    Write-Host "  • All 3 XML documentation files" -ForegroundColor Green
    if ($pdbFiles.Count -gt 0 -or $snupkgFile) {
        Write-Host "  • Symbol/PDB files" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Package location: $($nupkgFile.FullName)" -ForegroundColor Cyan
Write-Host "Extracted to: $extractPath" -ForegroundColor Cyan
Write-Host ""
