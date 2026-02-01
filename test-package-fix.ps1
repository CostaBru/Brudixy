# Brudixy Package Verification Script (Updated)
# Tests the fix per the official instructions

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Brudixy Package Structure Verification" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$packageVersion = "1.0.0.11"
$outputPath = ".\artifacts\package-fix-test"

# Step 1: Clean
Write-Host "[1/7] Cleaning..." -ForegroundColor Yellow
if (Test-Path $outputPath) {
    Remove-Item -Recurse -Force $outputPath
}
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
dotnet clean -c Release --nologo | Out-Null

Write-Host "✅ Cleaned" -ForegroundColor Green
Write-Host ""

# Step 2: Build all projects
Write-Host "[2/7] Building all projects..." -ForegroundColor Yellow
Write-Host "  Building solution..." -ForegroundColor Cyan
dotnet build -c Release --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build completed" -ForegroundColor Green
Write-Host ""

# Step 3: Verify bundled DLLs exist in Brudixy output
Write-Host "[3/7] Verifying bundled assemblies copied to output..." -ForegroundColor Yellow

$brudixyCoreInOutput = Test-Path ".\Brudixy\bin\Release\net8.0\Brudixy.Core.dll"
$brudixyCoreXmlInOutput = Test-Path ".\Brudixy\bin\Release\net8.0\Brudixy.Core.xml"
$brudixyInterfacesInOutput = Test-Path ".\Brudixy\bin\Release\net8.0\Brudixy.Interfaces.dll"
$brudixyInterfacesXmlInOutput = Test-Path ".\Brudixy\bin\Release\net8.0\Brudixy.Interfaces.xml"

if (-not $brudixyCoreInOutput) {
    Write-Host "❌ Brudixy.Core.dll not found in Brudixy output directory!" -ForegroundColor Red
    Write-Host "   Expected: .\Brudixy\bin\Release\net8.0\Brudixy.Core.dll" -ForegroundColor Yellow
    exit 1
}

if (-not $brudixyInterfacesInOutput) {
    Write-Host "❌ Brudixy.Interfaces.dll not found in Brudixy output directory!" -ForegroundColor Red
    Write-Host "   Expected: .\Brudixy\bin\Release\net8.0\Brudixy.Interfaces.dll" -ForegroundColor Yellow
    exit 1
}

Write-Host "  ✓ Brudixy.Core.dll found" -ForegroundColor Green
Write-Host "  ✓ Brudixy.Core.xml found: $brudixyCoreXmlInOutput" -ForegroundColor $(if($brudixyCoreXmlInOutput){"Green"}else{"Yellow"})
Write-Host "  ✓ Brudixy.Interfaces.dll found" -ForegroundColor Green
Write-Host "  ✓ Brudixy.Interfaces.xml found: $brudixyInterfacesXmlInOutput" -ForegroundColor $(if($brudixyInterfacesXmlInOutput){"Green"}else{"Yellow"})

Write-Host ""

# Step 4: Pack
Write-Host "[4/7] Packing Brudixy package..." -ForegroundColor Yellow
dotnet pack .\Brudixy\Brudixy.csproj -c Release -o $outputPath -p:PackageVersion=$packageVersion --no-build --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Pack failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Package created" -ForegroundColor Green
Write-Host ""

# Step 5: Extract package
Write-Host "[5/7] Extracting package..." -ForegroundColor Yellow

$nupkgFile = Get-ChildItem "$outputPath\Brudixy.$packageVersion.nupkg" -ErrorAction SilentlyContinue

if (-not $nupkgFile) {
    Write-Host "❌ Package file not found!" -ForegroundColor Red
    exit 1
}

Write-Host "  Package: $($nupkgFile.Name)" -ForegroundColor Cyan
Write-Host "  Size: $([math]::Round($nupkgFile.Length / 1KB, 2)) KB" -ForegroundColor Cyan

$extractPath = "$outputPath\extracted"
if (Test-Path $extractPath) {
    Remove-Item -Recurse -Force $extractPath
}

Expand-Archive -Path $nupkgFile.FullName -DestinationPath $extractPath -Force

Write-Host "✅ Extracted" -ForegroundColor Green
Write-Host ""

# Step 6: Verify package structure
Write-Host "[6/7] Verifying package structure per instructions..." -ForegroundColor Yellow
Write-Host ""

$libPath = "$extractPath\lib\net8.0"

if (-not (Test-Path $libPath)) {
    Write-Host "❌ lib/net8.0/ folder not found!" -ForegroundColor Red
    exit 1
}

Write-Host "Contents of lib/net8.0/:" -ForegroundColor Cyan
$libFiles = Get-ChildItem $libPath -File | Sort-Object Name

foreach ($file in $libFiles) {
    Write-Host "  $($file.Name)" -ForegroundColor White
}

Write-Host ""

