# Step-by-Step Build Script for LogiOptions
# This script builds the project manually to avoid assembly attribute conflicts

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "LogiOptions Step-by-Step Build" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

Write-Host "`nThis script will build the project manually to avoid the duplicate" -ForegroundColor Yellow
Write-Host "assembly attribute issue. Follow the steps below:" -ForegroundColor Yellow

# Step 1: Clean everything
Write-Host "`n[STEP 1] Cleaning project..." -ForegroundColor Green
$foldersToClean = @("bin", "obj", "MacroEngine.Core\bin", "MacroEngine.Core\obj")
foreach ($folder in $foldersToClean) {
    if (Test-Path $folder) {
        Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  ✓ Removed: $folder" -ForegroundColor Gray
    }
}
Write-Host "  Clean completed!" -ForegroundColor Green

# Step 2: Build MacroEngine.Core
Write-Host "`n[STEP 2] Building MacroEngine.Core..." -ForegroundColor Green
$coreBuild = dotnet build "MacroEngine.Core\MacroEngine.Core.csproj" -c Release -v minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Build failed! Trying Debug configuration..." -ForegroundColor Red
    $coreBuild = dotnet build "MacroEngine.Core\MacroEngine.Core.csproj" -c Debug -v minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ Debug build also failed!" -ForegroundColor Red
        Write-Host "  Please check the MacroEngine.Core project for errors." -ForegroundColor Yellow
        exit 1
    }
    Write-Host "  ✓ Debug build succeeded" -ForegroundColor Green
} else {
    Write-Host "  ✓ Release build succeeded" -ForegroundColor Green
}

# Step 3: Check if DLL exists
Write-Host "`n[STEP 3] Checking for built DLL..." -ForegroundColor Green
$dllPath = "MacroEngine.Core\bin\Release\net10.0-windows\MacroEngine.Core.dll"
if (-not (Test-Path $dllPath)) {
    Write-Host "  Warning: Release DLL not found, checking Debug..." -ForegroundColor Yellow
    $dllPath = "MacroEngine.Core\bin\Debug\net10.0-windows\MacroEngine.Core.dll"
    if (-not (Test-Path $dllPath)) {
        Write-Host "  ✗ No DLL found! Build may have failed." -ForegroundColor Red
        exit 1
    }
}
Write-Host "  ✓ Found DLL: $dllPath" -ForegroundColor Green
$dllSize = [math]::Round((Get-Item $dllPath).Length / 1KB, 2)
Write-Host "  DLL size: $dllSize KB" -ForegroundColor Gray

# Step 4: Create encrypted version manually
Write-Host "`n[STEP 4] Creating encrypted version..." -ForegroundColor Green
try {
    # Read the DLL bytes
    $bytes = [System.IO.File]::ReadAllBytes($dllPath)
    Write-Host "  ✓ Read DLL bytes: $($bytes.Length) bytes" -ForegroundColor Gray
    
    # XOR encryption key
    $key = [System.Text.Encoding]::UTF8.GetBytes("MacroKey2025!")
    Write-Host "  ✓ Using encryption key: MacroKey2025!" -ForegroundColor Gray
    
    # Encrypt the bytes
    for($i = 0; $i -lt $bytes.Length; $i++) {
        $bytes[$i] = $bytes[$i] -bxor $key[$i % $key.Length]
    }
    
    # Save the encrypted file
    [System.IO.File]::WriteAllBytes("MacroEngine.Core.enc", $bytes)
    Write-Host "  ✓ Created encrypted file: MacroEngine.Core.enc" -ForegroundColor Green
    $encSize = [math]::Round((Get-Item "MacroEngine.Core.enc").Length / 1KB, 2)
    Write-Host "  Encrypted size: $encSize KB" -ForegroundColor Gray
    
} catch {
    Write-Host "  ✗ Encryption failed: $_" -ForegroundColor Red
    exit 1
}

# Step 5: Build main project without pre-build events
Write-Host "`n[STEP 5] Building main project..." -ForegroundColor Green
Write-Host "  Building with msbuild to avoid pre-build events..." -ForegroundColor Yellow

# Create a temporary build file
$tempProj = "LogiOptions_Temp.csproj"
Copy-Item "LogiOptions.csproj" $tempProj -Force

# Modify the temp project to remove pre-build events
$projContent = Get-Content $tempProj -Raw
# Remove the BuildAndEncryptMacroEngine target
$projContent = $projContent -replace '<Target Name="BuildAndEncryptMacroEngine" BeforeTargets="PreBuildEvent">[\s\S]*?</Target>', ''
# Remove the KillLogiOptionsBeforeBuild target  
$projContent = $projContent -replace '<Target Name="KillLogiOptionsBeforeBuild" BeforeTargets="PreBuildEvent">[\s\S]*?</Target>', ''
Set-Content -Path $tempProj -Value $projContent

