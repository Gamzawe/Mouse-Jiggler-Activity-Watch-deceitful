# Quick Deployment Script for Logitech Update Service
# This script provides a simplified way to deploy the service
# Run as Administrator

param(
    [switch]$Install,
    [switch]$Uninstall,
    [switch]$CheckOnly,
    [string]$ServiceName = "LogitechUpdateService",
    [string]$DisplayName = "Logitech Update Service (On-Demand)",
    [string]$Description = "Provides UI automation test support and peripheral synchronization.",
    [string]$InstallPath = "C:\Program Files\Logitech\LogiOptions",
    [string]$ExecutablePath = ""
)

# Function to check if running as Administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsPrincipal]::Administrator)
}

# Function to display service status
function Show-ServiceStatus {
    Write-Host "`n=== Service Status ===" -ForegroundColor Cyan
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if ($service) {
        Write-Host "Service Name: $($service.Name)" -ForegroundColor Green
        Write-Host "Display Name: $($service.DisplayName)" -ForegroundColor Green
        Write-Host "Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Yellow' })
        Write-Host "Start Type: $($service.StartType)" -ForegroundColor Green
        
        # Check if executable exists
        $scQuery = sc.exe qc $ServiceName 2>$null
        $binPath = ($scQuery | Select-String "BINARY_PATH_NAME").ToString().Split(':')[1].Trim()
        if ($binPath) {
            $exePath = $binPath.Replace('"', '').Replace(' --service', '')
            if (Test-Path $exePath) {
                Write-Host "Executable: $exePath" -ForegroundColor Green
                Write-Host "Executable exists: Yes" -ForegroundColor Green
            } else {
                Write-Host "Executable: $exePath" -ForegroundColor Red
                Write-Host "Executable exists: No" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Red
    }
    
    # Check registry artifacts
    Write-Host "`n=== Registry Artifacts ===" -ForegroundColor Cyan
    $regPath = "HKLM:\SOFTWARE\Logitech\Options"
    if (Test-Path $regPath) {
        Write-Host "Registry path exists: Yes" -ForegroundColor Green
        Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue | Format-List
    } else {
        Write-Host "Registry path exists: No" -ForegroundColor Yellow
    }
}

# Function to install service
function Install-ServiceSimple {
    Write-Host "`n=== Installing $ServiceName ===" -ForegroundColor Cyan
    
    # Ask for executable path if not provided
    if ([string]::IsNullOrEmpty($ExecutablePath)) {
        Write-Host "`nPlease provide the path to LogiOptions.exe:" -ForegroundColor Yellow
        Write-Host "1. If you have already built the project, navigate to: bin\Release\net10.0-windows\LogiOptions.exe" -ForegroundColor Gray
        Write-Host "2. Or build the project first using: .\Build-Project.ps1 -Build" -ForegroundColor Gray
        Write-Host "3. Or use the pre-built executable if available" -ForegroundColor Gray
        
        $ExecutablePath = Read-Host "`nEnter full path to LogiOptions.exe"
    }
    
    # Validate executable
    if (-not (Test-Path $ExecutablePath)) {
        Write-Host "Error: Executable not found at: $ExecutablePath" -ForegroundColor Red
        Write-Host "Please build the project first or provide a valid path." -ForegroundColor Yellow
        return $false
    }
    
    Write-Host "Using executable: $ExecutablePath" -ForegroundColor Green
    
    # Create installation directory
    if (-not (Test-Path $InstallPath)) {
        Write-Host "Creating installation directory: $InstallPath" -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    }
    
    # Copy executable
    $destinationExe = Join-Path $InstallPath "LogiOptions.exe"
    Write-Host "Copying executable to: $destinationExe" -ForegroundColor Yellow
    Copy-Item $ExecutablePath $destinationExe -Force
    
    # Copy supporting files from current directory
    Write-Host "Copying supporting files..." -ForegroundColor Yellow
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
            Write-Host "  Copied: $file" -ForegroundColor Gray
        } else {
            Write-Host "  Warning: $file not found in current directory" -ForegroundColor Yellow
        }
    }
    
    # Create registry artifacts for deception
    Write-Host "`nCreating registry artifacts..." -ForegroundColor Yellow
    
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
    
    # Install the service
    Write-Host "`nCreating Windows service..." -ForegroundColor Green
    
    # Check if service already exists
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "Service already exists. Stopping and removing..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        sc.exe delete $ServiceName | Out-Null
        Start-Sleep -Seconds 2
    }
    
    # Create new service
    $scResult = sc.exe create $ServiceName binPath= "`"$destinationExe`" --service" DisplayName= "$DisplayName" start= demand obj= "LocalSystem"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service created successfully!" -ForegroundColor Green
        
        # Set service description
        sc.exe description $ServiceName "$Description"
        
        # Configure service recovery
        sc.exe failure $ServiceName reset= 86400 actions= "restart/5000/restart/5000/restart/5000"
        
        Write-Host "`n=== Installation Summary ===" -ForegroundColor Cyan
        Write-Host "Service Name: $ServiceName" -ForegroundColor Green
        Write-Host "Display Name: $DisplayName" -ForegroundColor Green
        Write-Host "Installation Path: $InstallPath" -ForegroundColor Green
        Write-Host "Executable: $destinationExe" -ForegroundColor Green
        Write-Host "`nThe service is installed with 'Demand' start type." -ForegroundColor Yellow
        Write-Host "It will start automatically when triggered by system events." -ForegroundColor Yellow
        
        return $true
    } else {
        Write-Host "Failed to create service. Error code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host "SC output: $scResult" -ForegroundColor Red
        return $false
    }
}

