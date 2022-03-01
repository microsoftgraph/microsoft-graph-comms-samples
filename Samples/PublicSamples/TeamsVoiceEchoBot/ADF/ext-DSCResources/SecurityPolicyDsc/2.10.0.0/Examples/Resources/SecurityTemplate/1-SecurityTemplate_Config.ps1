<#PSScriptInfo
.VERSION 1.0
.GUID 374899a2-937c-446c-9f00-6d6b930b04c8
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
        This configuration will manage user rights assignments that are defined
        in a security policy INF file.
#>
Configuration SecurityTemplate_Config
{
    Import-DscResource -ModuleName SecurityPolicyDsc

    node localhost
    {
        SecurityTemplate TrustedCredentialAccess
        {
            Path             = "C:\scratch\SecurityPolicyBackup.inf"
            IsSingleInstance = 'Yes'
        }
    }
}
