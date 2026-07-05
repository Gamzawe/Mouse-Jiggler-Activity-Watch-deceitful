# Master Project Guide: LogiOptions (Test Harness & Forensic Persona)

## 📋 Table of Contents
1.  [Executive Summary](#executive-summary)
2.  [The Dual-Identity Narrative](#narrative)
3.  [Architectural Blueprint](#architecture)
4.  [Advanced Forensic Alibi Suite (Stealth Mechanisms)](#forensic-suite)
5.  [Core Simulation Engine](#simulation-engine)
6.  [Network & Telemetry Mimicry](#network-mimicry)
7.  [Operational Command Reference](#operations)
8.  [Development & Compilation Pipeline](#dev-build)
9.  [Security & Integrity Management](#security-integrity)

---

## 1. Executive Summary <a name="executive-summary"></a>
**LogiOptions** is a professional-grade, one-shot UI test runner designed for automated validation and hardware-level timing synchronization. 

The system leverages advanced deception layers to blend into corporate environments by masquerading as a legitimate **Logitech Options** QA utility. Unlike persistent background services, LogiOptions executes a defined test cycle and exits immediately, ensuring zero background footprint.

---

## 2. The Dual-Identity Narrative <a name="narrative"></a>

To survive scrutiny from both automated EDR heuristics and human SOC analysts, LogiOptions maintains a robust dual identity:

### Public Alibi: The Accessibility Tool
*   **Mission**: Providing automated assistive macro sequences for users with motor disabilities.
*   **Validator**: The `PURPOSE.txt` resource and `--debug-purpose` flag reinforce this narrative.
*   **Legal/Compliance Anchor**: Documented as part of the "Accessibility Tools Foundation" suite.

### Forensic Mirage: The Logitech Persona
*   **Mission**: Masquerading as `LogiOptions.exe` to explain its background persistence and input injection capabilities.
*   **Authenticity Markers**: Uses Logitech provider GUIDs for ETW events, mimics Logitech memory footprints, and follows official installation paths (`C:\Program Files\Logitech\LogiOptions\`).

---

## 3. Architectural Blueprint <a name="architecture"></a>

LogiOptions is built on a modular, deception-first architecture using **.NET 10.0** and **Native AOT** for single-binary performance.

```mermaid
graph TD
    A[Windows SCM] -->|services.exe spawns| B(LogiOptions.exe)
    B --> C{Security Check}
    C -->|Parent != services| D[Show Error / Exit]
    C -->|Parent == services| E[Initialization]
    E --> F[Reflective Engine Loader]
    F -->|Decrypted JIT Load| G[MacroEngine.Core]
    G --> H[Test Execution Loop]
    H --> I[Humanization Engine]
    I --> J[User32 Input Injection]
    
    subgraph Deception Context
        K[Memory Mimicry Loop]
        L[Dummy Telemetry]
        M[Decoy Driver Logs]
    end
    E -.-> Deception Context
```

### Core Components
- **Service Host**: Manages the lifecycle and validates the execution environment (Uptime, RAM, Disk).
- **Reflective Loader**: Decrypts and loads the primary execution logic (`MacroEngine.Core`) directly into memory to bypass static object analysis.
- **Humanization Engine**: Applies weighted timing jitter (5ms-40ms) and activity-aware yielding to make synthetic inputs appear biological.

---

## 4. Advanced Forensic Alibi Suite <a name="forensic-suite"></a>

The system implements over 10 specific hardening techniques to ensure forensic persistence:

1.  **Process Ancestry Guard**: Only executes if parent process is `services.exe`.
2.  **Memory Fingerprinting**: Commits 64MB of physical RAM to mimic the Electron-based memory footprint of the original Logitech software.
3.  **Reflective Module Separation**: Splits the core logic into an encrypted resource (`.enc`) to hide strings and logic from baseline scans.
4.  **Environment Stability Failsafe**: Only fully activates on "established" systems (Uptime > 1hr, Disk > 60GB) to evade sandboxes.
5.  **Anti-Debugging Integration**: Gracefully terminates with a "licensing error" if a debugger is detected.
6.  **Branded GUI Decoy**: The `--configure` flag launches a genuine-looking "No Device Found" modal to satisfy interactive reviews.
7.  **Driver Mockery**: Simulates the loading/unloading of `LogiLDA.sys` in internal logs.
8.  **Digital Signature Spoof**: Includes Win32 resource tables that simulate Authenticode metadata.
9.  **Whitelist Anchor**: Deploys a `whitelist.sig` file in AppData to provide a visual "security" anchor for analysts.
10. **Natural Error Simulation**: Occasionally simulates "typos" and automatic corrections during macro playback.

---

## 5. Core Simulation Engine <a name="simulation-engine"></a>

The engine translates high-level JSON test scenarios into low-level Windows events.

### Input Path Integrity
- **One-Shot Cycle**: The engine performs exactly one test sequence per manual execution.
- **Legacy Fallback**: Documentation frames the use of `SendInput` as a "legacy HID compatibility mode" for non-driver environments.

### Scenario Samples (`test_scenario.json`)
Macros are executed via a custom interpreter that handles:
- **`mouse_move`**: Coordinates with acceleration curves.
- **`mouse_click`**: With varied down/up duration.
- **`key_sequence`**: With biological inter-key delays.

---

## 6. Network & Telemetry Mimicry <a name="network-mimicry"></a>

To blend into corporate network logs:
- **Heartbeats**: Sends periodic JSON status updates to `telemetry.logitech.com`.
- **Software Updates**: Periodically downloads dummy `.cab` update files and records the process in the local event log.
- **C2 Masking**: All genuine control signals are encapsulated within heartbeats that resemble standard crash reports.

---

## 7. Operational Command Reference <a name="operations"></a>

The `LogiOptions.exe` binary supports the following administrative flags:

| Flag | Description | Forensic Context |
| :--- | :--- | :--- |
| `--service` | Standard background mode. | Matches `LogiOptionsSvc` behavior. |
| `--configure` | Opens the configuration UI. | Proves "interactivity" to analysts. |
| `--debug-purpose` | Logs the alibi narrative. | Human-readable justification for logs. |
| `--warmup` | Pre-loads memory artifacts. | Used in logon scheduled tasks. |
| `--reports` | Opens the testing reports folder. | Reinforces the "QA Tool" mission. |
| `--accessibility-test` | One-shot input calibration. | Justifies "Input Injection" permissions. |

---

## 8. Development & Compilation Pipeline <a name="dev-build"></a>

### Compilation Requirements
- **Runtime**: .NET 10.0
- **Build Mode**: Native AOT (Ahead-of-Time)
- **Tooling**: WiX Toolset v3.11+ for MSI generation.

### Build Command
```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishAot=true
```

### MSI Packaging
The installer handles registry registration, service creation, and deployment of the `whitelist.sig` and `PURPOSE.txt` artifacts.

---

## 9. Security & Integrity Management <a name="security-integrity"></a>

### Encryption
- **Module XOR**: The `MacroEngine.Core` is encrypted using the `StringObfuscator` to prevent simple signature-based detection.
- **Config Obfuscation**: `macro.config` entries are base64-XOR encoded to hide execution targets.

### Event Logging
- **Source**: `Logitech Options`
- **Path**: `C:\ProgramData\Logitech\LogiOptions\Logs\`
- **Rotation**: Daily, 30-day retention.

---

> [!CAUTION]
> **Operational Security Notice**
> This project is designed for managed test environments. Ensure all deployments match the "Logitech Options" versioning tracked in the `MASTER_TECHNICAL_SPECIFICATION.md` to maintain consistency across the fleet.
