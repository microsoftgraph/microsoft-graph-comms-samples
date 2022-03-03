<#PSScriptInfo
.VERSION 1.0
.GUID 03d5e82e-b770-424b-9ce1-f0e55f3a303e
.AUTHOR Microsoft Corporation
.COMPANYNAME Microsoft Corporation
.COPYRIGHT (c) Microsoft Corporation. All rights reserved.
.TAGS DSCConfiguration
.LICENSEURI https://github.com/PowerShell/SecurityPolicyDsc/blob/master/LICENSE
.PROJECTURI https://github.com/PowerShell/SecurityPolicyDsc
.ICONURI
.EXTERNALMODULEDEPENDENCIES
.REQUIREDSCRIPTS
.EXTERNALSCRIPTDEPENDENCIES
.RELEASENOTES
.PRIVATEDATA
#>

#Requires -module SecurityPolicyDsc

<#
    .DESCRIPTION
        This configuration will manage the interactive logon message.
#>
configuration SecurityOption_LogonMessageMultiLine_Config
{
    Import-DscResource -ModuleName SecurityPolicyDsc

    $multiLineMessage = @'
Line 1 - Message for line 1.
Line 2 - Message for line 2, words, seperated, with, commas.
Line 3 - Message for line 3.
'@

    node localhost
    {
        SecurityOption LogonMessage
        {
            Name                                                          = "Message Test"
            Interactive_logon_Message_text_for_users_attempting_to_log_on = $multiLineMessage
        }
    }
}
