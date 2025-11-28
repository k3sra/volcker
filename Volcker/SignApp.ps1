$certName = "VolckerSelfSignedCert"
$exePath = "bin\Release\net8.0-windows\win-x64\publish\Volcker.exe"

# 1. Check if Cert exists, if not create it
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq "CN=$certName" }

if (-not $cert) {
    Write-Host "Creating Self-Signed Certificate..."
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=$certName" -CertStoreLocation Cert:\CurrentUser\My
    Write-Host "Certificate Created: $($cert.Thumbprint)"
}
else {
    Write-Host "Using existing Certificate: $($cert.Thumbprint)"
}

# 2. Sign the EXE
if (Test-Path $exePath) {
    Write-Host "Waiting for file access..."
    $maxRetries = 10
    $retryCount = 0
    while ($retryCount -lt $maxRetries) {
        try {
            $stream = [System.IO.File]::Open($exePath, 'Open', 'ReadWrite', 'None')
            $stream.Close()
            break
        }
        catch {
            Write-Host "File is locked, retrying in 1s..."
            Start-Sleep -Seconds 1
            $retryCount++
        }
    }

    if ($retryCount -eq $maxRetries) {
        Write-Error "Could not access $exePath. It might be running."
        exit 1
    }

    Write-Host "Signing $exePath..."
    Set-AuthenticodeSignature -FilePath $exePath -Certificate $cert
    Write-Host "Signing Complete."
}
else {
    Write-Error "Executable not found at $exePath. Please publish first."
}
