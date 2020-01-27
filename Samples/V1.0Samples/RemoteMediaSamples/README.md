# Getting Started with the Remote Media Graph Calling Bot Samples

This topic will provide information on running the Graph Calling Bot Samples.

## Prerequisites

* Visual Studio. You can download the community version [here](http://www.visualstudio.com) for free.
* Mirosoft Azure Subscription (If you do not already have a subscription, you can register for a <a href="https://azure.microsoft.com/en-us/free/" target="_blank">free account</a>)
* An Office 365 tenant enabled for Microsoft Teams, with at least two user accounts enabled for the Calls Tab in Microsoft Teams (Check [here](https://docs.microsoft.com/en-us/microsoftteams/configuring-teams-calling-quickstartguide) for details on how to enable users for the Calls Tab)
* Install .Net Framework 4.7.1.  The solution will not build if you do not install this.
* You will need Postman, Fiddler, or an equivalent installed to formulate HTTP requests and inspect the responses.  The following tools are widely used in web development, but if you are familiar with another tool, the instructions in this sample should still apply.
    + [Postman desktop app](https://www.getpostman.com/)
    + [Telerik Fiddler](http://www.telerik.com/fiddler)

## Introduction

The Graph Calling Bot Samples are split into 2 categories: Remote Media Bot Samples and Local Media Bot Samples.

## Remote Media Bot Samples

### Incident Bot Sample

The Incident Bot sample is a Remote Media sample demonstrating a simple incident process workflow started by a Calling Bot.  When an incident is raised (through a custom Web API call to the bot or some other trigger), the bot will join a pre-existing Teams meeting, voice call incident responder team members via Microsoft Teams, play an audio prompt about the incident, and connect users to the incident meeting after having the callers press "1". The whole process is kicked off by a Web API call that passes the scheduled Teams meeting information and incident responders Azure AD Identities. The sample also supports incoming direct voice calls to the bot.

#### Getting Started

* Clone the Git repo for the Microsoft Graph Calling API Samples. Please see the instructions [here](https://docs.microsoft.com/en-us/vsts/git/tutorial/clone?view=vsts&tabs=visual-studio) to get started with VSTS Git. 
* Log in to your Azure subscription to host web sites and bot services. 
* Install Visual Studio and launch IncidentBot.sln in <Repository>\RemoteMediaSamples with Visual Studio

#### Bot Registration

1. Follow the instructions [Register a Calling Bot](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html). Take a note of the registered config values (Bot Id, MicrosoftAppId and MicrosoftAppPassword). You will need these values in the code sample config.

1. Add the following Application Permissions to the bot:

    * Calls.AccessMedia.All
    * Calls.Initiate.All
    * Calls.JoinGroupCall.All
    * Calls.JoinGroupCallAsGuest.All

1. The permissions need to be consented by tenant admin. Go to "https://login.microsoftonline.com/common/adminconsent?client_id=<app_id>&state=<any_number>&redirect_uri=<any_callback_url>" using tenant admin to sign-in, then consent for the whole tenant.

#### Deploying the Sample

* Open and Build IncidentBot.sln
* Right click on IncidentBot > Connected Services and select "Add Connected Service".
* Next, select Publish tab, and follow instructions for "Publish your app to Azure or another host".
* At the next screen, select "App Service" then click "Create New" radio button, then click "Publish" button to create an App Service and publish the code.
    >Provide a valid subscription, resource group and hosting plan so you can successfully create the App service. 
* Make a note of the Website Url where the app service is published for later steps. Eg. http://yourapp.azurewebsites.net.  Your Website URL should match the root domain of the Webhook (for calling) configured for your bot during registration.  If not, you will need to update the Webhook URL in the Bot Registration to match your Website domain.

#### Configuring the Sample Settings for your bot

* Update the appsettings.json file in the project IncidentBot with the following values:
    * MicrosoftAppId: Obtained from Application Settings during registration. 
    * MicrosoftAppPassword: Obtained from Application Settings during registration. 
    * Webhook (for calling): Configured in the Bot Registration Eg. `https://{your domain}/callback/calling`, the root of this URL should match the URL of your App Service and updated in the BotBaseURL field below.  If necessary, return to the Bot Framework Portal to update the Calling Webhook URL to the URL of your App Service.

Example appsettings.json
```json
  "Bot": {
    "AppId": "00000000-0000-0000-0000-000000000000",  --> The MicrosoftAppId/BotId from above
    "AppSecret": "__placeholder__", --> The MicrosoftAppPassword from above
    "PlaceCallEndpointUrl": "https://graph.microsoft.com/beta", --> This is the Microsoft Graph entry point. Please keep the default value without any changes.
    "BotBaseUrl": "http://contosobot.azurewebsites.net/"  --> The BotBaseUrl is where the App Service is published
  }
```
* Build and Publish the solution again: 
    * Right click IncidentBot/"Connected Services" and select "Add Connected Service" in the project IncidentBot in Visual Studio, and click the "Publish" button.


#### Running the Sample 

Once your App Service is published and running, you will need to send a POST request to trigger the incident process workflow.  The POST request will contain a few key pieces of data to tell the bot what meeting to join and to which users to place an outbound call.

* Teams Meeting Information

    * Log in your User 1 and User 2 into the Microsoft Teams client.
    * From User 1 or 2's Teams client, create a Channel and add a Teams meeting to this Channel. 
    * Open this meeting in Teams, and right click the "Join Microsoft Teams Meeting" and copy the meeting hyperlink
    * Meeting uri should be in format https://teams.microsoft.com/l/meetup-join/{ThreadId}/{ThreadMessageId}?oid:{OrganizerObjectId}&tid:{TenantId}. 
    * Copy the Meeting URL. 

* Teams User ObjectID Information

    * This sample will place calls to users based on their Azure AD identity.  There are many ways to retreive the AAD Object ID of specific users in an Office 365 tenant.  A couple examples:
        * You can log into Azure portal and access Azure Active Directory to search for users by name.
        * You can use the Microsoft Graph or Azure Active Directory APIs for retreving users, to [look up a user in the same tenant by UserPrincipalName](https://developer.microsoft.com/en-us/graph/docs/api-reference/beta/api/user_get)

* Use Postman or Fiddler to send the following POST request to "\{WebSiteRootUri\}/incidents/raise", with header "Content-Type:application/json" and the json content in body as below:

```json
{
    "name": "<name-of-incident>", --> can be any string value
    "time": "<start-time-of-the-incident>", --> must be valid C# DateTime string
  
    "tenantId": "{TenantId}", --> TenantID of the users your bot will be calling
    "objectIds": [
        "{UserObjectId-1}",
        "{UserObjectId-2}"
    ],

    "joinURL": "https://teams.microsoft.com/l/meetup-join/...",
    "removeFromDefaultRoutingGroup": true,
    "allowConversationWithoutHost": true
}
```

Your request should receive a 200 OK response.  User 1 and User 2 will receive an audio call in the Microsoft Teams client.  The Incident Bot will also join the scheduled meeting.

When the audio call is answered by a user, the user will hear an audio prompt to notify an incident has occured, and the user will have the ability to press "1" or "0" DTMF tone input for different actions. If the user presses "1", a new invitation will be sent from the bot to invite the user to join the incident Teams meeting.  The user can click "video" or "voice only" on the incoming invite to get joined to the incident Teams meeting.
