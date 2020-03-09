# Introduction
The sample demostrates calling a group of users. 

# Sample status
1. A team of engineers use the Group Call Bot to call all of them at once, if any issue needing their attention occurs.
2. An incident occurs and the Group Call Bot creates a group call to all the members of the team.
3. The Group Call Bot uses the participants update notification to keep track of the participants of the call.
4. The incident is handled and the call ends.

# Getting Started 
1.	Installation process
  * Enable an Azure subscription to host web sites and bot services. 
  * Install Visual Studio 2017
  * Launch CommsSamples.sln in <Repository>\Samples with Visual Studio 2017 (VS2017)
  * Click menu Build/"Build Solution" to build the whole solution
  * Create an BotService in an Azure subscription with Azure Portal (https://portal.azure.com), then enable both Teams & Skype channels on the Azure portal, and configure the calling Uri of the bot. 
    - Go to "Bot Services" resource type page, click "Add", select "Bot Channels Registration", click "Create", then follow the instructions. 
      - Write down the application ID **\{ApplicationId}** and **\{ApplicationSecret}** for next steps. 
    - Type "GroupCallBot" in "Bot Services" resource type page, Click "Channels", Then select "Microsoft Teams" and "Skype" channels and enable both of them.
    - Click "edit" button of "Skype" channel, click "Calling" tab, select "Enable calling" radio button, then select "IVR - 1:1 IVR audio calls", and fill the Webhook (for calling) edit box with value "\{BotBaseUrl}/callback". 
  * Configure permissions for the Bot.
    - Go to Application Registration Portal (https://apps.dev.microsoft.com/).
    - Select your registered bot application.
    - Click "Add" under Microsoft Graph Permissions --> Application Permissions.
    - Select all permissions starting with "Calls.", i.e. "Calls.IntiateGroupCalls.All" etc.
    - Click "Ok" and then "Save"
  * Consent the permissions
    - Go to "https://login.microsoftonline.com/common/adminconsent?client_id=<app_id>&state=<any_number>&redirect_uri=<app_redirect_url>"
    - Sign in with a tenant admin
    - Consent for the whole tenant.
    
# Getting Started (Azure Version)
1.	Installation process
  * Create web site in Azure
    - Right click 1.0Samples/StatelessSamples/GroupCallBot/"Connected Services" and select "Add Connected Service" in project GroupCallBot in VS2017, then select Publish tab, and click "Create new profile" to lauch a dialog
    - Select "App Service" then click "Create New" radio button, then click "Publish" button to create a App Service and publish the code on. 
    - Write down the web site root uri **\{BotBaseUrl}** for next steps.
  
  * Update the following elements in appsettings.json file in project GroupCallBot.
    - Bot/AppId: "**\{ApplicationId}**"
    - Bot/AppSecret: "**\{ApplicationSecret}**"
    - Bot/BotBaseUrl: "**\{BotBaseUrl}**"

  * Publish the application again. 
    - Right click GroupCallBot/"Connected Services" and select "Add Connected Service" in project GroupCallBot in VS2017, click "Publish" button.

2. Update process
  * Update code properly.
  * Publish the application again.

3.	Software dependencies
  * .NET Framework 471
  * Nuget packages list are in \<ProjectName>\Dependencies\Nuget in Solution Explorer of Visual Studio 2017

4.	API references

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
  * Update the following elements in appsettings.json file in project GroupCallBot.
    - Bot/AppId: "**\{ApplicationId}**"
    - Bot/AppSecret: "**\{ApplicationSecret}**"
    - Bot/BotBaseUrl: "**\{BotBaseUrl}**"

# Build and Test
1. Create a tenant in O365, with Teams enabled. 

2. Create multiple users in O365, with Teams enabled.  

3. Install Teams client.

4. Login to Teams with all the list of users, for the scenario in order to get call from the Bot.

5. Install the GroupCallBot in the client using steps mentioned here: https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html#register-bot-in-microsoft-teams

6. Bot will call all the users.

7. Logs will have list of participants, updated as and when user will accept the group call.