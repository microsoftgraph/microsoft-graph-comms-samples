
@{
    "Enforce_password_history" = @{
        Value   = 'PasswordHistorySize'
        Section = 'System Access'
        Option  = @{
            String = ''
        }
    }

    "Maximum_Password_Age" = @{
        Value   = 'MaximumPasswordAge'
        Section = 'System Access'
        Option  = @{
            String = ''
        }
    }

    "Minimum_Password_Age" = @{
        Value   = 'MinimumPasswordAge'
        Section = 'System Access'
        Option  = @{
            String = ''
        }
    }

    "Minimum_Password_Length" = @{
        Value   = 'MinimumPasswordLength'
        Section = 'System Access'
        Option  = @{
            String = ''
        }
    }

    "Password_must_meet_complexity_requirements" = @{
        Value   = 'PasswordComplexity'
        Section = 'System Access'
        Option  = @{
            Enabled  = '1'
            Disabled = '0'
        }
    }

    "Store_passwords_using_reversible_encryption" = @{
        Value   = 'ClearTextPassword'
        Section = 'System Access'
        Option  = @{
            Enabled = '1'
            Disabled = '0'
        }
    }

    "Account_lockout_duration" = @{
        Value   = 'LockoutDuration'
        Section = 'System Access'
        Option  = @{
            String = ''
        }
    }

    "Account_lockout_threshold" = @{
        Value   = 'LockoutBadCount'
        Section = 'System Access'
        Option  = @{
            String = ''
        }
    }

    "Reset_account_lockout_counter_after" = @{
        Value   = 'ResetLockoutCount'
        Section = 'System Access'
        Option  = @{
            String = ''
        }
    }

    "Enforce_user_logon_restrictions" = @{
        Value   = 'TicketValidateClient'
        Section = 'Kerberos Policy'
        Option  = @{
            Enabled = '1'
            Disabled = '0'
        }
    }

    "Maximum_lifetime_for_service_ticket" = @{
        Value   = 'MaxServiceAge'
        Section = 'Kerberos Policy'
        Option  = @{
            String = ''
        }
    }

    "Maximum_lifetime_for_user_ticket" = @{
        Value   = 'MaxTicketAge'
        Section = 'Kerberos Policy'
        Option  = @{
            String = ''
        }
    }

    "Maximum_lifetime_for_user_ticket_renewal" = @{
        Value   = 'MaxRenewAge'
        Section = 'Kerberos Policy'
        Option  = @{
            String = ''
        }
    }

    "Maximum_tolerance_for_computer_clock_synchronization" = @{
        Value   = 'MaxClockSkew'
        Section = 'Kerberos Policy'
        Option  = @{
            String = ''
        }
    }
}
