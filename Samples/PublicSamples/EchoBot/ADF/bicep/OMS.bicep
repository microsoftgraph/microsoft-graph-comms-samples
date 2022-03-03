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
    linuxWeekly: false

    windowsNOW: true
    windowsWeekly: true
    windowsMonthly: true
}

var dataSources = [
    {
        name: 'AzureActivityLog'
        kind: 'AzureActivityLog'
        properties: {
            linkedResourceId: '${subscription().id}/providers/Microsoft.Insights/eventTypes/management'
        }
    }
    {
        name: 'LogicalDisk1'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Avg Disk sec/Read'
        }
    }
    {
        name: 'LogicalDisk2'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Avg Disk sec/Write'
        }
    }
    {
        name: 'LogicalDisk3'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Current Disk Queue Length'
        }
    }
    {
        name: 'LogicalDisk4'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Disk Reads/sec'
        }
    }
    {
        name: 'LogicalDisk5'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Disk Transfers/sec'
        }
    }
    {
        name: 'LogicalDisk6'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Disk Writes/sec'
        }
    }
    {
        name: 'LogicalDisk7'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Free Megabytes'
        }
    }
    {
        name: 'LogicalDisk8'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: '% Free Space'
        }
    }
    {
        name: 'LogicalDisk9'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Avg Disk sec/Transfer'
        }
    }
    {
        name: 'LogicalDisk10'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Disk Bytes/sec'
        }
    }
    {
        name: 'LogicalDisk11'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Disk Read Bytes/sec'
        }
    }
    {
        name: 'LogicalDisk12'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'LogicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Disk Write Bytes/sec'
        }
    }
    {
        name: 'PhysicalDisk'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: '% Free Space'
        }
    }
    {
        name: 'PhysicalDisk1'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: '% Disk Time'
        }
    }
    {
        name: 'PhysicalDisk2'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: '% Disk Read Time'
        }
    }
    {
        name: 'PhysicalDisk3'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: '% Disk Write Time'
        }
    }
    {
        name: 'PhysicalDisk4'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: 'Disk Transfers/sec'
        }
    }
    {
        name: 'PhysicalDisk5'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: 'Disk Reads/sec'
        }
    }
    {
        name: 'PhysicalDisk6'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: 'Disk Writes/sec'
        }
    }
    {
        name: 'PhysicalDisk7'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: 'Disk Bytes/sec'
        }
    }
    {
        name: 'PhysicalDisk8'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: 'Disk Read Bytes/sec'
        }
    }
    {
        name: 'PhysicalDisk9'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: 'Disk Write Bytes/sec'
        }
    }
    {
        name: 'PhysicalDisk10'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: 'Avg. Disk Queue Length'
        }
    }
    {
        name: 'PhysicalDisk11'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: 'Avg. Disk Read Queue Length'
        }
    }
    {
        name: 'PhysicalDisk12'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: 'Avg. Disk Write Queue Length'
        }
    }
    {
        name: 'PhysicalDisk13'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'PhysicalDisk'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Disk Transfers/sec'
        }
    }
    {
        name: 'Memory1'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'Memory'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Available MBytes'
        }
    }
    {
        name: 'Memory2'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'Memory'
            instanceName: '*'
            intervalSeconds: 10
            counterName: '% Committed Bytes In Use'
        }
    }
    {
        name: 'Network1'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'Network Adapter'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Bytes Received/sec'
        }
    }
    {
        name: 'Network2'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'Network Adapter'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Bytes Sent/sec'
        }
    }
    {
        name: 'Network3'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'Network Adapter'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Bytes Total/sec'
        }
    }
    {
        name: 'CPU1'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'Processor'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: '% Processor Time'
        }
    }
    {
        name: 'CPU2'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'Processor'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: '% Privileged Time'
        }
    }
    {
        name: 'CPU3'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'Processor'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: '% User Time'
        }
    }
    {
        name: 'CPU5'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'Processor Information'
            instanceName: '_Total'
            intervalSeconds: 10
            counterName: 'Processor Frequency'
        }
    }
    {
        name: 'CPU6'
        kind: 'WindowsPerformanceCounter'
        properties: {
            objectName: 'System'
            instanceName: '*'
            intervalSeconds: 10
            counterName: 'Processor Queue Length'
        }
    }
    {
        name: 'System'
        kind: 'WindowsEvent'
        properties: {
            eventLogName: 'System'
            eventTypes: [
                {
                    eventType: 'Error'
                }
                {
                    eventType: 'Warning'
                }
            ]
        }
    }
    {
        name: 'Application'
        kind: 'WindowsEvent'
        properties: {
            eventLogName: 'Application'
            eventTypes: [
                {
                    eventType: 'Error'
                }
                {
                    eventType: 'Warning'
                }
            ]
        }
    }
    {
        name: 'DSCEventLogs'
        kind: 'WindowsEvent'
        properties: {
            eventLogName: 'Microsoft-Windows-DSC/Operational'
            eventTypes: [
                {
                    eventType: 'Error'
                }
                {
                    eventType: 'Warning'
                }
                {
                    eventType: 'Information'
                }
            ]
        }
    }
    {
        name: 'TSSessionManager'
        kind: 'WindowsEvent'
        properties: {
            eventLogName: 'Microsoft-Windows-TerminalServices-LocalSessionManager/Operational'
            eventTypes: [
                {
                    eventType: 'Error'
                }
                {
                    eventType: 'Warning'
                }
                {
                    eventType: 'Information'
                }
            ]
        }
    }
    {
        name: 'Linux'
        kind: 'LinuxPerformanceObject'
        properties: {
            performanceCounters: [
                {
                    counterName: '% Used Inodes'
                }
                {
                    counterName: 'Free Megabytes'
                }
                {
                    counterName: '% Used Space'
                }
                {
                    counterName: 'Disk Transfers/sec'
                }
                {
                    counterName: 'Disk Reads/sec'
                }
                {
                    counterName: 'Disk Writes/sec'
                }
            ]
            objectName: 'Logical Disk'
            instanceName: '*'
            intervalSeconds: 10
        }
    }
    {
        name: 'LinuxPerfCollection'
        kind: 'LinuxPerformanceCollection'
        properties: {
            state: 'Enabled'
        }
    }
    {
        name: 'IISLog'
        kind: 'IISLogs'
        properties: {
            state: 'OnPremiseEnabled'
        }
    }
    {
        name: 'Syslog'
        kind: 'LinuxSyslog'
        properties: {
            syslogName: 'kern'
            syslogSeverities: [
                {
                    severity: 'emerg'
                }
                {
                    severity: 'alert'
                }
                {
                    severity: 'crit'
                }
                {
                    severity: 'err'
                }
                {
                    severity: 'warning'
                }
            ]
        }
    }
    {
        name: 'SyslogCollection'
        kind: 'LinuxSyslogCollection'
        properties: {
            state: 'Enabled'
        }
    }
]
var solutions = [
    // 'AzureAutomation'
    // 'Updates'
    'Security'
    'AgentHealthAssessment'
    // 'ChangeTracking'
    // 'AzureActivity'
    // 'ADAssessment'
    // 'ADReplication'
    // 'SQLAssessment'
    'ServiceMap'
    'AntiMalware'
    // 'DnsAnalytics'
    // 'ApplicationInsights'
    // 'AzureWebAppsAnalytics'
    // 'KeyVault'
    // 'AzureNSGAnalytics'
    // 'AlertManagement'
    // 'CapacityPerformance'
    // 'NetworkMonitoring'
    // 'WireData2'
    // 'Containers'
    // 'ContainerInsights'
    // 'ServiceFabric'
    // 'InfrastructureInsights'
    'VMInsights'
    // 'SecurityInsights'
]

