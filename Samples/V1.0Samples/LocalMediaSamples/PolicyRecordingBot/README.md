# Introduction

## Note

The system will load the bot and join it to appropriate calls and meetings in order for the bot to enforce compliance with the administrative set policy.

## About

The Policy Recording bot sample guides you through building, deploying and testing a bot. This sample demonstrates how a bot can receive media streams for recording. Please note that the sample does not actually record. This logic is left up to the developer.

## Getting Started

This section walks you through the process of deploying and testing the sample bot.

### Bot Registration

1. Follow the steps in [Register Calling Bot](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html). Save the bot name, bot app id and bot secret for configuration.
    * For the calling webhook, by default the notification will go to https://{your domain}/api/calling. This is configured with the `CallSignalingRoutePrefix` in [HttpRouteConstants.cs](https://github.com/microsoftgraph/microsoft-graph-comms-samples/blob/master/Samples/BetaSamples/LocalMediaSamples/PolicyRecordingBot/FrontEnd/Http/Controllers/HttpRouteConstants.cs).
    * Ignore the "Register bot in Microsoft Teams" section as the Policy Recording bot won't be called directly. These bots are related to the policies discussed below, and are "attached" to users, and will be automatically invited to the call.

1. Add the following Application Permissions to the bot:

    * Calls.AccessMedia.All
    * Calls.JoinGroupCall.All
   
1. The permission needs to be consented by tenant admin. Go to "https://login.microsoftonline.com/common/adminconsent?client_id=<app_id>&state=<any_number>&redirect_uri=<any_callback_url>" using tenant admin to sign-in, then consent for the whole tenant.

### Create an Application Instance

Open powershell (in admin mode) and run the following commands. When prompted for authentication, login with the tenant admin.
  * `> Import-Module SkypeOnlineConnector`
  * `> $userCredential = Get-Credential`
  * `> $sfbSession = New-CsOnlineSession -Credential $userCredential -Verbose`
  * `> Import-PSSession $sfbSession`
  * `> New-CsOnlineApplicationInstance -UserPrincipalName <upn@contoso.com> -DisplayName <displayName> -ApplicationId <your_botappId>`
  * `> Sync-CsOnlineApplicationInstance -ObjectId <objectId>`

### Create a Recording Policy
Requires the application instance ID created above. Continue your powershell session and run the following commands.
  * `> New-CsTeamsComplianceRecordingPolicy -Enabled $true -Description "Test policy created by <yourName>" <policyIdentity>`
  * ```> Set-CsTeamsComplianceRecordingPolicy -Identity <policyIdentity> -ComplianceRecordingApplications ` @(New-CsTeamsComplianceRecordingApplication -Parent <policyIdentity> -Id <objectId>)```

After 30-60 seconds, the policy should show up. To verify your policy was created correctly:
  * `> Get-CsTeamsComplianceRecordingPolicy <policyIdentity>`

### Assign the Recording Policy
Requries the policy identity created above. Contine your powershell session and run the following commands.
  * `> Grant-CsTeamsComplianceRecordingPolicy -Identity <userUnderPolicy@contoso.com> -PolicyName <policyIdentity>`

To verify your policy was assigned correctly:
  * `> Get-CsOnlineUser <userUnderPolicy@contoso.com> | ft sipaddress, tenantid, TeamsComplianceRecordingPolicy`

### Prerequisites

* Install the prerequisites:
    * [Visual Studio 2017+](https://visualstudio.microsoft.com/downloads/)
    * [PostMan](https://chrome.google.com/webstore/detail/postman/fhbjgbiflinjbdggehcddcbncdddomop)

### Deploy

1. Create a cloud service (classic) in Azure. Get your "Site URL" from Azure portal, this will be your DNS name and CN name for later configuration, for example: `bot.contoso.com`.

2. Set up SSL certificate and upload to the cloud service
    1. Create a wildcard certificate for your service. This certificate should not be a self-signed certificate. For instance, if your bot is hosted at `bot.contoso.com`, create the certificate for `*.contoso.com`.
    2. Upload the certificate to the cloud service.
    3. Copy the thumbprint for later.

3. Set up cloud service configuration
    1. Open powershell, go to the folder that contains file `configure_cloud.ps1`. The file is in the `Samples` directory.

    2. Run the powershell script with parameters:
        `> .\configure_cloud.ps1 -p {path to project} -dns {your DNS name} -cn {your CN name, should be the same as your DNS name} -thumb {your certificate thumbprint} -bid {your bot name} -aid {your bot app id} -as {your bot secret}`
        
        For example:
        
         `.\configure_cloud.ps1 -p .\V1.0Samples\LocalMediaSamples\PolicyRecordingBot\ -dns bot.contoso.com -cn bot.contoso.com -thumb ABC0000000000000000000000000000000000CBA -bid bot -aid 3853f935-2c6f-43d7-859d-6e8f83b519ae -as 123456!@#$%^`

4. Publish the bot from VS:
    1. Right click PolicyRecordingBot, then click `Publish...`. Publish it to the cloud service you created earlier.

### Test

1. Set up the test meeting and test clients:
   1. Sign in to Teams client with a non-recorded test tenant user.
   2. Use another Teams client to sign in with the recorded user. You could use an private browser window and open up https://teams.microsoft.com. If the call notification doesn't appear on web, use the Teams desktop client.

2. Place a call from the Teams client with the non-recorded user to the recorded user.

3. Your recording bot should receive the incoming call and join the call immediately. Use the recorded users' Teams client to accept the call. Once the P2P call is established, you'll see a banner indicating that the recording has started. See the next step to learn how you can retrieve the call log.
     ![Test PrBot1](Images/TestPrBot1.png)

3. Interact with your service, _adjusting the service URL appropriately_.
    1. Get diagnostics data from the bot. Open the url https://bot.contoso.com/calls in a browser for auto-refresh. Search for the most recent CallId and replace with it in the below url.
       * Active calls: https://bot.contoso.com/calls/{CallId}
       * Service logs: https://bot.contoso.com/logs

    2. By default, the call will be terminated when the recording status has failed. You can terminate the call through `DELETE`, as needed for testing. Replace the call id `311a0a00-53d9-4a42-aa78-c10a9ae95213` below with your call id from the first response.

        ##### Request
        ```json
            DELETE https://bot.contoso.com/calls/311a0a00-53d9-4a42-aa78-c10a9ae95213
        ```
