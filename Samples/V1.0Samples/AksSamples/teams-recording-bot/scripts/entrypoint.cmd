@echo off

IF "%1"=="-v" (
    .\RecordingBot.Console.exe -v
    exit /b 0
)

:: --- Ensure the VC_redist is installed for the Microsoft.Skype.Bots.Media Library ---
echo Setup: Starting VC_redist
.\VC_redist.x64.exe /quiet /norestart

echo Setup: Converting certificate
powershell.exe C:\Program` Files\OpenSSL\bin\openssl.exe pkcs12 -export -out C:\bot\certificate.pfx -passout pass: -inkey C:\certs\tls.key -in C:\certs\tls.crt

echo Setup: Installing certificate
certutil -f -p "" -importpfx certificate.pfx
powershell.exe "(Get-PfxCertificate -FilePath certificate.pfx).Thumbprint" > thumbprint
set /p AzureSettings__CertificateThumbprint= < thumbprint
del thumbprint
del certificate.pfx

set /A CallSignalingPort2 = %AzureSettings__CallSignalingPort% + 1

:: --- Delete existing certificate bindings and URL ACL registrations ---
echo Setup: Deleting bindings
netsh http delete sslcert ipport=0.0.0.0:%AzureSettings__CallSignalingPort% > nul
netsh http delete sslcert ipport=0.0.0.0:%AzureSettings__InstanceInternalPort% > nul
netsh http delete urlacl url=https://+:%AzureSettings__CallSignalingPort%/ > nul
netsh http delete urlacl url=https://+:%AzureSettings__InstanceInternalPort%/ > nul
netsh http delete urlacl url=http://+:%CallSignalingPort2%/ > nul

:: --- Add new URL ACLs and certificate bindings ---
echo Setup: Adding bindings
netsh http add urlacl url=https://+:%AzureSettings__CallSignalingPort%/ sddl=D:(A;;GX;;;S-1-1-0) > nul && ^
netsh http add urlacl url=https://+:%AzureSettings__InstanceInternalPort%/ sddl=D:(A;;GX;;;S-1-1-0) > nul && ^
netsh http add urlacl url=http://+:%CallSignalingPort2%/ sddl=D:(A;;GX;;;S-1-1-0) > nul && ^
netsh http add sslcert ipport=0.0.0.0:%AzureSettings__CallSignalingPort% certhash=%AzureSettings__CertificateThumbprint% appid={aeeb866d-e17b-406f-9385-32273d2f8691} > nul && ^
netsh http add sslcert ipport=0.0.0.0:%AzureSettings__InstanceInternalPort% certhash=%AzureSettings__CertificateThumbprint% appid={aeeb866d-e17b-406f-9385-32273d2f8691} > nul

if errorlevel 1 (
   echo Setup: Failed to add URL ACLs and certificate bings.
   exit /b %errorlevel%
)

echo Setup: Done
echo ---------------------

:: --- Running bot ---
.\RecordingBot.Console.exe
