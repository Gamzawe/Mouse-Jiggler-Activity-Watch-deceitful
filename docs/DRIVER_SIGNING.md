# Driver Signing for Production

To deploy the **LogiKbdHid** stealth driver in a production or target environment without disabling Driver Signature Enforcement (DSE), you must cryptographically sign the driver package. Since Windows 10 (version 1607) and Windows 11, Microsoft requires kernel-mode drivers to be signed through the **Windows Hardware Developer Center Dashboard** (WHDC) via Attestation Signing or HLK/HCK certification.

## Prerequisites

1. **EV (Extended Validation) Code Signing Certificate**: You must obtain an EV certificate on a hardware token (e.g., YubiKey, SafeNet) from a recognized CA (DigiCert, Sectigo, GlobalSign).
2. **Microsoft Partner Center Account**: Register your organization with the Windows Hardware Dev Center and link your EV certificate.
3. **Windows Driver Kit (WDK)**: Installed alongside Visual Studio on your build machine.
4. **SignTool**: Included with the Windows SDK (`signtool.exe`).

## Step 1: Build the Driver

Compile the driver as a Release (Non-Debug) build targeting the specific architecture (x64 normally) using the WDK or Enterprise WDK. Ensure the binary name is `LogiKbdHid.sys` and the `.inf` is named `LogiKbdHid.inf`.

## Step 2: Create and Sign the Catalog File

1. Generate the driver catalog (`.cat`) file using the `Inf2Cat` tool which processes your `LogiKbdHid.inf` and generates the hashes.
   ```cmd
   Inf2Cat /driver:path\to\driver\folder /os:10_x64,10_au_x64,10_rs2_x64
   ```
2. Sign the generated `LogiKbdHid.cat` and `LogiKbdHid.sys` with your EV Code Signing Certificate using `signtool.exe` and append a timestamp.
   ```cmd
   signtool sign /v /n "Your Company Name" /tr http://timestamp.digicert.com /td SHA256 /fd SHA256 LogiKbdHid.cat
   signtool sign /v /n "Your Company Name" /tr http://timestamp.digicert.com /td SHA256 /fd SHA256 LogiKbdHid.sys
   ```

## Step 3: Package for Microsoft Attestation

For modern Windows 10/11 platforms, your signature alone is not enough; it must get the WHQL/Microsoft signature cross-cert.

1. Package the driver folder (`.sys`, `.inf`, `.cat`) into a `.cab` file using `makecab`:
   ```cmd
   makecab /F driver.ddf
   ```
   *(Ensure you create a `.ddf` file that lists your driver artifacts.)*

2. Sign the `.cab` file with your EV certificate exactly as you did with the driver binaries.

## Step 4: Submit to Microsoft

1. Log into your Microsoft Windows Hardware Dev Center account.
2. Submit a new **Hardware Submission**. 
3. Select **Attestation Signing** (this skips rigorous HLK testing but issues a valid signature for Windows 10 and 11 desktop editions).
4. Upload your signed `.cab` wrapper.
5. Once processed and approved, download the signed catalog file (and driver package) provided by Microsoft. 

## Step 5: Final Deployment

Replace your locally signed `.cat` file with the Microsoft-signed `.cat` file. The driver is now ready for deployment:

```cmd
pnputil /add-driver LogiKbdHid.inf /install
```

### Note on Stealth and EDRs
The attestation signature will indicate Microsoft Windows Hardware Compatibility Publisher, adding significant legitimacy to the driver load event. Because you've changed the driver's metadata, INF details, and binary name to reflect a "Logitech HID Keyboard Driver", EDRs typically classify this driver validation positively alongside standard hardware peripheral drivers.
