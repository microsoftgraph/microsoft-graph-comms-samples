> **Note:**  
> Public Samples are provided by developers from the Microsoft Graph community.  
> Public Samples are not official Microsoft Communication samples, and not supported by the Microsoft Communication engineering team. It is recommended that you contact the sample owner before using code from Public Samples in production systems.

---
# Teams Voice Echo Bot

**Description:** This sample application shows how to work with the stream of data from the audio socket in a Teams meeting. When the Bot is added to a meeting it will echo everything that is said (in the speaker's voice). If you decide to use the Cognitive Services mode, then the bot will use Cognitive services to convert the Speech-To-Text and then convert the Text-To-Speech and you will hear the echo in a Bot's voice. This sample comes with automated pipelines that can deploy and configure the bot on the virtual machines with Virtual Machine Scale Sets (VMSS).
**Authors:** [@bcage29](https://github.com/bcage29) and [@brwilkinson](https://github.com/brwilkinson)

---

### Table of Contents
- **[Introduction](#introduction)**<br>
    - **[Echo Mode](#echo-mode)**<br>
    - **[Cognitive Services Mode](#cognitive-services-mode)**<br>
- **[Getting Started](#getting-started)**<br>
    - **[Create a PFX Certificate](#create-a-pfx-certificate)**<br>
- **[Bot Registration](#bot-registration)**<br>
- **[Prerequisites](#prerequisites)**<br>
    - **[General](#general)**<br>
    - **[Setup Script](#setup-script)**<br>
- **[Deploy](#deploy)**<br>
    - **[PowerShell DSC](#powershell-dsc)**<br>
    - **[Deploy the Prerequistes](#deploy-the-prerequistes)**<br>
    - **[Deploy the Infrastructure](#deploy-the-infrastructure)**<br>
        - **[Update DNS](#update-dns)**<br>
    - **[Deploy the Solution](#deploy-the-solution)**<br>
- **[Running the Sample](#running-the-sample)**<br>
<br/>

# Introduction

The Teams Voice Echo Bot is a sample demonstrating how to use the audio stream from a Teams call or Meeting. The sample also includes scripts and pipelines to deploy the infrastructure and code to run the Bot in Azure on VMSS.

Once you joined a meeting, you can request that your bot joins the meeting (through a custom Web API call to the bot or some other trigger). Depending on the mode set during deployment, the bot will either echo every sound or it will use Cognitive Services to convert the speech to text and then convert the text back to speech in the voice of the bot. Refer to the supported languages on the [Cognitive Services Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/overview)

## Echo Mode

This is the default mode when deployed (UseCognitiveServices == false). In this mode, the bot will listen to the inbound audio stream and will send the same stream of data back on the Audio Socket. This will create an echo and you will hear yourself repeated.

## Cognitive Services Mode

This is the secondary mode to demonstrate how to use the audio stream from a meeting and process the data. This sample takes the audio stream, uses Cognitive Services to do Speech to Text and then Text to Speech, the response is a stream that is sent back on the audio socket. In this mode, the bot does not constantly echo, but it listens for a simple keyword. Once it hears the keyword, it will start listening to what you want to echo. Depending on the language you set in the settings, it will listen and talk in that language.

To use Cognitive Services mode, set the following environment variables:
```json
"UseCognitiveServices": true,
"SpeechConfigKey": "", // key for your cognitive services
"SpeechConfigRegion": "eastus2", // region where your cognitive services service is deployed
"BotLanguage": "en-US", // es-MX, fr-FR
```

## Getting Started

* Clone the Git repo for the Microsoft Graph Calling API Samples. Please see the instructions [here](https://docs.microsoft.com/en-us/vsts/git/tutorial/clone?view=vsts&tabs=visual-studio) to get started with VSTS Git. 
* Log in to your Azure subscription to host web sites and bot services. 
* Launch Visual Studio Code or open a terminal to the root folder of the sample.
* Fork the repo or clone it and then push it to your own repo in GitHub.

### Create a PFX Certificate

The Bot requires an SSL certificate signed by a Certificate Authority. If you don't have a certificate for your domain, you can create a free SSL certificate.

1. Verify you have access to make DNS changes to your domain or buy a new domain.
2. Install [certbot](https://certbot.eff.org/instructions?ws=other&os=windows)
    - a. Follow the installation instructions
    - b. If 'certbot' command is not recognized in the terminal, add the path to the certbot.exe to the environment variables path ($env:Path)
3. Open a terminal as an Adminstrator where certbot is loaded
4. Execute
```
certbot certonly --manual --preferred-challenges=dns -d *.example.com
```
5. This will create a wildcard certificate for example.com.
6. Follow the instructions and add the TXT record to your domain
7. This will create PEM certificates and the default location is 'C:\Certbot\live\example.com'
8. Install [OpenSSL](https://slproweb.com/products/Win32OpenSSL.html) to convert the certifcate from PEM to PFX
9. Execute
```
openssl pkcs12 -export -out C:\Certbot\live\example.com\star_example_com.pfx -inkey C:\Certbot\live\example.com\privkey.pem -in C:\Certbot\live\example.com\cert.pem
```
10. Copy the path to the PFX certificate "C:\Certbot\live\example.com\star_example_com.pfx

## Bot Registration

1. Follow the instructions [Register a Calling Bot](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html). Take a note of the registered config values (Bot Id, MicrosoftAppId and MicrosoftAppPassword). You will need these values in the code sample config.

1. Add the following Application Permissions to the bot:

    * Calls.AccessMedia.All
    * Calls.JoinGroupCall.All

1. The permissions need to be consented by tenant admin. Go to "https://login.microsoftonline.com/common/adminconsent?client_id=<app_id>&state=<any_number>&redirect_uri=<any_callback_url>" using tenant admin to sign-in, then consent for the whole tenant.

## Prerequisites

### General

* Visual Studio (only needed if running locally). You can download the community version [here](http://www.visualstudio.com) for free.
* PowerShell 7.0+
* Mirosoft Azure Subscription (If you do not already have a subscription, you can register for a <a href="https://azure.microsoft.com/en-us/free/" target="_blank">free account</a>)
* An Office 365 tenant enabled for Microsoft Teams, with at least two user accounts enabled for the Calls Tab in Microsoft Teams (Check [here](https://docs.microsoft.com/en-us/microsoftteams/configuring-teams-calling-quickstartguide) for details on how to enable users for the Calls Tab)
* Install .Net Framework 4.7.1.  The solution will not build if you do not install this.
* You will need Postman, Fiddler, or an equivalent installed to formulate HTTP requests and inspect the responses.  The following tools are widely used in web development, but if you are familiar with another tool, the instructions in this sample should still apply.
    + [Postman desktop app](https://www.getpostman.com/)
    + [Telerik Fiddler](http://www.telerik.com/fiddler)

### Setup Script

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
| UseCognitiveServices | True or False setting to set the bot in Echo mode or Cognitive Services mode. If 'true', the following secrets need to be set. |
| SpeechConfigKey      | The Cognitive Services service Key |
| SpeechConfigRegion   | The region where the Cognitive Service is deployed |
| BotLanguage          | The language that you want your bot to understand (ie, en-US, es-MX, fr-FR) |
<br/>

## Deploy

### PowerShell DSC

PowerShell Desired State Configuration (DSC) enables you to manage your IT development infrastructure with configuration as code. This sample uses DSC to configure the VMs to run the Teams Voice Echo Bot. Here are a few examples of where we are using DSC:
- Set environment variables on the VM
- Install software
- Install the windows service

DSC Resources
- https://docs.microsoft.com/en-us/powershell/scripting/dsc/overview?view=powershell-5.1
- https://docs.microsoft.com/en-us/azure/virtual-machines/extensions/dsc-overview
- https://github.com/dsccommunity

### Deploy the Prerequistes

1. Navigate to the root directory of the sample in PowerShell.
3. Run `Get-AzContext` to ensure you are deploying to the correct subscription.
    - You need to have the owner role on the subscription
    - You need permissions to create a Service Principal
2. Run .\deploy.ps1 -OrgName <Your 2 - 7 Character Length Letter Abbreviation>
    - ie .\deploy.ps1 -OrgName TEB -Location eastus2
```powershell
    # Option 1. Execute all pre-req steps i.e. run setup to deploy
    . .\deploy.ps1 -orgName <yourOrgName> -Location centralus
    # E.g.
    . .\deploy.ps1 -orgName DNA -Location centralus
    
    # Option 2. After you have run setup the first time, re-execute setup
    . .\deploy.ps1 -orgName <yourOrgName> -Location centralus -RunSetup
    # E.g.
    . .\deploy.ps1 -orgName DNA -Location centralus -RunSetup
    
    # Option 3a. After you have run setup you can deploy from the commandline
    . .\deploy.ps1 -orgName <yourOrgName> -Location centralus -RunDeployment
    # E.g.
    . .\deploy.ps1 -orgName DNA -Location centralus -RunDeployment

    # Option 3b. Alternatively skip 4a, check your code changes in and push to your repo
    # The deployment will exectute via GitHub workflow instead
    - You can manually run the 'BUILD' workflow to build the code
    - You can manually run the 'INFRA' workflow after the previous workflow to deploy the infrastructure
```

This script will do the following:
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

### Deploy the Infrastructure

The GitHub Action app-infra-release-<YourOrgName>.yml deploys the infrastructure.

You can also run the infrastructure deployment locally using the -RunDeployment flag.
```
.\deploy.ps1 -OrgName TEB -RunDeployment
```

#### Update DNS
Your DNS Name for your bot needs to point to the public load balacer in order to call your bot and have it join a meeting.

1. Find the public IP resource for the load balancer and copy the DNS name.
2. Navigate to your DNS settings for your domain and create a new CNAME record.
    ie CNAME bot acu1-teb-bot-d1-lbplb01-1.eastus2.cloudapp.azure.com

### Deploy the Solution

The GitHub Action app-build-<YourOrgName>.yml builds the solution and uploads the output to the storage account. Once the infrastructure is deployed, DSC will pull the code from the storage account.

## Running the Sample 
Once your Bot is successfully deployed and running, you will need to send a POST request to trigger join the bot to a meeting.  The POST request will contain a few key pieces of data to tell the bot what meeting to join.

* Teams Meeting Information

    * Log into the Microsoft Teams client (this can be the web client https://teams.microsoft.com).
    * Create a meeting and join the meeting. 
    * Open this meeting in Teams, and right click the "Join Microsoft Teams Meeting" and copy the meeting hyperlink
    * Meeting uri should be in format https://teams.microsoft.com/l/meetup-join/{ThreadId}/{ThreadMessageId}?oid:{OrganizerObjectId}&tid:{TenantId}. 
    * Copy the Meeting URL.
    * Join the meeting.

* Use Postman or Fiddler to send the following POST request to your DNS name, ie bot.example.com/joinCall", with header "Content-Type:application/json" and the json content in body as below:

```json
{
    "joinURL": "https://teams.microsoft.com/l/meetup-join/...",
}
```

* Here is a sample curl request to join the bot to the meeting.
```c
curl --location --request POST 'https://bot.example.com/joinCall' --header 'Content-Type: application/json' --data-raw '{ "joinURL": "https://teams.microsoft.com/l/meetup-join/..." }'
```

Your request should receive a 200 OK response.  

## Local Testing
Refer to the Microsft Graph Documentation on (Local Testing)[https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/Testing.html]

Note: The certificate is used by the MediaPlatformInstanceSettings and needs to match the ServiceFqdn property of that class.

### Example: Using a custom domain with ngrok

- Domain: example.com
- Certificate: *.contoso.com
- ServiceDnsName: bot.contoso.com
- MediaDnsName: tcp.contoso.com
- MediaInstanceExternalPort: 12332

#### DNS Entries
| Type | Name | Value |
| -------------------- |:-------------|:-------------|
| CNAME | bot | ra8sxx2z.cname.us.ngrok.io. |
| CNAME | tcp | 1.tcp.ngrok.io. |

#### ngrok config
```yml
authtoken: <yourAuthToken>
tunnels:
  bot-signaling:
    proto: http
    addr: "9442"
    hostname: bot.contoso.com
  bot-media:
    proto: tcp
    addr: 8445
    remote_addr: 1.tcp.ngrok.io:12332
```

#### curl request
```c
curl --location --request POST 'https://bot.contoso.com/joinCall' --header 'Content-Type: application/json' --data-raw '{ "joinURL": "https://teams.microsoft.com/l/meetup-join/..." }'
```

### Example: Using an ngrok subdomain with multi-level subdomain certificate

- Domain: contoso.com
- Certificate: *.bot.contoso.com
- ServiceDnsName: bot.contoso.com
- MediaDnsName: 5.bot.contoso.com
- MediaInstanceExternalPort: 12332

#### DNS Entries
| Type | Name | Value |
| -------------------- |:-------------|:-------------|
| CNAME | 5.bot | 5.tcp.ngrok.io. |

#### ngrok config
```yml
authtoken: <yourAuthToken>
tunnels:
  bot-signaling:
    proto: http
    addr: "9442"
    hostname: signal.ngrok.io
  bot-media:
    proto: tcp
    addr: 8445
    remote_addr: 5.tcp.ngrok.io:12332
```

#### curl request
```c
curl --location --request POST 'https://signal.ngrok.io/joinCall' --header 'Content-Type: application/json' --data-raw '{ "joinURL": "https://teams.microsoft.com/l/meetup-join/..." }'
```