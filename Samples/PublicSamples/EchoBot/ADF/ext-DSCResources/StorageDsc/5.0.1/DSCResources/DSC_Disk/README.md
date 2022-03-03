# Description

The resource is used to initialize, format and mount the partition/volume as a drive
letter.
The disk to add the partition/volume to is selected by specifying the _DiskId_ and
optionally _DiskIdType_.
The _DiskId_ value can be a _Disk Number_, _Unique Id_,  _Guid_ or _Location_.

**Important: The _Disk Number_ is not a reliable method of selecting a disk because
it has been shown to change between reboots in some environments.
It is recommended to use the _Unique Id_ if possible.**

The _Disk Number_, _Unique Id_, _Guid_ and _Location_ can be identified for a
disk by using the PowerShell command:

```powershell
Get-Disk | Select-Object -Property FriendlyName,DiskNumber,UniqueId,Guid,Location
```

Note: The _Guid_ identifier method of specifying disks is only supported as an
identifier for disks with `GPT` partition table format. If the disk is `RAW`
(e.g. the disk has been initialized) then the _Guid_ identifier method can not
be used. This is because the _Guid_ for a disk is only assigned once the partition
table for the disk has been created.

## Known Issues

### Defragsvc Conflict

The 'defragsvc' service ('Optimize Drives') may cause the following errors when
enabled with this resource. The following error may occur when testing the state
of the resource:

```text
PartitionSupportedSize
+ CategoryInfo : NotSpecified: (StorageWMI:) [], CimException
+ FullyQualifiedErrorId : StorageWMI 4,Get-PartitionSupportedSize
+ PSComputerName : localhost
```

The 'defragsvc' service should be stopped and set to manual start up to prevent
this error. Use the `Service` resource in either the 'xPSDesiredStateConfgiuration'
or 'PSDSCResources' resource module to set the 'defragsvc' service is always
stopped and set to manual start up.

### Null Location

The _Location_ for a disk may be `null` for some types of disk,
e.g. file-based virtual disks. Physical disks or Virtual disks provided via a
hypervisor or other hardware virtualization platform should not be affected.

### Maximum Supported Partition Size

On some disks the _maximum supported partition size_ may differ from the actual
size of a partition created when specifying the maximum size. This difference
in reported size is always less than **1MB**, so if the reported _maximum supported
partition size_ is less than **1MB** then the partition will be considered to be
in the correct state. This is a work around for [this issue](https://windowsserver.uservoice.com/forums/301869-powershell/suggestions/36967870-get-partitionsupportedsize-and-msft-partition-clas)
that has been reported on user voice and also discussed in [issue #181](https://github.com/dsccommunity/StorageDsc/issues/181).

### ReFS on Windows Server 2019

On Windows Server 2019 (build 17763 and above), `Format-Volume` throws an
'Invalid Parameter' exception when called with `ReFS` as the `FileSystem`
parameter. This results in an 'Invalid Parameter' exception being thrown
in the `Set` in the 'Disk' resource.
There is currently no known work around for this issue. It is being tracked
in [issue #227](https://github.com/dsccommunity/StorageDsc/issues/227).
