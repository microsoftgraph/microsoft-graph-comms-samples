# Compliance Recording Policy

The following steps should be done by an Microsoft Entra Id administrator.

The Microsoft Entra Id tenant will require at least one user allocated for the bot to be used.
This can be an existing user or a new one can be created e.g. for testing.

## Prerequisites

- PowerShell (5.1, comes with Windows) as Administrator
- PowerShell execution policy of at least `RemoteSigned`
- PowerShell Module
  - [SkypeForBusiness](https://learn.microsoft.com/powershell/module/skype/)
  - or [MicrosoftTeams](https://learn.microsoft.com/powershell/module/teams/)
- A Microsoft Entra Id administrator

Throughout this Documentation we will use the `MicrosoftTeams` PowerShell Module.  
You can also use the SkypeForBusiness Module, either one works.
The relevant Commands are available in both Modules.

You will use the Microsoft Entra Id administrator for all commands.  

### Run PowerShell (as admin)

Just hit the Widnows Key on your Keyboard or click the Start menu button.  
Now start typeing `PowerShell`.  
Open the Context Menu (right click) of the `Windows PowerShell` entry and select `Run as Administrator`.

### Enable PowerShell Script execution

In an evelated (Run as Admin) PowerShell Terminal execute the following command  
`Set-PsExecutionPolicy RemoteSigned`  
For more information look [here](https://learn.microsoft.com/powershell/module/microsoft.powershell.security/set-executionpolicy)

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
You will need to sign in with your Azure Credentials (Microsoft Entra Id administrator) here

For further Information check [Install the Microsoft Teams PowerShell Module](https://learn.microsoft.com/microsoftteams/teams-powershell-install#installing-using-the-powershellgallery) and [sign in with your Azure Credentials](https://learn.microsoft.com/microsoftteams/teams-powershell-install#sign-in)

## Setup a Compliance Policy for Teams

>Note: All of the following commands are executed in the PowerShell Terminal that you have used to Activate the module.

To create a policy in Teams, we need 3 objects.

- An [Application Instance](../explanations/recording-bot-policy.md#application-instances) (Microsoft Entra ID Resource)
- A [Recording Policy](../explanations/recording-bot-policy.md#recording-policies) (the actual compliance policy)
- A [Recording Application](../explanations/recording-bot-policy.md#recording-applications) (A link between the policy and the application)

### Create the Application Instance

[New-CsOnlineApplicationInstance](https://learn.microsoft.com/powershell/module/skype/new-csonlineapplicationinstance)

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

[New-CsTeamsComplianceRecordingPolicy](https://learn.microsoft.com/powershell/module/skype/new-csteamscompliancerecordingpolicy)

```powershell
New-CsTeamsComplianceRecordingPolicy
   -Identity <provide a name for your policy>
   -Enabled $true
```

With this, you will just have a policy.  
You can already assign this policy to users, but it will not do anything,
because it does not have any Recording Applications assigned to it yet.

### Create the Recording Application

[New-CsTeamsComplianceRecordingApplication](https://learn.microsoft.com/powershell/module/skype/new-csteamscompliancerecordingapplication)

```powershell
New-CsTeamsComplianceRecordingApplication
   -Parent <Recording Policy Name>
   -Id <Application Instance Object Id>
```

The Parent is the Parameter `Identity` from the `New-CsTeamsComlianceRecordingPolicy`.  
So the Parent of a Recording Application is a Recording policy.  
The Id is the Object ID of the Object created with `New-CsOnlineApplicationInstance`.  
So basically you now have

- assigned an Application in your own Entra (Application Instance)
  - wich points to an application on an external Entra (Application Id that was assigned to Application Instance)
- to a Teams Compliance Policy (by using its name)

## Use the policy

To be able to use the Policy, you will need to assign the policy to Users or Groups.

> [!NOTE]  
> It may take a few minutes and logged in users need a new access token (logout and login again) before the recording policy takes effect.

### Assign the Policy to a tenant

[Grant-CsTeamsComplianceRecordingPolicy](https://learn.microsoft.com/powershell/module/teams/grant-csteamscompliancerecordingpolicy)

``` powershell
Grant-CsTeamsComplianceRecordingPolicy 
      -Global 
      -PolicyName <Recording Policy Name>
```

This assigns the policy to all users of your tenant.

### Assign the Policy to a user

[Grant-CsTeamsComplianceRecordingPolicy](https://learn.microsoft.com/powershell/module/teams/grant-csteamscompliancerecordingpolicy)

``` powershell
Grant-CsTeamsComplianceRecordingPolicy 
    -Identity <User Principal Name> 
    -PolicyName <Recording Policy Name>
```

This assigns the policy to the user specified by its user principal name(UPN).
The UPN is often also the email address of the user, but it does not have to be.
The upn of a user can be found in the user overview of the [Microsoft Entra Admin Center](https://entra.microsoft.com).

To verify if the policy was successfully assigned, you can run:

``` powershell
Get-CsOnlineUser <User Principal Name> | ft sipaddress, tenantid, TeamsComplianceRecordingPolicy
```

If the policy has been assigned successfully the output should look similar to

``` text
SipAddress                    TenantId                      TeamsComplianceRecordingPolicy
----------                    --------                      ------------------------------
sip:                          00000000-                     <Recording Policy Name>
```

### Assign the Policy to a group

[Grant-CsTeamsComplianceRecordingPolicy](https://learn.microsoft.com/powershell/module/teams/grant-csteamscompliancerecordingpolicy)

``` powershell
Grant-CsTeamsComplianceRecordingPolicy
      -Group <Group Object Id>
      -PolicyName <Recording Policy Name>
```

This assigns the policy to all users of the group specified by the object id of the group.
Groups can be security groups and Microsoft 365 groups,
the object id of a group can be found in the group overview of the [Microsft Entra Admin Center](https://entra.microsoft.com).

## Remove Recording Policy Assignment

Removing a recording policy Assignment is very similar to assigning a recording policy.
Passing `$null` as the `PolicyName` parameter will remove a recording policy.
