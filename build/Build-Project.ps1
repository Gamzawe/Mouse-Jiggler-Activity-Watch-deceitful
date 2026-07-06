# Build Script for LogiOptions Project
# This script builds the project in Release configuration

param(
    [switch]$Clean,
    [switch]$Build,
    [switch]$Publish,
    [string]$Configuration = "Release"
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "LogiOptions Project Build Tool" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Check if .NET is installed
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 10.0 or later." -ForegroundColor Yellow
    exit 1
}

Write-Host "Using .NET version: $dotnetVersion" -ForegroundColor Green

if ($Clean) {
    Write-Host "`nCleaning project..." -ForegroundColor Yellow
    dotnet clean -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Clean failed." -ForegroundColor Red
        exit 1
    }
    Write-Host "Clean completed." -ForegroundColor Green
}

if ($Build) {
    Write-Host "`nBuilding project..." -ForegroundColor Yellow
    dotnet build -c $Configuration --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`nBuild completed successfully!" -ForegroundColor Green
    
    # Show output location
    $outputDir = "bin\$Configuration\net10.0-windows"
    if (Test-Path $outputDir) {
        Write-Host "`nOutput files:" -ForegroundColor Cyan
        Get-ChildItem $outputDir | ForEach-Object {
            Write-Host "  $($_.Name)" -ForegroundColor Gray
        }
        
        $exePath = Join-Path $outputDir "LogiOptions.exe"
        if (Test-Path $exePath) {
            Write-Host "`nMain executable: $exePath" -ForegroundColor Green
            Write-Host "Size: $([math]::Round((Get-Item $exePath).Length / 1MB, 2)) MB" -ForegroundColor Gray
        }
    }
}

if ($Publish) {
    Write-Host "`nPublishing project..." -ForegroundColor Yellow
    dotnet publish -c $Configuration -r win-x64 --self-contained true
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish failed." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`nPublish completed!" -ForegroundColor Green
    
    $publishDir = "bin\$Configuration\net10.0-windows\win-x64\publish"
    if (Test-Path $publishDir) {
        Write-Host "`nPublished files in: $publishDir" -ForegroundColor Cyan
    }
}

if (-not $Clean -and -not $Build -and -not $Publish) {
    Write-Host "`nUsage: .\Build-Project.ps1 [options]" -ForegroundColor Yellow
    Write-Host "`nOptions:" -ForegroundColor Cyan
    Write-Host "  -Clean      Clean the project" -ForegroundColor Gray
    Write-Host "  -Build      Build the project (Release)" -ForegroundColor Gray
    Write-Host "  -Publish    Publish as self-contained" -ForegroundColor Gray
    Write-Host "`nExamples:" -ForegroundColor Cyan
    Write-Host "  .\Build-Project.ps1 -Build" -ForegroundColor Gray
    Write-Host "  .\Build-Project.ps1 -Clean -Build" -ForegroundColor Gray
    Write-Host "  .\Build-Project.ps1 -Publish" -ForegroundColor Gray
}

Write-Host "`n==========================================" -ForegroundColor Cyan