# Function to uninstall service
function Uninstall-ServiceSimple {
    Write-Host "`n=== Uninstalling $ServiceName ===" -ForegroundColor Cyan
    
    # Stop and remove service
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "Stopping service..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
        
        Write-Host "Removing service..." -ForegroundColor Yellow
        sc.exe delete $ServiceName | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Service removed successfully!" -ForegroundColor Green
        } else {
            Write-Host "Failed to remove service." -ForegroundColor Red
        }
    } else {
        Write-Host "Service not found." -ForegroundColor Yellow
    }
    
    # Ask about removing files
    $removeFiles = Read-Host "`nRemove installation directory ($InstallPath)? (y/n)"
    if ($removeFiles -eq 'y') {
        if (Test-Path $InstallPath) {
            Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "Installation directory removed." -ForegroundColor Green
        }
    }
    
    # Ask about removing registry entries
    $removeReg = Read-Host "Remove registry entries? (y/n)"
    if ($removeReg -eq 'y') {
        Remove-Item -Path "HKLM:\SOFTWARE\Logitech\Options" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Registry entries removed." -ForegroundColor Green
    }
}

# Main execution
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Logitech Update Service Quick Deploy Tool" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Check administrator privileges
if (-not (Test-Administrator)) {
    Write-Host "`nERROR: This script requires Administrator privileges!" -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

if ($CheckOnly) {
    Show-ServiceStatus
}
elseif ($Install) {
    $success = Install-ServiceSimple
    if ($success) {
        Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
        Write-Host "1. The service is now installed as '$ServiceName'" -ForegroundColor Green
        Write-Host "2. It will appear in Windows Services as '$DisplayName'" -ForegroundColor Green
        Write-Host "3. For XDR testing, monitor if it's detected as suspicious" -ForegroundColor Yellow
        Write-Host "4. Check employee awareness by seeing who questions it" -ForegroundColor Yellow
        Write-Host "`nTo verify installation, run: .\QuickDeploy-LogitechUpdateService.ps1 -CheckOnly" -ForegroundColor Gray
    }
}
elseif ($Uninstall) {
    Uninstall-ServiceSimple
}
else {
    Write-Host "`nUsage: .\QuickDeploy-LogitechUpdateService.ps1 [options]" -ForegroundColor Yellow
    Write-Host "`nOptions:" -ForegroundColor Cyan
    Write-Host "  -Install      Install the service" -ForegroundColor Gray
    Write-Host "  -Uninstall    Uninstall the service" -ForegroundColor Gray
    Write-Host "  -CheckOnly    Check service status without making changes" -ForegroundColor Gray
    Write-Host "`nExamples:" -ForegroundColor Cyan
    Write-Host "  .\QuickDeploy-LogitechUpdateService.ps1 -Install" -ForegroundColor Gray
    Write-Host "  .\QuickDeploy-LogitechUpdateService.ps1 -CheckOnly" -ForegroundColor Gray
    Write-Host "  .\QuickDeploy-LogitechUpdateService.ps1 -Uninstall" -ForegroundColor Gray
    Write-Host "`nNote: You will be prompted for the executable path during installation." -ForegroundColor Yellow
}

Write-Host "`n==========================================" -ForegroundColor Cyan