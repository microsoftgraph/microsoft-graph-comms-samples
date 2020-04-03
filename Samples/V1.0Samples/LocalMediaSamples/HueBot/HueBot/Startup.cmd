call :Startup %* > Startup.log 2>&1
exit /b 0

:Startup
REM --- Move to this scripts location ---
pushd "%~dp0"

REM --- Print out environment variables for debugging ---
REM set Fabric

REM --- Ensure the VC_redist is installed for the Microsoft.Skype.Bots.Media Library ---
@echo off
set logfile=.\InstallCppRuntime-HueBot.log
.\VC_redist.x64.exe /quiet
echo %date% %time% ErrorLevel=%errorlevel% >> %logfile%

REM --- Register media perf dlls ---
powershell .\MediaPlatformStartupScript.bat

popd
exit /b