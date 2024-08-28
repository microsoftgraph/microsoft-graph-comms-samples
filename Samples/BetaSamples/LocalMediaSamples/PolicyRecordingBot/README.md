# Introduction

## Note

The system will load the bot and join it to appropriate calls and meetings in order for the bot to enforce compliance with the administrative set policy.
This sample is only designed for compliance recording scenario. Do not use it for any other scenarios.

## About

The Policy Recording bot sample guides you through building, deploying and testing a bot. This sample demonstrates how a bot can receive media streams for recording. Please note that the sample does not actually record. This logic is left up to the developer.

## Getting Started

This section walks you through the process of deploying and testing the sample bot.

### Bot Registration

1. Follow the steps in [Register Calling Bot](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/articles/calls/register-calling-bot.html). Save the bot name, bot app id and bot secret for configuration.
    * For the calling webhook, by default the notification will go to https://{your domain}/api/calling. This is configured with the `CallSignalingRoutePrefix` in [HttpRouteConstants.cs](FrontEnd/Http/Controllers/HttpRouteConstants.cs).
    * Ignore the "Register bot in Microsoft Teams" section as the Policy Recording bot won't be called directly. These bots are related to the policies discussed below, and are "attached" to users, and will be automatically invited to the call.

1. Add the following Application Permissions to the bot:

    * Calls.AccessMedia.All
    * Calls.JoinGroupCall.All
   
1. The permission needs to be consented by tenant admin. Go to "https://login.microsoftonline.com/common/adminconsent?client_id=<app_id>&state=<any_number>&redirect_uri=<any_callback_url>" using tenant admin to sign-in, then consent for the whole tenant.

### Create an Application Instance

Open powershell (in admin mode) and run the following commands. When prompted for authentication, login with the tenant admin.
  * `Import-Module SkypeOnlineConnector`
  * `$Session=New-CsOnlineSession`
  * `Import-PSSession $Session`
  * `New-CsOnlineApplicationInstance -UserPrincipalName <upn@contoso.com> -DisplayName <displayName> -ApplicationId <your_botappId>`
  * `Sync-CsOnlineApplicationInstance -ObjectId <objectId>`

