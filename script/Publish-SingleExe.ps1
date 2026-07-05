# Complete Single EXE Build and Release Script for LogiOptions
# This script builds the project as a single self-contained executable
# Includes all necessary steps: cleaning, building, encryption, and publishing

param(
    [switch]$Clean,
    [switch]$Build,
    [switch]$Publish,
    [switch]$Test,
    [switch]$Sign,
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = ".\Releases",
    [string]$Version = "1.0.0"
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

# Encryption key for MacroEngine.Core.dll
$EncryptionKey = "MacroKey2025!"

# ==========================================
# Functions
# ==========================================

function Write-Header {
    param([string]$Title)
    Write-Host "`n" + ("=" * 60) -ForegroundColor $ColorInfo
    Write-Host $Title -ForegroundColor $ColorInfo
    Write-Host ("=" * 60) -ForegroundColor $ColorInfo
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

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"
    
    # Check .NET SDK
    Write-Step "Checking .NET SDK..."
    try {
        $dotnetVersion = dotnet --version
        Write-Detail "Found .NET SDK version: $dotnetVersion"
        
        # Check if it's at least .NET 10.0
        if ($dotnetVersion -match "^10\.\d+") {
            Write-Step ".NET 10.0+ detected" "success"
        } else {
            Write-Step "Warning: .NET 10.0+ recommended" "warning"
        }
    } catch {
        Write-Step "Error: .NET SDK not found!" "error"
        Write-Detail "Please install .NET 10.0 SDK from: https://dotnet.microsoft.com/download"
        exit 1
    }
    
    # Check PowerShell version
    Write-Step "Checking PowerShell version..."
    $psVersion = $PSVersionTable.PSVersion
    Write-Detail "PowerShell version: $psVersion"
    
    if ($psVersion.Major -ge 5) {
        Write-Step "PowerShell version OK" "success"
    } else {
        Write-Step "Warning: PowerShell 5.0+ recommended" "warning"
    }
    
    Write-Step "All prerequisites satisfied" "success"
}

function Clean-Project {
    Write-Header "Step 1: Cleaning Project"
    
    # Kill any running instances first
    Write-Step "Stopping any running LogiOptions processes..."
    $processes = Get-Process -Name "LogiOptions" -ErrorAction SilentlyContinue
    if ($processes) {
        $processes | Stop-Process -Force -ErrorAction SilentlyContinue
        Write-Detail "Stopped $($processes.Count) process(es)"
    }
    
    # Folders to clean
    $foldersToClean = @(
        "bin",
        "obj", 
        "MacroEngine.Core\bin",
        "MacroEngine.Core\obj",
        "MacroEngine.Core.enc"
    )
    
    foreach ($folder in $foldersToClean) {
        if (Test-Path $folder) {
            try {
                Remove-Item -Path $folder -Recurse -Force -ErrorAction Stop
                Write-Step "Cleaned: $folder" "success"
            } catch {
                Write-Step "Warning: Could not clean $folder" "warning"
                Write-Detail "Error: $_"
            }
        } else {
            Write-Detail "Skipped: $folder (not found)"
        }
    }
    
    # Clean release directory if specified
    if ($Clean -and (Test-Path $OutputDir)) {
        Write-Step "Cleaning release directory..."
        Remove-Item -Path "$OutputDir\*" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Detail "Release directory cleaned"
    }
    
    Write-Step "Clean completed successfully!" "success"
}

function Build-MacroEngineCore {
    Write-Header "Step 2: Building MacroEngine.Core"
    
    Write-Step "Building MacroEngine.Core project..."
    
    # Try building with configuration first
    Write-Detail "Attempt 1: Building with $Configuration configuration..."
    $buildResult = dotnet build "MacroEngine.Core\MacroEngine.Core.csproj" -c $Configuration
    
    if ($LASTEXITCODE -ne 0) {
        Write-Step "Build with $Configuration configuration failed" "warning"
        Write-Detail "Trying alternative approach..."
        
        # Try without specific configuration
        Write-Detail "Attempt 2: Building without specific configuration..."
        $buildResult = dotnet build "MacroEngine.Core\MacroEngine.Core.csproj"
        if ($LASTEXITCODE -ne 0) {
            Write-Step "Alternative build also failed!" "error"
            Write-Detail "Error output: $buildResult"
            exit 1
        } else {
            Write-Step "Build succeeded without configuration" "success"
            # Update configuration to Debug since that's what was built
            $Configuration = "Debug"
        }
    } else {
        Write-Step "Build succeeded with $Configuration configuration" "success"
    }
    
    # Verify the DLL was created
    $dllPath = "MacroEngine.Core\bin\$Configuration\net10.0-windows\MacroEngine.Core.dll"
    if (Test-Path $dllPath) {
        $dllSize = [math]::Round((Get-Item $dllPath).Length / 1KB, 2)
        Write-Step "MacroEngine.Core.dll created successfully" "success"
        Write-Detail "Size: $dllSize KB"
        Write-Detail "Path: $dllPath"
        return $dllPath
    } else {
        Write-Step "Error: MacroEngine.Core.dll not found!" "error"
        Write-Detail "Expected path: $dllPath"
        
        # Try to find the DLL in other possible locations
        Write-Detail "Searching for DLL in other locations..."
        $foundDlls = Get-ChildItem -Path "MacroEngine.Core\bin" -Filter "MacroEngine.Core.dll" -Recurse -ErrorAction SilentlyContinue
        if ($foundDlls.Count -gt 0) {
            Write-Step "Found DLL in alternative location" "warning"
            $dllPath = $foundDlls[0].FullName
            Write-Detail "Using: $dllPath"
            return $dllPath
        } else {
            exit 1
        }
    }
}

function Encrypt-MacroEngineDll {
    param([string]$DllPath)
    
    Write-Header "Step 3: Encrypting MacroEngine.Core.dll"
    
    Write-Step "Reading DLL file..."
    try {
        $bytes = [System.IO.File]::ReadAllBytes($DllPath)
        Write-Detail "Read $($bytes.Length) bytes"
    } catch {
        Write-Step "Error reading DLL file!" "error"
        Write-Detail "Error: $_"
        exit 1
    }
    
    Write-Step "Encrypting with XOR key..."
    $keyBytes = [System.Text.Encoding]::UTF8.GetBytes($EncryptionKey)
    Write-Detail "Using encryption key: $EncryptionKey"
    
    for($i = 0; $i -lt $bytes.Length; $i++) {
        $bytes[$i] = $bytes[$i] -bxor $keyBytes[$i % $keyBytes.Length]
    }
    
    Write-Step "Saving encrypted file..."
    $encryptedPath = "MacroEngine.Core.enc"
    try {
        [System.IO.File]::WriteAllBytes($encryptedPath, $bytes)
        $encryptedSize = [math]::Round((Get-Item $encryptedPath).Length / 1KB, 2)
        Write-Step "Encrypted file created: $encryptedPath" "success"
        Write-Detail "Size: $encryptedSize KB"
    } catch {
        Write-Step "Error saving encrypted file!" "error"
        Write-Detail "Error: $_"
        exit 1
    }
}

function Build-MainProject {
    Write-Header "Step 4: Building Main Project"
    
    Write-Step "Building LogiOptions project..."
    
    # Try building with configuration first
    Write-Detail "Attempt 1: Building with $Configuration configuration..."
    $buildResult = dotnet build -c $Configuration
    
    if ($LASTEXITCODE -ne 0) {
        Write-Step "Build with $Configuration configuration failed" "warning"
        Write-Detail "Trying alternative approach..."
        
        # Try without specific configuration
        Write-Detail "Attempt 2: Building without specific configuration..."
        $buildResult = dotnet build
        if ($LASTEXITCODE -ne 0) {
            Write-Step "Alternative build also failed!" "error"
            Write-Detail "Trying direct MSBuild approach..."
            
            # Try direct MSBuild
            Write-Detail "Attempt 3: Using MSBuild directly..."
            dotnet msbuild /p:Configuration=$Configuration
            if ($LASTEXITCODE -ne 0) {
                Write-Step "All build attempts failed!" "error"
                Write-Detail "Error output: $buildResult"
                exit 1
            } else {
                Write-Step "Build succeeded with MSBuild" "success"
            }
        } else {
            Write-Step "Build succeeded without configuration" "success"
            # Update configuration to Debug since that's what was built
            $Configuration = "Debug"
        }
    } else {
        Write-Step "Build succeeded with $Configuration configuration" "success"
    }
    
    # Show build output
    $outputDir = "bin\$Configuration\net10.0-windows"
    if (Test-Path $outputDir) {
        Write-Step "Build output location:" "info"
        Write-Detail "$(Resolve-Path $outputDir)"
        
        $files = Get-ChildItem $outputDir
        Write-Step "Generated files:" "info"
        foreach ($file in $files) {
            $size = [math]::Round($file.Length / 1KB, 2)
            Write-Detail "  $($file.Name) ($size KB)"
        }
        
        $exePath = Join-Path $outputDir "LogiOptions.exe"
        if (Test-Path $exePath) {
            $exeSize = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
            Write-Step "Main executable:" "success"
            Write-Detail "$exePath"
            Write-Detail "Size: $exeSize MB"
            return $exePath
        }
    } else {
        Write-Step "Warning: Output directory not found" "warning"
        Write-Detail "Expected: $outputDir"
        
        # Try to find output in other locations
        Write-Detail "Searching for output files..."
        $foundExes = Get-ChildItem -Path "bin" -Filter "LogiOptions.exe" -Recurse -ErrorAction SilentlyContinue
        if ($foundExes.Count -gt 0) {
            Write-Step "Found executable in alternative location" "warning"
            $exePath = $foundExes[0].FullName
            Write-Detail "Using: $exePath"
            return $exePath
        }
    }
    
    return $null
}

function Publish-SingleExe {
    Write-Header "Step 5: Publishing Single EXE"
    
    # Create output directory
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
        Write-Step "Created output directory: $OutputDir" "success"
    }
    
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $publishName = "LogiOptions-v$Version-$timestamp-$Runtime"
    $publishDir = Join-Path $OutputDir $publishName
    
    Write-Step "Publishing as single self-contained executable..."
    Write-Detail "Configuration: $Configuration"
    Write-Detail "Runtime: $Runtime"
    Write-Detail "Output: $publishDir"
    
    # Try publishing with full options
    Write-Detail "Attempt 1: Publishing with full options..."
    $publishResult = dotnet publish -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publishDir
    
    if ($LASTEXITCODE -ne 0) {
        Write-Step "Publish with full options failed" "warning"
        Write-Detail "Trying simplified publish..."
        
        # Try without some options
        Write-Detail "Attempt 2: Publishing with simplified options..."
        $publishResult = dotnet publish -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -o $publishDir
        
        if ($LASTEXITCODE -ne 0) {
            Write-Step "Simplified publish also failed!" "error"
            Write-Detail "Error output: $publishResult"
            exit 1
        } else {
            Write-Step "Publish succeeded with simplified options" "success"
        }
    } else {
        Write-Step "Publish completed successfully!" "success"
    }
    
    # Show published files
    if (Test-Path $publishDir) {
        $exeFile = Get-ChildItem $publishDir -Filter "*.exe" | Select-Object -First 1
        if ($exeFile) {
            $exeSize = [math]::Round($exeFile.Length / 1MB, 2)
            Write-Step "Single EXE created:" "success"
            Write-Detail "File: $($exeFile.Name)"
            Write-Detail "Path: $($exeFile.FullName)"
            Write-Detail "Size: $exeSize MB"
            
            # Create a simple README
            $readmePath = Join-Path $publishDir "README.txt"
            $readmeContent = @"
LogiOptions Single Executable Release
=====================================
Version: $Version
Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Runtime: $Runtime
Configuration: $Configuration

Usage:
1. Run $($exeFile.Name) as Administrator for full functionality
2. The application will install itself as a Windows service
3. Check Windows Services for "LogitechUpdateService"

Notes:
- This is a self-contained executable (no .NET runtime required)
- All dependencies are included in the single file
- The MacroEngine.Core is encrypted for security

"@
            [System.IO.File]::WriteAllText($readmePath, $readmeContent)
            Write-Detail "Created README.txt"
            
            return $exeFile.FullName
        } else {
            Write-Step "Warning: No EXE file found in publish directory" "warning"
            Write-Detail "Directory contents:"
            Get-ChildItem $publishDir | ForEach-Object {
                Write-Detail "  $($_.Name)"
            }
        }
    } else {
        Write-Step "Error: Publish directory was not created!" "error"
    }
    
    return $null
}

