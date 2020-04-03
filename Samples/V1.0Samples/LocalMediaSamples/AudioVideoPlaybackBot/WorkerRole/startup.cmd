REM --- Move to this scripts location ---
pushd "%~dp0"

REM --- Ensure the VC_redist is installed for the Microsoft.Skype.Bots.Media Library ---
.\VC_redist.x64.exe /quiet

REM --- Print out environment variables for debugging ---
set

REM --- Delete existing certificate bindings and URL ACL registrations ---
netsh http delete sslcert ipport=%InstanceIpAddress%:%PrivateDefaultCallControlPort%
netsh http delete sslcert ipport=%InstanceIpAddress%:%PrivateInstanceCallControlPort%
netsh http delete urlacl url=https://%InstanceIpAddress%:%PrivateDefaultCallControlPort%/
netsh http delete urlacl url=https://%InstanceIpAddress%:%PrivateInstanceCallControlPort%/

REM --- Delete new URL ACLs and certificate bindings ---
netsh http add urlacl url=https://%InstanceIpAddress%:%PrivateDefaultCallControlPort%/ user="NT AUTHORITY\NETWORK SERVICE"
netsh http add urlacl url=https://%InstanceIpAddress%:%PrivateInstanceCallControlPort%/ user="NT AUTHORITY\NETWORK SERVICE"
netsh http add sslcert ipport=%InstanceIpAddress%:%PrivateDefaultCallControlPort% "appid={00000000-0000-0000-0000-000000000001}" cert=%DefaultCertificate%
netsh http add sslcert ipport=%InstanceIpAddress%:%PrivateInstanceCallControlPort% "appid={00000000-0000-0000-0000-000000000001}" cert=%DefaultCertificate%

popd
exit /b 0