resource AA 'Microsoft.Automation/automationAccounts@2020-01-13-preview' = {
    name: AAName
    location: (contains(Global, 'AALocation') ? Global.AALocation : resourceGroup().location)
    properties: {
        sku: {
            name: AAserviceTier
        }
    }
}

resource AADiagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = {
    name: 'service'
    scope: AA
    properties: {
        workspaceId: OMS.id
        logs: [
            {
                category: 'JobLogs'
                enabled: true
                retentionPolicy: {
                    days: 0
                    enabled: false
                }
            }
            {
                category: 'JobStreams'
                enabled: true
                retentionPolicy: {
                    days: 0
                    enabled: false
                }
            }
            {
                category: 'DscNodeStatus'
                enabled: true
                retentionPolicy: {
                    days: 0
                    enabled: false
                }
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

resource OMS 'Microsoft.OperationalInsights/workspaces@2020-10-01' = {
    name: OMSWorkspaceName
    location: resourceGroup().location
    properties: {
        sku: {
            name: serviceTier
        }
        retentionInDays: dataRetention
        features: {
            legacy: 0
            searchVersion: 1
            enableLogAccessUsingOnlyResourcePermissions: false
        }
    }
}

resource OMSDiagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = {
    name: 'service'
    scope: OMS
    properties: {
        workspaceId: OMS.id
        logs: [
            {
                category: 'Audit'
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

resource OMSworkspaceName_Automation 'Microsoft.OperationalInsights/workspaces/linkedServices@2015-11-01-preview' = {
    parent: OMS
    name: 'Automation'
    properties: {
        resourceId: AA.id
    }
}


@batchSize(1)
resource updateConfigWindows3 'Microsoft.Automation/automationAccounts/softwareUpdateConfigurations@2019-06-01' = [for (zone, index) in patchingZones: if (bool(Stage.OMSUpdateMonthly)) {
    parent: AA
    name: 'Update-Third-Saturday-Windows-Zone${zone}'
    properties: {
        updateConfiguration: {
            operatingSystem: 'Windows'
            windows: {
                // #disable-next-line BCP036
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
        tasks: {
            // postTask: {
            //     parameters:
            //     source:
            // }
            // preTask: {
            //     parameters:
            //     source:
            // }
        }
        scheduleInfo: {
            isEnabled: patchingEnabled.windowsMonthly
            frequency: 'Month'
            timeZone: Global.patchSchedulerTimeZone
            interval: 1
            startTime: dateTimeAdd('${20 + int(zone)}:00', 'P1D') // offset the start time based on the zone
            advancedSchedule: {
                monthlyOccurrences: [
                    {
                        day: 'Saturday'
                        occurrence: 3
                    }
                ]
            }
        }
    }
}]

@batchSize(1)
resource updateConfigWindows 'Microsoft.Automation/automationAccounts/softwareUpdateConfigurations@2019-06-01' = [for (zone, index) in patchingZones: if (bool(Stage.OMSUpdateWeekly)) {
    parent: AA
    name: 'Update-Twice-Weekly-Windows-Zone${zone}'
    properties: {
        updateConfiguration: {
            operatingSystem: 'Windows'
            windows: {
                // #disable-next-line BCP036
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
            isEnabled: patchingEnabled.windowsWeekly
            frequency: 'Week'
            interval: 1
            timeZone: Global.patchSchedulerTimeZone
            startTime: dateTimeAdd('${12 + int(zone)}:00', 'P1D') // offset the start time based on the zone
            advancedSchedule: {
                weekDays: [
                    'Wednesday'
                    'Thursday'
                ]
            }
        }
    }
}]

resource VMInsights 'Microsoft.Insights/dataCollectionRules@2021-04-01' = if (bool(Extensions.VMInsights)) {
    name: '${DeploymentURI}VMInsights'
    location: resourceGroup().location
    properties: {
        description: 'Data collection rule for VM Insights health.'
        dataSources: {
            windowsEventLogs: [
                {
                    name: 'cloudSecurityTeamEvents'
                    streams: [
                        'Microsoft-WindowsEvent'
                    ]
                    scheduledTransferPeriod: 'PT1M'
                    xPathQueries: [
                        'Security!'
                    ]
                }
                {
                    name: 'appTeam1AppEvents'
                    streams: [
                        'Microsoft-WindowsEvent'
                    ]
                    scheduledTransferPeriod: 'PT5M'
                    xPathQueries: [
                        'System![System[(Level = 1 or Level = 2 or Level = 3)]]'
                        'Application!*[System[(Level = 1 or Level = 2 or Level = 3)]]'
                    ]
                }
            ]
            syslog: [
                {
                    name: 'cronSyslog'
                    streams: [
                        'Microsoft-Syslog'
                    ]
                    facilityNames: [
                        'cron'
                    ]
                    logLevels: [
                        'Debug'
                        'Critical'
                        'Emergency'
                    ]
                }
                {
                    name: 'syslogBase'
                    streams: [
                        'Microsoft-Syslog'
                    ]
                    facilityNames: [
                        'syslog'
                    ]
                    logLevels: [
                        'Alert'
                        'Critical'
                        'Emergency'
                    ]
                }
            ]
            performanceCounters: [
                {
                    name: 'VMHealthPerfCounters'
                    scheduledTransferPeriod: 'PT1M'
                    samplingFrequencyInSeconds: 30
                    counterSpecifiers: [
                        '\\Memory\\Available Bytes'
                        '\\Memory\\Committed Bytes'
                        '\\Processor(_Total)\\% Processor Time'
                        '\\LogicalDisk(*)\\% Free Space'
                        '\\LogicalDisk(_Total)\\Free Megabytes'
                        '\\PhysicalDisk(_Total)\\Avg. Disk Queue Length'
                    ]
                    streams: [
                        'Microsoft-Perf'
                    ]
                }
                {
                    name: 'appTeamExtraCounters'
                    streams: [
                        'Microsoft-Perf'
                    ]
                    scheduledTransferPeriod: 'PT5M'
                    samplingFrequencyInSeconds: 30
                    counterSpecifiers: [
                        '\\Process(_Total)\\Thread Count'
                    ]
                }
            ]
            extensions: [
                {
                    name: 'Microsoft-VMInsights-Health'
                    streams: [
                        'Microsoft-HealthStateChange'
                    ]
                    extensionName: 'HealthExtension'
                    extensionSettings: {
                        schemaVersion: '1.0'
                        contentVersion: ''
                        healthRuleOverrides: [
                            {
                                scopes: [
                                    '*'
                                ]
                                monitors: [
                                    'root'
                                ]
                                monitorConfiguration: {}
                                alertConfiguration: {
                                    isEnabled: true
                                }
                            }
                        ]
                    }
                    inputDataSources: [
                        'VMHealthPerfCounters'
                    ]
                }
            ]
        }
        destinations: {
            logAnalytics: [
                {
                    workspaceResourceId: OMS.id
                    name: 'LogAnalyticsWorkspace'
                }
            ]
        }
        dataFlows: [
            {
                streams: [
                    'Microsoft-HealthStateChange'
                    'Microsoft-Perf'
                    'Microsoft-Syslog'
                    'Microsoft-WindowsEvent'
                ]
                destinations: [
                    'LogAnalyticsWorkspace'
                ]
            }
        ]
    }
}

resource AppInsights 'Microsoft.Insights/components@2020-02-02-preview' = {
    name: appInsightsName
    location: resourceGroup().location
    kind: 'other'
    properties: {
        Application_Type: 'web'
        Flow_Type: null
        Request_Source: 'rest'
        HockeyAppId: ''
        SamplingPercentage: null
    }
}

resource AppInsightDiagnostics 'microsoft.insights/diagnosticSettings@2017-05-01-preview' = {
    name: 'service'
    scope: AppInsights
    properties: {
        workspaceId: OMS.id
        logs: [
            {
                enabled: true
                category: 'AppAvailabilityResults'
            }
            {
                enabled: true
                category: 'AppBrowserTimings'
            }
            {
                enabled: true
                category: 'AppEvents'
            }
            {
                enabled: true
                category: 'AppMetrics'
            }
            {
                enabled: true
                category: 'AppDependencies'
            }
            {
                enabled: true
                category: 'AppExceptions'
            }
            {
                enabled: true
                category: 'AppPageViews'
            }
            {
                enabled: true
                category: 'AppPerformanceCounters'
            }
            {
                enabled: true
                category: 'AppRequests'
            }
            {
                enabled: true
                category: 'AppSystemEvents'
            }
            {
                enabled: true
                category: 'AppTraces'
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

resource OMS_dataSources 'Microsoft.OperationalInsights/workspaces/dataSources@2020-08-01' = [for item in dataSources: if (bool(Stage.OMSDataSources)) {
    name: item.name
    parent: OMS
    kind: item.kind
    properties: item.properties
}]

resource OMS_solutions 'Microsoft.OperationsManagement/solutions@2015-11-01-preview' = [for item in solutions: if (bool(Stage.OMSSolutions)) {
    name: '${item}(${OMSWorkspaceName})'
    location: resourceGroup().location
    properties: {
        workspaceResourceId: OMS.id
    }
    plan: {
        name: '${item}(${OMSWorkspaceName})'
        product: 'OMSGallery/${item}'
        promotionCode: ''
        publisher: 'Microsoft'
    }
}]

//  below needs review

//   resource omsWorkspaceName_alertInfo_search_category_alertInfo_search_name 'Microsoft.OperationalInsights/workspaces/savedSearches@2017-03-15-preview' = [for item in alertInfo: {
//     name: '${OMSworkspaceName_var}/${toLower(item.search.category)}|${toLower(item.search.name)}'
//     location: resourceGroup().location
//     properties: {
//       etag: '*'
//       query: item.search.query
//       displayName: concat(item.search.name)
//       category: item.search.category
//     }
//     dependsOn: [
//       OMSworkspaceName
//     ]
//   }]

//   resource omsWorkspaceName_alertInfo_search_category_alertInfo_search_name_schedule_id_name_omsWorkspaceName_alertInfo_search_category_alertInfo_search_name 'Microsoft.OperationalInsights/workspaces/savedSearches/schedules@2017-03-15-preview' = [for (item, i) in alertInfo: if (contains(alertInfo[(i + 0)], 'alert')) {
//     name: '${OMSworkspaceName_var}/${toLower(item.search.category)}|${toLower(item.search.name)}/schedule-${uniqueString(resourceGroup().id, deployment().name, OMSworkspaceName_var, '/', item.search.category, '|', item.search.name)}'
//     properties: {
//       etag: '*'
//       interval: item.alert.schedule.interval
//       queryTimeSpan: item.alert.schedule.timeSpan
//       enabled: item.alert.enabled
//     }
//     dependsOn: [
//       'Microsoft.OperationalInsights/workspaces/${OMSworkspaceName_var}/savedSearches/${toLower(item.search.category)}|${toLower(item.search.name)}'
//     ]
//   }]

//   resource omsWorkspaceName_alertInfo_search_category_alertInfo_search_name_schedule_id_name_omsWorkspaceName_alertInfo_search_category_alertInfo_search_name_alert_id_name_omsWorkspaceName_alertInfo_search_category_alertInfo_search_name 'Microsoft.OperationalInsights/workspaces/savedSearches/schedules/actions@2017-03-15-preview' = [for (item, i) in alertInfo: if (contains(alertInfo[(i + 0)], 'alert')) {
//     name: '${OMSworkspaceName_var}/${toLower(item.search.category)}|${toLower(item.search.name)}/schedule-${uniqueString(resourceGroup().id, deployment().name, OMSworkspaceName_var, '/', item.search.category, '|', item.search.name)}/alert-${uniqueString(resourceGroup().id, deployment().name, OMSworkspaceName_var, '/', item.search.category, '|', item.search.name)}'
//     properties: {
//       etag: '*'
//       Type: 'Alert'
//       name: item.alert.displayName
//       Description: item.alert.description
//       Severity: item.alert.severity
//       Threshold: {
//         Operator: item.alert.thresholdOperator
//         Value: item.alert.thresholdValue
//       }
//       Throttling: {
//         DurationInMinutes: item.alert.throttleMinutes
//       }
//       emailNotification: (contains(item.alert, 'emailNotification') ? item.alert.emailNotification : json('null'))
//     }
//     dependsOn: [
//       'Microsoft.OperationalInsights/workspaces/${OMSworkspaceName_var}/savedSearches/${toLower(item.search.category)}|${toLower(item.search.name)}'
//       'Microsoft.OperationalInsights/workspaces/${OMSworkspaceName_var}/savedSearches/${toLower(item.search.category)}|${toLower(item.search.name)}/schedules/schedule-${uniqueString(resourceGroup().id, deployment().name, OMSworkspaceName_var, '/', item.search.category, '|', item.search.name)}'
//     ]
//   }]
