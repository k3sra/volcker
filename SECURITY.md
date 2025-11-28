# Security & Trust Guide

You asked for Volcker to be "never seen as a threat" on any computer. 

## 🚨 The Hard Truth: You Cannot Do This For Free
Windows Security (SmartScreen) is designed specifically to prevent what you are asking for: anonymous executables running without warnings.

To get the "Blue Badge" of trust and no warnings on your friends' PCs, you have **two options**:

### Option 1: Buy a Code Signing Certificate (Recommended)
This is the standard way.
1.  **Purchase** a Standard Code Signing Certificate from a Certificate Authority (Sectigo, DigiCert, GlobalSign).
    -   *Cost*: ~$100 - $400 USD / year.
    -   *Process*: They verify your identity (passport/ID).
2.  **Sign** your EXE with that certificate.
3.  **Result**: Windows knows exactly who you are. SmartScreen builds reputation quickly. No "Unknown Publisher" warnings.

### Option 2: Microsoft Store
Publish the app to the Microsoft Store.
-   *Cost*: ~$19 USD (one-time developer fee).
-   *Process*: Microsoft reviews your code.
-   *Result*: Microsoft signs it for you.
-   *Constraint*: You might need to package it as MSIX, and "Firewall" capabilities might be restricted or require special approval.

## What We Have Done (Self-Signed)
I have implemented **Self-Signing**.
-   **On Your PC**: You trust the certificate, so it runs fine.
-   **On Friends' PCs**: They will see "Windows protected your PC" (SmartScreen). They must click **More Info -> Run Anyway**.
-   **Why**: Because your "Volcker Security" certificate is not in their "Trusted Root" store.

## False Positives (Virus Detection)
If Windows Defender flags it as a *Virus* (Trojan, Malware), that is a **False Positive**.
-   **Cause**: We are manipulating the Windows Firewall. Heuristics might find this suspicious.
-   **Fix**: Submit the EXE to [Microsoft Security Intelligence](https://www.microsoft.com/en-us/wdsi/filesubmission) as a false positive.

### How to Submit
1.  Go to the [Submission Portal](https://www.microsoft.com/en-us/wdsi/filesubmission).
2.  Select **"Home IT Professional"** (or Developer).
3.  Click **"Continue"**.
4.  Login with your Microsoft Account.
5.  **Upload the file**:
    -   Path: `D:\Programming\Vlocker\Volcker\bin\Release\net8.0-windows\win-x64\publish\Volcker.exe`
6.  **Select "Incorrectly detected as malware/malicious"**.
7.  **Product**: Select "Microsoft Defender Antivirus".
8.  **Comments**: "This is a custom firewall management tool I developed. It uses netsh/PowerShell to block internet access for specific folders. It is not malicious."
9.  Click **Submit**.

## Summary
I have made the app as professional as possible (Metadata, Icon, Clean Code). But **without paying for a certificate**, you cannot bypass the "Unknown Publisher" warning on other computers. This is a fundamental security feature of Windows.
