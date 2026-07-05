# Driver Signing Guide

## Overview

Windows requires kernel-mode drivers to be signed.  During development you
can use **test signing**; for production you need an **EV certificate** and
**Microsoft attestation signing** via the Hardware Dev Center.

---

## 1. Test Signing (Development / Lab Use)

### Enable test signing on the target machine

```powershell
bcdedit /set testsigning on
# Reboot required
```

> **Warning:** Test signing mode displays a watermark on the desktop and is
> itself a detection signal.  Use only in isolated lab environments.

### Self-sign the driver

```powershell
# Create a test certificate (one-time)
makecert -r -pe -ss PrivateCertStore -n "CN=AudioTelDev" audiotel_test.cer

# Sign the driver
signtool sign /fd sha256 /s PrivateCertStore /n "AudioTelDev" /t http://timestamp.digicert.com audiotel.sys
```

### Compile-time flag

Add `/D TEST_SIGN_MODE` to the WDK build flags when building for test
environments.  This enables additional debug prints via `KdPrint`.

```c
# In Sources or .vcxproj:
C_DEFINES = $(C_DEFINES) /D TEST_SIGN_MODE
```

---

## 2. Production Signing (EV Certificate)

### Prerequisites

1. **EV Code Signing Certificate** from a CA trusted by Microsoft:
   - DigiCert, Sectigo, GlobalSign, etc.
   - Must be an Extended Validation (EV) certificate
   - Stored on a hardware token (USB HSM)

2. **Microsoft Partner Center account** (formerly Hardware Dev Center):
   - Register at https://partner.microsoft.com
   - Associate your EV certificate with the account

### Sign the driver locally

```powershell
signtool sign ^
    /fd sha256 ^
    /a ^
    /v ^
    /ph ^
    /n "Your Company Name" ^
    /t http://timestamp.digicert.com ^
    audiotel.sys
```

### Sign the catalog file

```powershell
# Create the catalog
inf2cat /driver:. /os:10_x64

# Sign it
signtool sign /fd sha256 /a /v /ph /n "Your Company Name" /t http://timestamp.digicert.com audiotel.cat
```

---

## 3. Microsoft Attestation Signing

For Windows 10 1607+ and Windows 11, kernel-mode drivers must be submitted
to Microsoft for attestation signing via the Partner Center.

### Steps

1. Build and locally sign the driver package (`.sys` + `.inf` + `.cat`)
2. Create a `.hlkx` or `.cab` submission package
3. Submit to the [Partner Center Dashboard](https://partner.microsoft.com/dashboard/hardware)
4. Select "Attestation signing"
5. Download the Microsoft counter-signed driver package
6. The returned package will be trusted on all Windows 10/11 machines

### HLK Testing (optional but recommended)

For WHQL certification (highest trust level):
1. Set up a Windows Hardware Lab Kit (HLK) test environment
2. Run the relevant HID test suites against the driver
3. Submit the HLK test results along with the driver package

---

## 4. Verification

After signing, verify the signature:

```powershell
signtool verify /pa /v audiotel.sys
```

Check the driver is loaded correctly:

```powershell
sc query AUDIOTEL
driverquery /v | findstr AUDIOTEL
```

---

## 5. Security Considerations

- **Never** ship a production system with test signing enabled
- Store EV certificate HSM tokens in a secure location
- Rotate certificates before expiry
- Monitor Microsoft Partner Center for revocation notices
- Keep driver source in a private repository with restricted access
