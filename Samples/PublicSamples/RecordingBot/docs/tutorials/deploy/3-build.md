# Clone and build recording bot sample

Next let us clone the source code for the recording bot sample, from
the source code we will build the application in a Docker container.

## Clone the sample

To clone the sample we first navigate to the directory we want to clone the code to:

```powershell
cd C:\Users\User\recordingbottutorial
```

> [!TIP]
> Make sure the directory existists beforehand, to create a directory run `mkdir C:\Users\User\recordingbottutorial`.

Next we use git to clone the source code from github:

```powershell
git clone https://github.com/LM-Development/aks-sample.git
```

The execution takes some time to download, the repository with all samples is downloaded,
and the output should look similar to:

```text
Cloning into 'aks-sample'...
remote: Enumerating objects: 10526, done.
remote: Counting objects: 100% (3138/3138), done.
remote: Compressing objects: 100% (1167/1167), done.
remote: Total 10526 (delta 2336), reused 2427 (delta 1912), pack-reused 7388
Receiving objects: 100% (10526/10526), 207.10 MiB | 10.09 MiB/s, done.
Resolving deltas: 100% (8606/8606), done.
Updating files: 100% (1289/1289), done.
```

Now we navigate to the aks sample in the repository we just downloaded.

```powershell
cd .\aks-sample\Samples\PublicSamples\RecordingBot\
```

## Build the application

To build the application we will push the dockerfile and the source code of the AKS sample to our
Azure container registry. The registry will build the application into a container and stores the
container in the registry. To do so we also have to provide the build job with the tag we want to
have for our container (`-t`-parameter):

```powershell
az acr build 
    --registry recordingbotregistry
    --resource-group recordingbottutorial
    -t recordingbottutorial/application:latest
    --platform Windows 
    --file ./build/Dockerfile 
    --subscription "recordingbotsubscription"
    .
```

The build of the Docker container takes a very long time. The source code is first uploaded and then
a quite large windows container starts building before the app is built.
However the complete output should look similar to:

