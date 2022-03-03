@{
# Version number of this module.
moduleVersion = '6.0.1'

# ID used to uniquely identify this module
GUID = '9FECD4F6-8F02-4707-99B3-539E940E9FF5'

# Author of this module
Author = 'DSC Community'

# Company or vendor of this module
CompanyName = 'DSC Community'

# Copyright statement for this module
Copyright = 'Copyright the DSC Community contributors. All rights reserved.'

# Description of the functionality provided by this module
Description = 'The ActiveDirectoryDsc module contains DSC resources for deployment and configuration of Active Directory.

These DSC resources allow you to configure new domains, child domains, and high availability domain controllers, establish cross-domain trusts and manage users, groups and OUs.'

# Minimum version of the Windows PowerShell engine required by this module
PowerShellVersion = '5.0'

# Minimum version of the common language runtime (CLR) required by this module
CLRVersion = '4.0'

# Nested modules to load when this module is imported.
NestedModules = 'Modules\ActiveDirectoryDsc.Common\ActiveDirectoryDsc.Common.psm1'

# Functions to export from this module
FunctionsToExport = @(
  # Exported so that WaitForADDomain can use this function in a separate scope.
  'Find-DomainController'
)

# Cmdlets to export from this module
CmdletsToExport = @()

# Variables to export from this module
VariablesToExport = @()

# Aliases to export from this module
AliasesToExport = @()

# Dsc Resources to export from this module
DscResourcesToExport = @(
    'ADComputer'
    'ADDomain'
    'ADDomainController'
    'ADDomainControllerProperties'
    'ADDomainDefaultPasswordPolicy'
    'ADDomainFunctionalLevel'
    'ADDomainTrust'
    'ADForestFunctionalLevel'
    'ADForestProperties'
    'ADGroup'
    'ADKDSKey'
    'ADManagedServiceAccount'
    'ADObjectEnabledState'
    'ADObjectPermissionEntry'
    'ADOptionalFeature'
    'ADOrganizationalUnit'
    'ADReplicationSite'
    'ADReplicationSiteLink'
    'ADServicePrincipalName'
    'ADUser'
    'WaitForADDomain'
)

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        Tags = @('DesiredStateConfiguration', 'DSC', 'DSCResourceKit', 'DSCResource')

        # A URL to the license for this module.
        LicenseUri = 'https://github.com/dsccommunity/ActiveDirectoryDsc/blob/master/LICENSE'

        # A URL to the main website for this project.
        ProjectUri = 'https://github.com/dsccommunity/ActiveDirectoryDsc'

        # A URL to an icon representing this module.
        IconUri = 'https://dsccommunity.org/images/DSC_Logo_300p.png'

        # ReleaseNotes of this module
        ReleaseNotes = '## [6.0.1] - 2020-04-16

### Fixed

- ActiveDirectoryDsc
  - The regular expression for `minor-version-bump-message` in the file
    `GitVersion.yml` was changed to only raise minor version when the
    commit message contain the word `add`, `adds`, `minor`, `feature`,
    or `features` ([issue #588](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/588)).
  - Rename folder ''Tests'' to folder ''tests'' (lower-case).
  - Moved oldest changelog details to historic changelog.
- ADDomain
  - Added additional Get-ADDomain retry exceptions
    ([issue #581](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/581)).
- ADUser
  - Fixed PasswordAuthentication parameter handling
  ([issue #582](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/582)).

### Changed

- ActiveDirectoryDsc
  - Only run CI pipeline on branch `master` when there are changes to files
    inside the `source` folder.

## [6.0.0] - 2020-03-12

### Added

- ActiveDirectoryDsc
  - Added [Codecov.io](https://codecov.io) support.
  - Fixed miscellaneous spelling errors.
  - Added Strict-Mode v1.0 to all unit tests.
- ADDomain
  - Added integration tests
    ([issue #345](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/345)).
- ADGroup
  - Added support for Managed Service Accounts
    ([issue #532](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/532)).
- ADForestProperties
  - Added TombstoneLifetime property
    ([issue #302](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/302)).
  - Added Integration tests
    ([issue #349](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/349)).

### Fixed

- ADForestProperties
  - Fixed ability to clear `ServicePrincipalNameSuffix` and `UserPrincipalNameSuffix`
    ([issue #548](https://github.com/PowerShell/ActiveDirectoryDsc/issues/548)).
- WaitForADDomain
  - Fixed `Find-DomainController` to correctly handle an exception thrown when a domain controller is not ready
    ([issue #530](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/530)).
- ADObjectPermissionEntry
  - Fixed issue where Get-DscConfiguration / Test-DscConfiguration throw an exception when target object path does not
    yet exist
    ([issue #552](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/552)).
  - Fixed issue where Get-TargetResource throw an exception, `Cannot find drive. A drive with the name ''AD'' does not
    exist`, when running soon after domain controller restart
    ([issue #547](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/547)).
- ADOrganizationalUnit
  - Fixed issue where Get-DscConfiguration/Test-DscConfiguration throws an exception when parent path does not yet exist
    ([issue #553](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/553)).
- ADReplicationSiteLink
  - Fixed issue creating a Site Link with options specified
    ([issue #571](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/571)).
- ADDomain
  - Added additional Get-ADDomain retry exceptions
    ([issue #574](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/574)).

### Changed

- ActiveDirectoryDsc
  - BREAKING CHANGE: Required PowerShell version increased from v4.0 to v5.0
  - Updated Azure Pipeline Windows image
    ([issue #551](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/551)).
  - Updated license copyright
    ([issue #550](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/550)).
- ADDomain
  - Changed Domain Install Tracking File to use NetLogon Registry Test.
    ([issue #560](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/560)).
  - Updated the Get-TargetResource function with the following:
    - Removed unused parameters.
    - Removed unnecessary domain membership check.
    - Removed unneeded catch exception blocks.
    - Changed Get-ADDomain and Get-ADForest to use localhost as the server.
    - Improved Try/Catch blocks to only cover cmdlet calls.
    - Simplified retry timing loop.
  - Refactored unit tests.
  - Updated NewChildDomain example to clarify the contents of the credential parameter and use Windows 2016 rather than
    2012 R2.
- ADDomainController
  - Updated the Get-TargetResource function with the following:
    - Removed unused parameters.
    - Added IsDnsServer read-only property
      ([issue #490](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/490)).
- ADForestProperties
  - Refactored unit tests.
- ADReplicationSiteLink
  - Refactored the `Set-TargetResource` function so that properties are only set if they have been changed.
  - Refactored the resource unit tests.
  - Added quotes to all the variables in the localised string data.
- ADOrganizationalUnit
  - Replaced throws with `New-InvalidOperationException`.
  - Refactored `Get-TargetResource` to not reference properties of a `$null` object
  - Fixed organization references to organizational.
  - Refactored `Test-TargetResource` to use `Compare-ResourcePropertyState` common function.
  - Reformatted code to keep line lengths to less than 120 characters.
  - Removed redundant `Assert-Module` and `Get-ADOrganizationalUnit` function calls from `Set-TargetResource`.
  - Wrapped `Set-ADOrganizationalUnit` and `Remove-ADOrganizationalUnit` with try/catch blocks and used common exception
    function.
  - Added `DistinguishedName` read-only property.
  - Refactored unit tests.
- ADUser
  - Improve Try/Catch blocks to only cover cmdlet calls.
  - Move the Test-Password function to the ActiveDirectoryDsc.Common module and add unit tests.
  - Reformat code to keep line lengths to less than 120 characters.
  - Fix Password parameter processing when PasswordNeverResets is $true.
  - Remove unnecessary Enabled parameter check.
  - Remove unnecessary Clear explicit parameter check.
  - Add check to only call Set-ADUser if there are properties to change.
  - Refactored Unit Tests - ([issue #467](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/467))

## [5.0.0] - 2020-01-14

### Added

- ADServicePrincipalName
  - Added Integration tests
    ([issue #358](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/358)).
- ADManagedServiceAccount
  - Added Integration tests.
- ADKDSKey
  - Added Integration tests
    ([issue #351](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/351)).

### Changed

- ADManagedServiceAccount
  - KerberosEncryptionType property added.
    ([issue #511](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/511)).
  - BREAKING CHANGE: AccountType parameter ValidateSet changed from (''Group'', ''Single'') to (''Group'', ''Standalone'') -
    Standalone is the correct terminology.
    Ref: [Service Accounts](https://docs.microsoft.com/en-us/windows/security/identity-protection/access-control/service-accounts).
    ([issue #515](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/515)).
  - BREAKING CHANGE: AccountType parameter default of Single removed. - Enforce positive choice of account type.
  - BREAKING CHANGE: MembershipAttribute parameter ValidateSet member SID changed to ObjectSid to match result property
    of Get-AdObject. Previous code does not work if SID is specified.
  - BREAKING CHANGE: AccountTypeForce parameter removed - unnecessary complication.
  - BREAKING CHANGE: Members parameter renamed to ManagedPasswordPrincipals - to closer match Get-AdServiceAccount result
    property PrincipalsAllowedToRetrieveManagedPassword. This is so that a DelegateToAccountPrincipals parameter can be
    added later.
  - Common Compare-ResourcePropertyState function used to replace function specific Compare-TargetResourceState and code
    refactored.
    ([issue #512](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/512)).
  - Resource unit tests refactored to use nested contexts and follow the logic of the module.
- ActiveDirectoryDsc
  - Updated PowerShell help files.
  - Updated Wiki link in README.md.
  - Remove verbose parameters from unit tests.
  - Fix PowerShell script file formatting and culture string alignment.
  - Add the `pipelineIndentationStyle` setting to the Visual Studio Code settings file.
  - Remove unused common function Test-DscParameterState
    ([issue #522](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/522)).

### Fixed

- ActiveDirectoryDsc
  - Fix tests ErrorAction on DscResource.Test Import-Module.
- ADObjectPermissionEntry
  - Updated Assert-ADPSDrive with PSProvider Checks
    ([issue #527](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/527)).
- ADReplicationSite
  - Fixed incorrect evaluation of site configuration state when no description is defined
    ([issue #534](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/534)).
- ADReplicationSiteLink
  - Fix RemovingSites verbose message
    ([issue #518](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/518)).
- ADComputer
  - Fixed the SamAcountName property description
    ([issue #529](https://github.com/dsccommunity/ActiveDirectoryDsc/issues/529)).

'

        # Set to a prerelease string value if the release should be a prerelease.
        Prerelease = ''

      } # End of PSData hashtable

} # End of PrivateData hashtable
}





