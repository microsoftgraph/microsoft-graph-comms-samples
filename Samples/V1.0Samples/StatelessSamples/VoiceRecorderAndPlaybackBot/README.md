# Introduction
The sample demostrate a recording audio by user and playing back to user workflow. 

# Sample status
1. The user will call bot and Bot will answer the call.
2. Bot plays audio prompt message, indicating it is ready to record.
3. Bot records what the user speaks, with maximum duration allowed 10 seconds.
4. Bot plays back the recorded message.
5. Bot hangs up the call.

# Getting Started 
1.	Installation process
  * Enable an Azure subscription to host web sites and bot services. 
  * Install Visual Studio 2017
  * Launch CommsSamples.sln in <Repository>\Samples with Visual Studio 2017 (VS2017)
  * Click menu Build/"Build Solution" to build the whole solution
  * Create an BotService in an Azure subscription with Azure Portal (https://portal.azure.com), then enable both Teams & Skype channels on the Azure portal, and configure the calling Uri of the bot. 
    - Go to "Bot Services" resource type page, click "Add", select "Bot Channels Registration", click "Create", then follow the instructions. 
      - Write down the application ID **\{ApplicationId}** and **\{ApplicationSecret}** for next steps. 
    - Click "VoiceRecorderAndPlaybackBot" in "Bot Services" resource type page, Click "Channels", Then select "Microsoft Teams" and "Skype" channels and enable both of them.
    - Click "edit" button of "Skype" channel, click "Calling" tab, select "Enable calling" radio button, then select "IVR - 1:1 IVR audio calls", and fill the Webhook (for calling) edit box with value "\{BotBaseUrl}/callback". 
  * Configure permissions for the Bot.
    - This Bot doesnt need any permissions
    
# Getting Started (Azure Version)
1.	Installation process
  * Create web site in Azure
    - Right click 1.0Samples/StatelessSamples/VoiceRecorderAndPlaybackBot/"Connected Services" and select "Add Connected Service" in project VoiceRecorderAndPlaybackBot in VS2017, then select Publish tab, and click "Create new profile" to lauch a dialog
    - Select "App Service" then click "Create New" radio button, then click "Publish" button to create a App Service and publish the code on. 
    - Write down the web site root uri **\{BotBaseUrl}** for next steps.
  
  * Update the following elements in appsettings.json file in project VoiceRecorderAndPlaybackBot.
    - Bot/AppId: "**\{ApplicationId}**"
    - Bot/AppSecret: "**\{ApplicationSecret}**"
    - Bot/BotBaseUrl: "**\{BotBaseUrl}**"

  * Publish the application again. 
    - Right click VoiceRecorderAndPlaybackBot/"Connected Services" and select "Add Connected Service" in project NotificationBot in VS2017, click "Publish" button.

2. Update process
  * Update code properly.
  * Publish the application again.

3.	Software dependencies
  * .NET Framework 471
  * Nuget packages list are in \<ProjectName>\Dependencies\Nuget in Solution Explorer of Visual Studio 2017
	
4.	Latest releases
  * version 0.2

5.	API references

# Getting Started (Local Run Version)
1.	Installation process
  * Install Visual Studio 2017
  * Launch CommsSamples.sln in <Repository>\Samples with Visual Studio 2017 (VS2017)
  * Click menu Build/"Build Solution" to build the whole solution
  * Setup ngrok.
    - Sign up for a free ngrok account. Once signed up, go to the ngrok [dashboard](https://dashboard.ngrok.com/) and get your auth token.
    - Create an ngrok configuration file `ngrok.yml` as follows:
        ```yaml
        authtoken: %replace_with_auth_token_from_dashboard%
        tunnels:
          signaling:
            addr: 9442
            proto: http
          media: 
            addr: 8445
            proto: tcp
        ```
    - Start ngrok: `ngrok http https://localhost:44379 -host-header=localhost`. You will see an output like this:
        ```ymal
        Session Status                online
        Account                       YourName (Plan: Free)
        Version                       x.x.xx
        Region                        United States (us)
        Web Interface                 http://127.0.0.1:4040
        Forwarding                    http://e6c2321a.ngrok.io -> https://localhost:44379
        Forwarding                    https://e6c2321a.ngrok.io -> https://localhost:44379
        ```
    - From **your** output, in line Forwarding (yours will be different) the first url`https://e6c2321a.ngrok.io` will be your bot base uri. Write down the bot base uri as **\{BotBaseUrl}** for next steps.
  * Update the following elements in appsettings.json file in project NotificationBot.
    - Bot/AppId: "**\{ApplicationId}**"
    - Bot/AppSecret: "**\{ApplicationSecret}**"
    - Bot/BotBaseUrl: "**\{BotBaseUrl}**"

# Build and Test
1. Create a tenant in O365, with Teams enabled. 

2. Create two users in O365, with Teams enabled. 
  * Write down the users' object IDs as \{UserObjectId-1} for user1 and \{UserObjectId-2} for user2 (optional).

3. Install Teams client.

4. Login to Teams

5. Install the VoiceRecorderAndPlaybackBot in the client using steps mentioned here: https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html#register-bot-in-microsoft-teams

6. user1 should call VoiceRecorderAndPlaybackBot using the Teams client, and bot should play prompt indicating it is ready to record the user's audio.
