# Create and configure Bot Service

Let us now create an App Registration with the application permission to access the calls and the
media streams of the calls. After that we can create a Bot Service, link our App Registration,
and configure the notification URL.

## Create Azure App Registration

To create the App Registration we run:

> [!NOTE]  
> The App Registration we create here is a multi tenant app registration, also see [this](https://learn.microsoft.com/de-de/cli/azure/ad/app?view=azure-cli-latest#az-ad-app-create-optional-parameters) for reference.

```powershell
az ad app create
    --display-name recordingbotregistration
    --sign-in-audience AzureADMultipleOrgs
    --key-type Password
```

The output should look similar to:

```json
{
  "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#applications/$entity",
  "addIns": [],
  "api": {
    "acceptMappedClaims": null,
    "knownClientApplications": [],
    "oauth2PermissionScopes": [],
    "preAuthorizedApplications": [],
    "requestedAccessTokenVersion": null
  },
  "appId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
  "appRoles": [],
  "applicationTemplateId": null,
  "certification": null,
  "createdDateTime": "2024-04-23T09:52:05.4291387Z",
  "defaultRedirectUri": null,
  "deletedDateTime": null,
  "description": null,
  "disabledByMicrosoftStatus": null,
  "displayName": "recordingbotregistration",
  "groupMembershipClaims": null,
  "id": "bbe622a5-6cc4-41f1-a682-1368751b8029",
  "identifierUris": [],
  "info": {
    "logoUrl": null,
    "marketingUrl": null,
    "privacyStatementUrl": null,
    "supportUrl": null,
    "termsOfServiceUrl": null
  },
  "isDeviceOnlyAuthSupported": null,
  "isFallbackPublicClient": null,
  "keyCredentials": [],
  "notes": null,
  "optionalClaims": null,
  "parentalControlSettings": {
    "countriesBlockedForMinors": [],
    "legalAgeGroupRule": "Allow"
  },
  "passwordCredentials": [],
  "publicClient": {
    "redirectUris": []
  },
  "publisherDomain": "lm-ag.de",
  "requestSignatureVerification": null,
  "requiredResourceAccess": [],
  "samlMetadataUrl": null,
  "serviceManagementReference": null,
  "servicePrincipalLockConfiguration": null,
  "signInAudience": "AzureADMultipleOrgs",
  "spa": {
    "redirectUris": []
  },
  "tags": [],
  "tokenEncryptionKeyId": null,
  "uniqueName": null,
  "verifiedPublisher": {
    "addedDateTime": null,
    "displayName": null,
    "verifiedPublisherId": null
  },
  "web": {
    "homePageUrl": null,
    "implicitGrantSettings": {
      "enableAccessTokenIssuance": false,
      "enableIdTokenIssuance": false
    },
    "logoutUrl": null,
    "redirectUriSettings": [],
    "redirectUris": []
  }
}
```

Now it is very important that you write down the value of your _appId_-field as this is the
App Registration Id, in the example output this value is: `cccccccc-cccc-cccc-cccc-cccccccccccc`

### Add Graph API application permission

Next we need to add the application permission that are required for the recording bot application
to our App Registration. The Permissions and the API of the App Registration are referenced by IDs
as we use Micrsoft Graph API the API Id is `00000003-0000-0000-c000-000000000000`(as this is the
App Registration Id of the Microsoft Graph API), for the permission ids we use the
[docs by microsoft](https://learn.microsoft.com/en-us/graph/permissions-reference) for reference.

The three permissions we add for our recording bot are:

- _Calls.AccessMedia.All_ : a7a681dc-756e-4909-b988-f160edc6655f
- _Calls.JoinGroupCall.All_ : f6b49018-60ab-4f81-83bd-22caeabfed2d
- _Calls.JoinGroupCallAsGuest.All_ : fd7ccf6b-3d28-418b-9701-cd10f5cd2fd4

```powershell
az ad app permission add
    --id cccccccc-cccc-cccc-cccc-cccccccccccc
    --api 00000003-0000-0000-c000-000000000000
    --api-permissions a7a681dc-756e-4909-b988-f160edc6655f=Role f6b49018-60ab-4f81-83bd-22caeabfed2d=Role fd7ccf6b-3d28-418b-9701-cd10f5cd2fd4=Role
```

The output the command should look similar to:

```text
Invoking `az ad app permission grant --id cccccccc-cccc-cccc-cccc-cccccccccccc --api 00000003-0000-0000-c000-000000000000` is needed to make the change effective
```

### Grant application permssion to tenant

For the application permissions to take effect, we have to grant the application permissions to our tenant:

```powershell
az ad app permission admin-consent
    --id cccccccc-cccc-cccc-cccc-cccccccccccc
```

If the command run successfully, we shouldn't see any output text in our console.

### Create App Secret

Next we create an App Secret for our App Registration. The bot application will uses this secret to
authenticate. The secret we generate will be valid for 1 year, after that we have to create a new secret.

```powershell
az ad app credential reset 
    --id cccccccc-cccc-cccc-cccc-cccccccccccc
    --years 1
    --query "password"
```

The output will look similar to:

```text
The output includes credentials that you must protect. Be sure that you do not include these credentials in your code or check the credentials into your source control. For more information, see https://aka.ms/azadsp-cli
"abcdefghijklmnopqrstuvwxyz"
```

The text in the quotation marks is the App Secret, we will store the secret later in a special
store in the AKS cluster. Handle this App Secret carefully, like it is your own password.

## Create Azure Bot Service

Since we have created and configured the App Registration we can continue with creating the Bot Service.

> [!NOTE]  
> As we created a multi tenant App Registration earlier we will also create the Bot Service as
> multi tenant, also see [this](https://learn.microsoft.com/en-us/cli/azure/bot?view=azure-cli-latest#az-bot-create-required-parameters) for reference.

```powershell
az bot create
    --name recordingbotservice
    --resource-group recordingbottutorial
    --appid cccccccc-cccc-cccc-cccc-cccccccccccc
    --app-type MultiTenant
    --location global
    --subscription "recordingbotsubscription"
```

The result of the command should look similar to:

```json
Resource provider 'Microsoft.BotService' used by this operation is not registered. We are registering for you.
Registration succeeded.
{
  "etag": "\"d71e2695-aaf4-4fe5-b1a5-81610d867c35\"",
  "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourceGroups/recordingbottutorial/providers/Microsoft.BotService/botServices/recordingbotservice",
  "kind": "azurebot",
  "location": "global",
  "name": "recordingbotservice",
  "properties": {
    "allSettings": null,
    "appPasswordHint": null,
    "cmekEncryptionStatus": "Off",
    "cmekKeyVaultUrl": null,
    "configuredChannels": [
      "webchat",
      "directline"
    ],
    "description": null,
    "developerAppInsightKey": null,
    "developerAppInsightsApiKey": null,
    "developerAppInsightsApplicationId": null,
    "disableLocalAuth": false,
    "displayName": "recordingbotservice",
    "enabledChannels": [
      "webchat",
      "directline"
    ],
    "endpoint": "",
    "endpointVersion": "3.0",
    "iconUrl": "https://docs.botframework.com/static/devportal/client/images/bot-framework-default.png",
    "isCmekEnabled": false,
    "isDeveloperAppInsightsApiKeySet": false,
    "isStreamingSupported": false,
    "luisAppIds": [],
    "luisKey": null,
    "manifestUrl": null,
    "migrationToken": null,
    "msaAppId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
    "msaAppMsiResourceId": null,
    "msaAppTenantId": null,
    "msaAppType": "MultiTenant",
    "openWithHint": null,
    "parameters": null,
    "privateEndpointConnections": null,
    "provisioningState": "Succeeded",
    "publicNetworkAccess": "Enabled",
    "publishingCredentials": null,
    "schemaTransformationVersion": "1.3",
    "storageResourceId": null,
    "tenantId": "99999999-9999-9999-9999-999999999999"
  },
  "resourceGroup": "recordingbottutorial",
  "sku": {
    "name": "F0",
    "tier": null
  },
  "tags": {},
  "type": "Microsoft.BotService/botServices",
  "zones": []
}
```

### Add notification URL to Bot Service

Next let us configure the notification URL of the recording bot. Even though we have not deployed
our recording bot yet, we already know the DNS name and the path and port we want to have the notifications on.

> [!NOTE]  
> As you might have noticed aready, we now need the
> fully quialified domain name that we created earlier for our AKS cluster.

```powershell
az bot msteams create 
    --name recordingbotservice
    --resource-group recordingbottutorial
    --enable-calling
    --calling-web-hook https://recordingbottutorial.westeurope.cloudapp.azure.com/api/calling
    --subscription "recordingbotsubscription"
```

The result should look similar to:

```json
Command group 'bot msteams' is in preview and under development. Reference and support levels: https://aka.ms/CLI_refstatus
{
  "etag": "W/\"911970bd-5993-471a-9f4a-cb55321f1710/23/2024 11:17:15 AM\"",
  "id": "/subscriptions/yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyyy/resourceGroups/recordingbottutorial/providers/Microsoft.BotService/botServices/recordingbotservice/channels/MsTeamsChannel",
  "kind": null,
  "location": "global",
  "name": "recordingbotservice/MsTeamsChannel",
  "properties": {
    "channelName": "MsTeamsChannel",
    "etag": "W/\"911970bd-5993-471a-9f4a-cb55321f1710/23/2024 11:17:15 AM\"",
    "location": "global",
    "properties": {
      "acceptedTerms": null,
      "callingWebHook": null,
      "callingWebhook": "https://recordingbottutorial.westeurope.cloudapp.azure.com/api/calling",
      "deploymentEnvironment": "CommercialDeployment",
      "enableCalling": true,
      "incomingCallRoute": null,
      "isEnabled": true
    },
    "provisioningState": "Succeeded"
  },
  "resourceGroup": "recordingbottutorial",
  "sku": null,
  "tags": null,
  "type": "Microsoft.BotService/botServices/channels",
  "zones": []
}
```

In the next step we will [deploy the sample recording bot application to our AKS cluster](5-helm.md).
