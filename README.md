# Mouse Jigglers - Activity Watch (Deceitful)

> **Intended for XDR and Cybersecurity team Testing** — This project is a synthetic activity generator designed for testing and validating Extended Detection and Response (XDR) solutions, endpoint detection capabilities, and security monitoring pipelines in controlled lab environments.

A Windows desktop application that simulates mouse and keyboard activity to prevent system sleep/lock. Designed for **forensic indistinguishability**, mimicking legitimate Logitech software.

## Overview

This tool executes automated UI test scenarios for validating application behavior under various conditions. It simulates peripheral input (mouse movements, keyboard activity) to keep workstations awake, appearing as standard Logitech Options software.

### Use Cases

- **XDR Validation**: Test whether endpoint detection systems correctly identify synthetic user activity
- **EDR Testing**: Validate endpoint detection and response capabilities against benign-looking but automated activity
- **SOC Exercises**: Incorporate into purple team/red team exercises to test security operations center responses
- **Forensic Analysis**: Study how security tools respond to peripheral emulation patterns

### Key Features

- **Macro Playback Engine**: Handles complex input sequences with natural timing variance
- **Peripheral Synchronization**: Monitors device state for DPI, RGB profiles, and button mappings
- **Accessibility Calibration**: Input latency verification via CLI flags
- **Robust Logging**: Daily-rotated logs in `C:\ProgramData\Logitech\LogiOptions\Logs\`
- **Anti-Analysis Techniques**: Service ancestry verification, anti-debugging, ETW logging, crash artifact generation

## Technical Details

| Property | Value |
|----------|-------|
| Service Name | `LogiOptionsSvc` |
| Display Name | `Logitech Options Background Service` |
| Executable | `LogiOptions.exe` |
| Product Version | `10.5.2` |
| Company | `Logitech` |
| Installation Path | `C:\Program Files\Logitech\LogiOptions\` |
| Application Data | `C:\ProgramData\Logitech\LogiOptions\` |

## Command-Line Interface

```
LogiOptions.exe [OPTIONS]
```

| Flag | Description |
|------|-------------|
| `--version` | Display product version and build metadata |
| `--help` | Display comprehensive CLI help menu |
| `--configure` | Open configuration synchronization module |
| `--checkupdate` | Trigger connectivity check to `update.logitech.com` |
| `--calibrate` | Perform input latency calibration |
| `--accessibility-test` | One-shot input calibration into target window |

## Building

The project uses MSBuild and includes PowerShell build scripts:

- `build/Build-Project.ps1` - Main build script
- `build/Build-StepByStep.ps1` - Step-by-step build instructions
- `build/SimpleBuild.ps1` - Simplified build process
- `build/CompleteBuildAndDeploy.ps1` - Full build and deployment

### Prerequisites

- .NET SDK (Framework or .NET 6+)
- Windows OS
- MSBuild or Visual Studio

### Build Commands

```powershell
# Simple build
.\build\SimpleBuild.ps1

# Or use dotnet CLI
dotnet build src\LogiOptions\LogiOptions.csproj -c Release
```

### Publishing

```powershell
# Publish as self-contained single-file executable (recommended)
dotnet publish src\LogiOptions\LogiOptions.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish
```

Output: `publish/LogiOptions.exe`

## Project Structure

```
LogiOptions/
├── src/LogiOptions/            # Main application
│   ├── App/                    # Entry point & UI
│   ├── Core/                   # Core services, models, input handling
│   ├── TestEngine/            # Embedded test execution engine
│   ├── Resources/             # Assets and resources
│   └── appsettings.json       # Configuration
├── tests/LogiOptions.Tests/   # Unit tests
├── build/                     # Build scripts
├── docs/                      # Documentation
├── LogiOptions.slnx           # Solution file
└── README.md
```

## Security Design

> **For Educational and Testing Purposes Only**

- **User-Mode Execution**: No kernel-mode components
- **Service Ancestry Guard**: Only executes when spawned by Windows Service Control Manager (`services.exe`)
- **DPAPI Encryption**: Sensitive configuration protected via Windows Data Protection API
- **Branded Metadata**: Assembly info mimics legitimate Logitech software
- **Network Deception**: Simulated telemetry to legitimate-looking endpoints

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - System architecture and components
- [docs/OPERATIONS.md](docs/OPERATIONS.md) - Compilation, deployment, and CLI reference
- [docs/COMPLETE_PROJECT_GUIDE.md](docs/COMPLETE_PROJECT_GUIDE.md) - Comprehensive project guide
- [docs/DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md) - Deployment instructions
- [docs/BUILD_GUIDE.md](docs/BUILD_GUIDE.md) - Build process documentation

## Language Distribution

- C# - 57.3%
- PowerShell - 42.2%

## License

See repository for license details.

---

**Disclaimer**: This tool is designed exclusively for authorized security testing, research, and validation purposes. Users are responsible for ensuring compliance with applicable laws and organizational policies. The maintainers assume no liability for misuse.