call :Startup %* > Startup.log 2>&1
exit /b 0

:Startup
REM --- Move to this scripts location ---
pushd "%~dp0"

REM --- Print out environment variables for debugging ---
REM set Fabric

REM --- Register media perf dlls ---
powershell .\MediaPlatformStartupScript.bat

popd
exit /b