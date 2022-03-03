param Deployment string
param DeploymentURI string
param DeploymentID string
param Environment string
param storageInfo object
param Global object
param Stage object
param OMSworkspaceID string
param now string = utcNow('F')

// var hubRG = Global.hubRGName
var storageLoggingAbstractions = [
  'blobServices'
  'fileServices'
  'queueServices'
  'tableService'
]
var azureFilesIdentityBasedAuthentication = {
  directoryServiceOptions: 'AD'
  activeDirectoryProperties: {
    domainName: Global.DomainName
    netBiosDomainName: first(split(Global.DomainName, '.'))
    forestName: Global.DomainName
    domainGuid: '99cbe596-b191-4853-aca3-4e19d44f67e0'
    domainSid: 'S-1-5-21-4089952384-727918856-4151886579'
    azureStorageSid: 'string'
  }
}

var fileShares = contains(storageInfo, 'fileShares') ? storageInfo.fileShares : []
var containers = contains(storageInfo, 'containers') ? storageInfo.containers : []

resource SA 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: toLower('${DeploymentURI}sa${storageInfo.nameSuffix}')
  location: resourceGroup().location
  sku: {
    name: storageInfo.skuName
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    supportsBlobContainerRetention: true
    azureFilesIdentityBasedAuthentication: ((contains(storageInfo, 'ADDS') && bool(storageInfo.ADDS)) ? azureFilesIdentityBasedAuthentication : null)
    largeFileSharesState: (contains(storageInfo, 'largeFileSharesState') ? storageInfo.largeFileSharesState : null)
    networkAcls: {
      bypass: 'Logging, Metrics, AzureServices'
      defaultAction: (contains(storageInfo, 'allNetworks') ? storageInfo.allNetworks : 'Allow')
    }
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    encryption: {
      keySource: 'Microsoft.Storage'
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
    }
  }
  dependsOn: []
}

resource SABlobService 'Microsoft.Storage/storageAccounts/blobServices@2021-04-01' = {
  name: 'default'
  parent: SA
  properties: {
    isVersioningEnabled: (contains(storageInfo, 'blobVersioning') ? storageInfo.blobVersioning : false)
    changeFeed: {
      enabled: (contains(storageInfo, 'changeFeed') ? storageInfo.changeFeed : false)
    }
    deleteRetentionPolicy: contains(storageInfo, 'softDeletePolicy') ? storageInfo.softDeletePolicy : null
  }
}

// https://docs.microsoft.com/en-us/azure/storage/files/files-smb-protocol?tabs=azure-powershell
resource SAFileService 'Microsoft.Storage/storageAccounts/fileServices@2020-08-01-preview' = {
  name: 'default'
  parent: SA
  properties: {
    shareDeleteRetentionPolicy: contains(storageInfo, 'softDeletePolicy') ? storageInfo.softDeletePolicy : null
    protocolSettings: {
      smb: {
        versions: 'SMB3.0;SMB3.1.1' // remove SMB2.1
        kerberosTicketEncryption: 'AES-256' // remove RC4-HMAC
        multichannel: ! contains(storageInfo, 'multichannel') ? null : {
          enabled: bool(storageInfo.multichannel)
        }
      }
    }
  }
}

resource SAQueueService 'Microsoft.Storage/storageAccounts/queueServices@2021-02-01' existing = {
  name: 'default'
  parent: SA
}

resource SATableService 'Microsoft.Storage/storageAccounts/tableServices@2021-02-01' existing = {
  name: 'default'
  parent: SA
}

resource SADiagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = {
  name: 'service'
  scope: SA
  properties: {
    workspaceId: OMSworkspaceID
    metrics: [
      {
        category: 'Capacity'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'Transaction'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

resource SABlobDiagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = {
  name: 'service'
  scope: SABlobService
  properties: {
    workspaceId: OMSworkspaceID
    metrics: [
      {
        category: 'Capacity'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'Transaction'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    logs: [
      {
        category: 'StorageRead'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.r) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'StorageWrite'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.w) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'StorageDelete'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.d) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

resource SAFileDiagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = {
  name: 'service'
  scope: SAFileService
  properties: {
    workspaceId: OMSworkspaceID
    metrics: [
      {
        category: 'Capacity'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'Transaction'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    logs: [
      {
        category: 'StorageRead'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.r) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'StorageWrite'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.w) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'StorageDelete'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.d) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

resource SAQueueDiagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = {
  name: 'service'
  scope: SAQueueService
  properties: {
    workspaceId: OMSworkspaceID
    metrics: [
      {
        category: 'Capacity'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'Transaction'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    logs: [
      {
        category: 'StorageRead'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.r) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'StorageWrite'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.w) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'StorageDelete'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.d) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

resource SATableDiagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = {
  name: 'service'
  scope: SATableService
  properties: {
    workspaceId: OMSworkspaceID
    metrics: [
      {
        category: 'Capacity'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'Transaction'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    logs: [
      {
        category: 'StorageRead'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.r) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'StorageWrite'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.w) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'StorageDelete'
        enabled: (contains(storageInfo, 'logging') ? bool(storageInfo.logging.d) : false)
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

module SAFileShares 'x.storageFileShare.bicep' = [for (share,index) in fileShares : {
  name: 'dp${Deployment}-SA-${storageInfo.nameSuffix}-FileShare-${share.name}'
  params: {
    SAName: SA.name
    fileShare: share
  }
}]

module SAContainers 'x.storageContainer.bicep' = [for (container,index) in containers : {
  name: 'dp${Deployment}-SA-${storageInfo.nameSuffix}-Container-${container.name}'
  params: {
    SAName: SA.name
    container: container
  }
}]

module vnetPrivateLink 'x.vNetPrivateLink.bicep' = if (contains(storageInfo, 'privatelinkinfo')) {
  name: 'dp${Deployment}-SA-privatelinkloop-${storageInfo.nameSuffix}'
  params: {
    Deployment: Deployment
    PrivateLinkInfo: storageInfo.privateLinkInfo
    providerType: 'Microsoft.Storage/storageAccounts'
    resourceName: SA.name
  }
}

// module privateLinkDNS 'x.vNetprivateLinkDNS.bicep' = if (contains(storageInfo, 'privatelinkinfo')) {
//   name: 'dp${Deployment}-SA-registerPrivateDNS-${storageInfo.nameSuffix}'
//   scope: resourceGroup(hubRG)
//   params: {
//     PrivateLinkInfo: storageInfo.privateLinkInfo
//     providerURL: '.${environment().suffixes.storage}/' // '.core.windows.net/' 
//     resourceName: SA.name
//     Nics: contains(storageInfo, 'privatelinkinfo') && length(storageInfo) != 0 ? array(vnetPrivateLink.outputs.NICID) : array('')
//   }
// }

