# Introduction

The teams-recording-bot sample guides you through building, deploying and testing a Teams recording bot running within a container, deployed into Azure Kubernetes Services.

## Contents

Outline the file contents of the repository. It helps users navigate the codebase, build configuration and any related assets.

| File/folder       | Description                                |
|-------------------|--------------------------------------------|
| `build`           | Contains `Dockerfile` to containerise app. |
| `deploy`          | Helm chart and other manifests to deploy.  |
| `docs`            | Markdown files with steps and guides.      |
| `scripts`         | Helpful scripts for running project.       |
| `src`             | Sample source code.                        |
| `.gitignore`      | Define what to ignore at commit time.      |
| `CHANGELOG.md`    | List of changes to the sample.             |
| `CONTRIBUTING.md` | Guidelines for contributing to the sample. |
| `README.md`       | This README file.                          |
| `LICENSE`         | The license for the sample.                |

## Prerequisites

* Windows 10
* [Visual Studio 2019](https://visualstudio.microsoft.com/vs/)
* [Ngrok Pro subscription](https://ngrok.com/)
* [Azure Subscription](https://azure.microsoft.com/account/free)
* [Office 365 with admin rights](https://developer.microsoft.com/en-us/microsoft-365/dev-program)

**Optional:**

* [Docker](https://docs.docker.com/docker-for-windows/install/)
* [Helm](https://helm.sh/docs/intro/install/)
* [Skype for Business Online](https://www.microsoft.com/en-us/download/details.aspx?id=39366)
* [Postman](https://www.postman.com/)
* [OpenSSL](https://chocolatey.org/packages/OpenSSL.Light)

## Developer Setup

1. [Setting up Ngrok](docs/setup/ngrok.md)
2. [Generating SSL Certificate and setting up URL ACL and Certificate Bindings](docs/setup/certificate.md)
3. [Configuring Bot Channel Registration and Granting Permission](docs/setup/bot.md)
4. Optional - [Creating and Assigning Compliance Policy](docs/setup/policy.md)
5. Using the configurations from the steps above, copy [.env-template](src/RecordingBot.Console/.env-template) and create a new file called `.env` in the same location. Your `.env` should look something like this:

    ```
    AzureSettings__BotName=BOT_NAME
    AzureSettings__AadAppId=BOT_ID
    AzureSettings__AadAppSecret=BOT_SECRET
    AzureSettings__ServiceDnsName=RESERVED_DOMAIN ## e.g. contoso.ngrok.io
    AzureSettings__CertificateThumbprint=THUMBPRINT
    AzureSettings__InstancePublicPort=RESERVED_PORT ## TCP
    AzureSettings__CallSignalingPort=LOCALHOST_HTTP_PORT ## 9441
    AzureSettings__InstanceInternalPort=LOCALHOST_TCP_PORT ## 8445
    AzureSettings__PlaceCallEndpointUrl=https://graph.microsoft.com/v1.0
    AzureSettings__CaptureEvents=false
    AzureSettings__PodName=bot-0 ## the number defines the cluster pod number
    AzureSettings__MediaFolder=archive ## the default name of the folder containing all media files
    AzureSettings__EventsFolder=events ## the default name of the folder containing all event logs
    AzureSettings__TopicName=recordingbotevents ## the name of the EventGrid topic for the monitoring app events
    AzureSettings__TopicKey=XXXXXX ## the key secret for the EventGrid topic
    AzureSettings__RegionName= ## the Azure location, i.e. EastUS
    AzureSettings__IsStereo=false ## the indicator if the audio files should be saved in stereo, 2-channels mode
    AzureSettings__WAVSampleRate= ## when omitted, by default 16000 Hz, but this env variable can be used to resample the audio stream into a different sample bit rate, i.e. 44.1 KHz for mp3 files
    AzureSettings__WAVQuality=100 ## from 0 to 100%, when omitted, by default is 100%
    ```

>Note: You don't need to create another `.env` file for your Testing project. It's there for the CI/CD build pipeline.

6. Optional - Including publishing of Bot Events with Azure Event Grid
   * Create an [Event Grid custom topic in Azure Event Grid](https://docs.microsoft.com/en-us/azure/event-grid/scripts/event-grid-cli-create-custom-topic) in the CLI or Azure portal (to receive events) with the default `TOPIC_NAME` of `recordingbotevents`.
   Make a note of the Access key (`TOPIC_KEY`) and Location (`TOPIC_REGION`) for the newly created custom topic.
     
   * Specify the Event Azure Settings below in the relevant `.env` files (e.g. console app or tests)
      ```
      AzureSettings__TopicKey=TOPIC_KEY
      AzureSettings__TopicName=recordingbotevents
      AzureSettings__RegionName=TOPIC_REGION
      ```
    * Consider a stategy for event message consumption

7. Optional - Configure WAV Audio Format settings
By default, each second of audio stream received from a Microsoft Teams meeting is represented as 16,000 samples, or 16KHz, with each sample containing 16-bits of data over a single channel. This is perfect audio format that matches the requirements for number of applicable applications, such as Microsoft Azure Speech-to-text Cognitive Services. To keep these default settings, leave the `AzureSettings__WAVSampleRate` environment variable uninitialized, empty, and the Recording Bot will produce audio WAV files in its default WAV format, which is 16KHz, 16-bits, 1 channel (mono).
However, if you ever wanted to change the default settings, you may do so by modifying the environment variables in your `.env` file accordingly (see above each variable settings definition).

## Running the sample

The following are step-by-steps instructions to run the sample project.

### Prerequisites

If you are running the project locally, you will need Ngrok running to forward traffic from Teams to your local machine. Go through the following steps to get Ngrok up and running:

1. Create a new file called `ngrok.yaml` in the [scripts](scripts) folder.
2. Copy the contents of [ngrok.yaml-template](scripts/ngrok.yaml-template) over to `ngrok.yaml`.
3. Update `ngrok.yaml` with 
    ```
    <AUTH_TOKEN>: Your Ngrok authentication token.

    <YOUR_SUBDOMAIN>: The subdomain portion of your Ngrok reserved domain.
    For example: if your reserved domain is `bot.ngrok.io`, then this value would be `bot`.

    <CALL_SIGNALING_PORT>: LOCALHOST_HTTP_PORT
    For example: 9441

    <INSTANT_INTERNAL_PORT>: LOCALHOST_TCP_PORT
    For example 8445
    ```

Once you've done that, run [runngrok.bat](scripts/runngrok.bat) in command prompt and leave it running.

### Visual Studio

1. Launch Visual Studio 2019 in `Administrator Mode` and open [TeamsRecordingBot.sln](src/TeamsRecordingBot.sln).
2. Make sure `RecordingBot.Console` is set as the startup project.
3. In your `Solution Explorer`, right click on `Solution 'TeamsRecordingBot'...` and click `Restore NuGet Packages`.
4. Click `Debug` -> `Start Debugging`.

A console app will pop up and you should see the following:

```cmd
RecordingBot: booting
RecordingBot: running
```

Once you see `RecordingBot: running` you should now be able to interact with the bot and have it join meetings and calls. To verify the bot is working, create a new meeting in teams, copy the `joinURL` meeting link and fire up [Postman](https://www.postman.com/).

```curl
POST https://bot.ngrok.io/joinCall
Content-Type: application/json
{
    "JoinURL": "JOIN_URL",
    "DisplayName": "Bot"
}
```

**Note**: Specifying `DisplayName` means you can not access individual audio streams through `UnmixedAudioBuffers`. This is because when setting `DisplayName`, the bot is joining as a guest participant. Not setting `DisplayName` means the bot joins as a bot and as such, can access the individual audio streams through `UnmixedAudioBuffers`.

 You should get back a response looking something like this:

```json
{
    "callId": "e51f2c00-0420-44af-a977-88dc307d2346",
    "scenarioId": "bda643f2-4a8e-4dbb-beff-94bef8534279",
    "call": "bot.ngrok.io/calls/e51f2c00-0420-44af-a977-88dc307d2346"
}
```

If all was configured correctly, the bot should appear in the meeting as a participant with the name `Bot`.

### Docker

To build the image, make sure Docker is running and is set to `Windows Containers`. If you have WSL installed, you may have to switch Docker to run Windows containers.

To do this, right click on Docker in your system tray and click `Switch to Windows containers...`. Wait for Docker to restart before continuing.

1. To build the container, open a new powershell terminal and make sure you've changed directories to the root of this repository. If you are, run the following command:

    ```powershell
    docker build `
        --build-arg CallSignalingPort=<CALL_SIGNALING_PORT> `
        --build-arg CallSignalingPort2=<CALL_SIGNALING_PORT+1> `
        --build-arg InstanceInternalPort=<INSTANT_INTERNAL_PORT> `
        -f ./build/Dockerfile . `
        -t [TAG]
    ```

2. Before we can run the project, you need to extract your `certificate.pfx` you generated in [Setting up URL ACL and Certificate Bindings](docs/setup/certificate.md) into individual `.key` and `.cert` files. You'll need to make sure you have [OpenSSL](https://chocolatey.org/packages/OpenSSL.Light) installed. Currently [entrypount.cmd](scripts/entrypoint.cmd) does not check if a `certificate.pfx` exists and expects it has to generate it and add it to the container's certificate store.

    To extract your `certificate.pfx`, run the following command in powershell:

    ```powershell
    openssl pkcs12 -in certificate.pfx -nocerts -out tls-encrypted.key
    openssl pkcs12 -in certificate.pfx -clcerts -nokeys -out tls.crt
    openssl rsa -in tls-encrypted.key -out tls.key
    ```

3. Copy `tls.key` and `tls.crt` to a safe location.
4. Now with that, from the root of this repository, run the following command to run the bot:

    ```powershell
    docker run -it `
        --cpus 2.5 `
        --env-file .\src\RecordingBot.Console\.env `
        --mount type=bind,source=<CERTIFICATE_PATH>,target=C:\certs `
        --mount type=bind,source=<LOCAL_WAV_FILES_PATH>,target=<DOCKER_WAV_FILES_PATH> `
        -p 9441:<CALL_SIGNALING_PORT> `
        -p 9442:<CALL_SIGNALING_PORT+1> `
        -p 8445:<INSTANT_INTERNAL_PORT> `
        [TAG] powershell
    ```

    - Where, <DOCKER_WAV_FILES_PATH> is: `C:\Users\ContainerAdministrator\AppData\Local\Temp\teams-recording-bot`
    - Make sure you replace `CERTIFICATE_PATH` with the folder location of your `tls.cert` and `tls.key`.
    - The bot needs at least 2 CPU cores for it to run. We specify this with `--cpus 2.5`.

**Note**: You can also join the docker later if you'd like to retrieve the wav files in the docker container itself by running this command:

```powershell
docker exec -it <container_id> powershell
```

**IMPORTANT**:

5. If you're attaching to the existing docker instance, make sure to run `.\entrypoint.cmd`. You'll see a bunch of activity in your console but once you see `RecordingBot: running`, you're good to go. Make sure you have Ngrok running before trying to interact with the bot through teams.

## Key concepts

Provide users with more context on the tools and services used in the sample. Explain some of the code that is being used and how services interact with each other.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
