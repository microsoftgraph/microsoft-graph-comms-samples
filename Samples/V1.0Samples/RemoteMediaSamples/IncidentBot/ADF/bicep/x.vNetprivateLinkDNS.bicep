param PrivateLinkInfo array
param resourceName string
param providerURL string
param Nics array

var DNSLookup = {
  vault: 'vaultcore'
  SQL: 'documents'
  MongoDB: 'mongo.cosmos'
  Cassandra: 'cassandra.cosmos'
  mysqlServer: 'mysql'
  mariadbServer: 'mariadb'
  configurationStores: 'azconfig'
  namespace: 'servicebus'
  sqlServer: 'database'
  redisCache: 'redis.cache'
}

//  dns private link group id
// https://docs.microsoft.com/en-us/azure/private-link/private-endpoint-overview#dns-configuration
//  Privatelink.   blob.          core.windows.net
//  Privatelink.   vaultcore.     azure.net
//  Privatelink.   mongo.cosmos.  azure.com
//  dns private link zone
// https://docs.microsoft.com/en-us/azure/private-link/private-endpoint-dns#azure-services-dns-zone-configuration

resource privateLinkDNS 'Microsoft.Network/privateDnsZones/A@2020-06-01' = [for (item, index) in PrivateLinkInfo: {
  name: 'privatelink.${(contains(DNSLookup, item.groupID) ? DNSLookup[item.groupID] : item.groupID)}${providerURL}${resourceName}'
  properties: {
    ttl: 3600
    aRecords: [
      {
        ipv4Address: reference(Nics[index], '2018-05-01').ipConfigurations[0].properties.privateIPAddress
      }
    ]
  }
}]
