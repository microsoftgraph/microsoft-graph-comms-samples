# culture="en-US"
ConvertFrom-StringData -StringData @'
        ErrorAppCmdNonZeroExitCode        = AppCmd.exe has exited with error code "{0}".
        VerboseAppPoolFound               = Application pool "{0}" was found.
        VerboseAppPoolNotFound            = Application pool "{0}" was not found.
        VerboseEnsureNotInDesiredState    = The "Ensure" state of application pool "{0}" does not match the desired state.
        VerbosePropertyNotInDesiredState  = The "{0}" property of application pool "{1}" does not match the desired state.
        VerboseCredentialToBeCleared      = Custom account credentials of application pool "{0}" need to be cleared because the "identityType" property is not set to "SpecificUser".
        VerboseCredentialToBeIgnored      = The "Credential" property is only valid when the "identityType" property is set to "SpecificUser".
        VerboseResourceInDesiredState     = The target resource is already in the desired state. No action is required.
        VerboseResourceNotInDesiredState  = The target resource is not in the desired state.
        VerboseNewAppPool                 = Creating application pool "{0}".
        VerboseRemoveAppPool              = Removing application pool "{0}".
        VerboseStartAppPool               = Starting application pool "{0}".
        VerboseStopAppPool                = Stopping application pool "{0}".
        VerboseSetProperty                = Setting the "{0}" property of application pool "{1}".
        VerboseClearCredential            = Clearing custom account credentials of application pool "{0}" because the "identityType" property is not set to "SpecificUser".
        VerboseRestartScheduleValueAdd    = Adding value "{0}" to the "restartSchedule" collection of application pool "{1}".
        VerboseRestartScheduleValueRemove = Removing value "{0}" from the "restartSchedule" collection of application pool "{1}".
'@
