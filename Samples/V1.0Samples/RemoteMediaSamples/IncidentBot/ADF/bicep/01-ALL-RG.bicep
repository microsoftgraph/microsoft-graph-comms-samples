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

@secure()
param saKey string = newGuid()

var Deployment = '${Prefix}-${Global.OrgName}-${Global.Appname}-${Environment}${DeploymentID}'
var DeploymentURI = toLower('${Prefix}${Global.OrgName}${Global.Appname}${Environment}${DeploymentID}')
var Deploymentnsg = '${Prefix}-${Global.OrgName}-${Global.AppName}-'
var networkId = '${Global.networkid[0]}${string((Global.networkid[1] - (2 * int(DeploymentID))))}'
var networkIdUpper = '${Global.networkid[0]}${string((1 + (Global.networkid[1] - (2 * int(DeploymentID)))))}'
var OMSworkspaceName = '${DeploymentURI}LogAnalytics'
var OMSworkspaceID = resourceId('Microsoft.OperationalInsights/workspaces/', OMSworkspaceName)
var addressPrefixes = [
  '${networkId}.0/23'
]
// var DC1PrivateIPAddress = contains(DeploymentInfo,'DNSServers') ? '${networkId}.${DeploymentInfo.DNSServers[0]}' : Global.DNSServers[0]
// var DC2PrivateIPAddress = contains(DeploymentInfo,'DNSServers') ? '${networkId}.${DeploymentInfo.DNSServers[1]}' : Global.DNSServers[1]

var AzureDNS = '168.63.129.16'
var DNSServerList = contains(DeploymentInfo,'DNSServers') ? DeploymentInfo.DNSServers : Global.DNSServers
var DNSServers = [for (server, index) in DNSServerList: length(server) <= 3 ? '${networkId}.${server}' : server]

var kvName = '${Prefix}-${Global.OrgName}-${Global.AppName}-${Environment}${DeploymentID}-kv'
resource KV 'Microsoft.KeyVault/vaults@2021-06-01-preview' existing = {
  name: kvName
}

module dp_Deployment_OMS 'OMS.bicep' = if (bool(Stage.OMS)) {
  name: 'dp${Deployment}-OMS'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
    devOpsPat: KV.getSecret('localadmin')
    sshPublic: KV.getSecret('localadmin')
    vmAdminPassword: KV.getSecret('localadmin')
  }
  dependsOn: []
}

module dp_Deployment_SA 'SA.bicep' = if (bool(Stage.SA)) {
  name: 'dp${Deployment}-SA'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
    devOpsPat: KV.getSecret('localadmin')
    sshPublic: KV.getSecret('localadmin')
    vmAdminPassword: KV.getSecret('localadmin')
  }
  dependsOn: [
    dp_Deployment_OMS
  ]
}

// module dp_Deployment_RSV 'RSV.bicep' = if (bool(Stage.RSV)) {
//   name: 'dp${Deployment}-RSV'
//   params: {
//     // move these to Splatting later
//     DeploymentID: DeploymentID
//     DeploymentInfo: DeploymentInfo
//     Environment: Environment
//     Extensions: Extensions
//     Global: Global
//     Prefix: Prefix
//     Stage: Stage
//     devOpsPat: devOpsPat
//     sshPublic: sshPublic
//     vmAdminPassword: vmAdminPassword
//   }
//   dependsOn: [
//     dp_Deployment_OMS
//   ]
// }

// module dp_Deployment_NATGW 'NATGW.bicep' = if (bool(Stage.NATGW)) {
//   name: 'dp${Deployment}-NATGW'
//   params: {
//     // move these to Splatting later
//     DeploymentID: DeploymentID
//     DeploymentInfo: DeploymentInfo
//     Environment: Environment
//     Extensions: Extensions
//     Global: Global
//     Prefix: Prefix
//     Stage: Stage
//     devOpsPat: devOpsPat
//     sshPublic: sshPublic
//     vmAdminPassword: vmAdminPassword
//   }
//   dependsOn: [
//     dp_Deployment_OMS
//   ]
// }

