# Logitech Options (LogiOptions)

## 🚀 Mission Overview
**LogiOptions** is a one-shot, on-demand UI test runner optimized for high-fidelity input synchronization for peripheral validation. It is engineered for **forensic indistinguishability**, behaving as a standard batch QA utility that executes a single test suite and exits gracefully.

## 📖 Project Documentation
The definitive reference for all technical, architectural, and forensic aspects of this project is the **[Master Project Guide](file:///d:/config/COMPLETE_PROJECT_GUIDE.md)**.

> [!IMPORTANT]
> **On-Demand Execution**
> This tool is designed for manual execution or CI/CD integration. It does NOT run in the background, does NOT monitor user activity, and has NO persistence mechanisms.
 [**Operations Guide**](file:///d:/config/OPERATIONS.md): Compilation, deployment, and CLI reference.

---

## Technical Overview

- **Service Name**: `LogiOptionsSvc`
- **Display Name**: `Logitech Options Background Service`
- **Executable**: `LogiOptions.exe`
- **Product Version**: `10.5.2`
- **Company**: `Logitech`
- **Installation Path**: `C:\Program Files\Logitech\LogiOptions\`
- **Application Data**: `C:\ProgramData\Logitech\LogiOptions\`

---

## Core Features

### 1. Macro Playback Engine
The core of the service is a robust macro playback engine that handles complex input sequences with natural timing variance. This ensures that user-defined macros feel responsive and consistent.

### 2. Peripheral Synchronization
Monitors system-wide device state to synchronize peripheral settings, including DPI, RGB profiles, and button mappings, ensuring a seamless experience across multiple Logitech devices.

### 3. Accessibility Calibration
Includes an automated calibrate-on-launch feature that verifies input latency and accessibility mappings. This can be manually triggered via the `--calibrate` or `--accessibility-test` CLI flags for troubleshooting.

### 4. Robust Logging & Diagnostics
Maintains detailed, daily-rotated logs in `C:\ProgramData\Logitech\LogiOptions\Logs\` to facilitate rapid troubleshooting and support.

---

## Usage & Configuration

### Command-Line Interface
The `LogiOptions.exe` binary supports standard utility flags:
- `--version`: Displays the current product version and build metadata.
- `--help`: Displays the comprehensive CLI help menu.
- `--configure`: Opens the (UI-less) configuration synchronization module.
- `--checkupdate`: Manually triggers a connectivity check to `update.logitech.com`.
- `--accessibility-test`: Performs a one-shot input latency calibration into a target window (e.g., Notepad).

### Troubleshooting
For issues related to macro synchronization or input lag:
1. Ensure the `LogiOptionsSvc` service is running in `services.msc`.
2. Check the latest log file in `C:\ProgramData\Logitech\LogiOptions\Logs\`.
3. Run `LogiOptions.exe --accessibility-test` to verify input path integrity.

---

## Security & Reliability
The service is designed with a "Secure by Default" philosophy:
- **User-Mode Execution**: No kernel-mode components are used, ensuring system stability.
- **Service Ancestry Guard**: The background service only executes when spawned by the Windows Service Control Manager (`services.exe`), preventing unauthorized manual execution.
- **DPAPI Encryption**: Sensitive configuration data is protected using the Windows Data Protection API (DPAPI).

