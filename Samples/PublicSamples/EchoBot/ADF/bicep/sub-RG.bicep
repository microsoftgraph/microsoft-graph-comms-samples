param Prefix string = 'ACU1'

@allowed([
    'G'
    'I'
    'D'
    'T'
    'U'
    'P'
    'S'
    'A'
])
param Environment string

@allowed([
    '0'
    '1'
    '2'
    '3'
    '4'
    '5'
    '6'
    '7'
    '8'
    '9'
])
param DeploymentID string
param Stage object
param Extensions object
param Global object
param DeploymentInfo object

var enviro = '${Environment}${DeploymentID}' // D1
var deployment = '${Prefix}-${Global.orgname}-${Global.AppName}-${enviro}' // AZE2-BRW-HUB-D1
var rg = '${Prefix}-${Global.orgname}-${Global.AppName}-RG-${enviro}' // AZE2-BRW-HUB-D1

targetScope = 'subscription'

// move location lookup to include file referencing this table: 
// https://github.com/brwilkinson/AzureDeploymentFramework/blob/main/docs/Naming_Standards_Prefix.md

var prefixLookup = json(loadTextContent( './global/prefix.json' ))
var location = prefixLookup[Prefix].location

var uaiInfo = (contains(DeploymentInfo, 'uaiInfo') ? DeploymentInfo.uaiInfo : [])

var identity = [for uai in uaiInfo: {
    name: uai.name
    match: Global.cn == '.' || contains(Global.cn, uai.name)
}]

resource RG 'Microsoft.Resources/resourceGroups@2021-01-01' = {
    name: rg
    location: location
    properties:{}
}

module UAI 'sub-RG-UAI.bicep' = [for (uai, index) in identity: if (uai.match) {
    name: 'dp-uai-${uai.name}'
    scope: RG
    params: {
        uai: uai
        deployment: deployment
    }
}]

output enviro string = enviro
output deployment string = deployment
output location string = location
