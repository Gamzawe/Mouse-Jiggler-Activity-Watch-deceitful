# Simple Build Script for LogiOptions Project
# This script handles the build process step-by-step

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "LogiOptions Simple Build Tool" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Step 1: Clean everything
Write-Host "`nStep 1: Cleaning project..." -ForegroundColor Yellow
try {
    # Remove bin and obj folders
    if (Test-Path "bin") {
        Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: bin folder" -ForegroundColor Gray
    }
    
    if (Test-Path "obj") {
        Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: obj folder" -ForegroundColor Gray
    }
    
    if (Test-Path "MacroEngine.Core\bin") {
        Remove-Item -Path "MacroEngine.Core\bin" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: MacroEngine.Core\bin folder" -ForegroundColor Gray
    }
    
    if (Test-Path "MacroEngine.Core\obj") {
        Remove-Item -Path "MacroEngine.Core\obj" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: MacroEngine.Core\obj folder" -ForegroundColor Gray
    }
    
    Write-Host "  Clean completed successfully!" -ForegroundColor Green
} catch {
    Write-Host "  Warning: Some cleanup operations failed, continuing anyway..." -ForegroundColor Yellow
}

# Step 2: Restore dependencies
Write-Host "`nStep 2: Restoring dependencies..." -ForegroundColor Yellow
$restoreResult = dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "  Restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  Restore completed successfully!" -ForegroundColor Green

# Step 3: Build MacroEngine.Core separately
Write-Host "`nStep 3: Building MacroEngine.Core..." -ForegroundColor Yellow
$buildCoreResult = dotnet build "MacroEngine.Core\MacroEngine.Core.csproj" -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "  MacroEngine.Core build failed!" -ForegroundColor Red
    Write-Host "  Trying alternative approach..." -ForegroundColor Yellow
    
    # Try building without specific configuration
    $buildCoreResult = dotnet build "MacroEngine.Core\MacroEngine.Core.csproj"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Alternative build also failed!" -ForegroundColor Red
        exit 1
    }
}
Write-Host "  MacroEngine.Core build completed!" -ForegroundColor Green

# Step 4: Build main project
Write-Host "`nStep 4: Building main LogiOptions project..." -ForegroundColor Yellow
$buildMainResult = dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "  Main build failed!" -ForegroundColor Red
    Write-Host "  Trying alternative approach..." -ForegroundColor Yellow
    
    # Try building without specific configuration
    $buildMainResult = dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Alternative build also failed!" -ForegroundColor Red
        
        # Try a different approach - build directly
        Write-Host "  Trying direct build..." -ForegroundColor Yellow
        dotnet msbuild /p:Configuration=Release
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  Direct build failed!" -ForegroundColor Red
            exit 1
        }
    }
}
Write-Host "  Main build completed!" -ForegroundColor Green

# Step 5: Show output
Write-Host "`nStep 5: Build output summary..." -ForegroundColor Cyan
$outputDir = "bin\Release\net10.0-windows"
if (Test-Path $outputDir) {
    Write-Host "  Output directory: $outputDir" -ForegroundColor Green
    
    $files = Get-ChildItem $outputDir
    Write-Host "  Files created:" -ForegroundColor Green
    foreach ($file in $files) {
        Write-Host "    - $($file.Name)" -ForegroundColor Gray
    }
    
    $exePath = Join-Path $outputDir "LogiOptions.exe"
    if (Test-Path $exePath) {
        $fileSize = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
        Write-Host "`n  Main executable: $exePath" -ForegroundColor Green
        Write-Host "  File size: $fileSize MB" -ForegroundColor Gray
        
        # Check if executable runs
        Write-Host "`n  Testing executable (version check)..." -ForegroundColor Yellow
        $versionOutput = & $exePath --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Executable test passed!" -ForegroundColor Green
            Write-Host "  Version: $versionOutput" -ForegroundColor Gray
        } else {
            Write-Host "  Warning: Executable version check failed" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "  Warning: Output directory not found!" -ForegroundColor Yellow
}

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "Build process completed!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan

Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Deploy the service: .\QuickDeploy-LogitechUpdateService.ps1 -Install" -ForegroundColor Gray
Write-Host "2. Provide executable path: $outputDir\LogiOptions.exe" -ForegroundColor Gray
Write-Host "3. Check service status: .\QuickDeploy-LogitechUpdateService.ps1 -CheckOnly" -ForegroundColor Gray