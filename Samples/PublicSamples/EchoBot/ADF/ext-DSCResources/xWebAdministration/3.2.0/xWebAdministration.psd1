@{
    # Version number of this module.
    moduleVersion = '3.2.0'

    # ID used to uniquely identify this module
    GUID = 'b3239f27-d7d3-4ae6-a5d2-d9a1c97d6ae4'

    # Author of this module
    Author = 'DSC Community'

    # Company or vendor of this module
    CompanyName = 'DSC Community'

    # Copyright statement for this module
    Copyright = 'Copyright the DSC Community contributors. All rights reserved.'

    # Description of the functionality provided by this module
    Description = 'Module with DSC Resources for Web Administration'

    # Minimum version of the Windows PowerShell engine required by this module
    PowerShellVersion = '4.0'

    # Minimum version of the common language runtime (CLR) required by this module
    CLRVersion = '4.0'

    # Functions to export from this module
    FunctionsToExport = @()

    # Cmdlets to export from this module
    CmdletsToExport = @()

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module
    AliasesToExport = @()

    DscResourcesToExport = @(
        'WebApplicationHandler'
        'xIisFeatureDelegation'
        'xIIsHandler'
        'xIisLogging'
        'xIisMimeTypeMapping'
        'xIisModule'
        'xSslSettings'
        'xWebApplication'
        'xWebAppPool'
        'xWebAppPoolDefaults'
        'xWebConfigKeyValue'
        'xWebConfigProperty'
        'xWebConfigPropertyCollection'
        'xWebSite'
        'xWebSiteDefaults'
        'xWebVirtualDirectory'
    )

    # Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
    PrivateData = @{

        PSData = @{
            Prerelease = ''

            # Tags applied to this module. These help with module discovery in online galleries.
            Tags = @('DesiredStateConfiguration', 'DSC', 'DSCResourceKit', 'DSCResource')

            # A URL to the license for this module.
            LicenseUri = 'https://github.com/dsccommunity/xWebAdministration/blob/master/LICENSE'

            # A URL to the main website for this project.
            ProjectUri = 'https://github.com/dsccommunity/xWebAdministration'

            # A URL to an icon representing this module.
            IconUri = 'https://dsccommunity.org/images/DSC_Logo_300p.png'

            # ReleaseNotes of this module
            ReleaseNotes = '## [3.2.0] - 2020-08-06

### Added

- xWebAdminstration
  - Integration tests are running on more Microsoft-hosted agents to
    test all possible operating systems ([issue #550](https://github.com/PowerShell/xWebAdministration/issues/550)).
  - Fix a few lingering bugs in CICD ([issue #567](https://github.com/PowerShell/xWebAdministration/issues/567))
  - Remove an image from testing that MS will be deprecating soon ([issue #565](https://github.com/PowerShell/xWebAdministration/issues/567))

### Changed

- xWebAdminstration
  - Module was wrongly bumped to `4.0.0` (there a no merged breaking changes)
    so the versions `4.0.0-preview1` to `4.0.0-preview5` have been unlisted
    from the Gallery and removed as GitHub releases. The latest release is
    `3.2.0`.
  - Azure Pipelines will no longer trigger on changes to just the CHANGELOG.md
    (when merging to master).
  - The deploy step is no longer run if the Azure DevOps organization URL
    does not contain ''dsccommunity''.
  - Changed the VS Code project settings to trim trailing whitespace for
    markdown files too.
  - Update pipeline to use NuGetVersionV2 from `GitVersion`.
  - Pinned PowerShell module Pester to v4.10.1 in the pipeline due to
    tests is not yet compatible with Pester 5.
  - Using latest version of the PowerShell module ModuleBuilder.
    - Updated build.yaml to use the correct values.
- xWebSite
  - Ensure that Test-TargetResource in xWebSite tests all properties before
    returning true or false, and that it uses a consistent style ([issue #221](https://github.com/PowerShell/xWebAdministration/issues/550)).
- xIisMimeTypeMapping
  - Update misleading localization strings
- xIisLogging
  - Add Ensure to LogCustomFields. ([issue #571](https://github.com/dsccommunity/xWebAdministration/issues/571))

### Fixed

- WebApplicationHandler
  - Integration test should no longer fail intermittent ([issue #558](https://github.com/PowerShell/xWebAdministration/issues/558)).

'

        } # End of PSData hashtable

    } # End of PrivateData hashtable
}



