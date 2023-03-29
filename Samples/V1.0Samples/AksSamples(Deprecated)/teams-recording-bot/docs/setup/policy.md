# Compliance Policy

The following steps should be done by an Office 365 administrator.

The Office 365 Tenant will require at least one user allocated for the bot to be used.
This can be an existing user or a new one can be created e.g. for testing.

## Create an Application Instance

Open powershell (in admin mode) and run the following commands. When prompted for authentication, login with the tenant admin. [Skype for Business Online](https://www.microsoft.com/en-us/download/details.aspx?id=39366) is needed for the following steps:

1. Logging into O365 in Powershell:

>Note: at the time of this writing, only Windows PowerShell was supported, the core PowerShell 7.0.x was not supported.

    ```powershell
    Set-ExecutionPolicy RemoteSigned
    Import-Module SkypeOnlineConnector
    $sfbSession = New-CsOnlineSession -Verbose
    Import-PSSession $sfbSession
    ```
2. Create a new application instance for the bot channel registration:

    ```powershell
    New-CsOnlineApplicationInstance `
        -UserPrincipalName <upn@contoso.com> `
        -DisplayName <displayName> `
        -ApplicationId <BOT_ID>
    ```
    >Note: if when executing this command you've encountered the error stating that:
    `The provided UserPrincipalName is already used by another application instance or user.`, it means you have already have another application instance associated with your email address. To update that instance with a new Bot Application Id and the new Display Name, issue the following commands:

    ```powershell
    $identity = Get-CsOnlineApplicationInstance | Where-Object {$_.DisplayName -Match "<displayName>"} | Select ObjectId
    Set-CsOnlineApplicationInstance `
        -Identity $identity."ObjectId" `
        -ApplicationId <BOT_ID> `
        -DisplayName <displayName>
    ```

3. Sync the newly created application instance:

    ```powershell
    $identity = Get-CsOnlineApplicationInstance | Where-Object {$_.DisplayName -Match "<displayName>"} | Select ObjectId
    Sync-CsOnlineApplicationInstance `
        -ObjectId $identity."ObjectId"
    ```

After initial set up you only need to run steps 1-3 in future sessions.

## Create a Recording Policy

Requires the application instance Object ID created above. Continue your powershell session and run the following commands.

1. Create a new Teams recording policy for governing automatic policy-based recording in your tenant:

    ```powershell
    $policyName = '<policyIdentityName>'
    New-CsTeamsComplianceRecordingPolicy `
	    -Identity $policyName `
	    -Enabled $true `
	    -ComplianceRecordingApplications @(New-CsTeamsComplianceRecordingApplication -Parent $policyName -Id $identity."ObjectId")
    ```

    >Note: when you need to re-run this command for the same policy and/or the same identity, use `Set-` commands instead of `New-`, as in the following example:

    ```powershell
    $tag = 'Tag:' + $policyName + '/' + $identity."ObjectId"
    Set-CsTeamsComplianceRecordingPolicy `
        -Identity $policyName `
        -Enabled $true
    ```

    >Note: if you ever need to remove the existing Teams Compliance Recording policy, you can execute the following command:

    ```powershell
    $tag = 'Tag:' + $policyName
    Get-CsTeamsComplianceRecordingPolicy -Filter $tag | Remove-CsTeamsComplianceRecordingPolicy
    ```

After 30-60 seconds, the policy should show up. To verify your policy was created correctly:

* `Get-CsTeamsComplianceRecordingPolicy <policyIdentityName>`

    ```powershell
    Get-CsTeamsComplianceRecordingPolicy $policyName
    ```
If the policy has been successfully created, you should see the output looks like this:
![Policy Output](../images/policy_output.png)

## Assign the Recording Policy

Requries the policy identity created above. Contine your powershell session and run the following commands.

1. Grant-CsTeamsComplianceRecordingPolicy

    ```powershell
    Grant-CsTeamsComplianceRecordingPolicy `
        -Identity <upn@contoso.com> `
        -PolicyName $policyName
    ```

    After a couple of minutes, to verify your policy was assigned correctly:

2. Get-CsOnlineUser

    ```powershell
    Get-CsOnlineUser <upn@contoso.com> | ft sipaddress, tenantid, TeamsComplianceRecordingPolicy
    ```
    If your policy has been assigned correctly, you should see a similar output:

    ![Policy Result](../images/policy-result.png)