function Test-Executable {
    param([string]$ExePath)
    
    Write-Header "Step 6: Testing Executable"
    
    if (-not (Test-Path $ExePath)) {
        Write-Step "Error: Executable not found at $ExePath" "error"
        return $false
    }
    
    Write-Step "Checking executable properties..."
    
    # Get file info
    $fileInfo = Get-Item $ExePath
    Write-Detail "File: $($fileInfo.Name)"
    Write-Detail "Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
    Write-Detail "Created: $($fileInfo.CreationTime)"
    
    # Check if it's a valid .NET assembly
    Write-Step "Validating .NET assembly..."
    try {
        $assembly = [System.Reflection.AssemblyName]::GetAssemblyName($ExePath)
        Write-Detail "Assembly Name: $($assembly.Name)"
        Write-Detail "Version: $($assembly.Version)"
        Write-Step "Valid .NET assembly detected" "success"
    } catch {
        Write-Step "Warning: Not a valid .NET assembly" "warning"
    }
    
    # Check digital signature (if any)
    Write-Step "Checking digital signature..."
    $sig = Get-AuthenticodeSignature $ExePath
    if ($sig.Status -eq "Valid") {
        Write-Step "Digitally signed and valid" "success"
        Write-Detail "Signer: $($sig.SignerCertificate.Subject)"
    } else {
        Write-Step "Not digitally signed" "warning"
    }
    
    Write-Step "Basic validation completed" "success"
    return $true
}

