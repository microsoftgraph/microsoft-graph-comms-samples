param Deployment string
param DeploymentID string
param Name string
param DATA object = {
  '1': 1
}
param DATASS object = {
  '1': 1
}
param SOFS object = {
  '1': 1
}
param LOGS object = {
  '1': 1
}
param TEMPDB object = {
  '1': 1
}
param BACKUP object = {
  '1': 1
}

param DISKS object = {
  '1': 1
}

param Global object



var Data_var = [for i in range(0, (contains(DATA, '1') ? 1 : length(DATA.LUNS))): {
  name: (contains(DATA, '1') ? 1 : '${Deployment}-${Name}-DATA-DATA${padLeft(DATA.LUNS[i][0], 2, '0')}')
  lun: (contains(DATA, '1') ? 1 : int(DATA.LUNS[i][0]))
  caching: (contains(DATA, '1') ? 1 : DATA.caching)
  diskSizeGB: (contains(DATA, '1') ? 1 : int(DATA.LUNS[i][1]))
  createOption: (contains(DATA, '1') ? 1 : ((length(DATA.LUNS[i]) == 3) ? DATA.LUNS[i][2] : 'Empty'))
  managedDisk: (contains(DATA, '1') ? 1 : (contains(DATA, 'saType') ? json('{"storageAccountType":"${DATA.saType}"}') : json('null')))
}]
var Datass_var = [for i in range(0, (contains(DATASS, '1') ? 1 : length(DATASS.LUNS))): {
  lun: (contains(DATASS, '1') ? 1 : int(DATASS.LUNS[i][0]))
  caching: (contains(DATASS, '1') ? 1 : DATASS.caching)
  diskSizeGB: (contains(DATASS, '1') ? 1 : int(DATASS.LUNS[i][1]))
  createOption: (contains(DATASS, '1') ? 1 : ((length(DATASS.LUNS[i]) == 3) ? DATASS.LUNS[i][2] : 'Empty'))
  managedDisk: (contains(DATASS, '1') ? 1 : (contains(DATASS, 'saType') ? json('{"storageAccountType":"${DATASS.saType}"}') : json('null')))
}]
var SOFS_var = [for i in range(0, (contains(SOFS, '1') ? 1 : length(SOFS.LUNS))): {
  name: (contains(SOFS, '1') ? 1 : '${Deployment}-${Name}-DATA-SOFS${padLeft(SOFS.LUNS[i][0], 2, '0')}')
  lun: (contains(SOFS, '1') ? 1 : int(SOFS.LUNS[i][0]))
  caching: (contains(SOFS, '1') ? 1 : SOFS.caching)
  diskSizeGB: (contains(SOFS, '1') ? 1 : int(SOFS.LUNS[i][1]))
  createOption: (contains(SOFS, '1') ? 1 : ((length(SOFS.LUNS[i]) == 3) ? SOFS.LUNS[i][2] : 'Empty'))
  managedDisk: (contains(SOFS, '1') ? 1 : (contains(SOFS, 'saType') ? json('{"storageAccountType":"${SOFS.saType}"}') : json('null')))
}]
var LOGS_var = [for i in range(0, (contains(LOGS, '1') ? 1 : length(LOGS.LUNS))): {
  name: (contains(LOGS, '1') ? 1 : '${Deployment}-${Name}-DATA-LOGS${padLeft(LOGS.LUNS[i][0], 2, '0')}')
  lun: (contains(LOGS, '1') ? 1 : int(LOGS.LUNS[i][0]))
  caching: (contains(LOGS, '1') ? 1 : LOGS.caching)
  diskSizeGB: (contains(LOGS, '1') ? 1 : int(LOGS.LUNS[i][1]))
  createOption: (contains(LOGS, '1') ? 1 : ((length(LOGS.LUNS[i]) == 3) ? LOGS.LUNS[i][2] : 'Empty'))
  managedDisk: (contains(LOGS, '1') ? 1 : (contains(LOGS, 'saType') ? json('{"storageAccountType":"${LOGS.saType}"}') : json('null')))
}]
var TEMPDB_var = [for i in range(0, (contains(TEMPDB, '1') ? 1 : length(TEMPDB.LUNS))): {
  name: (contains(TEMPDB, '1') ? 1 : '${Deployment}-${Name}-DATA-TEMPDB${padLeft(TEMPDB.LUNS[i][0], 2, '0')}')
  lun: (contains(TEMPDB, '1') ? 1 : int(TEMPDB.LUNS[i][0]))
  caching: (contains(TEMPDB, '1') ? 1 : TEMPDB.caching)
  diskSizeGB: (contains(TEMPDB, '1') ? 1 : int(TEMPDB.LUNS[i][1]))
  createOption: (contains(TEMPDB, '1') ? 1 : ((length(TEMPDB.LUNS[i]) == 3) ? TEMPDB.LUNS[i][2] : 'Empty'))
  managedDisk: (contains(TEMPDB, '1') ? 1 : (contains(TEMPDB, 'saType') ? json('{"storageAccountType":"${TEMPDB.saType}"}') : json('null')))
}]
var BACKUP_var = [for i in range(0, (contains(BACKUP, '1') ? 1 : length(BACKUP.LUNS))): {
  name: (contains(BACKUP, '1') ? 1 : '${Deployment}-${Name}-DATA-BACKUP${padLeft(BACKUP.LUNS[i][0], 2, '0')}')
  lun: (contains(BACKUP, '1') ? 1 : int(BACKUP.LUNS[i][0]))
  caching: (contains(BACKUP, '1') ? 1 : BACKUP.caching)
  diskSizeGB: (contains(BACKUP, '1') ? 1 : int(BACKUP.LUNS[i][1]))
  createOption: (contains(BACKUP, '1') ? 1 : ((length(BACKUP.LUNS[i]) == 3) ? BACKUP.LUNS[i][2] : 'Empty'))
  managedDisk: (contains(BACKUP, '1') ? 1 : (contains(BACKUP, 'saType') ? json('{"storageAccountType":"${BACKUP.saType}"}') : json('null')))
}]

output SOFS array = (contains(SOFS, '1') ? array('no SOFS disks') : SOFS_var)
output DATA array = (contains(DATA, '1') ? array('no DATA disks') : Data_var)
output DATASS array = (contains(DATASS, '1') ? array('no DATA disks') : Datass_var)
output LOGS array = (contains(LOGS, '1') ? array('no LOGS disks') : LOGS_var)
output TEMPDB array = (contains(TEMPDB, '1') ? array('no TEMPDB disks') : TEMPDB_var)
output BACKUP array = (contains(BACKUP, '1') ? array('no BACKUP disks') : BACKUP_var)
output DATADisks array = union((contains(SOFS, '1') ? [] : SOFS_var), (contains(DATA, '1') ? [] : Data_var), (contains(DATASS, '1') ? [] : Datass_var), (contains(LOGS, '1') ? [] : LOGS_var), (contains(TEMPDB, '1') ? [] : TEMPDB_var), (contains(BACKUP, '1') ? [] : BACKUP_var))
