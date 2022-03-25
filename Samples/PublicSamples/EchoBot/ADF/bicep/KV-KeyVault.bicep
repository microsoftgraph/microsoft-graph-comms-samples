param Deployment string
param DeploymentID string
param Environment string
param Prefix string
param KVInfo object
param Global object
param OMSworkspaceID string

var Defaults = {
  enabledForDeployment: true
  enabledForDiskEncryption: true
  enabledForTemplateDeployment: true
}

var keyVaultPermissions = {
  All: {
    keys: [
      'Get'
      'List'
      'Update'
      'Create'
      'Import'
      'Delete'
      'Recover'
      'Backup'
      'Restore'
    ]
    secrets: [
      'Get'
      'List'
      'Set'
      'Delete'
      'Recover'
      'Backup'
      'Restore'
    ]
    certificates: [
      'Get'
      'List'
      'Update'
      'Create'
      'Import'
      'Delete'
      'Recover'
      'Backup'
      'Restore'
      'ManageContacts'
      'ManageIssuers'
      'GetIssuers'
      'ListIssuers'
      'SetIssuers'
      'DeleteIssuers'
    ]
  }
  SecretsGet: {
    keys: []
    secrets: [
      'Get'
    ]
    certificates: []
  }
  SecretsGetAndList: {
    keys: []
    secrets: [
      'Get'
      'List'
    ]
    certificates: []
  }
}

var accessPolicies = [for i in range(0, ((!contains(KVInfo, 'accessPolicies')) ? 0 : length(KVInfo.accessPolicies))): {
  tenantId: subscription().tenantId
  objectId: KVInfo.accessPolicies[i].objectId
  permissions: keyVaultPermissions[KVInfo.accessPolicies[i].Permissions]
}]

resource KV 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: '${Deployment}-kv${KVInfo.Name}'
  location: resourceGroup().location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: KVInfo.skuName
    }
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: KVInfo.allNetworks
      // ipRules: ipRules
    }
    enabledForDeployment: Defaults.enabledForDeployment
    enabledForDiskEncryption: Defaults.enabledForDiskEncryption
    enabledForTemplateDeployment: Defaults.enabledForTemplateDeployment
    enableSoftDelete: KVInfo.softDelete
    enablePurgeProtection: KVInfo.PurgeProtection
    enableRbacAuthorization: (contains(KVInfo, 'PurgeProtection') ? KVInfo.PurgeProtection : false)
    accessPolicies: (KVInfo.RbacAuthorization ? [] : accessPolicies)
  }
}

resource KVDiagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = {
  name: 'service'
  scope: KV
  properties: {
    workspaceId: OMSworkspaceID
    logs: [
      {
        category: 'AuditEvent'
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
