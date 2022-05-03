param subscriptionID string
param resourceGroupName string
param vNetName string
param vNetNameHub string
param peeringName string

resource VNETHUB 'Microsoft.Network/virtualNetworks@2020-11-01' existing = {
  name: vNetNameHub
}

resource VNET 'Microsoft.Network/virtualNetworks@2020-11-01' existing = {
  name: vNetName
  scope: resourceGroup(subscriptionID,resourceGroupName)
}

resource VNETPeering 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2017-10-01' = {
  name: peeringName
  parent: VNETHUB
  properties: {
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: true
    allowGatewayTransit: false
    useRemoteGateways: false
    remoteVirtualNetwork: {
      id: VNET.id
    }
  }
}
