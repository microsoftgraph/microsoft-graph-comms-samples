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
param Environment string = 'D'

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
param DeploymentID string = '1'

param Global object
param Stage object
param Extensions object
param DeploymentInfo object

targetScope = 'subscription'

var region = deployment().location
var regionLookup = json(loadTextContent('./bicep/global/region.json'))
var Prefix = regionLookup[region].prefix
var resourceGroupName = '${Prefix}-${Global.OrgName}-${Global.AppName}-RG-${Environment}${DeploymentID}'
var Deployment = '${Prefix}-${Global.OrgName}-${Global.AppName}-${Environment}${DeploymentID}'



module dp_Deployment_SUB 'bicep/00-ALL-SUB.bicep' = if (Stage.SUB == 1) {
  name: 'dp${Deployment}-SUB'
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
}

module dp_Deployment_RG 'bicep/01-ALL-RG.bicep' = if (Stage.RG == 1) {
  name: 'dp${Deployment}-RG'
  scope: resourceGroup(resourceGroupName)
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
    dp_Deployment_SUB
  ]
}

output global object = Global
output rg string = resourceGroupName
output dp string = Deployment
