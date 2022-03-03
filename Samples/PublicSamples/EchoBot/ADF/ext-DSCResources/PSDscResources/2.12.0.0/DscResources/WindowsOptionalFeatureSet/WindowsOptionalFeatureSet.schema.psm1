$errorActionPreference = 'Stop'
Set-StrictMode -Version 'Latest'

# Import ResourceSetHelper for New-ResourceSetConfigurationScriptBlock
$script:dscResourcesFolderFilePath = Split-Path -Path $PSScriptRoot -Parent
$script:resourceSetHelperFilePath = Join-Path -Path $script:dscResourcesFolderFilePath -ChildPath 'ResourceSetHelper.psm1'
Import-Module -Name $script:resourceSetHelperFilePath

<#
    .SYNOPSIS
        A composite DSC resource to configure a set of similar WindowsOptionalFeature resources.

    .DESCRIPTION
        A composite DSC resource to configure a set of similar WindowsOptionalFeature resources.

    .PARAMETER Name
        The names of the Windows optional features to enable or disable.

    .PARAMETER Ensure
        Specifies whether the features should be enabled or disabled.

        To enable a set of features, set this property to Present.
        To disable a set of features, set this property to Absent.

    .PARAMETER RemoveFilesOnDisable
        Specifies whether or not to remove all files associated with the features when they are
        disabled.

    .PARAMETER NoWindowsUpdateCheck
        Specifies whether or not DISM should contact Windows Update (WU) when searching for the
        source files to restore Windows optional features on an online image.

    .PARAMETER LogPath
        The file path to which to log the opertation.

    .PARAMETER LogLevel
        The level of detail to include in the log.
#>
Configuration WindowsOptionalFeatureSet
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]
        $Name,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure,

        [Parameter()]
        [System.Boolean]
        $RemoveFilesOnDisable,

        [Parameter()]
        [System.Boolean]
        $NoWindowsUpdateCheck,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $LogPath,

        [Parameter()]
        [ValidateSet('ErrorsOnly', 'ErrorsAndWarning', 'ErrorsAndWarningAndInformation')]
        [System.String]
        $LogLevel
    )

    $newResourceSetConfigurationParams = @{
        ResourceName = 'WindowsOptionalFeature'
        ModuleName = 'PSDscResources'
        KeyParameterName = 'Name'
        Parameters = $PSBoundParameters
    }

    $configurationScriptBlock = New-ResourceSetConfigurationScriptBlock @newResourceSetConfigurationParams

    # This script block must be run directly in this configuration in order to resolve variables
    . $configurationScriptBlock
}
