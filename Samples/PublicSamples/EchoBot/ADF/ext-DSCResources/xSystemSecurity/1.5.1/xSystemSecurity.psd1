@{
    # Version number of this module.
    moduleVersion     = '1.5.1'

    # ID used to uniquely identify this module
    GUID              = 'e30107af-a22a-48fb-b7bc-7d2b98489ac5'

    # Author of this module
    Author            = 'DSC Community'

    # Company or vendor of this module
    CompanyName       = 'DSC Community'

    # Copyright statement for this module
    Copyright         = 'Copyright the DSC Community contributors. All rights reserved.'

    # Description of the functionality provided by this module
    Description       = 'This module contains DSC resources for configuring and managing computer security.'

    # Functions to export from this module
    FunctionsToExport = @()

    # Cmdlets to export from this module
    CmdletsToExport   = @()

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module
    AliasesToExport   = @()

    DscResourcesToExport = @(
        'xIEEsc'
        'xUAC'
        'xFileSystemAccessRule'
    )

    # Minimum version of the Windows PowerShell engine required by this module
    PowerShellVersion = '4.0'

    # Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
    PrivateData       = @{
        PSData = @{
            Prerelease = ''

            # Tags applied to this module. These help with module discovery in online galleries.
            Tags         = @('DesiredStateConfiguration', 'DSC', 'DSCResourceKit', 'DSCResource')

            # A URL to the license for this module.
            LicenseUri   = 'https://github.com/dsccommunity/xSystemSecurity/blob/master/LICENSE'

            # A URL to the main website for this project.
            ProjectUri   = 'https://github.com/dsccommunity/xSystemSecurity'

            # A URL to an icon representing this module.
            IconUri = 'https://dsccommunity.org/images/DSC_Logo_300p.png'

            # ReleaseNotes of this module
            ReleaseNotes = '## [1.5.1] - 2020-03-13

### Deprecated

- **THIS MODULE HAS BEEN DEPRECATED**. It will no longer be released. Please use
  the following modules instead:
  - The resource `xIEEsc` have been replaced by `IEEnhancedSecurityConfiguration`
    in the module [ComputerManagementDsc](https://github.com/dsccommunity/ComputerManagementDsc).
  - The resource `xUac` have been replaced by `UserAccountControl`
    in the module [ComputerManagementDsc](https://github.com/dsccommunity/ComputerManagementDsc).
  - The resource `xFileSystemAccessRule` have been replaced by `FileSystemAccessRule`
    in the module [FileSystemDsc](https://github.com/dsccommunity/FileSystemDsc).

### Fixed

- Fixes issue with importing composite resources ([issue #34](https://github.com/dsccommunity/xSystemSecurity/issues/34)).

## [1.5.0] - 2020-01-29

### Added

- xSystemSecurity
  - Added continuous delivery with a new CI pipeline.

### Fixed

- xSystemSecurity
  - Fixed the correct URL on status badges.
- xFileSystemAccessRule
  - Corrected flag handling so that the `Test-TargetResource` passes
    correctly.
  - Using `Ensure = ''Absent''` with no rights specified will now correctly
    remove existing ACLs for the specified identity, rather than silently
    leaving them there.
  - Correctly returns property `Ensure` from the function `Get-TargetResource`.

## [1.4.0.0] - 2018-06-13

- Changes to xFileSystemAccessRule
  - Fixed issue when cluster shared disk is not present on the server
    ([issue #16](https://github.com/dsccommunity/xSystemSecurity/issues/16)).
    [Dan Reist (@randomnote1)](https://github.com/randomnote1)

#

## [1.3.0.0] - 2017-12-20

- Updated FileSystemACL Set

#

## [1.2.0.0] - 2016-09-21

- Converted appveyor.yml to install Pester from PSGallery instead of from
  Chocolatey.
- Added xFileSystemAccessRule resource

#

## [1.1.0.0] - 2015-09-11

- Fixed encoding

#

## [1.0.0.0] - 2015-04-23

- Initial release with the following resources
  - xUAC
  - xIEEsc

'

        } # End of PSData hashtable

    } # End of PrivateData hashtable
}