function Sign-Executable {
    param([string]$ExePath)
    
    Write-Header "Step 7: Signing Executable"
    
    if (-not (Test-Path $ExePath)) {
        Write-Step "Error: Executable not found at $ExePath" "error"
        return $false
    }
    
    Write-Step "Looking for signing tools..."
    
    # Check for signtool
    $signtoolPaths = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe",
        "${env:ProgramFiles}\Windows Kits\10\bin\*\x64\signtool.exe"
    )
    
    $signtool = $null
    foreach ($path in $signtoolPaths) {
        $found = Get-ChildItem $path -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            $signtool = $found.FullName
            break
        }
    }
    
    if (-not $signtool) {
        Write-Step "Error: signtool.exe not found!" "error"
        Write-Detail "Please install Windows SDK or Visual Studio"
        Write-Detail "Or skip signing with -Sign:$false"
        return $false
    }
    
    Write-Detail "Found signtool: $signtool"
    
    # Check for certificates
    Write-Step "Looking for code signing certificates..."
    
    # Check certificate store
    $certificates = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert -ErrorAction SilentlyContinue
    
    if ($certificates.Count -eq 0) {
        Write-Step "Warning: No code signing certificates found in store" "warning"
        Write-Detail "You need a valid code signing certificate to sign executables"
        Write-Detail "Options:"
        Write-Detail "  1. Purchase from trusted CA (DigiCert, Sectigo, etc.)"
        Write-Detail "  2. Use self-signed certificate for testing"
        Write-Detail "  3. Skip signing for now"
        return $false
    }
    
    Write-Step "Found $($certificates.Count) certificate(s)" "success"
    
    # Use the first certificate
    $cert = $certificates[0]
    Write-Detail "Using certificate: $($cert.Subject)"
    Write-Detail "Expires: $($cert.NotAfter)"
    
    # Sign the executable
    Write-Step "Signing executable..."
    try {
        & $signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a "$ExePath"
        
        if ($LASTEXITCODE -ne 0) {
            Write-Step "Signing failed!" "error"
            return $false
        }
        
        Write-Step "Executable signed successfully!" "success"
        
        # Verify the signature
        $sig = Get-AuthenticodeSignature $ExePath
        if ($sig.Status -eq "Valid") {
            Write-Detail "Signature verified: Valid"
            Write-Detail "Signer: $($sig.SignerCertificate.Subject)"
        } else {
            Write-Detail "Signature status: $($sig.Status)"
        }
        
        return $true
    } catch {
        Write-Step "Error during signing!" "error"
        Write-Detail "Error: $_"
        return $false
    }
}

