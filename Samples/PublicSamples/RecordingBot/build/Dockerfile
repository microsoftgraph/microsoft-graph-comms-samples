FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

ARG CallSignalingPort=9441
ARG CallSignalingPort2=9442
ARG InstanceInternalPort=8445

COPY /src /src

WORKDIR /src/RecordingBot.Console
RUN dotnet build RecordingBot.Console.csproj --arch x64 --self-contained --configuration Release --output C:\app 


FROM mcr.microsoft.com/windows/server:ltsc2022
SHELL ["powershell", "-Command"]

ADD https://aka.ms/vs/17/release/vc_redist.x64.exe /bot/VC_redist.x64.exe

COPY /scripts/entrypoint.cmd /bot
COPY /scripts/halt_termination.ps1 /bot
COPY --from=build /app /bot

WORKDIR /bot

RUN Set-ExecutionPolicy Bypass -Scope Process -Force; \
    [System.Net.ServicePointManager]::SecurityProtocol = \
        [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; \
        iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))

RUN choco install openssl.light -y

EXPOSE $InstanceInternalPort
EXPOSE $CallSignalingPort
EXPOSE $CallSignalingPort2

ENTRYPOINT [ "entrypoint.cmd" ]
