$resourceModuleRootPath = Split-Path -Path (Split-Path $PSScriptRoot -Parent) -Parent
$modulesRootPath = Join-Path -Path $resourceModuleRootPath -ChildPath 'Modules'
Import-Module -Name (Join-Path -Path $modulesRootPath  `
        -ChildPath 'SecurityPolicyResourceHelper\SecurityPolicyResourceHelper.psm1') `
    -Force

$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_SecurityOption'

<#
    .SYNOPSIS
        Returns all the Security Options that are currently configured.

    .PARAMETER Name
        Describes the security option to be managed. This could be anything as long as it is unique. This property is not
        used during the configuration process.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name
    )

    $returnValue = @{}
    $currentSecurityPolicy = Get-SecurityPolicy -Area SECURITYPOLICY
    $securityOptionData = Get-PolicyOptionData -FilePath $("$PSScriptRoot\SecurityOptionData.psd1").Normalize()
    $securityOptionList = Get-PolicyOptionList -ModuleName MSFT_SecurityOption

    foreach ($securityOption in $securityOptionList)
    {
        $section = $securityOptionData.$securityOption.Section
        Write-Verbose -Message ($script:localizedData.Section -f $section)
        $valueName = $securityOptionData.$securityOption.Value
        Write-Verbose -Message ($script:localizedData.Value -f $valueName)
        $options = $securityOptionData.$securityOption.Option
        Write-Verbose -Message ($script:localizedData.Option -f $($options -join ','))
        $currentValue = $currentSecurityPolicy.$section.$valueName
        Write-Verbose -Message ($script:localizedData.RawValue -f $($currentValue -join ','))

        if ($options.keys -eq 'String')
        {
            if ($securityOption -eq 'Interactive_logon_Message_text_for_users_attempting_to_log_on')
            {
                $resultValue = ($currentValue -split '7,')[-1].Trim()
            }
            elseif ($securityOption -eq 'Network_access_Restrict_clients_allowed_to_make_remote_calls_to_SAM')
            {
                [array]$resultValue = ConvertTo-CimRestrictedRemoteSam -InputObject $currentValue
            }
            else
            {
                if ($currentValue -match ',')
                {
                    $null, $stringValue = $currentValue -split ','
                    $resultValue = (($stringValue -replace '"').Trim()) -join ','
                }
                else
                {
                    $resultValue = ($currentValue -replace '"').Trim()
                }
            }
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.RetrievingValue -f $valueName)
            if ($currentSecurityPolicy.$section.keys -contains $valueName)
            {
                if ($securityOption -eq "Network_security_Configure_encryption_types_allowed_for_Kerberos")
                {
                    $resultValue = ConvertTo-KerberosEncryptionOption -EncryptionValue $currentValue
                }
                else
                {
                    $resultValue = ($securityOptionData.$securityOption.Option.GetEnumerator() |
                            Where-Object -Property Value -eq $currentValue.Trim() ).Name
                }
            }
            else
            {
                $resultValue = $null
            }
        }
        $returnValue.Add($securityOption, $resultValue)
    }
    return $returnValue
}


<#
    .SYNOPSIS
        Applies the desired security option configuration.
