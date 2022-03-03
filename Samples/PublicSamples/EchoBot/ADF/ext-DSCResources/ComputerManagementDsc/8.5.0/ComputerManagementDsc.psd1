@{
    # Version number of this module.
    moduleVersion        = '8.5.0'

    # ID used to uniquely identify this module
    GUID                 = 'B5004952-489E-43EA-999C-F16A25355B89'

    # Author of this module
    Author               = 'DSC Community'

    # Company or vendor of this module
    CompanyName          = 'DSC Community'

    # Copyright statement for this module
    Copyright            = 'Copyright the DSC Community contributors. All rights reserved.'

    # Description of the functionality provided by this module
    Description          = 'DSC resources for configuration of a Windows computer. These DSC resources allow you to perform computer management tasks, such as renaming the computer, joining a domain and scheduling tasks as well as configuring items such as virtual memory, event logs, time zones and power settings.'

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
    DscResourcesToExport = @('Computer','OfflineDomainJoin','PendingReboot','PowerPlan','PowerShellExecutionPolicy','RemoteDesktopAdmin','ScheduledTask','SmbServerConfiguration','SmbShare','SystemLocale','TimeZone','VirtualMemory','WindowsEventLog','WindowsCapability','IEEnhancedSecurityConfiguration','UserAccountControl')

    # Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
    PrivateData          = @{

        PSData = @{
            # Set to a prerelease string value if the release should be a prerelease.
            Prerelease   = ''

            # Tags applied to this module. These help with module discovery in online galleries.
            Tags         = @('DesiredStateConfiguration', 'DSC', 'DSCResource')

            # A URL to the license for this module.
            LicenseUri   = 'https://github.com/dsccommunity/ComputerManagementDsc/blob/main/LICENSE'

            # A URL to the main website for this project.
            ProjectUri   = 'https://github.com/dsccommunity/ComputerManagementDsc'

            # A URL to an icon representing this module.
            IconUri      = 'https://dsccommunity.org/images/DSC_Logo_300p.png'

            # ReleaseNotes of this module
            ReleaseNotes = '## [8.5.0] - 2021-09-13

### Added

- WindowsEventLog
  - Added support to restrict guest access - Fixes [Issue #338](https://github.com/dsccommunity/ComputerManagementDsc/issues/338).
  - Added support to create custom event sources and optionally register
    resource files - Fixes [Issue #355](https://github.com/dsccommunity/ComputerManagementDsc/issues/355).
- WindowsCapability
  - Added the ''Source'' parameter for Add-WindowsCapability as an
    optional parameter - Fixes [Issue #361](https://github.com/dsccommunity/ComputerManagementDsc/issues/361)

### Changed

- WindowsEventLog
  - Reformatted code to better align with current DSCResources coding standards.
- Renamed `master` branch to `main` - Fixes [Issue #348](https://github.com/dsccommunity/ComputerManagementDsc/issues/348).
- Added support for publishing code coverage to `CodeCov.io` and
  Azure Pipelines - Fixes [Issue #367](https://github.com/dsccommunity/ComputerManagementDsc/issues/367).
- Updated build to use `Sampler.GitHubTasks` - Fixes [Issue #365](https://github.com/dsccommunity/ComputerManagementDsc/issues/365).
- Corrected case of module publish tasks - Fixes [Issue #368](https://github.com/dsccommunity/ComputerManagementDsc/issues/368).
- Corrected code coverage badge in `README.md`.
- Updated build pipeline tasks and remove unused environment variables.
- Removed duplicate code coverage badge.

### Fixed

- WindowsEventLog
  - Fixed issue requiring IsEnabled to be declared and set to $true in order
    to set the MaximumSizeInBytes property - Fixes [Issue #349](https://github.com/dsccommunity/ComputerManagementDsc/issues/349).
  - Fixed issue where configuring log retention on a non-classic event log will
    throw.
- ScheduledTask
  - Fixed issue with disabling scheduled tasks that have "Run whether user is
    logged on or not" configured - Fixes [Issue #306](https://github.com/dsccommunity/ComputerManagementDsc/issues/306).
  - Fixed issue with `ExecuteAsCredential` not returning fully qualified username
    on newer versions of Windows 10 and Windows Server 2019 for both local
    accounts and domain accounts - Fixes [Issue #352](https://github.com/dsccommunity/ComputerManagementDsc/issues/352).
  - Fixed issue with `StartTime` failing Test-Resource if not specified in the
    resource - Fixes [Issue #148](https://github.com/dsccommunity/ComputerManagementDsc/issues/148).
- PendingReboot
  - Fixed issue with loading localized data on non en-US operating systems -
    Fixes [Issue #350](https://github.com/dsccommunity/ComputerManagementDsc/issues/350).

'
        } # End of PSData hashtable
    } # End of PrivateData hashtable
}