// module dp_Deployment_NSGHUB 'NSG.hub.bicep' = if (bool(Stage.NSGHUB)) {
//   name: 'dp${Deployment}-NSGHUB'
//   params: {
//     // move these to Splatting later
//     DeploymentID: DeploymentID
//     DeploymentInfo: DeploymentInfo
//     Environment: Environment
//     Extensions: Extensions
//     Global: Global
//     Prefix: Prefix
//     Stage: Stage
//     devOpsPat: devOpsPat
//     sshPublic: sshPublic
//     vmAdminPassword: vmAdminPassword
//   }
//   dependsOn: [
//     dp_Deployment_OMS
//   ]
// }

module dp_Deployment_NSGSPOKE 'NSG.spoke.bicep' = if (bool(Stage.NSGSPOKE)) {
  name: 'dp${Deployment}-NSGSPOKE'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
    devOpsPat: KV.getSecret('localadmin')
    sshPublic: KV.getSecret('localadmin')
    vmAdminPassword: KV.getSecret('localadmin')
  }
  dependsOn: [
    dp_Deployment_OMS
  ]
}

module dp_Deployment_VNET 'VNET.bicep' = if (bool(Stage.VNET)) {
  name: 'dp${Deployment}-VNET'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
    devOpsPat: KV.getSecret('localadmin')
    sshPublic: KV.getSecret('localadmin')
    vmAdminPassword: KV.getSecret('localadmin')
  }
  dependsOn: [
    dp_Deployment_NSGSPOKE
    //dp_Deployment_NSGHUB
    //dp_Deployment_NATGW
  ]
}

module dp_Deployment_KV 'KV.bicep' = if (bool(Stage.KV)) {
  name: 'dp${Deployment}-KV'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
    devOpsPat: KV.getSecret('localadmin')
    sshPublic: KV.getSecret('localadmin')
    vmAdminPassword: KV.getSecret('localadmin')
  }
  dependsOn: [
    dp_Deployment_VNET
  ]
}

module dp_Deployment_BastionHost 'Bastion.bicep' = if (contains(Stage, 'BastionHost') && bool(Stage.BastionHost)) {
  name: 'dp${Deployment}-BastionHost'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
    devOpsPat: KV.getSecret('localadmin')
    sshPublic: KV.getSecret('localadmin')
    vmAdminPassword: KV.getSecret('localadmin')
  }
  dependsOn: [
    dp_Deployment_VNET
  ]
}

// module dp_Deployment_DNSPublicZone 'DNSPublic.bicep' = if (contains(Stage, 'DNSPublicZone') && bool(Stage.DNSPublicZone)) {
//   name: 'dp${Deployment}-DNSPublicZone'
//   params: {
//     // move these to Splatting later
//     DeploymentID: DeploymentID
//     DeploymentInfo: DeploymentInfo
//     Environment: Environment
//     Extensions: Extensions
//     Global: Global
//     Prefix: Prefix
//     Stage: Stage
//     devOpsPat: devOpsPat
//     sshPublic: sshPublic
//     vmAdminPassword: vmAdminPassword
//   }
//   dependsOn: []
// }

module dp_Deployment_LB 'LB.bicep' = if (bool(Stage.LB)) {
  name: 'dp${Deployment}-LB'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
    devOpsPat: KV.getSecret('localadmin')
    sshPublic: KV.getSecret('localadmin')
    vmAdminPassword: KV.getSecret('localadmin')
  }
  dependsOn: [
    dp_Deployment_VNET
  ]
}

module dp_Deployment_VMSS 'VMSS.bicep' = if (bool(Stage.VMSS)) {
  name: 'dp${Deployment}-VMSS'
  params: {
    // move these to Splatting later
    DeploymentID: DeploymentID
    DeploymentInfo: DeploymentInfo
    Environment: Environment
    Extensions: Extensions
    Global: Global
    Prefix: Prefix
    Stage: Stage
    devOpsPat: KV.getSecret('localadmin')
    sshPublic: KV.getSecret('localadmin')
    vmAdminPassword: KV.getSecret('localadmin')
  }
  dependsOn: [
    dp_Deployment_OMS
    dp_Deployment_LB
    dp_Deployment_SA
  ]
}

// module dp_Deployment_VNETDNSPublic 'x.setVNETDNS.bicep' = if (bool(Stage.ADPrimary) || contains(Stage,'CreateADPDC') && bool(Stage.CreateADPDC)) {
//   name: 'dp${Deployment}-VNETDNSPublic'
//   params: {
//     Deploymentnsg: Deploymentnsg
//     Deployment: Deployment
//     DeploymentID: DeploymentID
//     Prefix: Prefix
//     DeploymentInfo: DeploymentInfo
//     DNSServers: [
//       DNSServers[0]
//       AzureDNS
//     ]
//     Global: Global
//   }
//   dependsOn: [
//     dp_Deployment_VNET
//     dp_Deployment_OMS
//     dp_Deployment_SA
//   ]
// }