#>
function Set-TargetResource
{
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingPlainTextForPassword", "")]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingUserNameAndPassWordParams", "")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Accounts_Administrator_account_status,

        [Parameter()]
        [ValidateSet("This policy is disabled", "Users cant add Microsoft accounts", "Users cant add or log on with Microsoft accounts")]
        [System.String]
        $Accounts_Block_Microsoft_accounts,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Accounts_Guest_account_status,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Accounts_Limit_local_account_use_of_blank_passwords_to_console_logon_only,

        [Parameter()]
        [System.String]
        $Accounts_Rename_administrator_account,

        [Parameter()]
        [System.String]
        $Accounts_Rename_guest_account,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Audit_Audit_the_access_of_global_system_objects,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Audit_Audit_the_use_of_Backup_and_Restore_privilege,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Audit_Force_audit_policy_subcategory_settings_Windows_Vista_or_later_to_override_audit_policy_category_settings,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Audit_Shut_down_system_immediately_if_unable_to_log_security_audits,

        [Parameter()]
        [System.String]
        $DCOM_Machine_Access_Restrictions_in_Security_Descriptor_Definition_Language_SDDL_syntax,

        [Parameter()]
        [System.String]
        $DCOM_Machine_Launch_Restrictions_in_Security_Descriptor_Definition_Language_SDDL_syntax,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Devices_Allow_undock_without_having_to_log_on,

        [Parameter()]
        [ValidateSet("Administrators", "Administrators and Power Users", "Administrators and Interactive Users")]
        [System.String]
        $Devices_Allowed_to_format_and_eject_removable_media,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Devices_Prevent_users_from_installing_printer_drivers,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Devices_Restrict_CD_ROM_access_to_locally_logged_on_user_only,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Devices_Restrict_floppy_access_to_locally_logged_on_user_only,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_controller_Allow_server_operators_to_schedule_tasks,

        [Parameter()]
        [ValidateSet("None", "Require Signing")]
        [System.String]
        $Domain_controller_LDAP_server_signing_requirements,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_controller_Refuse_machine_account_password_changes,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_member_Digitally_encrypt_or_sign_secure_channel_data_always,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_member_Digitally_encrypt_secure_channel_data_when_possible,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_member_Digitally_sign_secure_channel_data_when_possible,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_member_Disable_machine_account_password_changes,

        [Parameter()]
        [ValidateRange(0, 999)]
        [System.String]
        $Domain_member_Maximum_machine_account_password_age,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_member_Require_strong_Windows_2000_or_later_session_key,

        [Parameter()]
        [ValidateSet("User displayname, domain and user names", "User display name only", "Do not display user information")]
        [System.String]
        $Interactive_logon_Display_user_information_when_the_session_is_locked,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Interactive_logon_Do_not_display_last_user_name,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Interactive_logon_Do_not_require_CTRL_ALT_DEL,

        [Parameter()]
        [ValidateRange(0, 999)]
        [System.String]
        $Interactive_logon_Machine_account_lockout_threshold,

        [Parameter()]
        [ValidateRange(0, 599940)]
        [System.String]
        $Interactive_logon_Machine_inactivity_limit,

        [Parameter()]
        [System.String]
        $Interactive_logon_Message_text_for_users_attempting_to_log_on,

        [Parameter()]
        [System.String]
        $Interactive_logon_Message_title_for_users_attempting_to_log_on,

        [Parameter()]
        [ValidateRange(0, 50)]
        [System.String]
        $Interactive_logon_Number_of_previous_logons_to_cache_in_case_domain_controller_is_not_available,

        [Parameter()]
        [ValidateRange(1, 999)]
        [System.String]
        $Interactive_logon_Prompt_user_to_change_password_before_expiration,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Interactive_logon_Require_Domain_Controller_authentication_to_unlock_workstation,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Interactive_logon_Require_smart_card,

        [Parameter()]
        [ValidateSet("No Action", "Lock workstation", "Force logoff", "Disconnect if a remote Remote Desktop Services session")]
        [System.String]
        $Interactive_logon_Smart_card_removal_behavior,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_client_Digitally_sign_communications_always,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_client_Digitally_sign_communications_if_server_agrees,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_client_Send_unencrypted_password_to_third_party_SMB_servers,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.String]
        $Microsoft_network_server_Amount_of_idle_time_required_before_suspending_session,

        [Parameter()]
        [ValidateSet("Default", "Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_server_Attempt_S4U2Self_to_obtain_claim_information,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_server_Digitally_sign_communications_always,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_server_Digitally_sign_communications_if_client_agrees,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_server_Disconnect_clients_when_logon_hours_expire,

        [Parameter()]
        [ValidateSet("Off", "Accept if provided by client", "Required from client")]
        [System.String]
        $Microsoft_network_server_Server_SPN_target_name_validation_level,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Allow_anonymous_SID_Name_translation,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Do_not_allow_anonymous_enumeration_of_SAM_accounts,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Do_not_allow_anonymous_enumeration_of_SAM_accounts_and_shares,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Do_not_allow_storage_of_passwords_and_credentials_for_network_authentication,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Let_Everyone_permissions_apply_to_anonymous_users,

        [Parameter()]
        [System.String]
        $Network_access_Named_Pipes_that_can_be_accessed_anonymously,

        [Parameter()]
        [System.String]
        $Network_access_Remotely_accessible_registry_paths,

        [Parameter()]
        [System.String]
        $Network_access_Remotely_accessible_registry_paths_and_subpaths,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Restrict_anonymous_access_to_Named_Pipes_and_Shares,

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $Network_access_Restrict_clients_allowed_to_make_remote_calls_to_SAM,

        [Parameter()]
        [System.String]
        $Network_access_Shares_that_can_be_accessed_anonymously,

        [Parameter()]
        [ValidateSet("Classic - Local users authenticate as themselves", "Guest only - Local users authenticate as Guest")]
        [System.String]
        $Network_access_Sharing_and_security_model_for_local_accounts,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_security_Allow_Local_System_to_use_computer_identity_for_NTLM,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_security_Allow_LocalSystem_NULL_session_fallback,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_Security_Allow_PKU2U_authentication_requests_to_this_computer_to_use_online_identities,

        [Parameter()]
        [ValidateSet('DES_CBC_CRC', 'DES_CBC_MD5', 'RC4_HMAC_MD5', 'AES128_HMAC_SHA1', 'AES256_HMAC_SHA1', 'FUTURE')]
        [System.String[]]
        $Network_security_Configure_encryption_types_allowed_for_Kerberos,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_security_Do_not_store_LAN_Manager_hash_value_on_next_password_change,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_security_Force_logoff_when_logon_hours_expire,

        [Parameter()]
        [ValidateSet("Send LM & NTLM responses", "Send LM & NTLM - use NTLMv2 session security if negotiated", "Send NTLM responses only", "Send NTLMv2 responses only", "Send NTLMv2 responses only. Refuse LM", "Send NTLMv2 responses only. Refuse LM & NTLM")]
        [System.String]
        $Network_security_LAN_Manager_authentication_level,

        [Parameter()]
        [ValidateSet("None", "Negotiate Signing", "Require Signing")]
        [System.String]
        $Network_security_LDAP_client_signing_requirements,

        [Parameter()]
        [ValidateSet("Require NTLMv2 session security", "Require 128-bit encryption", "Both options checked")]
        [System.String]
        $Network_security_Minimum_session_security_for_NTLM_SSP_based_including_secure_RPC_clients,

        [Parameter()]
        [ValidateSet("Require NTLMv2 session security", "Require 128-bit encryption", "Both options checked")]
        [System.String]
        $Network_security_Minimum_session_security_for_NTLM_SSP_based_including_secure_RPC_servers,

        [Parameter()]
        [System.String]
        $Network_security_Restrict_NTLM_Add_remote_server_exceptions_for_NTLM_authentication,

        [Parameter()]
        [System.String]
        $Network_security_Restrict_NTLM_Add_server_exceptions_in_this_domain,

        [Parameter()]
        [ValidateSet("Allow all", "Deny all domain accounts", "Deny all accounts")]
        [System.String]
        $Network_Security_Restrict_NTLM_Incoming_NTLM_Traffic,

        [Parameter()]
        [ValidateSet("Disable", "Deny for domain accounts to domain servers", "Deny for domain accounts", "Deny for domain servers", "Deny all")]
        [System.String]
        $Network_Security_Restrict_NTLM_NTLM_authentication_in_this_domain,

        [Parameter()]
        [ValidateSet("Allow all", "Audit all", "Deny all")]
        [System.String]
        $Network_Security_Restrict_NTLM_Outgoing_NTLM_traffic_to_remote_servers,

        [Parameter()]
        [ValidateSet("Disabled", "Enable auditing for domain accounts", "Enable auditing for all accounts")]
        [System.String]
        $Network_Security_Restrict_NTLM_Audit_Incoming_NTLM_Traffic,

        [Parameter()]
        [ValidateSet("Disable", "Enable for domain accounts to domain servers", "Enable for domain accounts", "Enable for domain servers", "Enable all")]
        [System.String]
        $Network_Security_Restrict_NTLM_Audit_NTLM_authentication_in_this_domain,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Recovery_console_Allow_automatic_administrative_logon,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Recovery_console_Allow_floppy_copy_and_access_to_all_drives_and_folders,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Shutdown_Allow_system_to_be_shut_down_without_having_to_log_on,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Shutdown_Clear_virtual_memory_pagefile,

        [Parameter()]
        [ValidateSet("User input is not required when new keys are stored and used", "User is prompted when the key is first used", "User must enter a password each time they use a key")]
        [System.String]
        $System_cryptography_Force_strong_key_protection_for_user_keys_stored_on_the_computer,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $System_cryptography_Use_FIPS_compliant_algorithms_for_encryption_hashing_and_signing,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $System_objects_Require_case_insensitivity_for_non_Windows_subsystems,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $System_objects_Strengthen_default_permissions_of_internal_system_objects_eg_Symbolic_Links,

        [Parameter()]
        [System.String]
        $System_settings_Optional_subsystems,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $System_settings_Use_Certificate_Rules_on_Windows_Executables_for_Software_Restriction_Policies,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Admin_Approval_Mode_for_the_Built_in_Administrator_account,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Allow_UIAccess_applications_to_prompt_for_elevation_without_using_the_secure_desktop,

        [Parameter()]
        [ValidateSet("Elevate without prompting", "Prompt for credentials on the secure desktop", "Prompt for consent on the secure desktop", "Prompt for credentials", "Prompt for consent", "Prompt for consent for non-Windows binaries")]
        [System.String]
        $User_Account_Control_Behavior_of_the_elevation_prompt_for_administrators_in_Admin_Approval_Mode,

        [Parameter()]
        [ValidateSet("Automatically deny elevation request", "Prompt for credentials on the secure desktop", "Prompt for credentials")]
        [System.String]
        $User_Account_Control_Behavior_of_the_elevation_prompt_for_standard_users,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Detect_application_installations_and_prompt_for_elevation,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Only_elevate_executables_that_are_signed_and_validated,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Only_elevate_UIAccess_applications_that_are_installed_in_secure_locations,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Run_all_administrators_in_Admin_Approval_Mode,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Switch_to_the_secure_desktop_when_prompting_for_elevation,

        [Parameter()][ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Virtualize_file_and_registry_write_failures_to_per_user_locations
    )

    $registryPolicies = @()
    $systemAccessPolicies = @()
    $nonComplaintPolicies = @()
    $securityOptionList = Get-PolicyOptionList -ModuleName MSFT_SecurityOption
    $securityOptionData = Get-PolicyOptionData -FilePath $("$PSScriptRoot\SecurityOptionData.psd1").Normalize()
    $script:seceditOutput = "$env:TEMP\Secedit-OutPut.txt"
    $securityOptionsToAddInf = "$env:TEMP\securityOptionsToAdd.inf"

    $desiredPolicies = $PSBoundParameters.GetEnumerator() | Where-Object -FilterScript { $PSItem.key -in $securityOptionList }

    foreach ($policy in $desiredPolicies)
    {
        $testParameters = @{
            Name        = 'Test'
            $policy.Key = $policy.Value
            Verbose     = $false
        }

        <#
            Define what policies are not in a desired state so we only add those policies
            that need to be changed to the INF consumed by secedit.exe.
        #>
        $isInDesiredState = Test-TargetResource @testParameters
        if (-not ($isInDesiredState))
        {
            $policyKey = $policy.Key
            $policyData = $securityOptionData.$policyKey
            $nonComplaintPolicies += $policyKey

            if ($policyData.Option.GetEnumerator().Name -eq 'String')
            {
                if ([String]::IsNullOrWhiteSpace($policyData.Option.String))
                {
                    $newValue = $policy.Value
                }
                else
                {
                    if ($policy.Key -eq 'Interactive_logon_Message_text_for_users_attempting_to_log_on')
                    {
                        $message = Format-LogonMessage -Message $policy.Value
                        $newValue = "$($policyData.Option.String)" + $message
                    }
                    elseif ($policy.Key -eq 'Network_access_Restrict_clients_allowed_to_make_remote_calls_to_SAM')
                    {
                        $message = Format-RestrictedRemoteSam -SecurityDescriptor $policy.Value
                        $newValue = "$($policyData.Option.String)" + $message
                    }
                    else
                    {
                        $newValue = "$($policyData.Option.String)" + "$($policy.Value)"
                    }
                }
            }
            elseif ($policy.Key -eq 'Network_security_Configure_encryption_types_allowed_for_Kerberos')
            {
                $newValue = ConvertTo-KerberosEncryptionValue -EncryptionType $policy.Value
            }
            else
            {
                $newValue = $($policyData.Option[$policy.value])
            }

            if ($policyData.Section -eq 'System Access')
            {
                $systemAccessPolicies += "$($policyData.Value)=$newValue"
            }
            else
            {
                $registryPolicies += "$($policyData.Value)=$newValue"
            }
        }
    }

    $infTemplate = Add-PolicyOption -SystemAccessPolicies $systemAccessPolicies -RegistryPolicies $registryPolicies
    Out-File -InputObject $infTemplate -FilePath $securityOptionsToAddInf -Encoding unicode -Force
    Invoke-Secedit -InfPath $securityOptionsToAddInf -SecEditOutput $script:seceditOutput

    $successResult = Test-TargetResource @PSBoundParameters

    if ($successResult -eq $false)
    {
        throw "$($script:localizedData.SetFailed -f $($nonComplaintPolicies -join ','))"
    }
    else
    {
        Write-Verbose -Message ($script:localizedData.SetSuccess)
    }
}

<#
    .SYNOPSIS
         Tests the current security options against the desired configuration
#>
function Test-TargetResource
{
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingPlainTextForPassword", "")]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingUserNameAndPassWordParams", "")]
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Accounts_Administrator_account_status,

        [Parameter()]
        [ValidateSet("This policy is disabled", "Users cant add Microsoft accounts", "Users cant add or log on with Microsoft accounts")]
        [System.String]
        $Accounts_Block_Microsoft_accounts,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Accounts_Guest_account_status,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Accounts_Limit_local_account_use_of_blank_passwords_to_console_logon_only,

        [Parameter()]
        [System.String]
        $Accounts_Rename_administrator_account,

        [Parameter()]
        [System.String]
        $Accounts_Rename_guest_account,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Audit_Audit_the_access_of_global_system_objects,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Audit_Audit_the_use_of_Backup_and_Restore_privilege,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Audit_Force_audit_policy_subcategory_settings_Windows_Vista_or_later_to_override_audit_policy_category_settings,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Audit_Shut_down_system_immediately_if_unable_to_log_security_audits,

        [Parameter()]
        [System.String]
        $DCOM_Machine_Access_Restrictions_in_Security_Descriptor_Definition_Language_SDDL_syntax,

        [Parameter()]
        [System.String]
        $DCOM_Machine_Launch_Restrictions_in_Security_Descriptor_Definition_Language_SDDL_syntax,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Devices_Allow_undock_without_having_to_log_on,

        [Parameter()]
        [ValidateSet("Administrators", "Administrators and Power Users", "Administrators and Interactive Users")]
        [System.String]
        $Devices_Allowed_to_format_and_eject_removable_media,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Devices_Prevent_users_from_installing_printer_drivers,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Devices_Restrict_CD_ROM_access_to_locally_logged_on_user_only,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Devices_Restrict_floppy_access_to_locally_logged_on_user_only,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_controller_Allow_server_operators_to_schedule_tasks,

        [Parameter()]
        [ValidateSet("None", "Require Signing")]
        [System.String]
        $Domain_controller_LDAP_server_signing_requirements,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_controller_Refuse_machine_account_password_changes,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_member_Digitally_encrypt_or_sign_secure_channel_data_always,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_member_Digitally_encrypt_secure_channel_data_when_possible,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_member_Digitally_sign_secure_channel_data_when_possible,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_member_Disable_machine_account_password_changes,

        [Parameter()]
        [ValidateRange(0, 999)]
        [System.String]
        $Domain_member_Maximum_machine_account_password_age,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Domain_member_Require_strong_Windows_2000_or_later_session_key,

        [Parameter()]
        [ValidateSet("User displayname, domain and user names", "User display name only", "Do not display user information")]
        [System.String]
        $Interactive_logon_Display_user_information_when_the_session_is_locked,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Interactive_logon_Do_not_display_last_user_name,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Interactive_logon_Do_not_require_CTRL_ALT_DEL,

        [Parameter()]
        [ValidateRange(0, 999)]
        [System.String]
        $Interactive_logon_Machine_account_lockout_threshold,

        [Parameter()]
        [ValidateRange(0, 599940)]
        [System.String]
        $Interactive_logon_Machine_inactivity_limit,

        [Parameter()]
        [System.String]
        $Interactive_logon_Message_text_for_users_attempting_to_log_on,

        [Parameter()]
        [System.String]
        $Interactive_logon_Message_title_for_users_attempting_to_log_on,

        [Parameter()]
        [ValidateRange(0, 50)]
        [System.String]
        $Interactive_logon_Number_of_previous_logons_to_cache_in_case_domain_controller_is_not_available,

        [Parameter()]
        [ValidateRange(1, 999)]
        [System.String]
        $Interactive_logon_Prompt_user_to_change_password_before_expiration,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Interactive_logon_Require_Domain_Controller_authentication_to_unlock_workstation,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Interactive_logon_Require_smart_card,

        [Parameter()]
        [ValidateSet("No Action", "Lock workstation", "Force logoff", "Disconnect if a remote Remote Desktop Services session")]
        [System.String]
        $Interactive_logon_Smart_card_removal_behavior,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_client_Digitally_sign_communications_always,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_client_Digitally_sign_communications_if_server_agrees,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_client_Send_unencrypted_password_to_third_party_SMB_servers,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.String]
        $Microsoft_network_server_Amount_of_idle_time_required_before_suspending_session,

        [Parameter()]
        [ValidateSet("Default", "Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_server_Attempt_S4U2Self_to_obtain_claim_information,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_server_Digitally_sign_communications_always,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_server_Digitally_sign_communications_if_client_agrees,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Microsoft_network_server_Disconnect_clients_when_logon_hours_expire,

        [Parameter()]
        [ValidateSet("Off", "Accept if provided by client", "Required from client")]
        [System.String]
        $Microsoft_network_server_Server_SPN_target_name_validation_level,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Allow_anonymous_SID_Name_translation,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Do_not_allow_anonymous_enumeration_of_SAM_accounts,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Do_not_allow_anonymous_enumeration_of_SAM_accounts_and_shares,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Do_not_allow_storage_of_passwords_and_credentials_for_network_authentication,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Let_Everyone_permissions_apply_to_anonymous_users,

        [Parameter()]
        [System.String]
        $Network_access_Named_Pipes_that_can_be_accessed_anonymously,

        [Parameter()]
        [System.String]
        $Network_access_Remotely_accessible_registry_paths,

        [Parameter()]
        [System.String]
        $Network_access_Remotely_accessible_registry_paths_and_subpaths,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_access_Restrict_anonymous_access_to_Named_Pipes_and_Shares,

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $Network_access_Restrict_clients_allowed_to_make_remote_calls_to_SAM,

        [Parameter()]
        [System.String]
        $Network_access_Shares_that_can_be_accessed_anonymously,

        [Parameter()]
        [ValidateSet("Classic - Local users authenticate as themselves", "Guest only - Local users authenticate as Guest")]
        [System.String]
        $Network_access_Sharing_and_security_model_for_local_accounts,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_security_Allow_Local_System_to_use_computer_identity_for_NTLM,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_security_Allow_LocalSystem_NULL_session_fallback,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_Security_Allow_PKU2U_authentication_requests_to_this_computer_to_use_online_identities,

        [Parameter()]
        [ValidateSet('DES_CBC_CRC','DES_CBC_MD5','RC4_HMAC_MD5','AES128_HMAC_SHA1','AES256_HMAC_SHA1','FUTURE')]
        [System.String[]]
        $Network_security_Configure_encryption_types_allowed_for_Kerberos,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_security_Do_not_store_LAN_Manager_hash_value_on_next_password_change,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Network_security_Force_logoff_when_logon_hours_expire,

        [Parameter()]
        [ValidateSet("Send LM & NTLM responses", "Send LM & NTLM - use NTLMv2 session security if negotiated", "Send NTLM responses only", "Send NTLMv2 responses only", "Send NTLMv2 responses only. Refuse LM", "Send NTLMv2 responses only. Refuse LM & NTLM")]
        [System.String]
        $Network_security_LAN_Manager_authentication_level,

        [Parameter()]
        [ValidateSet("None", "Negotiate Signing", "Require Signing")]
        [System.String]
        $Network_security_LDAP_client_signing_requirements,

        [Parameter()]
        [ValidateSet("Require NTLMv2 session security", "Require 128-bit encryption", "Both options checked")]
        [System.String]
        $Network_security_Minimum_session_security_for_NTLM_SSP_based_including_secure_RPC_clients,

        [Parameter()]
        [ValidateSet("Require NTLMv2 session security", "Require 128-bit encryption", "Both options checked")]
        [System.String]
        $Network_security_Minimum_session_security_for_NTLM_SSP_based_including_secure_RPC_servers,

        [Parameter()]
        [System.String]
        $Network_security_Restrict_NTLM_Add_remote_server_exceptions_for_NTLM_authentication,

        [Parameter()]
        [System.String]
        $Network_security_Restrict_NTLM_Add_server_exceptions_in_this_domain,

        [Parameter()]
        [ValidateSet("Allow all", "Deny all domain accounts", "Deny all accounts")]
        [System.String]
        $Network_Security_Restrict_NTLM_Incoming_NTLM_Traffic,

        [Parameter()]
        [ValidateSet("Disable", "Deny for domain accounts to domain servers", "Deny for domain accounts", "Deny for domain servers", "Deny all")]
        [System.String]
        $Network_Security_Restrict_NTLM_NTLM_authentication_in_this_domain,

        [Parameter()]
        [ValidateSet("Allow all", "Audit all", "Deny all")]
        [System.String]
        $Network_Security_Restrict_NTLM_Outgoing_NTLM_traffic_to_remote_servers,

        [Parameter()]
        [ValidateSet("Disabled", "Enable auditing for domain accounts", "Enable auditing for all accounts")]
        [System.String]
        $Network_Security_Restrict_NTLM_Audit_Incoming_NTLM_Traffic,

        [Parameter()]
        [ValidateSet("Disable", "Enable for domain accounts to domain servers", "Enable for domain accounts", "Enable for domain servers", "Enable all")]
        [System.String]
        $Network_Security_Restrict_NTLM_Audit_NTLM_authentication_in_this_domain,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Recovery_console_Allow_automatic_administrative_logon,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Recovery_console_Allow_floppy_copy_and_access_to_all_drives_and_folders,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Shutdown_Allow_system_to_be_shut_down_without_having_to_log_on,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Shutdown_Clear_virtual_memory_pagefile,

        [Parameter()]
        [ValidateSet("User input is not required when new keys are stored and used", "User is prompted when the key is first used", "User must enter a password each time they use a key")]
        [System.String]
        $System_cryptography_Force_strong_key_protection_for_user_keys_stored_on_the_computer,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $System_cryptography_Use_FIPS_compliant_algorithms_for_encryption_hashing_and_signing,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $System_objects_Require_case_insensitivity_for_non_Windows_subsystems,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $System_objects_Strengthen_default_permissions_of_internal_system_objects_eg_Symbolic_Links,

        [Parameter()]
        [System.String]
        $System_settings_Optional_subsystems,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $System_settings_Use_Certificate_Rules_on_Windows_Executables_for_Software_Restriction_Policies,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Admin_Approval_Mode_for_the_Built_in_Administrator_account,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Allow_UIAccess_applications_to_prompt_for_elevation_without_using_the_secure_desktop,

        [Parameter()]
        [ValidateSet("Elevate without prompting", "Prompt for credentials on the secure desktop", "Prompt for consent on the secure desktop", "Prompt for credentials", "Prompt for consent", "Prompt for consent for non-Windows binaries")]
        [System.String]
        $User_Account_Control_Behavior_of_the_elevation_prompt_for_administrators_in_Admin_Approval_Mode,

        [Parameter()]
        [ValidateSet("Automatically deny elevation request", "Prompt for credentials on the secure desktop", "Prompt for credentials")]
        [System.String]
        $User_Account_Control_Behavior_of_the_elevation_prompt_for_standard_users,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Detect_application_installations_and_prompt_for_elevation,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Only_elevate_executables_that_are_signed_and_validated,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Only_elevate_UIAccess_applications_that_are_installed_in_secure_locations,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Run_all_administrators_in_Admin_Approval_Mode,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Switch_to_the_secure_desktop_when_prompting_for_elevation,

        [Parameter()][ValidateSet("Enabled", "Disabled")]
        [System.String]
        $User_Account_Control_Virtualize_file_and_registry_write_failures_to_per_user_locations
    )

    $results = @()
    $currentSecurityOptions = Get-TargetResource -Name $Name -Verbose:0

    $desiredSecurityOptions = $PSBoundParameters

    foreach ($policy in $desiredSecurityOptions.Keys)
    {
        if ($currentSecurityOptions.ContainsKey($policy))
        {
            if ($policy -eq 'Interactive_logon_Message_text_for_users_attempting_to_log_on')
            {
                $desiredSecurityOptionValue = Format-LogonMessage -Message $desiredSecurityOptions[$policy]
            }
            elseif ($policy -eq 'Network_access_Restrict_clients_allowed_to_make_remote_calls_to_SAM')
            {
                $testRemoteSamParameters = @{
                    DesiredSetting = $Network_access_Restrict_clients_allowed_to_make_remote_calls_to_SAM
                    CurrentSetting = $currentSecurityOptions.Network_access_Restrict_clients_allowed_to_make_remote_calls_to_SAM
                }

                $results += Test-RestrictedRemoteSam @testRemoteSamParameters
                continue
            }
            else
            {
                $desiredSecurityOptionValue = $desiredSecurityOptions[$policy]
            }
            Write-Verbose -Message ( $script:localizedData.TestingPolicy -f $policy )
            Write-Verbose -Message ( $script:localizedData.PoliciesBeingCompared `
                    -f $($currentSecurityOptions[$policy] -join ',' ), $($desiredSecurityOptionValue -join ',' ) )

            if ($desiredSecurityOptionValue -is [array])
            {
                $compareResult = Compare-Array -ReferenceObject @($currentSecurityOptions[$policy]) -DifferenceObject $desiredSecurityOptionValue

                if ( -not $compareResult )
                {
                    $results += $false
                }
            }
            else
            {
                $results += ($($currentSecurityOptions[$policy]) -eq $desiredSecurityOptionValue)
            }
        }
    }

    return ($results -notcontains $false)
}

<#
    .SYNOPSIS
        Convert Kerberos encrytion numeric values to their corresponding value names

    .PARAMETER EncryptionValue
        Specifies the encryption value to convert
#>
function ConvertTo-KerberosEncryptionOption
{
    [OutputType([System.String[]])]
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [System.String]
        $EncryptionValue
    )

    $reverseOptions = @{}
    $kerberosSecurityOptionName = "Network_security_Configure_encryption_types_allowed_for_Kerberos"
    $securityOptionData = Get-PolicyOptionData -FilePath $("$PSScriptRoot\SecurityOptionData.psd1").Normalize()
    $kerberosOptionValues = $securityOptionData[$kerberosSecurityOptionName].Option

    $newValue = $(($EncryptionValue -split ',')[-1])

    foreach ($entry in $kerberosOptionValues.GetEnumerator())
    {
        $value = ($entry.Value -split ',')[-1]
        $reverseOptions.Add($value, $entry.Name)
    }

    $result = $reverseOptions.Keys | Where-Object -FilterScript { $PSItem -band $newValue } | ForEach-Object -Process { $reverseOptions.Get_Item($PSItem) }
    return $result
}

<#
    .SYNOPSIS
        Converts Kerberos encryption options to their corresponding numeric value

    .PARAMETER EncryptionType
        Specifies the EncryptionType that will be converted to their corresponding value(s).

    .NOTES
        The Network_security_Configure_encryption_types_allowed_for_Kerberos option has multiple values.
        Each value is represented by a number that is incremented exponentially by 2.  When allowing
        multiple options we have to add those values.
#>
function ConvertTo-KerberosEncryptionValue
{
    [OutputType([System.String])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true)]
        [ValidateSet('DES_CBC_CRC','DES_CBC_MD5','RC4_HMAC_MD5','AES256_HMAC_SHA1','AES128_HMAC_SHA1','FUTURE')]
        [System.String[]]
        $EncryptionType
    )

    $sumResult = 0
    $kerberosSecurityOptionName = "Network_security_Configure_encryption_types_allowed_for_Kerberos"
    $securityOptionData = Get-PolicyOptionData -FilePath $("$PSScriptRoot\SecurityOptionData.psd1").Normalize()
    $kerberosOptionValues = $securityOptionData[$kerberosSecurityOptionName].Option

    foreach ($type in $EncryptionType)
    {
        $sumResult = $sumResult + ($kerberosOptionValues.$type -split ',')[-1]
    }

    return $('4,' + $sumResult)
}

<#
    .SYNOPSIS
        Compares values that have array as a type
#>
function Compare-Array
{
    [OutputType([System.Boolean])]
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [System.String[]]
        $ReferenceObject,

        [Parameter()]
        [System.String[]]
        $DifferenceObject

    )

    return $null -eq (Compare-Object $ReferenceObject $DifferenceObject).SideIndicator
}

<#
    .SYNOPSIS
        Secedit.exe uses an INI file with security policies and their associated values (key value pair).
        The value to a policy must be on one line. If the message is a multiple line message a comma is used
        for the line break and if a comma is intended for grammar it must be surrounded with double quotes.

    .PARAMETER Message
        The logon message to be formated
#>
function Format-LogonMessage
{
    [OutputType([System.String])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]
        $Message
    )

    $formatText = $Message -split '\n'

    if ($formatText.count -gt 1)
    {
        $lines = $formatText -split '\n' | ForEach-Object -Process { ($PSItem -replace ',', '","').Trim() }
        $resultValue = $lines -join ','
    }
    else
    {
        $resultValue = $formatText
    }

    return $resultValue
}

<#
    .SYNOPSIS
        Converts the Permission and Identity for the 'Network access Restrict clients allowed to make remote calls to SAM' setting
        before it's written to the INF file consumed by secedit.exe

    .PARAMETER SecurityDescriptor
        A collection containing the Permission and Identity for the 'Network access Restrict clients allowed to make remote calls to SAM' setting.
#>
function Format-RestrictedRemoteSam
{
    [OutputType([System.String])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [object[]]
        $SecurityDescriptor
    )

    $preamble = "O:BAG:BAD:"

    foreach ($descriptor in $SecurityDescriptor)
    {
        $resolvedIdentity = ConvertTo-LocalFriendlyName -Identity $descriptor.Identity -Scope Set

        if ($resolvedIdentity -eq 'BUILTIN\Administrators')
        {
            $resolvedIdentity = 'BA'
        }
        else
        {
            $resolvedIdentity = ConvertTo-Sid -Identity $resolvedIdentity -Scope Set
        }

        # secpol.msc will display an error message if permissions pertaining to the builtin Administrators group is not displayed last in the security descriptor.
        if ($resolvedIdentity -eq 'BA')
        {
            $ending = "({0};;RC;;;{1})" -f $descriptor.Permission[0], $resolvedIdentity
            continue
        }

        $result = $result + "({0};;RC;;;{1})" -f $descriptor.Permission[0], $resolvedIdentity
    }

    return ('"' + $preamble + $result + $ending + '"')
}

<#
    .SYNOPSIS
        Converts a security descriptor to a MSFT_RestrictedRemoteSamSecurityDescriptor CimInstance.

    .PARAMETER InputObject
        Specifies the security descriptor to convert.
#>
function ConvertTo-CimRestrictedRemoteSam
{
    [CmdletBinding()]
    [OutputType([Microsoft.Management.Infrastructure.CimInstance[]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [System.String]
        $InputObject
    )

    # Match anything between parenthesis.
    $pattern = '(?<=\()[\s\S]*?(?=\))'
    $cimCollection = New-Object -TypeName 'System.Collections.ObjectModel.Collection`1[Microsoft.Management.Infrastructure.CimInstance]'

    # Parse security descriptor from secedit output.
    $descriptorCollection = Select-String -InputObject $InputObject -Pattern $pattern -AllMatches

    foreach ($descriptor in $descriptorCollection.Matches.Value)
    {
        $cimProperties = @{}
        $permission, $identity = $descriptor -split ';' | Select-Object -First 1 -Last 1

        switch ($permission)
        {
            'A'
            {
                $cimProperties.Add('Permission', 'Allow')
            }
            'D'
            {
                $cimProperties.Add('Permission', 'Deny')
            }
        }

        switch ($identity)
        {
            'BA'
            {
                $cimProperties.Add('Identity', (ConvertTo-LocalFriendlyName -Identity 'S-1-5-32-544'))
            }
            default
            {
                $cimProperties.Add('Identity', (ConvertTo-LocalFriendlyName -Identity $identity))
            }
        }

        $cimInstanceParameters = @{
            ClassName  = 'MSFT_RestrictedRemoteSamSecurityDescriptor'
            Namespace  = 'root/microsoft/Windows/DesiredStateConfiguration'
            Property   = $cimProperties
            ClientOnly = $true
        }

        $cimCollection += New-CimInstance @cimInstanceParameters
    }

    return $cimCollection
}

<#
    .SYNOPSIS
        Asserts 'Network access Restrict clients allowed to make remote calls to SAM' is in a desired state.

    .PARAMETER DesiredSetting
        Specifies the desired settings.

    .PARAMETER CurrentSetting
        Specifies the current state of the security setting.
#>
function Test-RestrictedRemoteSam
{
    [OutputType([bool])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $DesiredSetting,

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $CurrentSetting
    )

    $identitiesNotInDesiredState = @()
    $permissionsNotInDesiredState = @()

    foreach ($setting in $DesiredSetting)
    {
        $resolvedIdentity = ConvertTo-LocalFriendlyName -Identity $setting.Identity
        $permissionToTest = ($CurrentSetting | Where-Object -Property Identity -eq $resolvedIdentity).Permission

        if ($resolvedIdentity -notin $CurrentSetting.Identity)
        {
            Write-Verbose -Message ($script:localizedData.RestrictedRemoteSamIdentity -f $resolvedIdentity)
            $identitiesNotInDesiredState += $resolvedIdentity
        }

        if ($setting.Permission -ne $permissionToTest)
        {
            Write-Verbose -Message ($script:localizedData.RestrictedRemoteSamPermission -f $setting.Permission, $resolvedIdentity)
            $permissionsNotInDesiredState += $setting.Permission
        }
    }

    $result = $identitiesNotInDesiredState + $permissionsNotInDesiredState

    # If nothing is added to $result then we are in a desired state.
    return ($result.Length -eq 0)
}

Export-ModuleMember -Function *-TargetResource
