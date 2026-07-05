# Step-by-Step Build Tutorial: AudioTelemetrySvc

This guide takes you strictly from raw source code to the finished `.msi` enterprise deployment package for the Logitech-branded background utility.

## Step 1: Prepare the Environment

Before you compile anything, ensure your host has the requisite SDKs:
1.  **Visual Studio 2022 Community/Pro:** Installed with the `"Desktop development with C++"` and `"Windows Driver Development"` workloads.
2.  **Windows Driver Kit (WDK):** Ensure it matches your target Windows SDK version.
3.  **.NET 10 SDK:** Required for the `AudioTelemetrySvc` core.
4.  **WiX Toolset v3.11:** Ensure `candle.exe` and `light.exe` are in your System PATH.

---

## Step 2: Build the Kernel Driver (`LogiLDA.sys`)

The driver masquerades as the Logitech Lead-In Device Architect.

1. Open Visual Studio 2022.
2. Open the solution or create an **Empty WDF KMDF Driver** project named `LogiLDA`.
3. Add the following from the `driver/` directory:
    * `LogiLDA.c`, `LogiLDA.h`, `LogiLDA.rc`, `LogiLDA.inf`.
4. Configure for **Release | x64**.
5. Set properties:
    * **C/C++ -> General -> Treat Warnings As Errors**: `No`.
    * **Driver Settings -> Target OS Version**: `Windows 10 or later`.
6. **Build Solution**. The output `LogiLDA.sys` and `LogiLDA.inf` will be in `x64\Release\LogiLDA\`.

---

## Step 3: Generate the Catalog File (`LogiLDA.cat`)

Modern Windows requires a signed catalog file for driver installation.

1. Open the **"x64 Native Tools Command Prompt for VS 2022"** as Administrator.
2. Navigate to your build output:
   ```cmd
   cd d:\config\driver\
   ```
3. Run `Inf2Cat`:
   ```cmd
   Inf2Cat.exe /driver:.\ /os:10_x64,10_11_x64
   ```
4. This creates `LogiLDA.cat`. Ensure both the `.sys` and `.cat` are eventually signed with a trusted certificate for production use.

---

## Step 4: Build the Headless Simulator (Native AOT)

Compile the C# service into a standalone, obfuscated native executable.

1. Open a terminal in `d:\config\`.
2. Run the publish command:
   ```cmd
   dotnet publish -c Release -p:PublishAot=true -r win-x64 --self-contained true -o ./publish
   ```
3. The output binary will be named **`LogiOptions.exe`** (Standardized name for forensic deception).
4. Verify the binary version via CLI:
   ```cmd
   ./publish/LogiOptions.exe --version
   ```

---

## Step 5: Wrap the MSI Installer with WiX

The installer bundles the driver, the executable, and the registry authorization keys.

1. Ensure `installer.wxs` is configured to point to your `publish/LogiOptions.exe` and `driver/LogiLDA.sys`.
2. Compile the installer:
   ```cmd
   candle.exe -ext WixDifxAppExtension -ext WixFirewallExtension installer.wxs
   ```
3. Link the installer:
   ```cmd
   light.exe -ext WixDifxAppExtension -ext WixFirewallExtension installer.wixobj -o LogiOptionsSetup.msi
   ```

---

## Final Validation

To test the deployment silently:
```cmd
msiexec /i LogiOptionsSetup.msi /qn
```

### Verification Checklist:
- [ ] `Logitech Options Background Utility` service is running in `services.msc`.
- [ ] `LogiLDA` device appears in Device Manager under "Keyboards".
- [ ] Registry key `HKLM\Software\LogiHid\Enabled` is set to `1`.
- [ ] Registry key `HKLM\SYSTEM\CurrentControlSet\Services\LogiLDA\Parameters` contains spoofed metadata.
