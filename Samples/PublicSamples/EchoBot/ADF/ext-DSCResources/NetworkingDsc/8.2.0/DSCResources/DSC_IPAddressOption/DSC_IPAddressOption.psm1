$modulePath = Join-Path -Path (Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent) -ChildPath 'Modules'

# Import the Networking Common Modules
Import-Module -Name (Join-Path -Path $modulePath `
        -ChildPath (Join-Path -Path 'NetworkingDsc.Common' `
            -ChildPath 'NetworkingDsc.Common.psm1'))

Import-Module -Name (Join-Path -Path $modulePath -ChildPath 'DscResource.Common')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -DefaultUICulture 'en-US'

<#
    .SYNOPSIS
        Returns the current state of an IP address option.

    .PARAMETER IPAddress
        The desired IP address.

    .PARAMETER SkipAsSource
        The skip as source option.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $IPAddress,

        [Parameter()]
        [System.Boolean]
        $SkipAsSource = $false
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingIPAddressOptionMessage -f $IPAddress)
        ) -join '')

    $currentIPAddress = Get-NetIPAddress -IPAddress $IPAddress

    $returnValue = @{
        IPAddress    = $IPAddress
        SkipAsSource = $currentIPAddress.SkipAsSource
    }

    return $returnValue
}

<#
    .SYNOPSIS
        Set the IP address options.

    .PARAMETER IPAddress
        The desired IP address.

    .PARAMETER SkipAsSource
        The skip as source option.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $IPAddress,

        [Parameter()]
        [System.Boolean]
        $SkipAsSource = $false
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.ApplyingIPAddressOptionMessage -f $IPAddress)
        ) -join '')

    $currentConfig = Get-TargetResource @PSBoundParameters

    if ($currentConfig.SkipAsSource -ne $SkipAsSource)
    {
        Set-NetIPAddress -IPAddress $IPAddress -SkipAsSource $SkipAsSource
    }
}

<#
    .SYNOPSIS
        Tests the IP address options.

    .PARAMETER IPAddress
        The desired IP address.

    .PARAMETER SkipAsSource
        The skip as source option.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $IPAddress,

        [Parameter()]
        [System.Boolean]
        $SkipAsSource = $false
    )

    # Flag to signal whether settings are correct
    $desiredConfigurationMatch = $true

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckingIPAddressOptionMessage -f $IPAddress)
        ) -join '')

    $currentConfig = Get-TargetResource @PSBoundParameters

    $desiredConfigurationMatch = $desiredConfigurationMatch -and
                                 $currentConfig.SkipAsSource -eq $SkipAsSource

    return $desiredConfigurationMatch
}

Export-ModuleMember -function *-TargetResource
