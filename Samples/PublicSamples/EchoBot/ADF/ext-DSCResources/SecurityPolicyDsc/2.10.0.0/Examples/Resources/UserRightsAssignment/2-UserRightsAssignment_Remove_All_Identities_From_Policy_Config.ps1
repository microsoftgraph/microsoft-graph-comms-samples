<#PSScriptInfo
.VERSION 1.0
.GUID ecc41d8a-15d0-485f-b019-fa30842f3732
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
        This configuration will manage a User Rights Assignment policy.
        When Identity is an empty string all identities will be removed from the policy.
#>
Configuration UserRightsAssignment_Remove_All_Identities_From_Policy_Config
{
    Import-DscResource -ModuleName SecurityPolicyDsc

    Node localhost
    {
        UserRightsAssignment RemoveIdsFromSeTrustedCredManAccessPrivilege
        {
            Policy   = "Access_Credential_Manager_as_a_trusted_caller"
            Identity = ""
        }
    }
}
