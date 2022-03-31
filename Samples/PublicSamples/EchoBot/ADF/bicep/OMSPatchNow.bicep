param Prefix string = 'AZE2'

@allowed([
    'I'
    'D'
    'T'
    'U'
    'P'
    'S'
    'G'
    'A'
])
param Environment string = 'D'

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
param DeploymentID string = '1'
param Stage object
param Extensions object
param Global object
param DeploymentInfo object

@secure()
param vmAdminPassword string

@secure()
param devOpsPat string

@secure()
param sshPublic string

param now string = utcNow('F')

targetScope = 'resourceGroup'

var Deployment = '${Prefix}-${Global.OrgName}-${Global.Appname}-${Environment}${DeploymentID}'

var DeploymentURI = toLower('${Prefix}${Global.OrgName}${Global.Appname}${Environment}${DeploymentID}')
var OMSWorkspaceName = '${DeploymentURI}LogAnalytics'
var AAName = '${DeploymentURI}OMSAutomation'
var appInsightsName = '${DeploymentURI}AppInsights'

var appConfigurationInfo = contains(DeploymentInfo, 'appConfigurationInfo') ? DeploymentInfo.appConfigurationInfo : []

var dataRetention = 31
var serviceTier = 'PerNode'
var AAserviceTier = 'Basic' // 'Free'

var patchingZones = [
    '1'
    '2'
    '3'
]
var patchingEnabled = {
    linuxNOW: false
    windowsNOW: true
}

resource AA 'Microsoft.Automation/automationAccounts@2020-01-13-preview' existing = {
    name: AAName
}

resource updateConfigWindowsNOW 'Microsoft.Automation/automationAccounts/softwareUpdateConfigurations@2019-06-01' = [for (zone, index) in patchingZones: {
    parent: AA
    name: 'Update-NOW-Windows-Zone${zone}'
    properties: {
        updateConfiguration: {
            operatingSystem: 'Windows'
            windows: {
                includedUpdateClassifications: 'Critical, Definition, FeaturePack, Security, ServicePack, Tools, UpdateRollup, Updates'
                excludedKbNumbers: []
                includedKbNumbers: []
                rebootSetting: 'IfRequired'
            }
            duration: 'PT2H'
            // azureVirtualMachines: []
            // nonAzureComputerNames: []
            targets: {
                azureQueries: [
                    {
                        scope: [
                            resourceGroup().id
                        ]
                        tagSettings: {
                            tags: {
                                zone: [
                                    zone
                                ]
                            }
                            filterOperator: 'Any'
                        }
                        locations: []
                    }
                ]
            }
        }
        tasks: {}
        scheduleInfo: {
            isEnabled: patchingEnabled.windowsNOW
            frequency: 'OneTime'
            interval: 1
            startTime: dateTimeAdd(now, 'PT${int(zone) * 15}M') // 15, 30, 45 mins from now, zones 1,2,3
        }
    }
}]
