
$securityOptions = @{
    Accounts_Guest_account_status = 'Enabled'
    Accounts_Rename_guest_account = 'NewGuest'
    Accounts_Block_Microsoft_accounts = 'This policy is disabled'
    Network_access_Remotely_accessible_registry_paths_and_subpaths = 'Software\Microsoft\Windows NT\CurrentVersion\Print,Software\Microsoft\Windows NT\CurrentVersion\Windows,System\CurrentControlSet\Control\Print\Printers,System\CurrentControlSet\Services\Eventlog,Software\Microsoft\OLAP Server,System\CurrentControlSet\Control\ContentIndex,System\CurrentControlSet\Control\Terminal Server,System\CurrentControlSet\Control\Terminal Server\UserConfig,System\CurrentControlSet\Control\Terminal Server\DefaultUserConfiguration,Software\Microsoft\Windows NT\CurrentVersion\Perflib,System\CurrentControlSet\Services\SysmonLog'
}

configuration MSFT_SecurityOption_config
{
    Import-DscResource -ModuleName 'SecurityPolicyDsc'

    node localhost
    {
        SecurityOption Integration_Test
        {
            Name = 'IntegrationTest'
            Accounts_Guest_account_status = "$($securityOptions.Accounts_Guest_account_status)"
            Accounts_Rename_guest_account = "$($securityOptions.Accounts_Rename_guest_account)"
            Accounts_Block_Microsoft_accounts = "$($securityOptions.Accounts_Block_Microsoft_accounts)"
            Network_access_Remotely_accessible_registry_paths_and_subpaths = "$($securityOptions.Network_access_Remotely_accessible_registry_paths_and_subpaths)"
        }
    }
}
