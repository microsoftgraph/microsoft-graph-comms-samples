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
// var hubRG = Global.hubRGName

var KeyVaultInfo = contains(DeploymentInfo, 'KVInfo') ? DeploymentInfo.KVInfo : []

var KVInfo = [for (kv, index) in KeyVaultInfo: {
  match: ((Global.CN == '.') || contains(Global.CN, kv.name))
}]

module KeyVaults 'KV-KeyVault.bicep' = [for (kv, index) in KeyVaultInfo: if (KVInfo[index].match) {
  name: 'dp${Deployment}-KV-${kv.name}'
  params: {
    Deployment: Deployment
    DeploymentID: DeploymentID
    Environment: Environment
    Prefix: Prefix
    KVInfo: kv
    Global: Global
    OMSworkspaceID: OMSworkspaceID
  }
}]

module vnetPrivateLink 'x.vNetPrivateLink.bicep' = [for (kv, index) in KeyVaultInfo: if(KVInfo[index].match && contains(kv, 'privatelinkinfo')) {
  name: 'dp${Deployment}-KV-privatelinkloop${kv.name}'
  params: {
    Deployment: Deployment
    PrivateLinkInfo: kv.privateLinkInfo
    providerType: 'Microsoft.KeyVault/vaults'
    resourceName: '${Deployment}-kv${kv.name}'
  }
}]

// module KVPrivateLinkDNS 'x.vNetprivateLinkDNS.bicep' = [for (kv, index) in KeyVaultInfo: if(KVInfo[index].match && contains(kv, 'privatelinkinfo')) {
//   name: 'dp${Deployment}-KV-registerPrivateDNS${kv.name}'
//   scope: resourceGroup(hubRG)
//   params: {
//     PrivateLinkInfo: kv.privateLinkInfo
//     providerURL: '.azure.net/'
//     resourceName: '${Deployment}-kv${((length(KeyVaultInfo) == 0) ? 'na' : kv.name)}'
//     Nics: contains(kv, 'privatelinkinfo') && length(KeyVaultInfo) != 0 ? array(vnetPrivateLink[index].outputs.NICID) : array('na')
//   }
// }]
