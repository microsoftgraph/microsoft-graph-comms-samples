REM Set up Environment Variables
./set_env.cmd .env

set /A CallSignalingPort2 = %AzureSettings__CallSignalingPort% + 1

REM Deleting bindings
netsh http delete sslcert ipport=0.0.0.0:%AzureSettings__CallSignalingPort%
netsh http delete sslcert ipport=0.0.0.0:%AzureSettings__InstanceInternalPort%
netsh http delete urlacl url=https://+:%AzureSettings__CallSignalingPort%/
netsh http delete urlacl url=https://+:%AzureSettings__InstanceInternalPort%/
netsh http delete urlacl url=http://+:%CallSignalingPort2%/

REM Add URLACL bindings
netsh http add urlacl url=https://+:%AzureSettings__CallSignalingPort%/ sddl=D:(A;;GX;;;S-1-1-0)
netsh http add urlacl url=https://+:%AzureSettings__InstanceInternalPort%/ sddl=D:(A;;GX;;;S-1-1-0)
netsh http add urlacl url=http://+:%CallSignalingPort2%/ sddl=D:(A;;GX;;;S-1-1-0)

REM ensure the app id matches the GUID in AssemblyInfo.cs
REM Ensure the certhash matches the certificate

netsh http add sslcert ipport=0.0.0.0:%AzureSettings__CallSignalingPort% certhash=YOUR_CERT_THUMBPRINT appid={aeeb866d-e17b-406f-9385-32273d2f8691}
netsh http add sslcert ipport=0.0.0.0:%AzureSettings__InstanceInternalPort% certhash=YOUR_CERT_THUMBPRINT appid={aeeb866d-e17b-406f-9385-32273d2f8691}
