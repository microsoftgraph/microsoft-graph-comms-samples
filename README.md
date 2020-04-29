# Microsoft Graph Communications API and Samples

The Microsoft Graph Communications API allows developers to programmatically interact with Microsoft's Communications Platform, which also powers Microsoft Teams, to create amazing experiences and products. Check out our samples in this repo to understand the capabilities of these APIs.

## Get started

- View the **[changelog](changelog.md)**
- **[Review the documentation](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/)** to understand the concepts behind using our SDK (which is also used by the samples).

## Prerequisites

* Visual Studio. You can download the community version [here](http://www.visualstudio.com) for free.
* Mirosoft Azure Subscription (If you do not already have a subscription, you can register for a <a href="https://azure.microsoft.com/en-us/free/" target="_blank">free account</a>)
* An Office 365 tenant enabled for Microsoft Teams, with at least two user accounts enabled for the Calls Tab in Microsoft Teams (Check [here](https://docs.microsoft.com/en-us/microsoftteams/configuring-teams-calling-quickstartguide) for details on how to enable users for the Calls Tab)
* [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
* [Azure PowerShell](https://docs.microsoft.com/en-us/powershell/azure/install-azurerm-ps?view=azurermps-6.8.1)
* Install .Net Framework 4.7.1. Some samples will not build if you do not install this.
* You will need Postman, Fiddler, or an equivalent installed to formulate HTTP requests and inspect the responses.  The following tools are widely used in web development, but if you are familiar with another tool, the instructions in this sample should still apply.
    + [Postman desktop app](https://www.getpostman.com/)
    + [Telerik Fiddler](http://www.telerik.com/fiddler)

* [ngrok](https://ngrok.com/)

# Samples

<B>The Graph Calling Bot Samples are split into 2 </B>
categories: Remote Media Bot Samples and Local Media Bot Samples.

## Local media samples

Local media samples give the developer direct access to the inbound and outbound media streams.  

### [AudioVideoPlaybackBot](Samples\V1.0Samples\LocalMediaSamples\AudioVideoPlaybackBot\README.md)

The AudioVideoPlaybackBot demostrates several features of local media scenarios:
- Plays a movie in multiple resolutions as the main video output feed.
- Listens to dominant speaker events and subscribes to inbound video feeds of those participants.
- Allows switching between screen viewing sharer and viewer, and publishes video through the screen sharing socket.

[code:](Samples\V1.0Samples\LocalMediaSamples\AudioVideoPlaybackBot.sln)

### [HueBot](Samples\V1.0Samples\LocalMediaSamples\HueBot\README.md)

The HueBot demonstrates local media scenarios.
- Listens to dominant speaker events and changes the hue color of the dominant speaker video.

[code:](Samples\V1.0Samples\LocalMediaSamples\HueBot.sln)

### [Policy Recording bot sample](Samples\V1.0Samples\LocalMediaSamples\PolicyRecordingBot\README.md)
This sample demonstrates how a bot can receive media streams for recording. Please note that the sample does not actually record. This logic is left up to the developer.The system will load the bot and join it to appropriate calls and meetings in order for the bot to enforce compliance with the administrative set policy.

[code:](Samples\V1.0Samples\LocalMediaSamples\PolicyRecordingBot.sln)

## Remote Media Graph Calling Bot Samples

### [Incident Bot Sample](Samples\V1.0Samples\RemoteMediaSamples\README.md)

The Incident Bot sample is a Remote Media sample demonstrating a simple incident process workflow started by a Calling Bot.  When an incident is raised (through a custom Web API call to the bot or some other trigger), the bot will join a pre-existing Teams meeting, voice call incident responder team members via Microsoft Teams, play an audio prompt about the incident, and connect users to the incident meeting after having the callers press "1". The whole process is kicked off by a Web API call that passes the scheduled Teams meeting information and incident responders Azure AD Identities. The sample also supports incoming direct voice calls to the bot.

[code:](Samples\V1.0Samples\RemoteMediaSamples\IncidentBot.sln)

## Stateless Samples

### [Online Meeting Stateless Sample](Samples\BetaSamples\StatelessSamples\OnlineMeetingSamples\README.md)

The online meeting stateless sample demonstrates how one can consume Microsoft.Graph.Communications.Client in bot application to
- Get an online meeting based on on [vtcid](https://docs.microsoft.com/en-us/microsoftteams/cloud-video-interop)).
- Create a online meeting on behalf a user (delegated auth) in your tenant.

[code:](Samples\BetaSamples\StatelessSamples\OnlineMeetingsSample.sln)

### [Simple IVR Bot](Samples\V1.0Samples\StatelessSamples\SimpleIvrBot\README.md)
The sample demonstrates a playing a prompt with a simple IVR menu, subscribing to tones and call transfer to an agent.

[code:](Samples\V1.0Samples\StatelessSamples\SimpleIvrBot.sln)

### [Voice Recorder And Playback Bot](Samples\V1.0Samples\StatelessSamples\VoiceRecorderAndPlaybackBot\README.md)

The sample demostrate a recording audio by user and playing back to user workflow. 

[code:](Samples\V1.0Samples\StatelessSamples\VoiceRecorderAndPlaybackBot.sln)

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
