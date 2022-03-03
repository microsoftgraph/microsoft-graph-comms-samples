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
var snAzureBastionSubnet = 'AzureBastionSubnet'

var bst = contains(DeploymentInfo, 'BastionInfo') ? DeploymentInfo.BastionInfo : {}

resource BastionSubnet 'Microsoft.Network/virtualNetworks/subnets@2021-02-01' existing = {
  name: '${Deployment}-vn/${snAzureBastionSubnet}'
}

module PublicIP 'x.publicIP.bicep' = if(contains(bst,'name')) {
  name: 'dp${Deployment}-Bastion-publicIPDeploy${bst.Name}'
  params: {
    Deployment: Deployment
    DeploymentID: DeploymentID
    NICs: array(bst)
    VM: bst
    PIPprefix: 'bst'
    Global: Global
    OMSworkspaceID: OMSworkspaceID
  }
}

resource Bastion 'Microsoft.Network/bastionHosts@2021-02-01' = if(contains(bst,'name')) {
  name: '${Deployment}-bst${bst.name}'
  location: resourceGroup().location
  properties: {
    dnsName: toLower('${Deployment}-${bst.name}.bastion.azure.com')
    ipConfigurations: [
      {
        name: 'IpConf'
        properties: {
          privateIPAllocationMethod: 'Dynamic'
          publicIPAddress: {
            id: PublicIP.outputs.PIPID[0]
          }
          subnet: {
            id: BastionSubnet.id
          }
        }
      }
    ]
  }
}

resource BastionDiagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = if(contains(bst,'name')) {
  name: 'service'
  scope: Bastion
  properties: {
    workspaceId: OMSworkspaceID
    logs: [
      {
        category: 'BastionAuditLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        timeGrain: 'PT5M'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

