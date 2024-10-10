
# Introduction

## Note

The system will load the bot and join it to appropriate calls and meetings in order for the bot to enforce compliance with the administrative set policy. This sample is only designed for compliance recording scenarios. Do not use it for any other scenarios.

This sample should be used only for Org Regulated recording instead of other recording purposes. Otherwise, it might block the user calling experience. More details can be found [here](https://learn.microsoft.com/en-us/MicrosoftTeams/teams-recording-policy).

## About

The Policy Recording bot sample guides you through building, deploying, and testing a bot. This sample demonstrates how a bot can receive media streams for recording. Note that the sample does not actually record; this logic is left up to the developer.

# Getting Started

This section walks you through the process of deploying and testing the sample bot.

## Bot Registration

1. Follow the steps in [Register Calling Bot](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html). Save the bot name, bot app ID, and bot secret for configuration.
    - For the calling webhook, by default, the notification will go to `https://{your domain}/api/calling`. This is configured with the `CallSignalingRoutePrefix` in [HttpRouteConstants.cs](FrontEnd/Http/Controllers/HttpRouteConstants.cs).
    - Ignore the "Register bot in Microsoft Teams" section, as the Policy Recording bot won’t be called directly. These bots are "attached" to users and will be automatically invited to the call.

2. Add the following Application Permissions to the bot:
    - `Calls.AccessMedia.All`
    - `Calls.JoinGroupCall.All`
   
3. The permission needs to be consented by a tenant admin. Go to `https://login.microsoftonline.com/common/adminconsent?client_id=<app_id>&state=<any_number>&redirect_uri=<any_callback_url>` using tenant admin credentials to sign in and consent for the entire tenant.

## Create an Application Instance

1. Open PowerShell (in admin mode) and run the following commands. When prompted for authentication, log in with the tenant admin:

   ```powershell
   Import-Module MicrosoftTeams
   Connect-MicrosoftTeams 
   New-CsOnlineApplicationInstance -UserPrincipalName <upn@contoso.com> -DisplayName <displayName> -ApplicationId <your_botappId>
   Sync-CsOnlineApplicationInstance -ObjectId <objectId>
   ```

## Create a Recording Policy

1. With the application instance ID created above, continue your PowerShell session and run the following commands:

   ```powershell
   New-CsTeamsComplianceRecordingPolicy -Enabled $true -Description "Test policy created by <yourName>" <policyIdentity>
   Set-CsTeamsComplianceRecordingPolicy -Identity <policyIdentity> -ComplianceRecordingApplications @(New-CsTeamsComplianceRecordingApplication -Parent <policyIdentity> -Id <objectId>)
   ```

2. After 30-60 seconds, the policy should be created. Verify it with:

   ```powershell
   Get-CsTeamsComplianceRecordingPolicy <policyIdentity>
   ```

## Assign the Recording Policy

1. With the policy identity created above, continue your PowerShell session and run the following commands:

   ```powershell
   Grant-CsTeamsComplianceRecordingPolicy -Identity <userUnderPolicy@contoso.com> -PolicyName <policyIdentity>
   ```

2. To verify the policy assignment, use:

   ```powershell
   Get-CsOnlineUser <userUnderPolicy@contoso.com> | ft sipaddress, tenantid, TeamsComplianceRecordingPolicy
   ```

## Prerequisites

- Install the following prerequisites:
  - [Visual Studio 2017+](https://visualstudio.microsoft.com/downloads/)
  - [PostMan](https://chrome.google.com/webstore/detail/postman/fhbjgbiflinjbdggehcddcbncdddomop)

## Deploy

### Step 1: Securely Store Certificates with Azure Key Vault 

1. Certificates are crucial for securing communication between your services. Azure Key Vault is used to store and manage these certificates securely. Follow [this guide](https://learn.microsoft.com/en-us/azure/key-vault/general/quick-create-portal) to create an Azure Key Vault.

### Step 2: Obtain and Configure Your SSL Certificate 

1. Obtain a wildcard SSL certificate for your domain (e.g., `*.contoso.com`).
2. Upload your SSL certificate to the Azure Key Vault following [these steps](https://learn.microsoft.com/en-us/azure/key-vault/certificates/tutorial-import-certificate?tabs=azure-portal).
3. Copy the certificate thumbprint from Azure Key Vault and update the `.cscfg` and `.csdef` files:

   - In `.cscfg`:

     ```xml
     <Certificates>
       <Certificate name="MySSLCertificate" thumbprint="YOUR_THUMBPRINT" thumbprintAlgorithm="sha1" />
     </Certificates>
     ```

   - In `.csdef`:

     ```xml
     <Certificates>
       <Certificate name="YourCertificateName" storeLocation="LocalMachine" storeName="My" />
     </Certificates>
     ```

### Step 3: Define Your Virtual Network

1. For Azure Extended Services, you can define the virtual network and subnet configurations in your `.cscfg` file. Azure will automatically create the virtual network if it doesn’t exist.

   ```xml
   <NetworkConfiguration>
      <VirtualNetworkSite name="NewVNetName" />
      <AddressAssignments>
        <InstanceAddress roleName="CRWorkerRole">
          <Subnets>
            <Subnet name="NewSubnetName" />
          </Subnets>
        </InstanceAddress>
      </AddressAssignments>
   </NetworkConfiguration>
   ```

### Step 4: Deploy

1. Create Your Cloud Service (Extended Support) following [this guide](https://learn.microsoft.com/en-us/azure/cloud-services-extended-support/deploy-portal).
2. Set up your cloud service configuration using `configure_cloud.ps1` script:

   ```powershell
   .\configure_cloud.ps1 -p {path to project} -dns {your DNS name} -cn {your CN name} -bid {your bot name} -aid {your bot app id} -as {your bot secret}
   ```

3. Configure a Storage Account for Configuration Files and package your cloud service for deployment.

## Firewall Setup

1. Follow the steps outlined in the [FirewallREADME.md](FirewallREADME.md) to configure the firewall.

## Testing

1. Set up a test meeting with two Teams clients: one for a non-recorded user and one for the recorded user.
2. Place a call from the non-recorded user to the recorded user. You should see a recording banner.
3. Interact with your service by checking the diagnostics data. For active calls, visit `https://bot.contoso.com:10101/calls/{CallId}`.

## Frequently Asked Questions

**Q1:** Call was forwarded to voiceMail instead of calling.  
**Solution:** Ensure Microsoft Teams Channel is enabled under Bot Channels Registration.

**Q2:** Answering incoming call notification is slow.  
**Solution:** Cache the AAD token and place the bot server in the same geo-region as the user.

**Q3:** How to migrate to grouping mode?  
**Solution:** Use `participantCapacity` with `ICall.AnswerAsync` and handle user joining with `ParticipantJoiningHandler`.
