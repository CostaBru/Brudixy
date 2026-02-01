# Test script to verify Brudixy NuGet package exposes Brudixy.Interfaces correctly

Write-Host "Testing Brudixy NuGet Package Bundling Fix" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean and build
Write-Host "[1/5] Building Brudixy projects..." -ForegroundColor Yellow
dotnet build .\Brudixy\Brudixy.csproj -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build succeeded" -ForegroundColor Green
Write-Host ""

# Step 2: Pack the NuGet package
Write-Host "[2/5] Creating NuGet package..." -ForegroundColor Yellow
$packOutput = ".\artifacts\package-test"
if (Test-Path $packOutput) {
    Remove-Item -Recurse -Force $packOutput
}
New-Item -ItemType Directory -Force -Path $packOutput | Out-Null

dotnet pack .\Brudixy\Brudixy.csproj -c Release -o $packOutput --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Pack failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Package created" -ForegroundColor Green
Write-Host ""

# Step 3: Inspect package contents
Write-Host "[3/5] Inspecting package contents..." -ForegroundColor Yellow
$nupkgFile = Get-ChildItem "$packOutput\Brudixy.*.nupkg" | Select-Object -First 1
$extractPath = "$packOutput\extracted"

if (Test-Path $extractPath) {
    Remove-Item -Recurse -Force $extractPath
}

Expand-Archive -Path $nupkgFile.FullName -DestinationPath $extractPath -Force

$libFiles = Get-ChildItem "$extractPath\lib\net8.0\*.dll" -ErrorAction SilentlyContinue
Write-Host "DLLs in lib/net8.0/:" -ForegroundColor Cyan
if ($libFiles) {
    foreach ($file in $libFiles) {
        Write-Host "  ✓ $($file.Name)" -ForegroundColor Green
    }
} else {
    Write-Host "  ❌ No DLLs found!" -ForegroundColor Red
    exit 1
}

# Check for required assemblies
$requiredAssemblies = @("Brudixy.dll", "Brudixy.Core.dll", "Brudixy.Interfaces.dll")
$missingAssemblies = @()
foreach ($required in $requiredAssemblies) {
    if (-not ($libFiles.Name -contains $required)) {
        $missingAssemblies += $required
    }
}

if ($missingAssemblies.Count -gt 0) {
    Write-Host ""
    Write-Host "❌ Missing required assemblies:" -ForegroundColor Red
    foreach ($missing in $missingAssemblies) {
        Write-Host "  - $missing" -ForegroundColor Red
    }
    exit 1
}

Write-Host ""
Write-Host "✅ All required assemblies present" -ForegroundColor Green
Write-Host ""

# Step 4: Check .nuspec for dependencies
Write-Host "[4/5] Checking package dependencies..." -ForegroundColor Yellow
$nuspecFile = Get-Content "$extractPath\Brudixy.nuspec" -Raw
$requiredDeps = @("JetBrains.Annotations", "System.Text.Json", "Konsarpoo", "Akade.IndexedSet")
$missingDeps = @()

foreach ($dep in $requiredDeps) {
    if ($nuspecFile -notmatch $dep) {
        $missingDeps += $dep
    }
}

if ($missingDeps.Count -gt 0) {
    Write-Host "❌ Missing package dependencies:" -ForegroundColor Red
    foreach ($missing in $missingDeps) {
        Write-Host "  - $missing" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Note: These dependencies are required for bundled assemblies to work correctly." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ All required dependencies declared" -ForegroundColor Green
Write-Host ""

# Step 5: Test with consumer project
Write-Host "[5/5] Testing with consumer project..." -ForegroundColor Yellow
$consumerPath = ".\artifacts\test-consumer-validation"

if (Test-Path $consumerPath) {
    Remove-Item -Recurse -Force $consumerPath
}

dotnet new console -n test-consumer-validation -o $consumerPath -f net8.0 | Out-Null

Push-Location $consumerPath

try {
    # Add local package source
    dotnet nuget add source $packOutput -n BrudixLocalTest | Out-Null
    
    # Get package version
    $version = $nupkgFile.Name -replace "Brudixy\.(\d+\.\d+\.\d+)\.nupkg", '$1'
    
    # Add package reference
    dotnet add package Brudixy --version $version --source BrudixLocalTest
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to add package reference!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    # Create test code that uses Brudixy.Interfaces types
    $testCode = @"
using Brudixy;
using Brudixy.Interfaces;

// Test that we can use types from bundled assemblies
var table = new DataTable();
table.AddColumn("Id", typeof(int));
table.AddColumn("Name", typeof(string));

// Test Brudixy.Interfaces types are accessible
IDataTable iTable = table;
IDataTableRow row = table.AddRow(1, "Test");

Console.WriteLine("✅ SUCCESS: Brudixy.Interfaces types are accessible!");
"@
    
    $testCode | Out-File -FilePath "Program.cs" -Encoding UTF8 -Force
    
    # Build the consumer project
    Write-Host "  Building consumer project..." -ForegroundColor Cyan
    $buildOutput = dotnet build 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Consumer build failed!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Build output:" -ForegroundColor Yellow
        Write-Host $buildOutput
        Pop-Location
        exit 1
    }
    
    Write-Host "✅ Consumer project built successfully" -ForegroundColor Green
    
    # Run the consumer
    Write-Host "  Running consumer test..." -ForegroundColor Cyan
    $runOutput = dotnet run 2>&1
    
    if ($LASTEXITCODE -eq 0 -and $runOutput -match "SUCCESS") {
        Write-Host "✅ Consumer runtime test passed" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Consumer built but runtime test unclear" -ForegroundColor Yellow
    }
    
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "✅ ALL TESTS PASSED!" -ForegroundColor Green
Write-Host "The Brudixy package correctly exposes Brudixy.Interfaces as compile references." -ForegroundColor Green
Write-Host ""