### Create a Recording Policy
Requires the application instance ID created above. Continue your powershell session and run the following commands.
  * `New-CsTeamsComplianceRecordingPolicy -Enabled $true -Description "Test policy created by <yourName>" <policyIdentity>`
  * ```Set-CsTeamsComplianceRecordingPolicy -Identity <policyIdentity> -ComplianceRecordingApplications ` @(New-CsTeamsComplianceRecordingApplication -Parent <policyIdentity> -Id <objectId>)```

After 30-60 seconds, the policy should show up. To verify your policy was created correctly:
  * `Get-CsTeamsComplianceRecordingPolicy <policyIdentity>`

### Assign the Recording Policy
Requries the policy identity created above. Contine your powershell session and run the following commands.
  * `Grant-CsTeamsComplianceRecordingPolicy -Identity <userUnderPolicy@contoso.com> -PolicyName <policyIdentity>`

To verify your policy was assigned correctly:
  * `Get-CsOnlineUser <userUnderPolicy@contoso.com> | ft sipaddress, tenantid, TeamsComplianceRecordingPolicy`

### Prerequisites

* Install the prerequisites:
    * [Visual Studio 2017+](https://visualstudio.microsoft.com/downloads/)
    * [PostMan](https://chrome.google.com/webstore/detail/postman/fhbjgbiflinjbdggehcddcbncdddomop)

### Deploy

* Prerequisites for deploying Azure Cloud Services (extended support)(https://learn.microsoft.com/en-us/azure/cloud-services-extended-support/deploy-prerequisite)

Step 1: Securely Store Certificates with Azure Key Vault
    * Certificates are crucial for securing communication between your services. Azure Key Vault is used to store and manage these certificates securely.

   Create an Azure Key Vault:
    * Follow these instructions to create your Azure Key Vault: https://learn.microsoft.com/en-us/azure/key-vault/general/quick-create-portal.

Step 2: Obtain and Configure Your SSL Certificate
    * To secure your service, you need a valid SSL certificate. Here’s how to obtain and configure it:

   Get a Wildcard Certificate:
     * Obtain a wildcard SSL certificate for your domain. For example, if your service is hosted at bot.contoso.com,get a certificate for *.contoso.com. 
     * Ensure that the certificate is issued by a trusted Certificate Authority (CA) and not self-signed.

   Upload to Azure Key Vault:
     * Upload your SSL certificate to the Azure Key Vault. Follow these steps: https://learn.microsoft.com/en-us/azure/key-vault/certificates/tutorial-import-certificate?tabs=azure-portal.

   Get the Thumbprint:
     * Copy the certificate thumbprint from Azure Key Vault. You will need to add this thumbprint to your .cscfg (cloud service configuration) and .csdef (cloud service definition) files.
      1. Update the Certificate section in your .cscfg file with the thumbprint.    
         <Certificates>
         <!-- Certificate Configuration:
           This is where you specify the thumbprint for your SSL certificate.
           Replace 'YOUR_THUMBPRINT' with the actual thumbprint of your certificate. -->
         <Certificate name="MySSLCertificate" thumbprint="YOUR_THUMBPRINT" thumbprintAlgorithm="sha1" />
         </Certificates>
     2. Update the Certificate element in your .csdef file.
         <Certificates>
          <Certificate name="YourCertificateName" storeLocation="LocalMachine" storeName="My" />
         </Certificates>
       * Replace YourCertificateName with the actual name of your certificate as it appears in your Azure Key Vault or wherever it is stored. Here are the key attributes:
         name: This should match the certificate's name as referenced in your Azure Key Vault or local certificate store.
         storeLocation: Specifies where the certificate is stored. LocalMachine is a common location for certificates installed on the local machine.
         storeName: Specifies the store name where the certificate is located. My is a common store name used for personal certificates.

Step 3: Define Your Virtual Network
     * For Azure Extended Services, you can define the virtual network and subnet configurations in your .cscfg file. Azure can create the virtual network during the service setup if it doesn't already exist.
     * When deploying your cloud service (extended) in Azure, virtual network and subnet configurations are managed automatically based on your .cscfg file. Follow these guidelines to ensure proper configuration:

#### Using Existing Virtual Network:

   * If you have an existing Virtual Network (VNet) that you want to use:
        <NetworkConfiguration>
          <VirtualNetworkSite name="YourExistingVNetName" />
          <AddressAssignments>
            <InstanceAddress roleName="CRWorkerRole">
              <Subnets>
                <Subnet name="YourExistingSubnetName" />
              </Subnets>
            </InstanceAddress>
          </AddressAssignments>
        </NetworkConfiguration>

  * Replace "YourExistingVNetName" with the name of your existing Virtual Network and "YourExistingSubnetName" with the name of your existing subnet within that Virtual Network.

### Automatic Creation of Virtual Network:

   * If the Virtual Network doesn't exist, Azure will create it based on the configuration provided:
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
   * Replace "NewVNetName" with the name of the Virtual Network you want Azure to create, and "NewSubnetName" with the name of the subnet within that Virtual Network.
   
 ### Note on Domain Name and Public IP:

    <PublicIPs>
        <PublicIP name="MyPublicIP" domainNameLabel="myservice" />
      </PublicIPs>
   * PublicIP name: "MyPublicIP" – Provide a unique name for the public IP.
   * domainNameLabel: "myservice" – Set this to your service's domain label.
   
   Azure Extended Services:
   * Public IP: You must provide a Public IP name in your configuration when creating the service.
   * Domain Name: The domain name (domainNameLabel) is optional during initial creation and can be specified or updated later.
   
   Match Public IP Name:
   * Ensure that the name attribute under <PublicIP> (MyPublicIP in this example) matches the name used in your application code to fetch the public IP address dynamically.
   * To ensure that the domainNameLabel matches between your configuration (.cscfg file) and the Azure portal settings.
   
 Step 4: Deploy

   1. Create Your Cloud Service (Extended Support)
      1. Use the Azure portal to create a Cloud Service (Extended Support). 
        Follow this guide: Create a Cloud Service (Extended Support).(https://learn.microsoft.com/en-us/azure/cloud-services-extended-support/deploy-portal)
      2. Obtain Your Public IP DNS name:
        After the service is created, obtain the "Public IP DNS name" from the Azure portal. This URL will serve as your DNS name and Common Name (CN) for further configurations (e.g. bot.contoso.com).
        ![Public IP DNS name](Images/PublicIPDNSName.png).  

   2. Update the app configs with values
      1. Set up cloud service configuration
        1. Open powershell, go to the folder that contains file `configure_cloud.ps1`. The file is in the `Samples` directory.
        2. Run the powershell script with parameters:
          ` .\configure_cloud.ps1 -p {path to project} -dns {your DNS name} -cn {your CN name, should be the same as your DNS name} -bid {your bot name} -aid {your bot app id} -as {your bot secret}`
        
         For example:
        
         `.\configure_cloud.ps1 -p .\V1.0Samples\LocalMediaSamples\PolicyRecordingBot\ -dns bot.contoso.com -cn bot.contoso.com -bid bot -aid 3853f935-2c6f-43d7-859d-6e8f83b519ae -as 123456!@#$%^`

   3. Deploy to Cloud Service (Extended Support)
     1. Configure Storage Account for Configuration Files.
       * To store configuration files for your Azure extended service, you'll need to set up a storage account. Follow these steps to configure the storage account:(https://learn.microsoft.com/en-us/azure/storage/common/storage-account-create?tabs=azure-portal).
     2. Package Your Cloud Service for Deployment
       * Before you can create your Azure extended service, you need to package your cloud service application to include configuration files and dependencies.
       * Right click PolicyRecordingBot, then click `Package...`.
     
   Option 1: Upload to Azure Storage Account:
    * Upload your packaged application (cspkg file) along with the .cscfg and .csdef files to an Azure Storage Account container.
    * This method allows you to deploy directly from the Azure Storage Account during service creation.

   Option 2: Use Local Files:
    * Deploy directly from local files during the service creation process.
    * Ensure all required files, including .cscfg and .csdef, are accessible and correctly referenced during deployment.

