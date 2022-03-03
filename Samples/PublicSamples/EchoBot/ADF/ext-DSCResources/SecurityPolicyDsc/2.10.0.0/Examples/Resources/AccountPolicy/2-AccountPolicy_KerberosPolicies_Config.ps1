<#PSScriptInfo
.VERSION 1.0
.GUID b8e54087-a68d-4bca-8222-3bc34b2e857d
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
        This configuration will manage the kerberos security policies.

        Since kerberos policies are domain policies they can only be modified with
        domain admin privileges.
#>

Configuration AccountPolicy_KerberosPolicies_Config
{
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCredential]
        $DomainCred
    )

    Import-DscResource -ModuleName SecurityPolicyDsc

    node localhost
    {
        AccountPolicy KerberosPolicies
        {
            Name                                                 = 'KerberosPolicies'
            Enforce_user_logon_restrictions                      = 'Enabled'
            Maximum_lifetime_for_service_ticket                  = 600
            Maximum_lifetime_for_user_ticket                     = 10
            Maximum_lifetime_for_user_ticket_renewal             = 7
            Maximum_tolerance_for_computer_clock_synchronization = 5
            PsDscRunAsCredential                                 = $DomainCred
        }
    }
}
