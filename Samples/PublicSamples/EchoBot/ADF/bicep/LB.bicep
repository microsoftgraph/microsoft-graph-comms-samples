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
var subscriptionId = subscription().subscriptionId
var Domain = split(Global.DomainName, '.')[0]
var resourceGroupName = resourceGroup().name
var OMSworkspaceName = replace('${Deployment}LogAnalytics', '-', '')
var OMSworkspaceID = resourceId('Microsoft.OperationalInsights/workspaces/', OMSworkspaceName)
var VNetID = resourceId(subscriptionId, resourceGroupName, 'Microsoft.Network/VirtualNetworks', '${Deployment}-vn')
var networkId = '${Global.networkid[0]}${string((Global.networkid[1] - (2 * int(DeploymentID))))}'
var networkIdUpper = '${Global.networkid[0]}${string((1 + (Global.networkid[1] - (2 * int(DeploymentID)))))}'

var LBInfo = contains(DeploymentInfo, 'LBInfo') ? DeploymentInfo.LBInfo : []

var LB = [for (lb,Index) in LBInfo : {
  match: ((Global.CN == '.') || contains(Global.CN, lb.Name))
}]

module PublicIP 'x.publicIP.bicep' = [for (lb,index) in LBInfo: if(LB[index].match) {
  name: 'dp${Deployment}-LB-publicIPDeploy${lb.Name}'
  params: {
    Deployment: Deployment
    DeploymentID: DeploymentID
    NICs: lb.FrontEnd
    VM: lb
    PIPprefix: 'lb'
    Global: Global
    OMSworkspaceID: OMSworkspaceID
  }
}]

module LBs 'LB-LB.bicep' = [for (lb,index) in LBInfo: if(LB[index].match) {
  name: 'dp${Deployment}-LB-Deploy${lb.Name}'
  params: {
    Deployment: Deployment
    DeploymentID: DeploymentID
    backEndPools: (contains(lb, 'BackEnd') ? lb.BackEnd : json('[]'))
    NATRules: (contains(lb, 'NATRules') ? lb.NATRules : json('[]'))
    NATPools: (contains(lb, 'NATPools') ? lb.NATPools : json('[]'))
    outboundRules: (contains(lb, 'outboundRules') ? lb.outboundRules : json('[]'))
    Services: (contains(lb, 'Services') ? lb.Services : json('[]'))
    probes: (contains(lb, 'probes') ? lb.probes : json('[]'))
    LB: lb
    Global: Global
    OMSworkspaceID: OMSworkspaceID
  }
  dependsOn: [
    PublicIP
  ]
}]
