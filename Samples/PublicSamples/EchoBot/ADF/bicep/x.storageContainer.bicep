param SAName string
param container object

resource SABlobService 'Microsoft.Storage/storageAccounts/blobServices@2021-04-01' existing = {
  name: '${SAName}/default'
}

resource SAContainers 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-04-01' = {
  name: toLower('${container.name}')
  parent: SABlobService
  properties: {
    metadata: {}
  }
}
