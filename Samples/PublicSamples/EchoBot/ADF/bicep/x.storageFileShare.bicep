param SAName string
param fileShare object

resource SAFileService 'Microsoft.Storage/storageAccounts/fileServices@2021-04-01' existing = {
  name: '${SAName}/default'
}

resource SAFileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2021-04-01' = {
  name: toLower('${fileShare.name}')
  parent: SAFileService
  properties: {
    shareQuota: fileShare.quota
    metadata: {}
  }
}

output  SAFileServiceId string = SAFileService.id
output  SAFileService string = SAFileService.name
output  share string = SAFileShare.name
