param Deployment string
param Prefix string
param rgName string
param Enviro string
param Global object
param rolesLookup object = {}
param rolesGroupsLookup object = {}
param roleInfo object
param providerPath string
param namePrefix string
param providerAPI string
param principalType string = ''

targetScope = 'subscription'

// Role Assignments can be very difficult to troubleshoot, once a role assignment exists, it can only be redeployed if it has the same GUID for the name
// This code and outputs will ensure it's easy to troubleshoot and also that you have consistency in Deployments

// GUID will always have the following format concatenated together
// source Subscription ID
// source RGName where the UAI/Identity is created
// Name of the Role
// destination Subscription ID
// Destination RG, which is actually the Enviro e.g. G0
// The Destination Prefix or region e.g. AZE2
// The Destination Tenant or App e.g. PSO 
// Note if the destination info is not provides, assume it's local info
// Only the Name is required if local

var roleAssignment = [for rbac in roleInfo.RBAC : {
    SourceSubscriptionID: subscription().subscriptionId
    SourceRG: rgName
    RoleName: rbac.Name
    RoleID: rolesGroupsLookup[rbac.Name].Id
    DestSubscriptionID: (contains(rbac, 'SubscriptionID') ? rbac.SubScriptionID : subscription().subscriptionId)
    DestSubscription: (contains(rbac, 'SubscriptionID') ? rbac.SubScriptionID : subscription().id)
    DestManagementGroup: (contains(rbac, 'ManagementGroupName') ? rbac.ManagementGroupName : null)
    DestRG: (contains(rbac, 'RG') ? rbac.RG : Enviro)
    DestPrefix: (contains(rbac, 'Prefix') ? rbac.Prefix : Prefix)
    DestApp: (contains(rbac, 'Tenant') ? rbac.Tenant : Global.AppName)
    principalType: principalType
    GUID: guid(subscription().subscriptionId, rgName, roleInfo.Name, rbac.Name, (contains(rbac, 'SubscriptionID') ? rbac.SubScriptionID : subscription().subscriptionId), (contains(rbac, 'RG') ? rbac.RG : Enviro), (contains(rbac, 'Prefix') ? rbac.Prefix : Prefix), (contains(rbac, 'Tenant') ? rbac.Tenant : Global.AppName))
    FriendlyName: 'source: ${rgName} --> ${roleInfo.Name} --> ${rbac.Name} --> destination: ${(contains(rbac, 'Prefix') ? rbac.Prefix : Prefix)}-${(contains(rbac, 'RG') ? rbac.RG : Enviro)}-${(contains(rbac, 'Tenant') ? rbac.Tenant : Global.AppName)}'
}]

// // todo for MG
// resource mg 'Microsoft.Management/managementGroups@2021-04-01' existing = [for (rbac, index) in roleAssignment: if (Enviro == 'M0') {
//     name: rbac.DestManagementGroup
//     scope: tenant()
// }]

// module RBACRAMG 'sub-RBAC-ALL-RA-MG.bicep' = [for (rbac, index) in roleAssignment: if (Enviro == 'M0') {
//     name: replace('dp-rbac-all-ra-${roleInfo.name}-${index}','@','_')
//     scope: managementGroup(rbac.DestManagementGroup)
//     params:{
//         description: roleInfo.name
//         name: rbac.GUID
//         roledescription: rbac.RoleName
//         roleDefinitionId: '/providers/Microsoft.Authorization/roleDefinitions/${rbac.RoleID}'
//         principalType: rbac.principalType
//         principalId: providerPath == 'guid' ? roleInfo.name : length(providerPath) == 0 ? rolesLookup[roleInfo.name] : /*
//               */ reference('${rbac.DestSubscription}/resourceGroups/${rbac.SourceRG}/providers/${providerPath}/${Deployment}${namePrefix}${roleInfo.Name}',providerAPI).principalId
//     }
// }]

module RBACRASUB 'sub-RBAC-ALL-RA.bicep' = [for (rbac, index) in roleAssignment: if (Enviro == 'G0') {
    name: replace('dp-rbac-all-ra-${roleInfo.name}-${index}','@','_')
    scope: subscription()
    params:{
        description: roleInfo.name
        name: rbac.GUID
        roledescription: rbac.RoleName
        roleDefinitionId: '${rbac.DestSubscription}/providers/Microsoft.Authorization/roleDefinitions/${rbac.RoleID}'
        principalType: rbac.principalType
        principalId: providerPath == 'guid' ? roleInfo.name : length(providerPath) == 0 ? rolesLookup[roleInfo.name] : /*
              */ reference('${rbac.DestSubscription}/resourceGroups/${rbac.SourceRG}/providers/${providerPath}/${Deployment}${namePrefix}${roleInfo.Name}',providerAPI).principalId
    }
}]

module RBACRARG 'sub-RBAC-ALL-RA-RG.bicep' = [for (rbac, index) in roleAssignment: if (Enviro != 'G0' && Enviro != 'M0') {
    name: replace('dp-rbac-all-ra-${roleInfo.name}-${index}','@','_')
    scope: resourceGroup(rbac.DestSubscriptionID,'${rbac.DestPrefix}-${Global.OrgName}-${rbac.DestApp}-RG-${rbac.DestRG}')
    params:{
        description: roleInfo.name
        name: rbac.GUID
        roledescription: rbac.RoleName
        roleDefinitionId: '${rbac.DestSubscription}/providers/Microsoft.Authorization/roleDefinitions/${rbac.RoleID}'
        principalType: rbac.principalType
        principalId: providerPath == 'guid' ? roleInfo.name : length(providerPath) == 0 ? rolesLookup[roleInfo.name] : /*
              */ reference('${rbac.DestSubscription}/resourceGroups/${rbac.SourceRG}/providers/${providerPath}/${Deployment}${namePrefix}${roleInfo.Name}',providerAPI).principalId
    }
}]

output RoleAssignments array = roleAssignment

