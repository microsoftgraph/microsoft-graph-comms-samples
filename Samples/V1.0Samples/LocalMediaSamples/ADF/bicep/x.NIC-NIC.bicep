param Deployment string
param DeploymentID string
param NIC object
param NICNumber string
param VM object
param Global object

var networkId = '${Global.networkid[0]}${string((Global.networkid[1] - (2 * int(DeploymentID))))}'
var networkIdUpper = '${Global.networkid[0]}${string((1 + (Global.networkid[1] - (2 * int(DeploymentID)))))}'
var OMSworkspaceName = replace('${Deployment}LogAnalytics', '-', '')
var OMSworkspaceID = resourceId('Microsoft.OperationalInsights/workspaces/', OMSworkspaceName)
var VNetID = resourceId('Microsoft.Network/VirtualNetworks', '${Deployment}-vn')

var subnetID = '${VNetID}/subnets/sn${NIC.Subnet}'
var acceleratedNetworking = contains(NIC, 'FastNic') ? true : false
var NICSuffix = NICNumber == '1' ? '' : NICNumber
var IPAllocation = contains(NIC, 'StaticIP') ? 'Static' : 'Dynamic'
var privateIPAddress = contains(NIC, 'StaticIP') ? '${((NIC.Subnet == 'MT02') ? networkIdUpper : networkId)}.${NIC.StaticIP}' : null

var publicIPAddress = ! contains(NIC, 'PublicIP') ? null : {
  id: resourceId('Microsoft.Network/publicIPAddresses', '${Deployment}-vm${VM.Name}-publicip${NICNumber}')
}

var rules = contains(NIC,'NatRules') ? NIC.NatRules : []
var loadBalancerInboundNatRules = [for (nat,index) in  rules : {
  id: '${resourceGroup().id}/providers/Microsoft.Network/loadBalancers/${Deployment}-lb${(contains(NIC, 'PLB') ? NIC.PLB : 'none')}/inboundNatRules/${(contains(NIC, 'NATRules') ? nat : 'none')}'
}]

resource NIC1 'Microsoft.Network/networkInterfaces@2021-02-01' = if ( !( contains(NIC, 'LB') || contains(NIC, 'PLB') || contains(NIC, 'SLB') || contains(NIC, 'ISLB')) ) {
  location: resourceGroup().location
  name: '${Deployment}-nic${NICSuffix}${VM.Name}'
  properties: {
    enableAcceleratedNetworking: acceleratedNetworking
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          publicIPAddress: contains(NIC, 'PublicIP') ? publicIPAddress : null
          privateIPAllocationMethod: IPAllocation
          privateIPAddress: privateIPAddress
          subnet: {
            id: subnetID
          }
        }
      }
    ]
  }
}

resource NIC1Diag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!(contains(NIC, 'LB') || (contains(NIC, 'PLB') || (contains(NIC, 'SLB') || contains(NIC, 'ISLB'))))) {
  name: 'service'
  scope: NIC1
  properties: {
    workspaceId: OMSworkspaceID
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

resource NICPLB 'Microsoft.Network/networkInterfaces@2021-02-01' = if (contains(NIC, 'PLB')) {
  location: resourceGroup().location
  name: '${Deployment}-nicplb${NICSuffix}${VM.Name}'
  properties: {
    enableAcceleratedNetworking: acceleratedNetworking
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          loadBalancerBackendAddressPools: [
            {
              id: resourceId('Microsoft.Network/loadBalancers/backendAddressPools', '${Deployment}-lb${NIC.PLB}', NIC.PLB)
            }
          ]
          loadBalancerInboundNatRules: contains(NIC, 'NATRules') ? loadBalancerInboundNatRules : null
          privateIPAllocationMethod: IPAllocation
          privateIPAddress: privateIPAddress
          subnet: {
            id: subnetID
          }
        }
      }
    ]
  }
}

resource NICPLBDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (contains(NIC, 'PLB')) {
  name: 'service'
  scope: NICPLB
  properties: {
    workspaceId: OMSworkspaceID
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

resource NICLB 'Microsoft.Network/networkInterfaces@2021-02-01' = if (contains(NIC, 'LB')) {
  location: resourceGroup().location
  name: '${Deployment}-niclb${NICSuffix}${VM.Name}'
  properties: {
    enableAcceleratedNetworking: acceleratedNetworking
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          loadBalancerBackendAddressPools: [
            {
              id: resourceId('Microsoft.Network/loadBalancers/backendAddressPools', '${Deployment}-ilb${NIC.LB}', NIC.LB)
            }
          ]
          privateIPAllocationMethod: IPAllocation
          privateIPAddress: privateIPAddress
          subnet: {
            id: subnetID
          }
        }
      }
    ]
  }
}

resource NICLBDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (contains(NIC, 'LB')) {
  name: 'service'
  scope: NICLB
  properties: {
    workspaceId: OMSworkspaceID
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

resource NICSLB 'Microsoft.Network/networkInterfaces@2021-02-01' = if (contains(NIC, 'SLB')) {
  location: resourceGroup().location
  name: '${Deployment}-nicslb${NICSuffix}${VM.Name}'
  properties: {
    enableAcceleratedNetworking: acceleratedNetworking
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          loadBalancerBackendAddressPools: [
            // use Azure NATGW instead
            // {
            //   id: resourceId('Microsoft.Network/loadBalancers/backendAddressPools', '${Deployment}-lbPLB01', 'PLB01')
            // }
            {
              id: resourceId('Microsoft.Network/loadBalancers/backendAddressPools', '${Deployment}-lb${NIC.SLB}', NIC.SLB)
            }
          ]
          privateIPAllocationMethod: IPAllocation
          privateIPAddress: privateIPAddress
          subnet: {
            id: subnetID
          }
        }
      }
    ]
  }
}

resource NICSLBDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (contains(NIC, 'SLB')) {
  name: 'service'
  scope: NICSLB
  properties: {
    workspaceId: OMSworkspaceID
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

output foo7 array = loadBalancerInboundNatRules
output foo object = NIC
