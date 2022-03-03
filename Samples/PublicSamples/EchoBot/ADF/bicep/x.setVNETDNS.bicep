param Deploymentnsg string
param Deployment string
param DeploymentID string
param DeploymentInfo object
param DNSServers array
param Global object
param Prefix string

var networkId = '${Global.networkid[0]}${string((Global.networkid[1] - (2 * int(DeploymentID))))}'
var networkIdUpper = '${Global.networkid[0]}${string((1 + (Global.networkid[1] - (2 * int(DeploymentID)))))}'
var addressPrefixes = [
  '${networkId}.0/23'
]
var SubnetInfo = (contains(DeploymentInfo, 'SubnetInfo') ? DeploymentInfo.SubnetInfo : [])

// var Domain = split(Global.DomainName, '.')[0]

// var RouteTableGlobal = {
//   id: resourceId(Global.HubRGName, 'Microsoft.Network/routeTables/', '${replace(Global.hubVNetName, 'vn', 'rt')}${Domain}${Global.RTName}')
// }

var delegations = {
  default: []
  'Microsoft.Web/serverfarms': [
    {
      name: 'delegation'
      properties: {
        serviceName: 'Microsoft.Web/serverfarms'
      }
    }
  ]
  'Microsoft.ContainerInstance/containerGroups': [
    {
      name: 'aciDelegation'
      properties: {
        serviceName: 'Microsoft.ContainerInstance/containerGroups'
      }
    }
  ]
}

resource VNET 'Microsoft.Network/virtualNetworks@2021-02-01' = {
  name: '${Deployment}-vn'
  location: resourceGroup().location
  properties: {
    addressSpace: {
      addressPrefixes: addressPrefixes
    }
    dhcpOptions: {
      dnsServers: array(DNSServers)
    }
    subnets: [for sn in SubnetInfo: {
      name: sn.name
      properties: {
        addressPrefix: '${((sn.name == 'snMT02') ? networkIdUpper : networkId)}.${sn.Prefix}'
        networkSecurityGroup: ((contains(sn, 'NSG') && ((sn.NSG == 'Hub') || (sn.NSG == 'Spoke'))) ? json('{"id":"${string(resourceId(Global.HubRGName, 'Microsoft.Network/networkSecurityGroups', '${Deploymentnsg}${sn.NSG}-nsg${sn.name}'))}"}') : json('null'))
        //routeTable: ((contains(sn, 'RT') && bool(sn.RT)) ? RouteTableGlobal : json('null'))
        privateEndpointNetworkPolicies: 'Disabled'
        delegations: (contains(sn, 'delegations') ? delegations[sn.delegations] : delegations.default)
      }
    }]
  }
}
