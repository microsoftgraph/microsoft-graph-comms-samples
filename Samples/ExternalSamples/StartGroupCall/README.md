# Microsoft Teams StartGroupCall
This repository is for developers who want to know..
- How to start Microsoft Teams group call from your app
- How to let user join existing Microsoft Teams online meeting by your app

## Background
[Microsoft Teams](https://products.office.com/en-us/microsoft-teams/group-chat-software) is popular collaboration tool. Users can chat, call and having online meeting with their colleagues.
Developers want to integrate their application with Teams. For example, their app triggers to start new group call with specific members. Thus users can collaborate well even if developers don't need to implement their own online meeting features and infrastructure.

## What you can see with this repo
A user can get group call from your app.

![demo](./document/demo.png)

# Technical consideration
We uses following language and tools. As prerequistics, please read and try each tools tutorial.

## Architecture
![Architecture](./document/Arc.png)

## Language, SDK and utilities
### Programming language
- C# with .Net Core 3.1: Because we can utilize dependecy injection on Azure Functions and Azure SDKs, we picked up C# for this sample.

### Tools for back-end application
- [Azure Functions](https://azure.microsoft.com/en-us/services/functions/): Serverless platform to run your code. In this repository, we simply implement without database for keeping sample simple.

### Tools for Teams call
- [Microsoft Graph](https://developer.microsoft.com/en-us/graph/): You can utilize Micorosft Graph to utilize Microsoft 365 back-end. For example, you can fetch users' email, calendar.. etc. In this repository, we utilize it to integrate Microsoft Teams.
- [Azure Active Directory](https://azure.microsoft.com/en-us/services/active-directory/): This is identitiy platform. For utilizing Microsoft Graph, you need to utilize it for making secure connection between your app and Microsoft Graph (Microsoft Teams).
- [Azure Bot Service](https://azure.microsoft.com/en-us/services/bot-service/): Microsoft graph need bot to start Teams Call.

### Authorization flow
- [OAuth 2.0 client credential flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow): Because our app is worker/deamon type service and can't have user interaction, we need to utilize client credential flow to fetch access token for Microsoft Graph.

# How to setup environment and run application
1. Create your Microsoft 365 environment with [developer program](https://developer.microsoft.com/en-us/microsoft-365/dev-program).
1. Create 2 - 3 users in the Microsoft 365 environment.
1. Register app in you Azure AD and memo following information by referring [microsoft document](https://docs.microsoft.com/en-us/graph/auth-v2-service)
   - Client Id
   - Client Secret
   - Tenant Id
1. Setup the app permissions in Azure AD [(detailed permissions list)](./document/Permissions.md)
1. Install [Visual Studio Code](https://code.visualstudio.com/)
1. Clone repository and open with Visual Studio Code. If you get recommendation to install dependencies (extension and cli), please install them.
1. Copy `local.settings.sample.json` and paste as `local.settings.json` in same path.
1. Update `local.settings.json` with copied `Client Id` and `Client Secret` values.
1. Setup Azure Bot Service and enable Teams feature. [(document)](https://docs.microsoft.com/en-us/microsoftteams/platform/bots/calls-and-meetings/registering-calling-bot)
1. Setup [ngrok](https://ngrok.com/) for accepting webhook from Microsoft Graph. After you installed ngrok, please run it by `ngrok http 7071`. Then, you can see https forwarding url. Please copy the **https** url and paste it as value of `CallBackUrl` in `local.settings.json`.
1. Press [F5] key to run Azure Functions locally.

# How to call API
We recommend you to install Microsoft Teams mobile app in your phone and sign-in with your Microsoft 365 developer program account.

## Start group call with specific users
App can call this API endpoint when app want to start group call with specific users.

HTTP POST http://localhost:7071/api/calls

BODY
```json
{
	"TenantId": "tenant id",
	"ParticipantEmails": [
		"email address",
      "email address"
	]
}
```

### Sample request
HTTP POST http://localhost:7071/api/calls

BODY
```json
{
	"TenantId": "b21a0d16-4e90-4cdb-a05b-ad3846369881",
	"ParticipantEmails": [
		"masota@masotadev.onmicrosoft.com",
        "AdeleV@masotadev.onmicrosoft.com"
	]
}
```

## Start call with meeting attendees/Join existing online meeting
App can call this API for
- Start group call with meeting attendees
- Join existing online meeting if the meeting is set up with online meeting

HTTP POST http://localhost:7071/api/calls/{meeting id}

Body
```json
{
	"TenantId":"tenant id",
	"MeetingId":"meetin id which a user want to join",
	"UserEmail":  "user email who want to join call"
}
```

### Sample request
HTTP POST http://localhost:7071/api/calls/AAMkADViZGQxZWY4LWQ4YzUtNDRhOS04OTQyLWU1NWI5N2JkOWU0ZQBGAAAAAABlonO6N9eNRYv3Fm0mCU2XBwAMGKVZFv8rR4rhKgq_-6brAAAAAAENAAAMGKVZFv8rR4rhKgq_-6brAAAyaeaJAAA=

Body
```json
{
	"TenantId":"b21a0d16-4e90-4cdb-a05b-ad3846369881",
	"MeetingId":"AAMkADViZGQxZWY4LWQ4YzUtNDRhOS04OTQyLWU1NWI5N2JkOWU0ZQBGAAAAAABlonO6N9eNRYv3Fm0mCU2XBwAMGKVZFv8rR4rhKgq_-6brAAAAAAENAAAMGKVZFv8rR4rhKgq_-6brAAAyaeaJAAA=",
	"UserEmail":  "masota@masotadev.onmicrosoft.com"
}
```


# How to run test
In this section, we assume you've finished previous section to setup dev environment.
1. Open project with Visual Studio Code
1. Ctrl+Shift+P(windows)/Command+ShiftP(Mac) to show command pallet.
1. Select `Tasks:Run test tasks` to run test

You can see test results and coverage in the VS Code terminal. If you've installed [Coverage Gutters](https://marketplace.visualstudio.com/items?itemName=ryanluker.vscode-coverage-gutters) extension, you can see code covered inline by clicking [Watch] button.

# Consideration for productions
- For using App secret securely, you may want to use [Azure Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/general/overview).
- For multi-tenant usage, you need to register your app as Multitenant application and let Aministrator in customer tenant grant app by [Admin consent flow](https://docs.microsoft.com/en-us/graph/auth-v2-service#3-get-administrator-consent).
