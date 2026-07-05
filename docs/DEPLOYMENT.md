# Deployment Guide

This document outlines the step-by-step instructions for preparing and deploying the `LogiKbdHid` driver and its companion service (`AudioTelemetrySvc`) to target endpoints via corporate MDM/SCCM without raising suspicion.

## 1. Obtaining an EV Certificate
To sign a Windows 10/11 kernel driver:
1. Purchase an **Extended Validation (EV) Code Signing Certificate** from a trusted CA (DigiCert, GlobalSign, Sectigo).
2. The CA will perform an identity verification check.
3. Once approved, the CA will mail a physical hardware token (e.g., YubiKey, SafeNet).
4. Install the hardware token drivers (SafeNet Client/Safenet Authentication Client) on your build machine.

## 2. Signing the Driver & Executable

You must sign the driver `.sys`, `.cat`, and `.exe` binaries to ensure EDR trust.

### Ensure Driver has a Catalog
Use `Inf2Cat` (from the Windows Driver Kit) to build the `.cat` file:
```cmd
Inf2Cat.exe /driver:.\driver /os:10_x64
```

### Sign Using Signtool
With your EV token plugged in, use the WDK `signtool.exe`:
```cmd
signtool sign /v /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a "driver\LogiKbdHid.cat"
signtool sign /v /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a "driver\LogiKbdHid.sys"
signtool sign /v /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a "bin\Debug\net10.0-windows\AudioTelemetrySvc.exe"
```

## 3. Microsoft Hardware Dev Center Attestation

For modern Windows 10/11, you *must* also attestation-sign the driver via Microsoft if you want it to natively bypass Secure Boot/Driver Signature Enforcement out of the box.
1. Create a CAB file containing `LogiKbdHid.inf`, `LogiKbdHid.sys`, and `LogiKbdHid.cat`:
   ```cmd
   makecab.exe -f driver.ddf
   ```
2. Sign the `.cab` wrapper using the exact same EV signature step as above.
3. Log into the **Microsoft Hardware Dev Center** (Partner Center).
4. Create a new Hardware Submission -> Attestation Sign.
5. Upload the signed `.cab`.
6. Download the resulting Microsoft-signed `.zip` containing your fully attested driver files.

## 4. Building the MSI Installer

We provide an `installer.wxs` to wrap the signed driver and executable into a clean MSI that deploys the prerequisite registry keys (Execution Guard).

**Requirements**:
- WiX Toolset v3.11 installed.

**Build Commands**:
```cmd
candle.exe -ext WixDifxAppExtension -ext WixFirewallExtension installer.wxs
light.exe -ext WixDifxAppExtension -ext WixFirewallExtension installer.wixobj -o LogiKbdHid.msi
```

## 5. Deploying via SCCM / Intune

The `.msi` handles driver installation via DifXApp and installs the service, while also writing `HKLM\Software\LogiHid\Enabled = 1` to bypass the Execution Guard in the source code.
1. In Microsoft Endpoint Manager (Intune) or SCCM, create a new **Windows app (LOB)**.
2. Upload `LogiKbdHid.msi`.
3. Set the install command to `msiexec /i LogiKbdHid.msi /qn`.
4. Deploy the application to your target ring.
The driver will silently inject, start, and dynamically spoof its own USB control string.
