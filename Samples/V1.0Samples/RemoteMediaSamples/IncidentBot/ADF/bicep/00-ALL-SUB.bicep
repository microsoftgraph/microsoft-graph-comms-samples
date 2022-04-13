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

var Deployment = '${Prefix}-${Global.OrgName}-${Global.Appname}-${Environment}${DeploymentID}'

module dp_Deployment_RG 'sub-RG.bicep' = if (bool(Stage.RG) && (!('${DeploymentID}${Environment}' == 'G0'))) {
  name: 'dp${Deployment}-RG'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
  }
  dependsOn: []
}

module dp_Deployment_RBAC 'sub-RBAC.bicep' = if (bool(Stage.RBAC)) {
  name: 'dp${Deployment}-RBAC'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
  }
  dependsOn: [
    dp_Deployment_RG
  ]
}

module dp_Deployment_RoleDefinition 'sub-RoleDefinitions.bicep' = if (contains(Stage, 'RoleDefinition') && bool(Stage.RoleDefinition)) {
  name: 'dp${Deployment}-RoleDefinition'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
  }
  dependsOn: [
    dp_Deployment_RG
  ]
}