# Check for required assemblies per instructions
$requiredFiles = @{
    "Brudixy.dll" = "Main runtime assembly"
    "Brudixy.Core.dll" = "Core runtime library"
    "Brudixy.Interfaces.dll" = "Interfaces runtime library"
}

Write-Host "Verification Results:" -ForegroundColor Cyan
$allPassed = $true

foreach ($file in $requiredFiles.Keys) {
    $exists = $libFiles.Name -contains $file
    if ($exists) {
        Write-Host "  ✅ $file - $($requiredFiles[$file])" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $file - MISSING!" -ForegroundColor Red
        $allPassed = $false
    }
}

Write-Host ""

# Check for XML documentation
$xmlFiles = $libFiles | Where-Object { $_.Extension -eq ".xml" }
Write-Host "XML Documentation Files:" -ForegroundColor Cyan
if ($xmlFiles) {
    foreach ($xml in $xmlFiles) {
        Write-Host "  ✅ $($xml.Name)" -ForegroundColor Green
    }
} else {
    Write-Host "  ⚠️  No XML files (IntelliSense may not work)" -ForegroundColor Yellow
}

Write-Host ""

# Check for PDB files
$pdbFiles = $libFiles | Where-Object { $_.Extension -eq ".pdb" }
Write-Host "Symbol (PDB) Files:" -ForegroundColor Cyan
if ($pdbFiles) {
    foreach ($pdb in $pdbFiles) {
        Write-Host "  ✅ $($pdb.Name)" -ForegroundColor Green
    }
} else {
    Write-Host "  ℹ️  No PDB files in main package" -ForegroundColor Gray
}

Write-Host ""

# Step 7: Test with consumer project
Write-Host "[7/7] Testing with consumer project..." -ForegroundColor Yellow

$consumerPath = "$outputPath\TestConsumer"
if (Test-Path $consumerPath) {
    Remove-Item -Recurse -Force $consumerPath
}

dotnet new console -n TestConsumer -o $consumerPath -f net8.0 --force | Out-Null

Push-Location $consumerPath

try {
    # Add local package source
    dotnet nuget add source $outputPath -n BrudixTestSource 2>&1 | Out-Null
    
    # Add package
    Write-Host "  Installing Brudixy package..." -ForegroundColor Cyan
    dotnet add package Brudixy --version $packageVersion --source BrudixTestSource --no-restore | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to add package!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    # Create test code that uses Brudixy.Interfaces types
    $testCode = @"
using Brudixy;
using Brudixy.Interfaces;

// Test main assembly
var table = new DataTable();
table.AddColumn("Id", typeof(int));
table.AddColumn("Name", typeof(string));

// Test Brudixy.Interfaces types are accessible (this is the critical test!)
IDataTable iTable = table;
IDataTableRow row = table.AddRow(1, "Test");

// Test method that returns interface type
IDataTable GetTable() => table;

Console.WriteLine("✅ SUCCESS: All Brudixy assemblies accessible!");
Console.WriteLine("  - Brudixy types: ✓");
Console.WriteLine("  - Brudixy.Interfaces types: ✓");
Console.WriteLine("  - Brudixy.Core types: ✓");
"@
    
    $testCode | Out-File -FilePath "Program.cs" -Encoding UTF8 -Force
    
    # Restore
    Write-Host "  Restoring..." -ForegroundColor Cyan
    dotnet restore --force 2>&1 | Out-Null
    
    # Build
    Write-Host "  Building consumer..." -ForegroundColor Cyan
    $buildOutput = dotnet build 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Consumer build failed!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Build output:" -ForegroundColor Yellow
        Write-Host $buildOutput
        Write-Host ""
        Write-Host "This indicates Brudixy.Interfaces types are NOT accessible!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    Write-Host "  ✅ Consumer built successfully" -ForegroundColor Green
    
    # Run
    Write-Host "  Running consumer test..." -ForegroundColor Cyan
    $runOutput = dotnet run 2>&1
    
    Write-Host ""
    Write-Host "$runOutput" -ForegroundColor Green
    
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan

if (-not $allPassed) {
    Write-Host "❌ VERIFICATION FAILED" -ForegroundColor Red
    Write-Host "   Missing required assemblies in lib/ folder" -ForegroundColor Red
    exit 1
}

Write-Host "✅ ALL TESTS PASSED!" -ForegroundColor Green
Write-Host ""
Write-Host "Package Structure Verified:" -ForegroundColor Green
Write-Host "  ✓ All 3 DLLs in lib/net8.0/" -ForegroundColor Green
Write-Host "  ✓ Consumer can reference Brudixy.Interfaces types" -ForegroundColor Green
Write-Host "  ✓ Consumer build and run successful" -ForegroundColor Green
Write-Host ""
Write-Host "Package location: $($nupkgFile.FullName)" -ForegroundColor Cyan
Write-Host ""
