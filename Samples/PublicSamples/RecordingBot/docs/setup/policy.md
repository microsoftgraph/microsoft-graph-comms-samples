# Compliance Policy

The following steps should be done by an Office 365 administrator.

The Office 365 Tenant will require at least one user allocated for the bot to be used.
This can be an existing user or a new one can be created e.g. for testing.

## Prerequisites

- PowerShell (5.1, comes with Windows) as Administrator
- PowerShell execution policy of at least `RemoteSigned`
- PowerShell Module
 - [SkypeForBusiness](https://learn.microsoft.com/en-us/powershell/module/skype/?view=skype-ps)
 - or [MicrosoftTeams](https://learn.microsoft.com/en-us/powershell/module/teams/?view=teams-ps)
- An Office 365 administrator
- An Office 365 user account

Throughout this Documentation we will use the `MicrosoftTeams` PowerShell Module.
You can also use the SkypeForBusiness Module, either one works.
The relevant Commands are available in both Modules.

### Run PowerShell (as admin)

Just hit the Widnows Key on your Keyboard or click the Start menu button.
Now start typeing `PowerShell`.
Open the Context Menu (right click) of the `Windows PowerShell` entry and select `Run as Administrator`.

### Enable PowerShell Script execution

In an evelated (Run as Admin) PowerShell Terminal execute the following command
`Set-PsExecutionPolicy RemoteSigned`
For more information look [here](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-5.1)

### Install the Module

In an evelated (Run as Admin) PowerShell Terminal execute the following command
`Install-Module MicrosoftTeams`
Or if it is already installed, update the module
`Update-Module MicrosoftTeams`

### Activate the Module

In an evelated (Run as Admin) PowerShell Terminal execute the following command
`Import-Module MicrosoftTeams`
and then
`Connect-MicrosoftTeams`
You will need to sign in with your Azure Credentials here

For further Information check [Install the Microsoft Teams PowerShell Module](https://learn.microsoft.com/en-us/microsoftteams/teams-powershell-install#installing-using-the-powershellgallery) and [sign in with your Azure Credentials](https://learn.microsoft.com/en-us/microsoftteams/teams-powershell-install#sign-in)

## Setup a Compliance Policy for Teams

>Note: All of the following commands are executed in the PowerShell Terminal that you have used to Activate the module.

To create a policy in Teams, we need 3 objects.
- An Application Instance (Micrisift Entra ID Resource)
- A Recording policy (the actual compliance policy)
- A Recording Policy Application (A link between the policy and the application)

### Create the Application Instance

[New-CsOnlineApplicationInstance](https://learn.microsoft.com/en-us/powershell/module/skype/new-csonlineapplicationinstance?view=skype-ps)
```powershell
New-CsOnlineApplicationInstance
   -UserPrincipalName <the email of the generated micrsoft entra id resource> `
   -DisplayName <a name for the generated microsoft entra id resource> `
   -ApplicationId <the Application Id of the Bot Application Registration>
```
The Application Id might not be from your own Microsoft Entra ID,
but from the Microsoft Entra ID that hosts this Bot.
So basically from the Service Provider.

You will now have to Synchronize this Application Instance into the Agent Provisioning Service
```powershell
Sync-CsOnlineApplicationInstance
    -ObjectId <the object id of the Application Instance>
    -ApplicationId <the application id of the remote application registration>
```
You can get the Object Id by executing the `Get-CsOnlineApplicationInstance -Displayname <the name you provided>` and checking the output.
The Object Id will also be displayed after the `New-CsOnlineApplicationInstance` command.
The Application Id is the same Application Id you used to create the Application Instance.

### Create the policy

[New-CsTeamsComplianceRecordingPolicy](https://learn.microsoft.com/en-us/powershell/module/skype/new-csteamscompliancerecordingpolicy?view=skype-ps)
```powershell
New-CsTeamsComplianceRecordingPolicy
   -Identity <provide a name for your policy>
   -Enabled $true
```
With this, you will just have a policy.
You can already assign this policy to users, but it will not do anything,
because it does not have any Recording Applications assigned to it yet.

### Create the Recording Application

[New-CsTeamsComplianceRecordingApplication](https://learn.microsoft.com/en-us/powershell/module/skype/new-csteamscompliancerecordingapplication?view=skype-ps)
```powershell
New-CsTeamsComplianceRecordingApplication
   -Parent <Recording Policy Name>
   -Id <Application Instance Object Id>
```
The <Recording Policy Name> is the Parameter `Identity` from the `New-CsTeamsComlianceRecordingPolicy`.
So the Parent of a Recording Application is a Recording policy.
The <Application Instance Object Id> is the Object ID of the Object created with `New-CsOnlineApplicationInstance`.
So basically you now have
- assigned an Application in your own Entra (Application Instance)
 - wich points to an application on an external Entra (Application Id that was assigned to Application Instance)
- to a Teams Compliance Policy (by using its name)

### Activate the policy

[Grant-CsTeamsComplianceRecordingPolicy](https://learn.microsoft.com/en-us/powershell/module/skype/grant-csteamscompliancerecordingpolicy?view=skype-ps)
```powershell
Grant-CsTeamsComplianceRecordingPolicy
     -Identity <User Principal name of the Application Instance>
     -PolicyName <Name of the Recording Policy>
```
The Identity is the same as passed into the `UserPrincipalName` parameter of the `New-CsOnlineApplicationInstance` command.
The PolicyName is the same as passed into the `Identity` parameter of the `New-CsTeamsComplianceRecordingPolicy` command.

## Use the policy

To be able to use the Policy, you will need to assign Users or Groups to this policy.

