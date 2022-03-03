<#PSScriptInfo
.VERSION 1.0
.GUID 917ea628-b937-4ace-99df-28d8cc8bb4f9
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

        The 'AssignShutdownPrivilegesToAdmins' resource will enforce the assignment to
        contain only the specified identity as the Force attribute is set to $true.

        The 'AccessComputerFromNetwork' resource will add the specified identities to
        the assignment without overwriting any pre-existing values, as the 'Force' parameter
        is not specified, and therefore defaults to $false.
#>
Configuration UserRightsAssignment_Basic_Config
{
    Import-DscResource -ModuleName SecurityPolicyDsc

    Node localhost
    {
        # Assign shutdown privileges to only Builtin\Administrators
        UserRightsAssignment AssignShutdownPrivilegesToAdmins
        {
            Policy   = "Shut_down_the_system"
            Identity = "Builtin\Administrators"
            Force    = $true
        }

        UserRightsAssignment AccessComputerFromNetwork
        {
            Policy   = "Access_this_computer_from_the_network"
            Identity = "contoso\TestUser1", "contoso\TestUser2"
        }
    }
}
