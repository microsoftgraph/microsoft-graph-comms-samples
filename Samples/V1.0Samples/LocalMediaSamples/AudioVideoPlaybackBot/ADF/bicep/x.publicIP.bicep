param Deployment string
param DeploymentID string
param PIPprefix string
param NICs array
param VM object
param Global object
param OMSworkspaceID string


resource PublicIP 'Microsoft.Network/publicIPAddresses@2021-02-01' = [for (nic,index) in NICs: if (contains(nic, 'PublicIP')) {
  name: '${Deployment}-${PIPprefix}${VM.Name}-publicip${index + 1}'
  location: resourceGroup().location
  sku: {
    name: contains(VM, 'SKU') && VM.SKU == 'Standard' ? 'Standard' : 'Basic'
  }
  properties: {
    publicIPAllocationMethod: nic.PublicIP
    dnsSettings: {
      domainNameLabel: toLower('${Deployment}-${PIPprefix}${VM.Name}-${index + 1}')
    }
  }
}]

resource PublicIPDiag 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = [for (nic,index) in NICs: if (contains(nic, 'PublicIP')) {
  name: 'service'
  scope: PublicIP[index]
  properties: {
    workspaceId: OMSworkspaceID
    logs: [
      {
        category: 'DDoSProtectionNotifications'
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
}]

output PIPID array = [for (nic,index) in NICs: contains(nic, 'PublicIP') ? PublicIP[index].id : '' ]
