
param Prefix string = 'AZE2'

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


var Deployment = '${Prefix}-${Global.OrgName}-${Global.Appname}-${Environment}${DeploymentID}'
var DeploymentURI = toLower('${Prefix}${Global.OrgName}${Global.Appname}${Environment}${DeploymentID}')

var OMSworkspaceName = '${DeploymentURI}LogAnalytics'
var OMSworkspaceID = resourceId('Microsoft.OperationalInsights/workspaces/', OMSworkspaceName)

var NATGWInfo = contains(DeploymentInfo, 'NATGWInfo') ? DeploymentInfo.NATGWInfo : []

var NGW = [for (ngw, index) in NATGWInfo: {
  match: ((Global.CN == '.') || contains(Global.CN, ngw.Name))
}]

module FireWall 'NATGW-NGW.bicep' = [for (ngw, index) in NATGWInfo: if(NGW[index].match) {
  name: 'dp${Deployment}-NATGW-Deploy${ngw.name}'
  params: {
    Deployment: Deployment
    DeploymentID: DeploymentID
    Environment: Environment
    NATGWInfo: ngw
    Global: Global
    Stage: Stage
    OMSworkspaceID: OMSworkspaceID
  }
}]
