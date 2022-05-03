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

var Deployment = '${Prefix}-${Global.OrgName}-${Global.Appname}-${Environment}${DeploymentID}'

// os config now shared across subscriptions
var computeGlobal = json(loadTextContent('./global/Global-ConfigVM.json'))
var OSType = computeGlobal.OSType
var DataDiskInfo = computeGlobal.DataDiskInfo

var AppServers = contains(DeploymentInfo, 'VMSSInfo') ? DeploymentInfo.VMSSInfo : []

var VM = [for (vm, index) in AppServers: {
  match: Global.CN == '.' || contains(Global.CN, vm.Name)
  name: vm.Name
  Extensions: contains(OSType[vm.OSType], 'RoleExtensions') ? union(Extensions, OSType[vm.OSType].RoleExtensions) : Extensions
  DataDisk: contains(vm, 'DDRole') ? DataDiskInfo[vm.DDRole] : json('null')
  NodeType: toLower(concat(Global.AppName, vm.Name))
  vmHostName: toLower('${Environment}${DeploymentID}${vm.Name}')
  Name: '${Prefix}${Global.AppName}-${Environment}${DeploymentID}-${vm.Name}'
  // Primary: vm.IsPrimary
  // durabilityLevel: vm.durabilityLevel
  // placementProperties: vm.placementProperties
}]

module DISKLOOKUP 'y.disks.bicep' = [for (vm,index) in AppServers: if (VM[index].match) {
  name: 'dp${Deployment}-VMSS-diskLookup${vm.Name}'
  params: {
    Deployment: Deployment
    DeploymentID: DeploymentID
    Name: vm.Name
    DATASS: (contains(DataDiskInfo[vm.DDRole], 'DATASS') ? DataDiskInfo[vm.DDRole].DATASS : json('{"1":1}'))
    Global: Global
  }
}]

module VMSS 'VMSS-VM.bicep' = [for (vm,index) in AppServers: if (VM[index].match) {
  name: 'dp${Deployment}-VMSS-Deploy${vm.Name}'
  params: {
    Prefix: Prefix
    DeploymentID: DeploymentID
    Environment: Environment
    AppServer: vm
    VM: VM[index]
    Global: Global
    vmAdminPassword: vmAdminPassword
    devOpsPat: devOpsPat
    sshPublic: sshPublic
  }
}]