### Firewall setup

   * Please follow below steps to configure firewall.
     - [document](FirewallREADME.md)

### Test

1. Set up the test meeting and test clients:
   1. Sign in to Teams client with a non-recorded test tenant user.
   2. Use another Teams client to sign in with the recorded user. You could use an private browser window and open up https://teams.microsoft.com. If the call notification doesn't appear on web, use the Teams desktop client.

2. Place a call from the Teams client with the non-recorded user to the recorded user.

3. Your recording bot should receive the incoming call and join the call immediately. Use the recorded users' Teams client to accept the call. Once the P2P call is established, you'll see a banner indicating that the recording has started. See the next step to learn how you can retrieve the call log.
     ![Recording Banner](Images/RecordingBanner.png)

3. Interact with your service, _adjusting the service URL appropriately_.
    1. Get diagnostics data from the bot. Open the url https://bot.contoso.com:10101/calls in a browser for auto-refresh. Search for the most recent CallId and replace with it in the below url.
       * Active calls: https://bot.contoso.com:10101/calls/{CallId}
       * Service logs: https://bot.contoso.com:10101/logs

    2. Terminating the call through `DELETE`, as needed for testing. Replace the {CallId} below with your call id from the first response.

        ##### Request
        ```json
            DELETE https://bot.contoso.com/calls/{CallId}
        ```
### Frequently Asked Questions:

1. **Question**: Call was forwarded to voiceMail directly instead of calling.

    **Solution**: Make sure Microsoft Teams Channel is enabled under Bot Channels Registration.
    ![Enable Microsoft Teams Channel](Images/EnableMicrosoftTeamsChannel.png)

2. **Question**: Answering incoming call notification taking too long resulting in call not found error.
    
    **Solution**: Policy Recording scenario has a rather small timeout window set to receive answer from the bot, in order to make sure user can have time to pick up the call after bot joins the call.
    Something to consider to improve the performance of answering incoming call:
    1. Make sure the AAD token used to authenticate outbound request is cached, instead of acquiring one everytime.
    2. Make sure the bot server is located in the same geo-region as the user.

3. **Question**: How to migrate to grouping mode?

    **Solution**:
    1. Answer the call with **participantCapacity** to specify the capacity of how many policy-based users this bot instance can handle as a group. If the bot passes null or a value of 0 or 1, it means that the bot does not support grouping. We expect the participantCapacity to be quite large, like 100 or more.
   ```csharp
   await ICall.AnswerAsync(mediaSession: mediaSession, participantCapacity: capacity);
   ```
    2. Handle user joining in same group by hooking **ParticipantJoiningHandler**.
      * Accept join by returning **AcceptJoinResponse**.
    ```csharp
    ICall.ParticipantJoiningHandler = (call) => {
      // your logic
      return new AcceptJoinResponse();
    }
    ```
      * Redirect to new bot instance by returning **InviteNewBotResponse** with **InviteUri**.
    ```csharp
    ICall.ParticipantJoiningHandler = (call) => {
      // your logic
      return new InviteNewBotResponse() { InviteUri = “https://redirect.url/” };
    }
    ```
      * Reject by returning **RejectJoinResponse** with **Reason**.
    ```csharp
    ICall.ParticipantJoiningHandler = (call) => {
      // your logic
      return new RejectJoinResponse() { Reason = “Busy” };
    }
    ```
    3. Handle user left by hooking **ParticipantLeftHandler**.
    ```csharp
    ICall.ParticipantLeftHandler = (call, participantId) => {
      // your logic
    }
    ```