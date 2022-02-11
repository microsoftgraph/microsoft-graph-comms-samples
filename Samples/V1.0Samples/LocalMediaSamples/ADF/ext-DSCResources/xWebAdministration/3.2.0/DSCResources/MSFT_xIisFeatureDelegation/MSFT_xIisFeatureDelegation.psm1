$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'
$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'xWebAdministration.Common'

Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'xWebAdministration.Common.psm1')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_xIisFeatureDelegation'

<#
    .SYNOPSIS
        This will return a hashtable of results

    .PARAMETER Filter
        Specifies the IIS configuration section to lock or unlock.

    .PARAMETER Path
        Specifies the configuration path. This can be either an IIS configuration path in the format
        computer machine/webroot/apphost, or the IIS module path in this format IIS:\sites\Default Web Site.

    .PARAMETER OverrideMode
        Determines whether to lock or unlock the specified section.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Filter,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [ValidateSet('Allow', 'Deny')]
        [String]
        $OverrideMode,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Path
    )

    [String] $currentOverrideMode = Get-OverrideMode -Filter $Filter -Path $Path

    Write-Verbose -Message $script:localizedData.VerboseGetTargetResource

    return @{
        Path         = $Path
        Filter       = $Filter
        OverrideMode = $currentOverrideMode
    }
}

<#
    .SYNOPSIS
        This will set the resource to the desired state.

    .PARAMETER Filter
        Specifies the IIS configuration section to lock or unlock.

    .PARAMETER Path
        Specifies the configuration path. This can be either an IIS configuration path in the format
        computer machine/webroot/apphost, or the IIS module path in this format IIS:\sites\Default Web Site.

    .PARAMETER OverrideMode
        Determines whether to lock or unlock the specified section.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Filter,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [ValidateSet('Allow', 'Deny')]
        [String]
        $OverrideMode,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Path
    )

     Write-Verbose -Message ( $script:localizedData.VerboseSetTargetResource -f $Filter, $OverrideMode )

     Set-WebConfiguration -Filter $Filter -PsPath $Path -Metadata 'overrideMode' -Value $OverrideMode
}

<#
    .SYNOPSIS
        This will return whether the resource is in desired state.

    .PARAMETER Filter
        Specifies the IIS configuration section to lock or unlock.

    .PARAMETER OverrideMode
        Determines whether to lock or unlock the specified section.

    .PARAMETER Path
        Specifies the configuration path. This can be either an IIS configuration path in the format
        computer machine/webroot/apphost, or the IIS module path in this format IIS:\sites\Default Web Site.

#>
function Test-TargetResource
{
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSDSCUseVerboseMessageInDSCResource", "",
        Justification = 'Verbose messaging in helper function')]
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Filter,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [ValidateSet('Allow', 'Deny')]
        [String]
        $OverrideMode,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Path
    )

    [String] $currentOverrideMode = Get-OverrideMode -Filter $Filter -Path $Path

    if ($currentOverrideMode -eq $OverrideMode)
    {
        return $true
    }

    return $false
}

#region Helper functions
<#
    .SYNOPSIS
        This will return the current override mode for the specified configsection.

    .PARAMETER Filter
        Specifies the IIS configuration section.

    .PARAMETER Path
        Specifies the configuration path. This can be either an IIS configuration path in the format
        computer machine/webroot/apphost, or the IIS module path in this format IIS:\sites\Default Web Site.

#>
function Get-OverrideMode
{
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Filter,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Path
    )

    Assert-Module

    Write-Verbose -Message ( $script:localizedData.GetOverrideMode -f $Filter )

    $webConfig = Get-WebConfiguration -PsPath $Path -Filter $Filter -Metadata

    $currentOverrideMode = $webConfig.Metadata.effectiveOverrideMode

    if ($currentOverrideMode -notmatch "^(Allow|Deny)$")
    {
        $errorMessage = $($script:localizedData.UnableToGetConfig) -f $Filter
        New-TerminatingError -ErrorId UnableToGetConfig `
                             -ErrorMessage $errorMessage `
                             -ErrorCategory:InvalidResult
    }

    return $currentOverrideMode
}
#endregion

Export-ModuleMember -function *-TargetResource