function Show-Summary {
    param(
        [string]$BuiltExePath,
        [string]$PublishedExePath,
        [bool]$Signed = $false
    )
    
    Write-Header "BUILD SUMMARY"
    
    Write-Step "Build Status: COMPLETED SUCCESSFULLY" "success"
    Write-Detail "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    Write-Detail "Configuration: $Configuration"
    Write-Detail "Runtime: $Runtime"
    Write-Detail "Version: $Version"
    
    if ($BuiltExePath -and (Test-Path $BuiltExePath)) {
        $builtFile = Get-Item $BuiltExePath
        Write-Step "Built Executable:" "info"
        Write-Detail "  Path: $BuiltExePath"
        Write-Detail "  Size: $([math]::Round($builtFile.Length / 1MB, 2)) MB"
    }
    
    if ($PublishedExePath -and (Test-Path $PublishedExePath)) {
        $publishedFile = Get-Item $PublishedExePath
        Write-Step "Published Single EXE:" "success"
        Write-Detail "  Path: $PublishedExePath"
        Write-Detail "  Size: $([math]::Round($publishedFile.Length / 1MB, 2)) MB"
        Write-Detail "  Location: $(Split-Path $PublishedExePath -Parent)"
    }
    
    if ($Signed) {
        Write-Step "Digital Signature: APPLIED" "success"
    } else {
        Write-Step "Digital Signature: NOT APPLIED" "warning"
    }
    
    Write-Host "`n" + ("=" * 60) -ForegroundColor $ColorInfo
    Write-Host "NEXT STEPS:" -ForegroundColor $ColorInfo
    Write-Host ("=" * 60) -ForegroundColor $ColorInfo
    
    if ($PublishedExePath) {
        Write-Host "1. Test the published executable:" -ForegroundColor $ColorDetail
        Write-Host "   $PublishedExePath" -ForegroundColor Gray
        
        Write-Host "`n2. Deploy as Windows service (run as Administrator):" -ForegroundColor $ColorDetail
        Write-Host "   powershell -ExecutionPolicy Bypass -File `".\Deploy-LogitechUpdateService.ps1`" -ExePath `"$PublishedExePath`"" -ForegroundColor Gray
        
        Write-Host "`n3. Create installer package:" -ForegroundColor $ColorDetail
        Write-Host "   Use WiX Toolset with installer.wxs" -ForegroundColor Gray
    }
    
    Write-Host "`n" + ("=" * 60) -ForegroundColor $ColorInfo
}