// module dp_Deployment_VNETDNSDC1 'x.setVNETDNS.bicep' = if (bool(Stage.ADPrimary) || contains(Stage,'CreateADPDC') && bool(Stage.CreateADPDC)) {
//   name: 'dp${Deployment}-VNETDNSDC1'
//   params: {
//     Deploymentnsg: Deploymentnsg
//     Deployment: Deployment
//     DeploymentID: DeploymentID
//     Prefix: Prefix
//     DeploymentInfo: DeploymentInfo
//     DNSServers: [
//       DNSServers[0]
//     ]
//     Global: Global
//   }
//   dependsOn: [
//     ADPrimary
//     CreateADPDC
//   ]
// }

// module dp_Deployment_VNETDNSDC2 'x.setVNETDNS.bicep' = if (bool(Stage.ADSecondary) || contains(Stage,'CreateADBDC') && bool(Stage.CreateADBDC)) {
//   name: 'dp${Deployment}-VNETDNSDC2'
//   params: {
//     Deploymentnsg: Deploymentnsg
//     Deployment: Deployment
//     DeploymentID: DeploymentID
//     DeploymentInfo: DeploymentInfo
//     Prefix: Prefix
//     DNSServers: [
//       DNSServers[0]
//       DNSServers[1]
//     ]
//     Global: Global
//   }
//   dependsOn: [
//     ADSecondary
//     CreateADBDC
//   ]
// }


// module dp_Deployment_DASHBOARD 'Dashboard.bicep' = if (bool(Stage.DASHBOARD)) {
//   name: 'dp${Deployment}-DASHBOARD'
//   params: {
//     // move these to Splatting later
//     DeploymentID: DeploymentID
//     DeploymentInfo: DeploymentInfo
//     Environment: Environment
//     Extensions: Extensions
//     Global: Global
//     Prefix: Prefix
//     Stage: Stage
//     devOpsPat: devOpsPat
//     sshPublic: sshPublic
//     vmAdminPassword: vmAdminPassword
//   }
//   dependsOn: []
// }

/*

module dp_Deployment_SQLMI '?' = if (bool(Stage.SQLMI)) {
  name: 'dp${Deployment}-SQLMI'
  params: {}
  dependsOn: [
    dp_Deployment_VNET
    dp_Deployment_VNETDNSDC1
    dp_Deployment_VNETDNSDC2
  ]
}

module dp_Deployment_WAFPOLICY '?' = if (bool(Stage.WAFPOLICY)) {
  name: 'dp${Deployment}-WAFPOLICY'
  params: {}
  dependsOn: [
    dp_Deployment_VNET
  ]
}

module dp_Deployment_WAF '?' = if (bool(Stage.WAF)) {
  name: 'dp${Deployment}-WAF'
  params: {}
  dependsOn: [
    dp_Deployment_VNET
    dp_Deployment_OMS
  ]
}

module VMSS '?' = if (bool(Stage.VMSS)) {
  name: 'VMSS'
  params: {}
  dependsOn: [
    dp_Deployment_VNETDNSDC1
    dp_Deployment_VNETDNSDC2
    dp_Deployment_OMS
    dp_Deployment_LB
    dp_Deployment_WAF
    dp_Deployment_SA
  ]
}

module dp_Deployment_AKS '?' = if (bool(Stage.AKS)) {
  name: 'dp${Deployment}-AKS'
  params: {}
  dependsOn: [
    dp_Deployment_WAF
    dp_Deployment_VNET
    dp_Deployment_ACR
  ]
}

module dp_Deployment_MySQLDB '?' = if (bool(Stage.MySQLDB)) {
  name: 'dp${Deployment}-MySQLDB'
  params: {}
  dependsOn: [
    dp_Deployment_VNET
    dp_Deployment_OMS
    dp_Deployment_WebSite
  ]
}

module dp_Deployment_AzureSQL '?' = if (bool(Stage.AzureSQL)) {
  name: 'dp${Deployment}-AzureSQL'
  params: {}
  dependsOn: [
    dp_Deployment_VNET
    dp_Deployment_OMS
  ]
}


*/
