@{
    # Version number of this module.
    moduleVersion        = '5.0.1'

    # ID used to uniquely identify this module
    GUID                 = '00d73ca1-58b5-46b7-ac1a-5bfcf5814faf'

    # Author of this module
    Author               = 'DSC Community'

    # Company or vendor of this module
    CompanyName          = 'DSC Community'

    # Copyright statement for this module
    Copyright            = 'Copyright the DSC Community contributors. All rights reserved.'

    # Description of the functionality provided by this module
    Description          = 'DSC resources for managing storage on Windows Servers.'

    # Minimum version of the Windows PowerShell engine required by this module
    PowerShellVersion    = '4.0'

    # Minimum version of the common language runtime (CLR) required by this module
    CLRVersion           = '4.0'

    # Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
    FunctionsToExport    = @()

    # Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
    CmdletsToExport      = @()

    # Variables to export from this module
    VariablesToExport    = @()

    # Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
    AliasesToExport      = @()

    # DSC resources to export from this module
    DscResourcesToExport = @(
        'DiskAccessPath',
        'MountImage',
        'OpticalDiskDriveLetter',
        'WaitForDisk',
        'WaitForVolume',
        'Disk'
    )

    # Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
    PrivateData          = @{
        PSData = @{
            # Set to a prerelease string value if the release should be a prerelease.
            Prerelease   = ''

            # Tags applied to this module. These help with module discovery in online galleries.
            Tags         = @('DesiredStateConfiguration', 'DSC', 'DSCResource', 'Disk', 'Storage', 'Partition', 'Volume')

            # A URL to the license for this module.
            LicenseUri   = 'https://github.com/dsccommunity/StorageDsc/blob/master/LICENSE'

            # A URL to the main website for this project.
            ProjectUri   = 'https://github.com/dsccommunity/StorageDsc'

            # A URL to an icon representing this module.
            IconUri      = 'https://dsccommunity.org/images/DSC_Logo_300p.png'

            # ReleaseNotes of this module
            ReleaseNotes = '## [5.0.1] - 2020-08-03

### Changed

- Fixed build failures caused by changes in `ModuleBuilder` module v1.7.0
  by changing `CopyDirectories` to `CopyPaths` - Fixes [Issue #237](https://github.com/dsccommunity/StorageDsc/issues/237).
- Updated to use the common module _DscResource.Common_ - Fixes [Issue #234](https://github.com/dsccommunity/StorageDsc/issues/234).
- Pin `Pester` module to 4.10.1 because Pester 5.0 is missing code
  coverage - Fixes [Issue #238](https://github.com/dsccommunity/StorageDsc/issues/238).
- OpticalDiskDriveLetter:
  - Removed integration test that tests when a disk is not in the
    system as it is not a useful test, does not work correctly
    and is covered by unit tests - Fixes [Issue #240](https://github.com/dsccommunity/StorageDsc/issues/240).
- StorageDsc
  - Automatically publish documentation to GitHub Wiki - Fixes [Issue #241](https://github.com/dsccommunity/StorageDsc/issues/241).

### Fixed

- Disk:
  - Fix bug when multiple partitions with the same drive letter are
    reported by the disk subsystem - Fixes [Issue #210](https://github.com/dsccommunity/StorageDsc/issues/210).

'
        } # End of PSData hashtable
    } # End of PrivateData hashtable
}



