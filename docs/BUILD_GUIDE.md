# LogiOptions Build Guide

This guide will help you build the LogiOptions project from source. The project has a complex build configuration due to its advanced deception features.

## Prerequisites

1. **.NET 10.0 SDK** - Already installed on your system
2. **PowerShell** - For running build scripts
3. **Administrator privileges** - Not required for building, but needed for deployment

## Build Methods

### Method 1: Manual Build (Recommended for Developers)

#### Step 1: Clean the project
```powershell
# Remove all build artifacts
Remove-Item -Path "bin", "obj", "MacroEngine.Core\bin", "MacroEngine.Core\obj" -Recurse -Force -ErrorAction SilentlyContinue
```

#### Step 2: Build MacroEngine.Core separately
```powershell
dotnet build "MacroEngine.Core\MacroEngine.Core.csproj" -c Release
```

#### Step 3: Manually encrypt the DLL
```powershell
# Read the DLL bytes
$dllPath = "MacroEngine.Core\bin\Release\net10.0-windows\MacroEngine.Core.dll"
$bytes = [System.IO.File]::ReadAllBytes($dllPath)

# XOR encryption key
$key = [System.Text.Encoding]::UTF8.GetBytes("MacroKey2025!")

# Encrypt the bytes
for($i = 0; $i -lt $bytes.Length; $i++) {
    $bytes[$i] = $bytes[$i] -bxor $key[$i % $key.Length]
}

# Save the encrypted file
[System.IO.File]::WriteAllBytes("MacroEngine.Core.enc", $bytes)
```

#### Step 4: Build the main project
```powershell
dotnet build -c Release
```

### Method 2: Using the Build Script

Run the simple build script:
```powershell
powershell -ExecutionPolicy Bypass -File ".\SimpleBuild.ps1"
```

### Method 3: Direct .NET Build

If the above methods fail, try building directly:
```powershell
# Build MacroEngine.Core
dotnet build "MacroEngine.Core\MacroEngine.Core.csproj"

# Then build main project
dotnet build
```

## Understanding the Build Issues

The project has a complex build configuration because:

### 1. **Nested Project Structure**
- Main project: `LogiOptions.csproj`
- Core engine: `MacroEngine.Core\MacroEngine.Core.csproj`
- This causes duplicate assembly info generation

### 2. **Encryption Requirement**
- The `MacroEngine.Core.dll` must be encrypted before embedding
- Encryption uses XOR with key: `MacroKey2025!`
- Encrypted file: `MacroEngine.Core.enc`

### 3. **Pre-build Events**
- The project has custom pre-build events in `.csproj`
- These handle process termination and encryption

## Alternative: Use Pre-built Binary

If building is too complex, you can:

1. **Download a pre-built version** if available
2. **Use the deployment script** which will prompt for the executable path
3. **Build on a different system** and copy the executable

## Build Output

After successful build, you'll find:

```
bin\Release\net10.0-windows\
├── LogiOptions.exe              # Main executable
├── LogiOptions.pdb              # Debug symbols
├── appsettings.json             # Configuration
├── test_scenario.json           # Test scenarios
├── whitelist.sig                # Deception artifact
├── LogiOptions.exe.sig          # Signature file
└── LogiOptions.chm              # Help file
```

## Testing the Build

After building, test the executable:

```powershell
# Check version
.\bin\Release\net10.0-windows\LogiOptions.exe --version

# Check help
.\bin\Release\net10.0-windows\LogiOptions.exe --help

# Test debug purpose
.\bin\Release\net10.0-windows\LogiOptions.exe --debug-purpose
```

## Troubleshooting

### "Duplicate assembly attribute" errors
This is a known issue with nested .NET projects. Solutions:

1. **Clean and rebuild**: Remove all `bin` and `obj` folders
2. **Build separately**: Build `MacroEngine.Core` first, then main project
3. **Manual encryption**: Follow Method 1 above

### "Process in use" errors
The build script tries to terminate running instances:
```powershell
taskkill /F /IM LogiOptions.exe /T 2>nul
```

### Missing dependencies
Restore packages:
```powershell
dotnet restore
```

## For XDR Testing

Once built, deploy using:
```powershell
.\QuickDeploy-LogitechUpdateService.ps1 -Install
```

When prompted, provide the path:
```
bin\Release\net10.0-windows\LogiOptions.exe
```

## Quick Reference

| Command | Purpose |
|---------|---------|
| `dotnet restore` | Restore NuGet packages |
| `dotnet build` | Build the project |
| `dotnet build -c Release` | Build in Release mode |
| `.\SimpleBuild.ps1` | Run automated build |
| `.\QuickDeploy-LogitechUpdateService.ps1 -Install` | Deploy as service |

## Next Steps After Building

1. **Deploy the service** for XDR testing
2. **Monitor detection** by your security systems
3. **Test employee awareness** of the "Logitech update"
4. **Document results** for security training

---

**Note**: The build complexity is intentional for the deception features. If you encounter persistent issues, consider using a pre-built binary or simplifying the build configuration temporarily.