param uai object
param deployment string

resource UAI 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
    location: resourceGroup().location
    name: '${deployment}-uai${uai.name}'
}
