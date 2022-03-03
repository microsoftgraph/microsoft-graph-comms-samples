ConvertFrom-StringData @'
    IdentityIsNullRemovingAll     = Identity is NULL. Removing all Identities from {0}
    GrantingPolicyRightsToIds     = Granting {0} rights to {1}
    TaskSuccess                   = Task successfully completed
    TaskFail                      = Task did not complete successfully
    TestIdentityIsPresentOnPolicy = Testing {0} is present on policy {1}
    NoIdentitiesFoundOnPolicy     = No identities found on {0}
    IdNotFoundOnPolicy            = {0} not found on {1}
    ErrorCantTranslateSID         = Error processing {0}. Error message: {1}
    EchoDebugInf                  = Temp inf {0}
    IdentityFoundExpectedNull     = Identity found on {0}. Expected NULL
    IdentityNotSpecified          = An Identity must be sepcified even if it is NULL
    AttemptingSetPolicy           = Attempting to Set {0} for policy {1}
    UserRightAppliedSuccess       = {0} successfully given rights to {1} policy
    IdentityDoesNotHaveRight      = {0} does not have Privilege {1}
    ShouldNotHaveRights           = {0} are users that should not have rights to {1} policy
'@
