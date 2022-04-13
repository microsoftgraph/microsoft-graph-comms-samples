# Introduction
The sample demostrate an incident process workflow. When a incident raised (through a web API call to the bot), the bot will join a scheduled incident meeting, call incident responders, play audio prompt about the incident, and connect them to the incident meeting if they press "1". 

# Sample status
1. Other applications could call a web api to raise a incident process, with scheduled meeting information and incident resonders identities.
2. Bot join a scheduled meeting
3. Bot call incident responders
4. Play audio prompt message about the incident
5. Decode DTMF from incident responders
6. Connect the responders to incident meeting

# Getting Started
1.	Installation process
  * Enable an Azure subscription to host web sites and bot services. 
  * Install Visual Studio 2017
  * Launch IncidentBot.sln in <Repository>\RemoteMediaSamples with Visual Studio 2017 (VS2017)
  * Click menu Build/"Build Solution" to build the whole solution
  * Create web site in Azure
    - Right click IncidentBot/"Connected Services" and select "Add Connected Service" in project IncidentBot in VS2017, then select Publish tab, and click "Create new profile" to lauch a dialog
    - Select "App Service" then click "Create New" radio button, then click "Publish" button to create a App Service and publish the code on. 
    - Write down the web site root uri \{BotBaseUrl} for next steps.
  * Create an BotService in an Azure subscription with Azure Portal (https://portal.azure.com), then enable both Teams & Skype channels on the Azure portal, and configure the calling Uri of the bot. 
    - Go to "Bot Services" resource type page, click "Add", select "Bot Channels Registration", click "Create", then follow the instructions. 
      - Write down the application ID \{ApplicationId} and \{ApplicationSecret} for next steps. 
    - Click "IncidentBot" in "Bot Services" resource type page, Click "Channels", Then select "Microsoft Teams" and "Skype" channels and enable both of them.
    - Click "edit" button of "Skype" channel, click "Calling" tab, select "Enable calling" radio button, then select "IVR - 1:1 IVR audio calls", and fill the Webhook (for calling) edit box with value "\{BotBaseUrl}/callback/calling". 
  * Configure permissions for the Bot.
    - Go to Application Registration Portal (https://apps.dev.microsoft.com/).
    - Select your registered bot application.
    - Click "Add" under Microsoft Graph Permissions --> Application Permissions.
    - Select all permissions starting with "Calls.", i.e. "Calls.AccessMedia.All", "Calls.Initiate.All", etc.
    - Click "Ok" and then "Save"
  * Consent the permissions
    - Go to "https://login.microsoftonline.com/common/adminconsent?client_id=<app_id>&state=<any_number>&redirect_uri=<any_callback_url>"
    - Sign in with a tenant admin
    - Consent for the whole tenant.
  * Update the following elements in appsettings.json file in project IncidentBot.
    - Bot/AppId: "\{ApplicationId}"
    - Bot/AppSecret: "\{ApplicationSecret}"
    - Bot/BotBaseUrl: "\{BotBaseUrl}"
    - AzureAD/Domain: "\<reserved for later auth work, no need to change now>"
    - AzureAD/TenantId: "\<reserved for later auth work, no need to change now>"
    - AzureAD/AppId: "\<reserved for later auth work, no need to change now>"
    - AzureAD/AppSecret: "\<reserved for later auth work, no need to change now>"
  * Publish the application again. 
    - Right click IncidentBot/"Connected Services" and select "Add Connected Service" in project IncidentBot in VS2017, click "Publish" button.

2. Update process
  * Update code properly.
  * Publish the application again.

3.	Software dependencies
  * .NET Framework 471
  * Nuget packages list are in \<ProjectName>\Dependencies\Nuget in Solution Explorer of Visual Studio 2017
	
4.	Latest releases
  * version 0.2

5.	API references

# Automated VMSS deployment with Github Worflows (Optional)
## Deployment Script Pre-Reqs

* [PowerShell 7.0+](https://docs.microsoft.com/en-us/powershell/scripting/whats-new/what-s-new-in-powershell-70)
* [Azure Az PowerShell Module](https://docs.microsoft.com/en-us/powershell/azure/install-az-ps)
    * Install-Module -Name Az -Scope CurrentUser -Repository PSGallery -Force
* [GitHub CLI](https://cli.github.com/)
    * This is not a hard requirement, but will automate the step to save the secret in your repo.
* Must be an owner of the Azure subscription where you are deploying the infrastructure.
* Must have permissions to create an Azure AD Application.
* Note: The Azure Bot must be created in a tenant where you are an adminstrator because the bot permissions require admin consent. The bot infrastructure does not need to be in the same tenant where the Azure bot was created. This is useful if you are not an administrator in your tenant and you can use a separate tenant for the Azure Bot and Teams calling.

| Secret Name          | Message |
| -------------------- |:-------------|
| localadmin           | 'localadmin' is the username for the admin on the provisioned VMSS VMs. The password entered is the password to login and will be configured for all VMs. |
| AadAppId             | This is the Azure AD Application Client Id that was created when creating an Azure Bot. Refer to the [registration instructions](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html) |
| AadAppSecret         | Client Secret created for the Azure AD Application during the Azure Bot registration. |
| ServiceDNSName       | Your public domain that will be used to join the bot to a call (ie bot.example.com) |
<br/>

## Installation
1. Set up SSL certificate and upload to the cloud service
    1. Create a wildcard certificate for your service. This certificate should not be a self-signed certificate. For instance, if your bot is hosted at `bot.contoso.com`, create the certificate for `*.contoso.com`.
    2. Copy the path to the PFX certificate
        1. Install [OpenSSL](https://slproweb.com/products/Win32OpenSSL.html) to convert the certifcate from PEM to PFX
1. Navigate to the root directory of the sample in PowerShell.
1. Run `Get-AzContext` to ensure you are deploying to the correct subscription.
    1. You need to have the owner role on the subscription
    2. You need permissions to create a Service Principal
1. Run .\deploy.ps1 -OrgName <Your 2 - 7 Character Length Letter Abbreviation>
    1. - ie .\deploy.ps1 -OrgName TEB -Location eastus2
```powershell
    # Option 1 Run setup to deploy
    . .\deploy.ps1 -orgName <yourOrgName> -Location centralus
    # Option 1 Example
    . .\deploy.ps1 -orgName DNA -Location centralus
    
    # Option 2 Re-execute setup
    . .\deploy.ps1 -orgName <yourOrgName> -Location centralus -RunSetup
    # Option 2 Example
    . .\deploy.ps1 -orgName DNA -Location centralus -RunSetup
    
    # Option 3a Deploy from the commandline
    . .\deploy.ps1 -orgName <yourOrgName> -Location centralus -RunDeployment
    # Option 3a Example
    . .\deploy.ps1 -orgName DNA -Location centralus -RunDeployment
    # Option 3b Automated GitHub workflow
    The deployment will exectute via GitHub workflow
    - You can also manually run the 'BUILD' workflow to build the code
    - You can also manually run the 'INFRA' workflow after the previous workflow to deploy the infrastructure
```
## Script Internals:
The deployment script uses ADF (Azure Deployment Framework) under the path RemoteMediaSamples\IncidentBot\ADF. This folder has template and boilerplate code files 
that are used to define the github workflow actions and define the infrastructure resources required to deploy the bot and install it using DSC extensions. 

1. Create a resource group with the naming convention ACU1-TEB-BOT-RG-D1 (Region Abbreviation - Your Org Name - BOT - Resource Group - Environment)
2. Create a storage account
    - Grant current user the 'Storage Blob Data Contributor' role
    - Grant the service principal the 'Storage Blob Data Contributor' role
3. Create a Key Vault
    - And grant current user the 'Key Vault Administrator' role
4. Create an Azure AD Application
    - The Application will be granted the 'Owner' role to the subscription.
5. Crete a GitHub Secret wiht name AZURE_CREDENTIALS_<YOURORGNAME>_BOT
    ```json
    {
        "clientId": "<GitHub Service Principal Client Id>",
        "clientSecret": "<GitHub Service Principal Secret>",
        "tenantId": "<Tenant ID>",
        "subscriptionId": "<Subscription ID>",
        "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
        "resourceManagerEndpointUrl": "https://management.azure.com/",
        "activeDirectoryGraphResourceId": "https://graph.windows.net/",
        "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
        "galleryEndpointUrl": "https://gallery.azure.com/",
        "managementEndpointUrl": "https://management.core.windows.net/"
    }
    ```
5. Generate the deployment parameters file, build workflow and infrastructure workflow
6. Upload the PFX certificate to Key Vault
7. Add the secrets and environment variables to Key Vault

After the script runs successfully, you should see the following:
1. New resource group with the following resources:
    - Storage Account
    - Key Vault
2. Azure AD Application in Azure AD
3. In your GitHub Repo, Navigate to Settings > Secrets. You should see a new secret named 'AZURE_CREDENTIALS_<YOURORGNAME>_BOT'
4. Three new files have been created. Check these files in and push them to your repo.
    - app-build-<YourOrgName>.yml
    - app-infra-release-<YourOrgName>.yml
    - azuredeploy<YourOrgName>.parameters.json
5. Once these files have been pushed to your repo, they will kick of the infrastructure and code deployment workflows.

## Infrastructure Deployment 

The GitHub Action app-infra-release-<YourOrgName>.yml deploys the infrastructure.

You can also run the infrastructure deployment locally using the -RunDeployment flag.
```
.\deploy.ps1 -OrgName TEB -RunDeployment
```
## Update DNS

Your DNS Name for your bot needs to point to the public load balacer in order to call your bot and have it join a meeting.

1. Find the public IP resource for the load balancer and copy the DNS name.
2. Navigate to your DNS settings for your domain and create a new CNAME record.
    ie CNAME bot acu1-teb-bot-d1-lbplb01-1.eastus2.cloudapp.azure.com

## Deploy the Solution

The GitHub Action app-build-<YourOrgName>.yml builds the solution and uploads the output to the storage account. 
Once the infrastructure is deployed, DSC will pull the code from the storage account.

# Build and Test
1. Create a tenant in O365, with Teams enabled. 

2. Create two users in O365, with Teams enabled. 
  * Write down the users' object IDs as \{UserObjectId-1} for user1 and \{UserObjectId-2} for user2.

3. Install Teams client.

4. Login Teams with user1. Create a teams channel and add a meeting there. 
  * Meeting uri should be in format https://teams.microsoft.com/l/meetup-join/... 

5. If the meeting created is a VTC meeting and **{videoTeleconferenceId}** is provided in request body, then **{videoTeleconferenceId}** will be used as a replacement of **{joinURL}**.
  ![Test Meeting1](Images/TestMeeting1.png)

6. Use the postman to send request to "**{WebSiteRootUri}/incidents/raise**", with header "Content-Type:application/json" and the json content in body as below:
    ```json
    {
      "name": "<name-of-incident>",
      "time": "<start-time-of-the-incident-in-ISO-8601-format>",
      "tenantId": "{TenantId}",
      "objectIds": [
        "{UserObjectId-1}",
        "{UserObjectId-2}"
      ],
      "videoTeleconferenceId": "{VTC Conference ID}, alternative parameter",
      "joinURL": "https://teams.microsoft.com/l/meetup-join/..., alternative parameter",
      "removeFromDefaultRoutingGroup": true,
      "allowConversationWithoutHost": true
    }
    ```

6. user1 & user2 should get audio call, and bot should joined the schedule meeting. 

7. Choose user1 as a example(currently, user2's behaviors are same as user1's), when the call is picked up, user1 should be able to hear the audio prompt to notice an incident is occured, and press "1" or "0" for different actions.

8. If user1 pressed "1", an new invitation will be sent from the bot to invite user1 to join the incident meeting.
  * If Teams Client is in the P2P call panel layout with Incident Bot, user1 will join the Incident Meeting directly with "voice only" by default.
  * If Teams Client is in the menu layout with P2P call layout minimized on Top-left, user1 will received an invitation from Bot, with choices of "video" or "voice only" to join the incident meeting.

# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 



