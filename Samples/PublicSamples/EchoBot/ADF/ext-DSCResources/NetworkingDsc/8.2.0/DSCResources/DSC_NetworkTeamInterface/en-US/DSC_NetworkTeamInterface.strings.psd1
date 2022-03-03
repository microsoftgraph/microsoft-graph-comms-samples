# Localized resources for DSC_NetwrkTeamInterface

ConvertFrom-StringData @"
    GetTeamNicInfo = Getting network team interface information for '{0}'.
    FoundTeamNic = Found a network team interface with name '{0}'.
    TeamNicNotFound = Network team interface with name '{0}' not found.
    TeamNicVlanMismatch = Vlan ID is different from the requested ID of '{0}'.
    ModifyTeamNic = Modifying the network team interface named '{0}'.
    CreateTeamNic = Creating a network team interface with the name '{0}'.
    RemoveTeamNic = Removing a network team interface with the name '{0}'.
    TeamNicExistsNoAction = Network team interface with name '{0}' exists. No action needed.
    TeamNicExistsWithDifferentConfig = Network team interface with name '{0}' exists but with different configuration. This will be modified.
    TeamNicDoesNotExistShouldCreate = Network team interface with name '{0}' does not exist. It will be created.
    TeamNicExistsShouldRemove = Network team interface with name '{0}' exists. It will be removed.
    CreatedNetTeamNic = Network Team Interface was created successfully.
    FailedToCreateTeamNic = Failed to create the network team interface with specific configuration.
"@
