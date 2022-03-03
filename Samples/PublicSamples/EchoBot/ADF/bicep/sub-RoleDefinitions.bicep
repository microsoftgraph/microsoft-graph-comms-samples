param Prefix string

@allowed([
    'I'
    'D'
    'T'
    'U'
    'P'
    'S'
    'G'
    'A'
])
param Environment string

@allowed([
    '0'
    '1'
    '2'
    '3'
    '4'
    '5'
    '6'
    '7'
    '8'
    '9'
])
param DeploymentID string
param Stage object
param Extensions object
param Global object
param DeploymentInfo object

targetScope = 'subscription'

var Enviro = '${Environment}${DeploymentID}' // D1
var Deployment = '${Prefix}-${Global.orgname}-${Global.Appname}-${Enviro}' // AZE2-BRW-HUB-D1
var rg = '${Prefix}-${Global.orgname}-${Global.Appname}-RG-${Enviro}' // AZE2-BRW-HUB-D1

// move location lookup to include file referencing this table: 
// https://github.com/brwilkinson/AzureDeploymentFramework/blob/main/docs/Naming_Standards_Prefix.md

// var locationlookup = {
//     AZE2: 'eastus2'
//     AZC1: 'centralus'
//     AEU2: 'eastus2'
//     ACU1: 'centralus'
// }
// var location = locationlookup[Prefix]

var roleDefinitionsInfo = DeploymentInfo.RoleDefinitionsInfo

module roleDefinitions './sub-RoleDefinitions-Roles.bicep' = [for (rd, index) in roleDefinitionsInfo: {
    name: rd.RoleName
    params: {
        actions: rd.actions
        assignableScopes: contains(rd, 'assignableScopes') ? rd.assignableScopes : array(null)
        description: rd.description
        notActions: rd.notactions
        RoleName: rd.RoleName
    }
}]

output Enviro string = Enviro
output Deployment string = Deployment
// output location string = location
