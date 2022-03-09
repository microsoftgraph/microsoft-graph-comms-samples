@allowed([
  'AEU2'
  'ACU1'
  'AWU2'
  'AEU1'
])
param Prefix string = 'ACU1'

@allowed([
  'I'
  'D'
  'T'
  'U'
  'P'
  'S'
  'G'
  'A'
  'M'
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
param Stage object
param Extensions object
param Global object
param DeploymentInfo object

@secure()
param vmAdminPassword string

@secure()
param devOpsPat string

@secure()
param sshPublic string

targetScope = 'managementGroup'

var mgInfo = contains(DeploymentInfo, 'mgInfo') ? DeploymentInfo.mgInfo : []

var managementGroupInfo = [for (mg, index) in mgInfo: {
  match: ((Global.CN == '.') || contains(Global.CN, mg.name))
}]

@batchSize(1)
module mgInfo_displayName 'man-MG-ManagementGroups.bicep' = [for (mg,index) in mgInfo: if (managementGroupInfo[index].match) {
  name: 'dp-${mg.name}'
  params: {
    mgInfo: mg
  }
}]
