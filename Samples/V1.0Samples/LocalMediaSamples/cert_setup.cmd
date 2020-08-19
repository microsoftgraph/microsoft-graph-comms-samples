REM --- Move to this scripts location ---
pushd "%~dp0"

REM --- Print out environment variables for debugging ---
set

REM --- Ensure the VC_redist is installed for the Microsoft.Skype.Bots.Media Library ---
.\VC_redist.x64.exe /quiet /norestart

REM --- Delete existing certificate bindings and URL ACL registrations ---
netsh http delete sslcert ipport=0.0.0.0:9441
netsh http delete sslcert ipport=0.0.0.0:8445
netsh http delete urlacl url=https://+:8445/
netsh http delete urlacl url=https://+:9441/

REM --- Add new URL ACLs and certificate bindings ---
netsh http add urlacl url=https://+:8445/ sddl=D:(A;;GX;;;S-1-1-0)
netsh http add urlacl url=https://+:9441/ sddl=D:(A;;GX;;;S-1-1-0)
netsh http add sslcert ipport=0.0.0.0:9441 certhash=%YOUR-SSL-CERTIFICATE-THUMBPRINT% appid={%GUID from AssemblyInfo.cs of your project%}
netsh http add sslcert ipport=0.0.0.0:8445 certhash=%YOUR-SSL-CERTIFICATE-THUMBPRINT% appid={%GUID from AssemblyInfo.cs of your project%}

popd
exit /b 0