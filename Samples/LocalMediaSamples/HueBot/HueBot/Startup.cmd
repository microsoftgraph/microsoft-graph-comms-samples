call :Startup %* > Startup.log 2>&1
exit /b 0

:Startup
REM --- Move to this scripts location ---
pushd "%~dp0"

REM --- Print out environment variables for debugging ---
set Fabric

REM --- Register media perf dlls ---
powershell .\MediaPlatformStartupScript.bat

REM --- Service Fabric variables https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-environment-variables-reference
set /a MediaPort=%Fabric_NodeName:_Node_=% + %1
set DefaultPort=%Fabric_Endpoint_ServiceEndpoint%
set ip=%Fabric_Endpoint_IPOrFQDN_ServiceEndpoint:localhost=127.0.0.1%

REM --- Delete existing certificate bindings and URL ACL registrations ---
netsh http delete sslcert ipport=%ip%:%DefaultPort%
netsh http delete sslcert ipport=%ip%:%MediaPort%
netsh http delete urlacl url=https://%ip%:%DefaultPort%/
netsh http delete urlacl url=https://%ip%:%MediaPort%/

REM --- Delete new URL ACLs and certificate bindings ---
netsh http add urlacl url=https://%ip%:%DefaultPort%/ user="NT AUTHORITY\NETWORK SERVICE"
netsh http add urlacl url=https://%ip%:%MediaPort%/ user="NT AUTHORITY\NETWORK SERVICE"
netsh http add sslcert ipport=%ip%:%DefaultPort% "appid={00000000-0000-0000-0000-000000000001}" cert=%2
netsh http add sslcert ipport=%ip%:%MediaPort% "appid={00000000-0000-0000-0000-000000000001}" cert=%2

popd
exit /b