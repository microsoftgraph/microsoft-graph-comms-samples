ConvertFrom-StringData @'
    Section       = Section: {0}
    Value         = ValueName: {0}
    Option        = Options: {0}
    RawValue      = Raw current value: {0}
    TestingPolicy = Testing SecurityOption: {0}
    SetFailed     = Failed to update security option {0}. Refer to %windir%\\security\\logs\\scesrv.log for details.
    SetSuccess    = Successfully update security option
    PoliciesBeingCompared = Current policy: {0} Desired policy: {1}
    RetrievingValue = Retrieving value for {0}
    RestrictedRemoteSamIdentity =  Network access Restrict clients allowed to make remote calls to SAM: Identity {0} not found
    RestrictedRemoteSamPermission =  Network access Restrict clients allowed to make remote calls to SAM: Permission '{0}' not in desired state for {1}
'@