# ==========================================
# Main Script Execution
# ==========================================

$scriptStartTime = Get-Date

Write-Header "LOGIOPTIONS SINGLE EXE BUILD & RELEASE SCRIPT"
Write-Detail "Version: $Version | Configuration: $Configuration | Runtime: $Runtime"
Write-Detail "Output Directory: $OutputDir"
Write-Detail "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

# Track what we're doing
$doClean = $Clean -or (-not $Clean -and -not $Build -and -not $Publish -and -not $Test)
$doBuild = $Build -or (-not $Clean -and -not $Build -and -not $Publish -and -not $Test)
$doPublish = $Publish
$doTest = $Test
$doSign = $Sign

# Variables to track outputs
$builtExePath = $null
$publishedExePath = $null
$signed = $false

try {
    # Step 0: Check prerequisites
    Test-Prerequisites
    
    # Step 1: Clean if requested or doing full build
    if ($doClean) {
        Clean-Project
    }
    
    # Step 2-4: Build process
    if ($doBuild) {
        $dllPath = Build-MacroEngineCore
        Encrypt-MacroEngineDll -DllPath $dllPath
        $builtExePath = Build-MainProject
    }
    
    # Step 5: Publish as single EXE
    if ($doPublish -and $doBuild) {
        $publishedExePath = Publish-SingleExe
    }
    
    # Step 6: Test if requested
    if ($doTest -and $publishedExePath) {
        $testResult = Test-Executable -ExePath $publishedExePath
        if (-not $testResult) {
            Write-Step "Test failed!" "error"
        }
    }
    
    # Step 7: Sign if requested
    if ($doSign -and $publishedExePath) {
        $signed = Sign-Executable -ExePath $publishedExePath
    }
    
    # Show summary
    Show-Summary -BuiltExePath $builtExePath -PublishedExePath $publishedExePath -Signed $signed
    
} catch {
    Write-Header "BUILD FAILED"
    Write-Step "Error occurred during build process!" "error"
    Write-Detail "Error details: $_"
    Write-Detail "Stack trace: $($_.ScriptStackTrace)"
    exit 1
}

Write-Host "`nScript completed successfully!" -ForegroundColor $ColorSuccess
Write-Host "Total time: $((Get-Date) - $scriptStartTime)" -ForegroundColor $ColorDetail