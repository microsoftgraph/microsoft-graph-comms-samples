# Introduction
The sample demostrates calling a group of users. 

# Sample status
    1. A team of engineers use the Group Call Bot to call all of them at once, if any issue needing their attention occurs.
    2. An incident occurs and the Group Call Bot creates a group call to all the members of the team.
    3. The Group Call Bot uses the participants update notification to keep track of the participants of the call.
    4. The incident is handled and the call ends.

# Installation process
    1. Enable an Azure subscription to host web sites and bot services. 
    2. Install Visual Studio 2017
    3. Launch GroupCallBot.sln in <Repository>\Samples\V1.0Samples\StatelessSamples with Visual Studio 2017 (VS2017)
    4. Click menu Build/"Build Solution" to build the whole solution
    5. Create an BotService in an Azure subscription with Azure Portal (https://portal.azure.com), then enable both Teams & Skype channels on the Azure portal, and configure the calling Uri of the bot. 
        * Go to "Bot Services" resource type page, click "Add", select "Bot Channels Registration", click "Create", then follow the instructions. 
            - Write down the application ID **\{ApplicationId}** and **\{ApplicationSecret}** for next steps. 
            - Type "GroupCallBot" in "Bot Handle" under "Bot Services", fill rest fields like Resource Group. Pricing Tier as per the requirement and click "Create".
        * Under "Bot Services" click "GroupCallBot", Click "Channels", Then select "Microsoft Teams" channel and enable it with next step.
            - Click "Calling" tab, select "Enable calling" check box, then fill the "Webhook (for calling)" edit box with value "\{BotBaseUrl}/callback". Click "Save".
    6. Configure permissions for the Bot. 
        * Go to the Azure Portal (https://portal.azure.com).
        * Select your registered bot application, in this case "GroupCallBot", click "Settings" --> "Manage" and then click  "API Permissions".
        * Click "Add a Permission", click "Microsoft Graph" --> Application Permissions.
        * Select all permissions starting with "Calls.", i.e. "Calls.IntiateGroupCalls.All" etc.
        * Click "Ok" and then "Save".
    7. Consent the permissions
        * Go to "https://login.microsoftonline.com/common/adminconsent?client_id=<app_id>&state=<any_number>&redirect_uri=<app_redirect_url>"
        * Sign in with a tenant admin
        * Consent for the whole tenant.
    
# Getting Started (Azure Version)
    1. Installation process
        * Create web site in Azure
            - Right click V1.0Samples/StatelessSamples/GroupCallBot/"Connected Services" and select "Add Connected Service" in project GroupCallBot in VS2017, then select Publish tab, and click "Create new profile" to lauch a dialog
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

    3. Software dependencies
        * Nuget packages list are in \<ProjectName>\Dependencies\Nuget in Solution Explorer of Visual Studio 2017

    4. API references

# Getting Started (Local Run Version)
    1. Installation process
        * Install Visual Studio 2017
        * Launch GroupCallBot.sln in <Repository>\Samples\V1.0Samples\StatelessSamples with Visual Studio 2017 (VS2017)
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
    1. Get the users' object IDs as \{UserObjectId-1} for user1, \{UserObjectId-2} for user2 and \{UserObjectId-3} for user3, to all the group call needs to be made. 

    2. Login to Teams client with all the list of users, for the scenario in order to get call from the Bot.

    3. Open "Postman", create a Post request --> {BotBaseUrl}/participantscalling/raise and then add objectIds and tenantId in Body (like mentioned below). Click on Send.
        {  
            "objectIds": [
                "{UserObjectId-1}",
                "{UserObjectId-2}",
                "{UserObjectId-3}"
            ],
            "tenantId": "{TenantId}"
        }

    4. Bot will call all the users.

    5. Logs will have the list of participants, updated as and when user will accept the group call.