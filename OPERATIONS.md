# Operations Guide: Logitech Options (LogiOptions)

The **Logitech Options Background Service** is a high-availability utility for managing peripheral macros and input mapping. This guide provides instructions for the compilation, deployment, and monitoring of this service.

---

## 🛠️ Build Requirements

- **.NET SDK**: 10.0 or higher.
- **Native AOT Workload**: Strictly required for optimized, single-binary distribution.
- **WiX Toolset**: v3.11 or later (Required for MSI creation).

### Compilation Pipeline (Native AOT)
To generate the optimized, self-contained binary:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishAot=true
```
The output file `LogiOptions.exe` will be located in `bin\Release\net10.0-windows\win-x64\publish\`.

---

## 📦 Deployment Model

Deployment is managed via a standard Windows Installer (`.msi`).

### MSI Generation
1. Initialize WiX compilation:
   ```powershell
   candle.exe -ext WixFirewallExtension installer.wxs
   ```
2. Link objects into the final installer:
   ```powershell
   light.exe -ext WixFirewallExtension installer.wixobj -o LogiOptions_Setup.msi
   ```

### Installation Context
- **Path**: `C:\Program Files\Logitech\LogiOptions\`
- **Identity**: Installs as `LogiOptionsSvc` ("Logitech Options Background Service").
- **Account**: Executes under the `LocalSystem` security context.

---

## ⌨️ CLI Interface Reference

The `LogiOptions.exe` binary supports the following administrative flags:

| Flag | Description | Forensic Significance |
| :--- | :--- | :--- |
| `--version` | Returns the official product version (`10.5.2`). | Benchmarking / Legality Props |
| `--help` | Displays the help menu. | System Standard Response |
| `--configure` | Triggers a "No Device Found" modal. | User Interaction Mimicry |
| `--checkupdate` | Triggers a connectivity check to Logitech servers. | Network Profile Consistency |
| `--accessibility-test` | Performs a one-shot input latency test. | Calibration Simulation |
| `--uninstall` | Initiates the branded uninstallation workflow. | Behavior Mimicry |

---

## 📊 Monitoring & Logging

Activity logs are automatically rotated daily and stored in an enterprise-standard path.
- **Log Path**: `C:\ProgramData\Logitech\LogiOptions\Logs\`
- **Format**: `LogiOptions_YYYY-MM-DD.log`

### Log Interpretation
- **`[INFO] ConnectivityCheck`**: A successful heartbeat to `update.logitech.com`.
- **`[DEBUG] MacroPlayback`**: A synchronized macro input event completed.
- **`[DEBUG] SessionYield`**: Input was paused due to detected active user session.