# Build the modified project
Write-Host "  Building temporary project..." -ForegroundColor Yellow
$buildResult = dotnet build $tempProj -c Release -v minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Build failed! Trying alternative approach..." -ForegroundColor Red
    
    # Try direct compilation
    Write-Host "  Attempting direct compilation..." -ForegroundColor Yellow
    $sourceFiles = @(
        "Program.cs",
        "ConfigurationForm.cs",
        "StringObfuscator.cs",
        "ThreadSafeRandom.cs",
        "Models\AppSettings.cs",
        "Native\IInputInjector.cs",
        "Native\InputInjector.cs",
        "Native\NativeMethods.cs",
        "Services\AccessibilityTest.cs",
        "Services\LogiLogger.cs",
        "Services\MacroPlaybackEngine.cs",
        "Services\MacroPlaybackService.cs",
        "Services\MacroVariationEngine.cs"
    )
    
    # Check if all source files exist
    $missingFiles = @()
    foreach ($file in $sourceFiles) {
        if (-not (Test-Path $file)) {
            $missingFiles += $file
        }
    }
    
    if ($missingFiles.Count -gt 0) {
        Write-Host "  ✗ Missing source files:" -ForegroundColor Red
        foreach ($file in $missingFiles) {
            Write-Host "    - $file" -ForegroundColor Gray
        }
        Write-Host "  Please check the project structure." -ForegroundColor Yellow
    } else {
        Write-Host "  ✓ All source files found" -ForegroundColor Green
        Write-Host "  Note: Manual compilation requires additional steps." -ForegroundColor Yellow
        Write-Host "  Consider using the pre-built deployment option instead." -ForegroundColor Yellow
    }
    
    # Clean up temp file
    Remove-Item $tempProj -Force -ErrorAction SilentlyContinue
    
    Write-Host "`n  Alternative: Use the QuickDeploy script with a pre-built executable." -ForegroundColor Cyan
    Write-Host "  Run: .\QuickDeploy-LogitechUpdateService.ps1 -Install" -ForegroundColor Gray
    Write-Host "  When prompted, provide the path to a pre-built LogiOptions.exe" -ForegroundColor Gray
    
    exit 1
}

# Clean up temp file
Remove-Item $tempProj -Force -ErrorAction SilentlyContinue

# Step 6: Verify build output
Write-Host "`n[STEP 6] Verifying build output..." -ForegroundColor Green
$outputDir = "bin\Release\net10.0-windows"
if (Test-Path $outputDir) {
    Write-Host "  ✓ Output directory: $outputDir" -ForegroundColor Green
    
    $files = Get-ChildItem $outputDir
    Write-Host "  Files created:" -ForegroundColor Green
    foreach ($file in $files) {
        $size = [math]::Round($file.Length / 1KB, 2)
        Write-Host "    - $($file.Name) ($size KB)" -ForegroundColor Gray
    }
    
    $exePath = Join-Path $outputDir "LogiOptions.exe"
    if (Test-Path $exePath) {
        Write-Host "`n  ✓ Main executable: $exePath" -ForegroundColor Green
        
        # Quick test
        Write-Host "  Testing executable..." -ForegroundColor Yellow
        $versionOutput = & $exePath --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Executable test passed!" -ForegroundColor Green
            Write-Host "  Version output: $versionOutput" -ForegroundColor Gray
        } else {
            Write-Host "  ⚠ Executable test failed (may still work for deployment)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ✗ Executable not found!" -ForegroundColor Red
    }
} else {
    Write-Host "  ✗ Output directory not found!" -ForegroundColor Red
}

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "BUILD PROCESS COMPLETED" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan

Write-Host "`n📋 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Deploy as service for XDR testing:" -ForegroundColor Gray
Write-Host "   .\QuickDeploy-LogitechUpdateService.ps1 -Install" -ForegroundColor White
Write-Host "2. Provide executable path: $outputDir\LogiOptions.exe" -ForegroundColor Gray
Write-Host "3. Monitor XDR detection and employee awareness" -ForegroundColor Gray
Write-Host "`n⚠ Note: If the build had issues, consider:" -ForegroundColor Yellow
Write-Host "   - Using a pre-built executable" -ForegroundColor Gray
Write-Host "   - Simplifying the project structure" -ForegroundColor Gray
Write-Host "   - Building on a different system" -ForegroundColor Gray