```text
Packing source code into tar to upload...
Excluding '.gitignore' based on default ignore rules
Uploading archived source code from 'C:\Users\FKA\AppData\Local\Temp\build_archive_062a5cd756dc416e81d889bca3b223f5.tar.gz'...
D:\a\_work\1\s\build_scripts\windows\artifacts\cli\Lib\site-packages\cryptography/hazmat/backends/openssl/backend.py:17: UserWarning: You are using cryptography on a 32-bit Python on a 64-bit Windows Operating System. Cryptography will be significantly faster if you switch to using a 64-bit Python.
Sending context (79.930 MiB) to registry: recordingbotregistry...
Queued a build with ID: cb1
Waiting for an agent...
2024/04/22 13:10:09 Downloading source code...
2024/04/22 13:10:16 Finished downloading source code
2024/04/22 13:10:18 Using acb_vol_b0ea293c-940e-44ed-b386-a0ecc9e0ec89 as the home volume
2024/04/22 13:10:19 Setting up Docker configuration...
2024/04/22 13:10:27 Successfully set up Docker configuration
2024/04/22 13:10:27 Logging in to registry: recordingbotregistry.azurecr.io
2024/04/22 13:10:31 Successfully logged into recordingbotregistry.azurecr.io
2024/04/22 13:10:31 Executing step ID: build. Timeout(sec): 28800, Working directory: '', Network: ''
2024/04/22 13:10:31 Scanning for dependencies...
2024/04/22 13:10:35 Successfully scanned dependencies
2024/04/22 13:10:35 Launching container with name: build
Sending build context to Docker daemon  87.51MB
Step 1/20 : FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022 AS build
8.0-windowsservercore-ltsc2022: Pulling from dotnet/sdk
7c76e5cf7755: Already exists
197484daab96: Pulling fs layer
f2260092360a: Pulling fs layer
12604b42eee2: Pulling fs layer
a56b46197d8a: Pulling fs layer
40de75951aee: Pulling fs layer
675965b9e219: Pulling fs layer
d63d65ec8653: Pulling fs layer
fbeeb0f49213: Pulling fs layer
4295a548cbb3: Pulling fs layer
e9692349cfe4: Pulling fs layer
d506a2a67773: Pulling fs layer
a56b46197d8a: Waiting
40de75951aee: Waiting
675965b9e219: Waiting
d63d65ec8653: Waiting
fbeeb0f49213: Waiting
4295a548cbb3: Waiting
e9692349cfe4: Waiting
d506a2a67773: Waiting
f2260092360a: Verifying Checksum
f2260092360a: Download complete
a56b46197d8a: Download complete
40de75951aee: Verifying Checksum
40de75951aee: Download complete
12604b42eee2: Verifying Checksum
12604b42eee2: Download complete
675965b9e219: Verifying Checksum
675965b9e219: Download complete
d63d65ec8653: Verifying Checksum
d63d65ec8653: Download complete
fbeeb0f49213: Verifying Checksum
fbeeb0f49213: Download complete
e9692349cfe4: Verifying Checksum
e9692349cfe4: Download complete
d506a2a67773: Verifying Checksum
d506a2a67773: Download complete
4295a548cbb3: Verifying Checksum
4295a548cbb3: Download complete
197484daab96: Verifying Checksum
197484daab96: Download complete
197484daab96: Pull complete
f2260092360a: Pull complete
12604b42eee2: Pull complete
a56b46197d8a: Pull complete
40de75951aee: Pull complete
675965b9e219: Pull complete
d63d65ec8653: Pull complete
fbeeb0f49213: Pull complete
4295a548cbb3: Pull complete
e9692349cfe4: Pull complete
d506a2a67773: Pull complete
Digest: sha256:ce3009d6cb2c647ae0e1bb8cc984d643611ced83704b1c1f331178853e5d7e7d
Status: Downloaded newer image for mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022
 ---> dd178c759d24
Step 2/20 : ARG CallSignalingPort=9441
 ---> Running in 363dc8870d49
Removing intermediate container 363dc8870d49
 ---> dc70870cd8c8
Step 3/20 : ARG CallSignalingPort2=9442
 ---> Running in e661813679b5
Removing intermediate container e661813679b5
 ---> b07121138db6
Step 4/20 : ARG InstanceInternalPort=8445
 ---> Running in ab423a8d39f0
Removing intermediate container ab423a8d39f0
 ---> 850b8862d20c
Step 5/20 : COPY /src /src
 ---> 1370e31c2d05
Step 6/20 : WORKDIR /src/RecordingBot.Console
 ---> Running in ae57b889db82
Removing intermediate container ae57b889db82
 ---> d5ae5532d288
Step 7/20 : RUN dotnet build RecordingBot.Console.csproj --arch x64 --self-contained --configuration Release --output C:\app
 ---> Running in b1dd2e2f9ec5

MSBuild version 17.9.8+b34f75857 for .NET
  Determining projects to restore...
  Restored C:\src\RecordingBot.Services\RecordingBot.Services.csproj (in 42.65 sec).
  Restored C:\src\RecordingBot.Model\RecordingBot.Model.csproj (in 42.65 sec).
  Restored C:\src\RecordingBot.Console\RecordingBot.Console.csproj (in 151 ms).

  RecordingBot.Model -> C:\app\RecordingBot.Model.dll
  RecordingBot.Services -> C:\app\RecordingBot.Services.dll
  RecordingBot.Console -> C:\app\RecordingBot.Console.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:01:28.54

Removing intermediate container b1dd2e2f9ec5
 ---> 45573b270fe0
Step 8/20 : FROM mcr.microsoft.com/windows/server:ltsc2022
ltsc2022: Pulling from windows/server
7d7d659851e2: Pulling fs layer
0e72f557f0f3: Pulling fs layer
0e72f557f0f3: Verifying Checksum
0e72f557f0f3: Download complete
7d7d659851e2: Verifying Checksum
7d7d659851e2: Download complete
7d7d659851e2: Pull complete
0e72f557f0f3: Pull complete
Digest: sha256:f2a7ad9732bdaf680bcadb270101f1908cf9969581b094c3279f1481eb181a71
Status: Downloaded newer image for mcr.microsoft.com/windows/server:ltsc2022
 ---> 38f56eb00da7
Step 9/20 : SHELL ["powershell", "-Command"]
 ---> Running in 8f5b3b9696c3
Removing intermediate container 8f5b3b9696c3
 ---> 4f412d93e1e2
Step 10/20 : ADD https://aka.ms/vs/17/release/vc_redist.x64.exe /bot/VC_redist.x64.exe


 ---> 039bdabdcd43
Step 11/20 : COPY /scripts/entrypoint.cmd /bot
 ---> c79f6cd07136
Step 12/20 : COPY /scripts/halt_termination.ps1 /bot
 ---> 624da28ae9b6
Step 13/20 : COPY --from=build /app /bot
 ---> b2d03bb4edfa
Step 14/20 : WORKDIR /bot
 ---> Running in 706ec3610b4d
Removing intermediate container 706ec3610b4d
 ---> a2138a17a902
Step 15/20 : RUN Set-ExecutionPolicy Bypass -Scope Process -Force;     [System.Net.ServicePointManager]::SecurityProtocol =         [System.Net.ServicePointManager]::SecurityProtocol -bor 3072;         iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
 ---> Running in 6bf19d935028

Forcing web requests to allow TLS v1.2 (Required for requests to Chocolatey.org)
Getting latest version of the Chocolatey package for download.
Not using proxy.
Getting Chocolatey from https://community.chocolatey.org/api/v2/package/chocolatey/2.2.2.
Downloading https://community.chocolatey.org/api/v2/package/chocolatey/2.2.2 to C:\Users\ContainerAdministrator\AppData\Local\Temp\chocolatey\chocoInstall\chocolatey.zip
Not using proxy.
Extracting C:\Users\ContainerAdministrator\AppData\Local\Temp\chocolatey\chocoInstall\chocolatey.zip to C:\Users\ContainerAdministrator\AppData\Local\Temp\chocolatey\chocoInstall
Installing Chocolatey on the local machine
Creating ChocolateyInstall as an environment variable (targeting 'Machine')
  Setting ChocolateyInstall to 'C:\ProgramData\chocolatey'
WARNING: It's very likely you will need to close and reopen your shell
  before you can use choco.
Restricting write permissions to Administrators
We are setting up the Chocolatey package repository.
The packages themselves go to 'C:\ProgramData\chocolatey\lib'
  (i.e. C:\ProgramData\chocolatey\lib\yourPackageName).
A shim file for the command line goes to 'C:\ProgramData\chocolatey\bin'
  and points to an executable in 'C:\ProgramData\chocolatey\lib\yourPackageName'.

Creating Chocolatey folders if they do not already exist.

chocolatey.nupkg file not installed in lib.
 Attempting to locate it from bootstrapper.
PATH environment variable does not have C:\ProgramData\chocolatey\bin in it. Adding...
WARNING: Not setting tab completion: Profile file does not exist at
'C:\Users\ContainerAdministrator\Documents\WindowsPowerShell\Microsoft.PowerShe
ll_profile.ps1'.
Chocolatey (choco.exe) is now ready.
You can call choco from anywhere, command line or powershell by typing choco.
Run choco /? for a list of functions.
You may need to shut down and restart powershell and/or consoles
 first prior to using choco.
Ensuring Chocolatey commands are on the path
Ensuring chocolatey.nupkg is in the lib folder
Removing intermediate container 6bf19d935028
 ---> 94b203190fa7
Step 16/20 : RUN choco install openssl.light -y
 ---> Running in c6ba27fdfc95

Chocolatey v2.2.2
Installing the following packages:
openssl.light
By installing, you accept licenses for the packages.
Progress: Downloading chocolatey-compatibility.extension 1.0.0... 100%

chocolatey-compatibility.extension v1.0.0 [Approved]
chocolatey-compatibility.extension package files install completed. Performing other installation steps.
 Installed/updated chocolatey-compatibility extensions.
 The install of chocolatey-compatibility.extension was successful.
  Software installed to 'C:\ProgramData\chocolatey\extensions\chocolatey-compatibility'
Progress: Downloading chocolatey-core.extension 1.4.0... 100%

chocolatey-core.extension v1.4.0 [Approved]
chocolatey-core.extension package files install completed. Performing other installation steps.
 Installed/updated chocolatey-core extensions.
 The install of chocolatey-core.extension was successful.
  Software installed to 'C:\ProgramData\chocolatey\extensions\chocolatey-core'
Progress: Downloading chocolatey-windowsupdate.extension 1.0.5... 100%

chocolatey-windowsupdate.extension v1.0.5 [Approved]
chocolatey-windowsupdate.extension package files install completed. Performing other installation steps.
 Installed/updated chocolatey-windowsupdate extensions.
 The install of chocolatey-windowsupdate.extension was successful.
  Software installed to 'C:\ProgramData\chocolatey\extensions\chocolatey-windowsupdate'
Progress: Downloading KB2919442 1.0.20160915... 100%

KB2919442 v1.0.20160915 [Approved]
KB2919442 package files install completed. Performing other installation steps.
Skipping installation because this hotfix only applies to Windows 8.1 and Windows Server 2012 R2.
 The install of KB2919442 was successful.
  Software install location not explicitly set, it could be in package or
  default install location of installer.
Progress: Downloading KB2919355 1.0.20160915... 100%

KB2919355 v1.0.20160915 [Approved]
KB2919355 package files install completed. Performing other installation steps.
Skipping installation because this hotfix only applies to Windows 8.1 and Windows Server 2012 R2.
 The install of KB2919355 was successful.
  Software install location not explicitly set, it could be in package or
  default install location of installer.
Progress: Downloading KB2999226 1.0.20181019... 100%

KB2999226 v1.0.20181019 [Approved] - Possibly broken
KB2999226 package files install completed. Performing other installation steps.
Skipping installation because update KB2999226 does not apply to this operating system (Microsoft Windows Server 2022 Datacenter).
 The install of KB2999226 was successful.
  Software install location not explicitly set, it could be in package or
  default install location of installer.
Progress: Downloading KB3035131 1.0.3... 100%

KB3035131 v1.0.3 [Approved]
KB3035131 package files install completed. Performing other installation steps.
Skipping installation because update KB3035131 does not apply to this operating system (Microsoft Windows Server 2022 Datacenter).
 The install of KB3035131 was successful.
  Software install location not explicitly set, it could be in package or
  default install location of installer.
Progress: Downloading KB3033929 1.0.5... 100%

KB3033929 v1.0.5 [Approved]
KB3033929 package files install completed. Performing other installation steps.
Skipping installation because update KB3033929 does not apply to this operating system (Microsoft Windows Server 2022 Datacenter).
 The install of KB3033929 was successful.
  Software install location not explicitly set, it could be in package or
  default install location of installer.
Progress: Downloading vcredist140 14.38.33135... 100%

vcredist140 v14.38.33135 [Approved]
vcredist140 package files install completed. Performing other installation steps.
Downloading vcredist140-x86
  from 'https://download.visualstudio.microsoft.com/download/pr/71c6392f-8df5-4b61-8d50-dba6a525fb9d/510FC8C2112E2BC544FB29A72191EABCC68D3A5A7468D35D7694493BC8593A79/VC_redist.x86.exe'
Progress: 100% - Completed download of C:\Users\ContainerAdministrator\AppData\Local\Temp\chocolatey\vcredist140\14.38.33135\VC_redist.x86.exe (13.21 MB).
Download of VC_redist.x86.exe (13.21 MB) completed.
Hashes match.
Installing vcredist140-x86...

vcredist140-x86 has been installed.
Downloading vcredist140-x64 64 bit
  from 'https://download.visualstudio.microsoft.com/download/pr/6ba404bb-6312-403e-83be-04b062914c98/1AD7988C17663CC742B01BEF1A6DF2ED1741173009579AD50A94434E54F56073/VC_redist.x64.exe'
Progress: 100% - Completed download of C:\Users\ContainerAdministrator\AppData\Local\Temp\chocolatey\vcredist140\14.38.33135\VC_redist.x64.exe (24.24 MB).
Download of VC_redist.x64.exe (24.24 MB) completed.
Hashes match.
Installing vcredist140-x64...

vcredist140-x64 has been installed.
  vcredist140 may be able to be automatically uninstalled.
 The install of vcredist140 was successful.
  Software installed as 'exe', install location is likely default.
Progress: Downloading OpenSSL.Light 3.1.4... 100%

OpenSSL.Light v3.1.4 [Approved]
OpenSSL.Light package files install completed. Performing other installation steps.
Installing 64-bit OpenSSL.Light...
OpenSSL.Light has been installed.
Installed to 'C:\Program Files\OpenSSL'
PATH environment variable does not have C:\Program Files\OpenSSL\bin in it. Adding...
  OpenSSL.Light can be automatically uninstalled.
Environment Vars (like PATH) have changed. Close/reopen your shell to
 see the changes (or in powershell/cmd.exe just type `refreshenv`).
 The install of OpenSSL.Light was successful.
  Software installed to 'C:\Program Files\OpenSSL\'

Chocolatey installed 10/10 packages.
 See the log for details (C:\ProgramData\chocolatey\logs\chocolatey.log).

Installed:
 - chocolatey-compatibility.extension v1.0.0
 - chocolatey-core.extension v1.4.0
 - chocolatey-windowsupdate.extension v1.0.5
 - KB2919355 v1.0.20160915
 - KB2919442 v1.0.20160915
 - KB2999226 v1.0.20181019
 - KB3033929 v1.0.5
 - KB3035131 v1.0.3
 - OpenSSL.Light v3.1.4
 - vcredist140 v14.38.33135
Removing intermediate container c6ba27fdfc95
 ---> 84129bb8fe16
Step 17/20 : EXPOSE $InstanceInternalPort
 ---> Running in cbd1cddb7c1d
Removing intermediate container cbd1cddb7c1d
 ---> 1288edc4cedf
Step 18/20 : EXPOSE $CallSignalingPort
 ---> Running in 0f39208ecd1d
Removing intermediate container 0f39208ecd1d
 ---> d3e9672a7c58
Step 19/20 : EXPOSE $CallSignalingPort2
 ---> Running in db0051825c9c
Removing intermediate container db0051825c9c
 ---> 1ffd1f3a836e
Step 20/20 : ENTRYPOINT [ "entrypoint.cmd" ]
 ---> Running in 3534e4f45cc9
Removing intermediate container 3534e4f45cc9
 ---> 08e17dfd3ff1
Successfully built 08e17dfd3ff1
Successfully tagged recordingbotregistry.azurecr.io/recordingbottutorial/application:latest
2024/04/22 13:26:57 Successfully executed container: build
2024/04/22 13:26:57 Executing step ID: push. Timeout(sec): 3600, Working directory: '', Network: ''
2024/04/22 13:26:57 Pushing image: recordingbotregistry.azurecr.io/recordingbottutorial/application:latest, attempt 1
The push refers to repository [recordingbotregistry.azurecr.io/recordingbottutorial/application]
b9617601bebb: Preparing
bcb99b7d948d: Preparing
8a7e2c66e2ef: Preparing
fca722bde849: Preparing
4f95d08eea8e: Preparing
e7f48d9387aa: Preparing
b4a29475681c: Preparing
f873e4575f3d: Preparing
a5ffa8236791: Preparing
937f4a68c6f2: Preparing
804723c997b5: Preparing
9ea5853ffacc: Preparing
f1f5d7dbc442: Preparing
058f8a7cd302: Preparing
e7f48d9387aa: Waiting
b4a29475681c: Waiting
f873e4575f3d: Waiting
a5ffa8236791: Waiting
937f4a68c6f2: Waiting
804723c997b5: Waiting
9ea5853ffacc: Waiting
f1f5d7dbc442: Waiting
058f8a7cd302: Waiting
bcb99b7d948d: Pushed
fca722bde849: Pushed
b4a29475681c: Pushed
b9617601bebb: Pushed
8a7e2c66e2ef: Pushed
a5ffa8236791: Pushed
937f4a68c6f2: Pushed
e7f48d9387aa: Pushed
9ea5853ffacc: Pushed
804723c997b5: Pushed
4f95d08eea8e: Pushed
f873e4575f3d: Pushed

f1f5d7dbc442: Pushed
058f8a7cd302: Pushed
latest: digest: sha256:425bde01b22d5b1829f9f79117e51ee9ca3ca822f7477a09a788f390a04379d0 size: 3258
2024/04/22 13:34:01 Successfully pushed image: recordingbotregistry.azurecr.io/recordingbottutorial/application:latest
2024/04/22 13:34:01 Step ID: build marked as successful (elapsed time in seconds: 986.312550)
2024/04/22 13:34:01 Populating digests for step ID: build...

2024/04/22 13:34:12 Successfully populated digests for step ID: build
2024/04/22 13:34:12 Step ID: push marked as successful (elapsed time in seconds: 423.445314)
2024/04/22 13:34:12 The following dependencies were found:
2024/04/22 13:34:12
- image:
    registry: recordingbotregistry.azurecr.io
    repository: recordingbottutorial/application
    tag: latest
    digest: sha256:425bde01b22d5b1829f9f79117e51ee9ca3ca822f7477a09a788f390a04379d0
  runtime-dependency:
    registry: mcr.microsoft.com
    repository: windows/server
    tag: ltsc2022
    digest: sha256:f2a7ad9732bdaf680bcadb270101f1908cf9969581b094c3279f1481eb181a71
  buildtime-dependency:
  - registry: mcr.microsoft.com
    repository: dotnet/sdk
    tag: 8.0-windowsservercore-ltsc2022
    digest: sha256:ce3009d6cb2c647ae0e1bb8cc984d643611ced83704b1c1f331178853e5d7e7d
  git: {}


Run ID: cb1 was successful after 24m5s
```

We now have a docker container in our registry and can continue with [creating and configuring a Bot Service](./4-bot-service.md).
