# Mouse Jigglers - Activity Watch (Deceitful)

[![Release](https://img.shields.io/github/release/Gamzawe/Mouse-Jigglers-Activity-Watch-deceitful.svg)](https://github.com/Gamzawe/Mouse-Jigglers-Activity-Watch-deceitful/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Gamzawe/Mouse-Jigglers-Activity-Watch-deceitful/total.svg)](https://github.com/Gamzawe/Mouse-Jigglers-Activity-Watch-deceitful/releases)

> **Intended for XDR and cybersecurity testing** in controlled lab environments.

This project is a Windows application that generates synthetic mouse and keyboard activity. It is built for security teams who want to validate how activity monitoring, XDR, EDR, and purple-team workflows respond to simulated user behavior.

## What This Project Is

`LogiOptions` is a Windows-only .NET application that:

- simulates mouse movement, scrolling, and key presses
- uses randomized timing so activity does not look perfectly scripted
- is packaged and branded to resemble a legitimate peripheral utility
- helps defenders study detection quality in realistic test scenarios

## Who It Is For

- XDR and EDR engineers validating detections
- purple teams running controlled adversary simulation
- SOC teams reviewing alerts and telemetry quality
- researchers analyzing user-activity emulation behavior

## How It Works

At a high level, the project combines three ideas:

1. **Input simulation**
   It sends mouse and keyboard events through standard Windows user-mode APIs.

2. **Behavior variation**
   It adds timing jitter and mixes different action types so the activity looks less repetitive.

3. **Legitimate-looking presentation**
   It uses Logitech-like naming, metadata, and packaging so security teams can test whether tooling relies too heavily on superficial trust signals.

## Why Security Teams Use It

This project is useful when you want to answer questions like:

- Does the XDR detect synthetic user activity?
- Does the SOC notice suspicious but low-noise automation?
- Are detections based only on filenames, vendor strings, or process names?
- How much telemetry is needed to separate real behavior from simulated behavior?

## Main Features

- **Synthetic activity generation**: mouse movement, mouse scroll, and key press simulation
- **Randomized timing**: reduces repetitive patterns during testing
- **Windows service-style presentation**: helps evaluate trust assumptions in tooling
- **Configuration support**: reads settings from `appsettings.json`
- **Logging**: writes operational logs for review and troubleshooting
- **Test-focused architecture**: includes a separate embedded test engine and unit tests

## Safety And Scope

- This project is for **authorized testing, research, and validation only**
- It is intended for **controlled labs, training, and internal security exercises**
- It should not be used outside approved environments
- Users are responsible for policy and legal compliance

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

## Quick Start

```powershell
# Build
dotnet build src\LogiOptions\LogiOptions.csproj -c Release

# Publish a single-file exe
dotnet publish src\LogiOptions\LogiOptions.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish

# Output
publish\LogiOptions.exe
```

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

## Design Notes

- **User-mode only**: no kernel driver is required
- **Windows-focused**: built for `net10.0-windows` and WinForms
- **Config-based**: runtime behavior is driven by `appsettings.json`
- **Service-themed**: naming and packaging intentionally resemble a common peripheral utility
- **Telemetry-friendly**: useful for testing how monitoring tools classify and correlate activity

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) - System architecture and components
- [docs/OPERATIONS.md](docs/OPERATIONS.md) - Compilation, deployment, and CLI reference
- [docs/COMPLETE_PROJECT_GUIDE.md](docs/COMPLETE_PROJECT_GUIDE.md) - Comprehensive project guide
- [docs/DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md) - Deployment instructions
- [docs/BUILD_GUIDE.md](docs/BUILD_GUIDE.md) - Build process documentation

## License

See repository for license details.

---

**Disclaimer**: This tool is designed exclusively for authorized security testing, research, and validation purposes. Users are responsible for ensuring compliance with applicable laws and organizational policies. The maintainers assume no liability for misuse.
