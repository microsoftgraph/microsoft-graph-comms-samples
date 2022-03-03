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
        Returns the current Networking Connection Profile for the specified interface.

    .PARAMETER InterfaceAlias
        Specifies the alias for the Interface that is being changed.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Position = 0, Mandatory = $true)]
        [System.String]
        $InterfaceAlias
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
        $($script:localizedData.GettingNetConnectionProfile) -f $InterfaceAlias
    ) -join '')

    $result = Get-NetConnectionProfile -InterfaceAlias $InterfaceAlias

    return @{
        InterfaceAlias   = $result.InterfaceAlias
        NetworkCategory  = $result.NetworkCategory
        IPv4Connectivity = $result.IPv4Connectivity
        IPv6Connectivity = $result.IPv6Connectivity
    }
}

<#
    .SYNOPSIS
        Sets the Network Connection Profile for a specified interface.

    .PARAMETER InterfaceAlias
        Specifies the alias for the Interface that is being changed.

    .PARAMETER IPv4Connectivity
        Specifies the IPv4 Connection Value.

    .PARAMETER IPv6Connectivity
        Specifies the IPv6 Connection Value.

    .PARAMETER NetworkCategory
        Sets the Network Category for the interface
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $InterfaceAlias,

        [Parameter()]
        [ValidateSet('Disconnected', 'NoTraffic', 'Subnet', 'LocalNetwork', 'Internet')]
        [System.String]
        $IPv4Connectivity,

        [Parameter()]
        [ValidateSet('Disconnected', 'NoTraffic', 'Subnet', 'LocalNetwork', 'Internet')]
        [System.String]
        $IPv6Connectivity,

        [Parameter()]
        [ValidateSet('Public', 'Private')]
        [System.String]
        $NetworkCategory
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
        $($script:localizedData.SetNetConnectionProfile) -f $InterfaceAlias
    ) -join '')

    Assert-ResourceProperty @PSBoundParameters

    Set-NetConnectionProfile @PSBoundParameters
}

<#
    .SYNOPSIS
        Tests is the Network Connection Profile for the specified interface is in the correct state.

    .PARAMETER InterfaceAlias
        Specifies the alias for the Interface that is being changed.

    .PARAMETER IPv4Connectivity
        Specifies the IPv4 Connection Value.

    .PARAMETER IPv6Connectivity
        Specifies the IPv6 Connection Value.

    .PARAMETER NetworkCategory
        Sets the NetworkCategory for the interface
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $InterfaceAlias,

        [Parameter()]
        [ValidateSet('Disconnected', 'NoTraffic', 'Subnet', 'LocalNetwork', 'Internet')]
        [System.String]
        $IPv4Connectivity,

        [Parameter()]
        [ValidateSet('Disconnected', 'NoTraffic', 'Subnet', 'LocalNetwork', 'Internet')]
        [System.String]
        $IPv6Connectivity,

        [Parameter()]
        [ValidateSet('Public', 'Private')]
        [System.String]
        $NetworkCategory
    )

    Assert-ResourceProperty @PSBoundParameters

    $current = Get-TargetResource -InterfaceAlias $InterfaceAlias

    if (-not [System.String]::IsNullOrEmpty($IPv4Connectivity) -and `
        ($IPv4Connectivity -ne $current.IPv4Connectivity))
    {
        Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestIPv4Connectivity) -f $IPv4Connectivity, $current.IPv4Connectivity
        ) -join '')

        return $false
    }

    if (-not [System.String]::IsNullOrEmpty($IPv6Connectivity) -and `
        ($IPv6Connectivity -ne $current.IPv6Connectivity))
    {
        Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestIPv6Connectivity) -f $IPv6Connectivity, $current.IPv6Connectivity
        ) -join '')

        return $false
    }

    if (-not [System.String]::IsNullOrEmpty($NetworkCategory) -and `
        ($NetworkCategory -ne $current.NetworkCategory))
    {
        Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestNetworkCategory) -f $NetworkCategory, $current.NetworkCategory
        ) -join '')

        return $false
    }

    return $true
}

<#
    .SYNOPSIS
        Check the parameter combination that was passed was valid.
        Ensures interface exists. If any problems are detected an
        exception will be thrown.

    .PARAMETER InterfaceAlias
        Specifies the alias for the Interface that is being changed.

    .PARAMETER IPv4Connectivity
        Specifies the IPv4 Connection Value.

    .PARAMETER IPv6Connectivity
        Specifies the IPv6 Connection Value.

    .PARAMETER NetworkCategory
        Sets the NetworkCategory for the interface
#>
function Assert-ResourceProperty
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $InterfaceAlias,

        [Parameter()]
        [ValidateSet('Disconnected', 'NoTraffic', 'Subnet', 'LocalNetwork', 'Internet')]
        [System.String]
        $IPv4Connectivity,

        [Parameter()]
        [ValidateSet('Disconnected', 'NoTraffic', 'Subnet', 'LocalNetwork', 'Internet')]
        [System.String]
        $IPv6Connectivity,

        [Parameter()]
        [ValidateSet('Public', 'Private')]
        [System.String]
        $NetworkCategory
    )

    if (-not (Get-NetAdapter | Where-Object -Property Name -EQ $InterfaceAlias ))
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.InterfaceNotAvailableError -f $InterfaceAlias)
    }

    if ([System.String]::IsNullOrEmpty($IPv4Connectivity) -and `
        [System.String]::IsNullOrEmpty($IPv6Connectivity) -and `
        [System.String]::IsNullOrEmpty($NetworkCategory))
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.ParameterCombinationError)
    }
} # Assert-ResourceProperty
