# Localized resources for DSC_NetworkTeam

ConvertFrom-StringData @'
    GetTeamInfo = Getting network team information for '{0}'.
    FoundTeam = Found a network team with name '{0}'.
    TeamMembersMatch = Members in the network team '{0}' exist as per the configuration.
    TeamMembersNotMatch = Members in the network team '{0}' do not match configuration.
    TeamNotFound = Network team with name '{0}' not found.
    LoadBalancingAlgorithmDifferent = Load Balancing algorithm is different from the requested '{0}' algorithm.
    TeamingModeDifferent = Teaming mode is different from the requested '{0}' mode.
    ModifyTeam = Modifying the network team named '{0}'.
    MembersDifferent = Members within the team named '{0}' are different from that requested in the configuration.
    RemovingMembers = Removing members '{0}' not specified in the configuration.
    AddingMembers = Adding members '{0}' that are not a part of the team configuration.
    CreateTeam = Creating a network team with the name '{0}'.
    RemoveTeam = Removing a network team with the name '{0}'.
    TeamExistsNoAction = Network team with name '{0}' exists. No action needed.
    TeamExistsWithDifferentConfig = Network team with name '{0}' exists but with different configuration. This will be modified.
    TeamDoesNotExistShouldCreate = Network team with name '{0}' does not exist. It will be created.
    TeamExistsShouldRemove = Network team with name '{0}' exists. It will be removed.
    TeamDoesNotExistNoAction = Network team with name '{0}' does not exist. No action needed.
    CreatedNetTeam = Network Team was created successfully.
    FailedToCreateTeam = Failed to create the network team with specific configuration: {0}.
'@
