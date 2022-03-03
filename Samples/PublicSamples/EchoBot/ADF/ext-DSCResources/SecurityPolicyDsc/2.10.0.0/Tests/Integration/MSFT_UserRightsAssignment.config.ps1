$script:DSCResourceName = 'MSFT_UserRightsAssignment'

$resourcePath = (Get-DscResource -Name $script:DSCResourceName).Path
Import-Module $resourcePath -Force

# S-1-5-6 = NT Authority\Service
# S-1-5-90-0 = 'window manager\window manager group'

$rule = @{
    Policy   = 'Access_Credential_Manager_as_a_trusted_caller'
    Identity = 'builtin\Administrators','*S-1-5-6','S-1-5-90-0'
}

$removeAll = @{    
    Policy = 'Act_as_part_of_the_operating_system'
    Identity = ""
}

$removeGuests = @{
    Policy = 'Deny_log_on_locally'
    Identity = 'Guests'
}

# Add an identities so we can verify it gets removed
Set-TargetResource -Policy $removeAll.Policy -Identity 'Administrators' -Ensure 'Present'
Set-TargetResource -Policy $removeGuests.Policy -Identity 'Guests' -Ensure 'Present'

configuration MSFT_UserRightsAssignment_config {
    Import-DscResource -ModuleName SecurityPolicyDsc
    
    UserRightsAssignment AccessCredentialManagerAsaTrustedCaller
    {
        Policy   = $rule.Policy
        Identity = $rule.Identity
    }
    
    UserRightsAssignment RemoveAllActAsOS
    {
        Policy   = $removeAll.Policy
        Identity = $removeAll.Identity
    }

    UserRightsAssignment DenyLogOnLocally
    {
        Policy   = $removeGuests.Policy
        Identity = $removeGuests.Identity
        Ensure   = 'Absent'
    }
}
