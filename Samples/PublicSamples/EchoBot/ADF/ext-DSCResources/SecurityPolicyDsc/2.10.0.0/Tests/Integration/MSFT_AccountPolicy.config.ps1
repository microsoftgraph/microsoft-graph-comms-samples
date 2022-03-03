
$accountPolicies = @{
    Enforce_password_history = 15
    Maximum_Password_Age = 42
    Minimum_Password_Age = 1
    Minimum_Password_Length = 12
    Password_must_meet_complexity_requirements = 'Enabled'
    Store_passwords_using_reversible_encryption = 'Disabled'
}

configuration MSFT_AccountPolicy_config {

    Import-DscResource -ModuleName 'SecurityPolicyDsc'

    node localhost {

        AccountPolicy Integration_Test 
        {
            Name = 'IntegrationTest'
            Enforce_password_history = $accountPolicies.Enforce_password_history
            Maximum_Password_Age = $accountPolicies.Maximum_Password_Age
            Minimum_Password_Age = $accountPolicies.Minimum_Password_Age
            Minimum_Password_Length = $accountPolicies.Minimum_Password_Length
            Password_must_meet_complexity_requirements = $accountPolicies.Password_must_meet_complexity_requirements
            Store_passwords_using_reversible_encryption = $accountPolicies.Store_passwords_using_reversible_encryption
        }
    }
}
