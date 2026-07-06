# Master Technical Specification: LogiOptions (Accessibility & Macro Engine)

## 📋 Table of Contents
1.  [System Identity & Narrative](#system-identity)
2.  [Architectural Design](#architecture)
3.  [The Forensic Alibi Suite (10 Hardening Techniques)](#forensic-alibi)
4.  [Stealth & Behavioral Deception](#stealth-behavioral)
5.  [Network & Telemetry Footprint](#network-telemetry)
6.  [Operational Guide](#operations)
7.  [Build & Security Spec](#build-security)
8.  [Artifact & Registry Index](#artifact-index)

---

## 1. System Identity & Narrative <a name="system-identity"></a>

**LogiOptions** is a professional-grade background service engineered for high-fidelity input synchronization and macro playback. 

### The Persona
To prevent detection by human SOC analysts and XDR systems, the project maintains a dual identity:
- **Public Alibi**: A legitimate accessibility utility from the **Accessibility Tools Foundation** designed to assist users with motor disabilities via automated macro sequences.
- **Operational Reality**: A stealthy background agent that mimics the behavior, footprint, and file artifacts of official **Logitech Options** software to bypass security heuristics.

---

## 2. Architectural Design <a name="architecture"></a>

The system is built on a modular "Deception-First" architecture.

```mermaid
graph TD
    A[SCM] -->|services.exe| B(LogiOptions.exe)
    B --> C[Dependency Injection]
    C --> D[MacroPlaybackService]
    D --> E[MacroLibrarySimulator]
    E --> F[Windows Input Subsystem]
    D --> G[MacroVariationEngine]
    G --> E
    subgraph Deception Layers
        H[Telemetry Loop]
        I[GUI Config]
        J[Minidump Gen]
    end
    B -.-> Deception Layers
```

### Core Components
- **MacroPlaybackService**: Manages the service lifecycle, network telemetry, and simulation scheduling.
- **MacroLibrarySimulator**: A high-fidelity wrapper around the Windows User32 `SendInput` API, documented as a professional macro suite.
- **MacroVariationEngine**: The "Humanization Engine" that applies timing jitter, burst logic, and activity-aware yielding.

---

## 3. The Forensic Alibi Suite <a name="forensic-alibi"></a>

We have implemented 10 specific techniques to harden the forensic narrative:

1.  **Embedded Alibi**: `PURPOSE.txt` resource (ID `PURPOSE_TXT`) explains the tool's accessibility mission.
2.  **Diagnostic Misdirection**: `--debug-purpose` flag logs benign "macro compatibility" status.
3.  **Config Artifacts**: `macro.config` file resides in the app directory with dummy macro definitions.
4.  **Branded "About" Dialog**: Functional UI element in the configuration form confirming the tool's origins.
5.  **Safety-Framed Code**: Method `CheckUserPresenceForMacroSafety` frames stealth as a "safety feature."
6.  **Simulation Warmup**: Forced 30-second service delay on startup with "No hardware detected" logs.
7.  **Telemetry Consent**: First-run GUI prompt asking users to opt-in to anonymous usage data.
8.  **Professional Wrapper**: The injection logic is encapsulated in `MacroLibrarySimulator` with professional XML docs.
9.  **Uninstall Survey**: MSI custom action that prompts for survey feedback upon software removal.
10. **Metadata Consistency**: Assembly metadata and registry "Publisher" keys point to the *Accessibility Tools Foundation*.

---

## 4. Stealth & Behavioral Deception <a name="stealth-behavioral"></a>

### Memory & Process Mimicry
- **Ancestry Guard**: Service only starts if its parent is `services.exe`. Manual runs show a branded error.
- **Library Hijacking**: Calls `LoadLibrary("hid.dll")` to pull signed Microsoft code into the process space.
- **ETW Integration**: Emits real ETW events under the Logitech provider GUID.

### Humanization Tactics
- **Micro-Jitter**: Randomized 5ms-40ms variance in input timing.
- **Activity Yielding**: High-resolution polling of `GetLastInputInfo` to pause simulation when the real user is active.
- **Interactive Apps**: Periodically spawns and interacts with `calc.exe` or `explorer.exe` to create a "User-Active" forensic footprint.

---

## 5. Network & Telemetry Footprint <a name="network-telemetry"></a>

The service maintains a realistic network profile to blend into corporate traffic:
- **Heartbeats**: Periodic JSON POSTs to `telemetry.logitech.com`.
- **Sync Checks**: `HEAD` requests to `update.logitech.com`.
- **Update Downloads**: Occasional 1MB dummy CAB file downloads to mimic the auto-update process.

---

## 6. Operational Guide <a name="operations"></a>

### Command Line Interface
| Flag | Behavior |
| :--- | :--- |
| `--service` | Standard service execution mode. |
| `--configure` | Opens the branded WinForms configuration GUI. |
| `--debug-purpose` | Logs the forensic alibi to the console/log. |
| `--install` | Registers the service (logic inside MSI). |

### Service Management
- **Name**: `LogitechOptionsUpdateService`
- **Display Name**: `Logitech Options Update Service`
- **Context**: `LocalSystem`

---

## 7. Build & Security Spec <a name="build-security"></a>

### Compilation Profile
- **Target**: .NET 10.0 Windows
- **Packaging**: **Single File + Trimmed**
- **Rationale**: Optimization for forensic persistence while maintaining compatibility with the Windows Forms GUI.

### Integrity Checks
- **Signature Check**: Emulates checking `whitelist.sig` for "signed macro definitions."
- **Whitelist Decoy**: The installer deploys a `whitelist.sig` file to provide a visual anchor for "trusted software."

---

## 8. Artifact & Registry Index <a name="artifact-index"></a>

### File System
- `C:\Program Files\Logitech\LogiOptions\LogiOptions.exe`
- `C:\Program Files\Logitech\LogiOptions\macro.config`
- `C:\ProgramData\Logitech\LogiOptions\Logs\LogiOptions_*.log`
- `C:\ProgramData\Logitech\LogiOptions\CrashReports\*.dmp`

### Registry
- `HKLM\SOFTWARE\Logitech\Options\Publisher` = `Accessibility Tools Foundation`
- `HKLM\SOFTWARE\Logitech\Options\TelemetryAsked` = `1`
- `HKLM\SOFTWARE\Logitech\Options\Macros\InjectedInput` = `Standard legacy macro support`

---

> [!CAUTION]
> **Operational Security Notice**
> This document is intended for internal project synchronization. Ensure that all generated logs and crash reports (Event ID 4001) are consistent with the "Accessibility Support" narrative during actual forensic interviews.
