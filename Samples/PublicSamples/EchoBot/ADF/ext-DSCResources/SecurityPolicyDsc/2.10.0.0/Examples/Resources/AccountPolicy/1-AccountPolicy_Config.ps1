<#PSScriptInfo
.VERSION 1.0
.GUID 6052dbbe-d7bd-46f3-9407-00ae446ef1a2
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
        This configuration will manage the local security account policy.
#>

Configuration AccountPolicy_Config
{
    Import-DscResource -ModuleName SecurityPolicyDsc

    node localhost
    {
        AccountPolicy AccountPolicies
        {
            Name                                        = 'PasswordPolicies'
            Enforce_password_history                    = 15
            Maximum_Password_Age                        = 42
            Minimum_Password_Age                        = 1
            Minimum_Password_Length                     = 12
            Password_must_meet_complexity_requirements  = 'Enabled'
            Store_passwords_using_reversible_encryption = 'Disabled'
        }
    }
}
