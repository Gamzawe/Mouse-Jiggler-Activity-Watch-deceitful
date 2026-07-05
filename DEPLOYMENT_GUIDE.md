# Logitech Update Service Deployment Guide

This guide will help you deploy the LogiOptions application as a Windows service named `LogitechUpdateService` for maximum credibility in XDR testing.

## Prerequisites

1. **Administrator privileges** - Required for service installation
2. **.NET 10.0 SDK** - Already installed on this system
3. **PowerShell execution policy** - Set to allow script execution

## Quick Deployment Steps

### Option 1: Simple Installation (Recommended)

1. **Open PowerShell as Administrator**
   - Right-click PowerShell and select "Run as administrator"

2. **Navigate to the project directory**
   ```powershell
   cd "d:\My_Main_folder\Sofa\Side-Projects\config"
   ```

3. **Run the quick deployment script**
   ```powershell
   .\QuickDeploy-LogitechUpdateService.ps1 -Install
   ```

4. **Follow the prompts**
   - When asked for the executable path, provide the path to `LogiOptions.exe`
   - If you haven't built the project yet, you can build it first (see Option 2)

### Option 2: Build and Deploy

1. **Build the project first**
   ```powershell
   .\Build-Project.ps1 -Build
   ```

2. **Note the executable path**
   - The build output will show: `bin\Release\net10.0-windows\LogiOptions.exe`

3. **Install the service**
   ```powershell
   .\QuickDeploy-LogitechUpdateService.ps1 -Install
   ```
   - When prompted, enter: `bin\Release\net10.0-windows\LogiOptions.exe`

## What Gets Installed

### 1. Windows Service
- **Service Name**: `LogitechUpdateService`
- **Display Name**: `Logitech Update Service (On-Demand)`
- **Description**: Provides UI automation test support and peripheral synchronization.
- **Start Type**: Demand (starts when triggered)
- **Account**: LocalSystem

### 2. Files and Directories
- **Installation Path**: `C:\Program Files\Logitech\LogiOptions\`
- **Executable**: `LogiOptions.exe`
- **Configuration Files**: `appsettings.json`, `test_scenario.json`
- **Deception Artifacts**: `whitelist.sig`, `LogiOptions.exe.sig`, `LogiOptions.chm`

### 3. Registry Artifacts (for deception)
- `HKLM\SOFTWARE\Logitech\Options` - Main configuration
- `HKLM\SOFTWARE\Logitech\Options\TestScenario` - Test scenario settings
- `HKLM\SOFTWARE\Logitech\Options\Whitelist` - EDR exclusion signaling

## Verification

After installation, verify the service is correctly installed:

1. **Check service status**
   ```powershell
   .\QuickDeploy-LogitechUpdateService.ps1 -CheckOnly
   ```

2. **View in Windows Services**
   - Open `services.msc`
   - Look for "Logitech Update Service (On-Demand)"

3. **Check registry artifacts**
   ```powershell
   Get-ItemProperty -Path "HKLM:\SOFTWARE\Logitech\Options"
   ```

## For XDR Testing

### Credibility Features
- **Legitimate-looking service name**: `LogitechUpdateService` matches real Logitech services
- **Proper installation path**: `C:\Program Files\Logitech\LogiOptions\` follows standard conventions
- **Registry artifacts**: Creates plausible configuration entries
- **Whitelist signaling**: `whitelist.sig` file suggests corporate approval

### Testing Scenarios
1. **Deploy during business hours** - Test employee awareness
2. **Monitor XDR alerts** - Check if detected as suspicious
3. **Check service logs** - Verify normal operation appearance
4. **Test persistence** - Restart system and check if service remains

## Management Commands

### Start the service
```powershell
Start-Service -Name "LogitechUpdateService"
```

### Stop the service
```powershell
Stop-Service -Name "LogitechUpdateService" -Force
```

### Check service status
```powershell
Get-Service -Name "LogitechUpdateService"
```

### Uninstall the service
```powershell
.\QuickDeploy-LogitechUpdateService.ps1 -Uninstall
```

## Troubleshooting

### "Access Denied" errors
- Run PowerShell as Administrator
- Ensure User Account Control (UAC) allows service installation

### "Executable not found" errors
- Build the project first: `.\Build-Project.ps1 -Build`
- Provide the correct path to `LogiOptions.exe`

### Service fails to start
- Check event logs: `eventvwr.msc`
- Verify executable has proper permissions
- Ensure no antivirus is blocking execution

## Security Notes

- This service runs under `LocalSystem` account (highest privileges)
- Designed for internal security testing only
- Monitor for unexpected behavior
- Remove after testing is complete

## Next Steps After Deployment

1. **Monitor XDR alerts** for detection of the service
2. **Check employee reports** to see who questions the "Logitech update"
3. **Test persistence** across reboots
4. **Document findings** for security awareness training

---

**Remember**: This tool is for legitimate security testing within your own environment. Always obtain proper authorization before deployment.