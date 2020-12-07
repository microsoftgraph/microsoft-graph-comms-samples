# Introduction
    The sample demonstrates a playing a prompt with a simple IVR menu, subscribing to tones and call transfer to an agent.

# Sample status
    1. user calls an app belonging to an auto dealership.
    2. Bot answers the call.
    3. Bot plays a prompt saying Press 1 for sales, Press 2 for service, Press 3 for other questions.
    4. user selects 2 and gets transferred to an agent dealing with service. 


# Installation process 
    1. Enable an Azure subscription to host web sites and bot services. 
    2. Install Visual Studio 2017
    3. Launch SimpleIvrBot.sln in <Repository>\Samples\V1.0Samples\StatelessSamples with Visual Studio 2017 (VS2017)
    4. Click menu Build/"Build Solution" to build the whole solution
    5. Create an BotService in an Azure subscription with Azure Portal (https://portal.azure.com), then enable Teams channel on the Azure portal and configure the calling Uri of the bot. 
        * Go to "Bot Services" resource type page, click "Add", select "Bot Channels Registration", click "Create", then follow the instructions. 
            - Write down the application ID **\{ApplicationId}** and **\{ApplicationSecret}** for next steps. 
            - Type "SimpleIvrBot" in "Bot Handle" under "Bot Services", fill rest fields like Resource Group. Pricing Tier as per the requirement and click "Create".
        * Under "Bot Services" click "SimpleIvrBot", Click "Channels", Then select "Microsoft Teams" channel and enable it with next step.
            - Click "Calling" tab, select "Enable calling" check box, then fill the "Webhook (for calling)" edit box with value "\{BotBaseUrl}/callback". Click "Save".
     6. Configure permissions for the Bot. 
        * Go to the Azure Portal (https://portal.azure.com).
        * Select your registered bot application, in this case "SimpleIvrBot", click "Settings" --> "Manage" and then click  "API Permissions".
        * Click "Add a Permission", click "Microsoft Graph" --> Application Permissions.
        * Select all permissions starting with "Calls.", i.e. "Calls.AccessMedia.All", "Calls.Initiate.All", etc.
        * Click "Ok" and then "Save".
     7. Consent the permissions
        * Go to "https://login.microsoftonline.com/common/adminconsent?client_id=<app_id>&state=<any_number>&redirect_uri=<app_redirect_url>"
        * Sign in with a tenant admin
        * Consent for the whole tenant.
    
# Getting Started (Azure Version)
    1. Installation process
        * Create web site in Azure
            - Right click V1.0Samples/StatelessSamples/SimpleIvrBot/"Connected Services" and select "Add Connected Service" in project SimpleIvrBot in VS2017, then select Publish tab, and click "Create new profile" to lauch a dialog
            - Select "App Service" then click "Create New" radio button, then click "Publish" button to create a App Service and publish the code on. 
            - Write down the web site root uri **\{BotBaseUrl}** for next steps.
  
        * Update the following elements in appsettings.json file in project SimpleIvrBot.
            - Bot/AppId: "**\{ApplicationId}**"
            - Bot/AppSecret: "**\{ApplicationSecret}**"
            - Bot/BotBaseUrl: "**\{BotBaseUrl}**"

        * Publish the application again. 
            - Right click SimpleIvrBot/"Connected Services" and select "Add Connected Service" in project SimpleIvrBot in VS2017, click "Publish" button.

    2. Update process
        * Update code properly.
        * Publish the application again.

    3. Software dependencies
        * Nuget packages list are in \<ProjectName>\Dependencies\Nuget in Solution Explorer of Visual Studio 2017

    4. API references

# Getting Started (Local Run Version)
    1. Installation process
        * Install Visual Studio 2017
        * Launch SimpleIvrBot.sln in <Repository>\Samples\V1.0Samples\StatelessSamples with Visual Studio 2017 (VS2017)
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
        * Update the following elements in appsettings.json file in project SimpleIvrBot.
            - Bot/AppId: "**\{ApplicationId}**"
            - Bot/AppSecret: "**\{ApplicationSecret}**"
            - Bot/BotBaseUrl: "**\{BotBaseUrl}**"
	        - Bot/objectIds: {
		        "First":"**\{objectId-1}**"
		        "Second":"**\{objectId-2}**"
		        "Third":"**\{objectId-3}**"
                }

# Build and Test
    1. Login to Teams client.

    2. Install the SimpleIvrBot in the client using steps mentioned here: https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html#register-bot-in-microsoft-teams 

    3. Bot plays a prompt with an IVR menu, and let the caller select a menu option using DTMF tones, then transfer the call to the selected menu option, as mentioned below.

    4. On pressing key 1, call will be transferred to objectId-1, on 2 to objectId-2 and for any other key press it will transfer to objectId-3.