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
        Returns the current state of the Default Gateway for an interface.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the default gateway address is set.

    .PARAMETER AddressFamily
        IP address family.

    .PARAMETER Address
        The desired default gateway address - if not provided default gateway will be removed.
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
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily,

        [Parameter()]
        [System.String]
        $Address
    )

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingDefaultGatewayAddressMessage)
        ) -join '' )

    $destinationPrefix = Get-NetDefaultGatewayDestinationPrefix `
        -AddressFamily $AddressFamily

    $defaultRoutes = Get-NetDefaultRoute `
        -InterfaceAlias $InterfaceAlias `
        -AddressFamily $AddressFamily

    $returnValue = @{
        AddressFamily  = $AddressFamily
        InterfaceAlias = $InterfaceAlias
    }

    <#
        If there is a Default Gateway defined for this interface/address family add it
        to the return value.
    #>
    if ($defaultRoutes)
    {
        $returnValue += @{
            Address = $defaultRoutes.NextHop
        }
    }
    else
    {
        $returnValue += @{
            Address = $null
        }
    }

    return $returnValue
}

<#
    .SYNOPSIS
        Sets the Default Gateway for an interface.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the default gateway address is set.

    .PARAMETER AddressFamily
        IP address family.

    .PARAMETER Address
        The desired default gateway address - if not provided default gateway will be removed.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily,

        [Parameter()]
        [System.String]
        $Address
    )

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.ApplyingDefaultGatewayAddressMessage)
        ) -join '' )

    $defaultRoutes = @(Get-NetDefaultRoute `
        -InterfaceAlias $InterfaceAlias `
        -AddressFamily $AddressFamily)

    # Remove any existing default routes
    foreach ($defaultRoute in $defaultRoutes)
    {
        Remove-NetRoute `
            -DestinationPrefix $defaultRoute.DestinationPrefix `
            -NextHop $defaultRoute.NextHop `
            -InterfaceIndex $defaultRoute.InterfaceIndex `
            -AddressFamily $defaultRoute.AddressFamily `
            -Confirm:$false -ErrorAction Stop
    }

    if ($Address)
    {
        $destinationPrefix = Get-NetDefaultGatewayDestinationPrefix `
            -AddressFamily $AddressFamily

        # Set the correct Default Route
        $newNetRouteParameters = @{
            DestinationPrefix = $destinationPrefix
            InterfaceAlias    = $InterfaceAlias
            AddressFamily     = $AddressFamily
            NextHop           = $Address
        }

        New-NetRoute @newNetRouteParameters -ErrorAction Stop

        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.DefaultGatewayAddressSetToDesiredStateMessage)
            ) -join '' )
    }
    else
    {
        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.DefaultGatewayRemovedMessage)
            ) -join '' )
    }
}

<#
    .SYNOPSIS
        Tests the state of the Default Gateway for an interface.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the default gateway address is set.

    .PARAMETER AddressFamily
        IP address family.

    .PARAMETER Address
        The desired default gateway address - if not provided default gateway will be removed.
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
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily,

        [Parameter()]
        [System.String]
        $Address
    )

    # Flag to signal whether settings are correct
    $desiredConfigurationMatch = $true

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckingDefaultGatewayAddressMessage)
        ) -join '' )

    Assert-ResourceProperty @PSBoundParameters

    $defaultRoutes = @(Get-NetDefaultRoute `
        -InterfaceAlias $InterfaceAlias `
        -AddressFamily $AddressFamily)

    # Test if the Default Gateway passed is equal to the current default gateway
    if ($Address)
    {
        if ($defaultRoutes)
        {
            $nextHopRoute = $defaultRoutes.Where( {
                $_.NextHop -eq $Address
            } )

            if ($nextHopRoute)
            {
                Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                        $($script:localizedData.DefaultGatewayCorrectMessage)
                    ) -join '' )
            }
            else
            {
                Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                        $($script:localizedData.DefaultGatewayNotMatchMessage) -f $Address, $defaultRoutes.NextHop
                    ) -join '' )
                $desiredConfigurationMatch = $false
            }
        }
        else
        {
            Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                    $($script:localizedData.DefaultGatewayDoesNotExistMessage) -f $Address
                ) -join '' )
            $desiredConfigurationMatch = $false
        }
    }
    else
    {
        # Is a default gateway address set?
        if ($defaultRoutes)
        {
            Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                    $($script:localizedData.DefaultGatewayExistsButShouldNotMessage)
                ) -join '' )
            $desiredConfigurationMatch = $false
        }
        else
        {
            Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                    $($script:localizedData.DefaultGatewayExistsAndShouldMessage)
                ) -join '' )
        }
    }

    return $desiredConfigurationMatch
}

<#
    .SYNOPSIS
        Check the Address details are valid and do not conflict with Address family.
        Ensures interface exists. If any problems are detected an exception will be thrown.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the default gateway address is set.

    .PARAMETER AddressFamily
        IP address family.

    .PARAMETER Address
        The desired default gateway address - if not provided default gateway will be removed.
#>
function Assert-ResourceProperty
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias,

        [Parameter()]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily = 'IPv4',

        [Parameter()]
        [System.String]
        $Address
    )

    if (-not (Get-NetAdapter | Where-Object -Property Name -EQ $InterfaceAlias ))
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.InterfaceNotAvailableError -f $InterfaceAlias)
    }

    if ($Address)
    {
        Assert-IPAddress -Address $Address -AddressFamily $AddressFamily
    }
} # Assert-ResourceProperty

<#
    .SYNOPSIS
    Get the default gateway destination prefix for the IP address family.

    .PARAMETER AddressFamily
    IP address family.
#>
function Get-NetDefaultGatewayDestinationPrefix
{
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter()]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily = 'IPv4'
    )

    if ($AddressFamily -eq 'IPv4')
    {
        $destinationPrefix = '0.0.0.0/0'
    }
    else
    {
        $destinationPrefix = '::/0'
    }

    return $destinationPrefix
} # Get-NetDefaultGatewayDestinationPrefix

<#
    .SYNOPSIS
        Get the default network routes assigned to the interface.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the default gateway address is set.

    .PARAMETER AddressFamily
        IP address family.
#>
function Get-NetDefaultRoute
{
    [CmdletBinding()]
    [OutputType([System.String[]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias,

        [Parameter()]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily = 'IPv4'
    )

    $destinationPrefix = Get-NetDefaultGatewayDestinationPrefix `
        -AddressFamily $AddressFamily

    return @(Get-NetRoute `
        -InterfaceAlias $InterfaceAlias `
        -AddressFamily $AddressFamily `
        -ErrorAction Stop).Where({
            $_.DestinationPrefix -eq $destinationPrefix
        })
} # Get-NetDefaultRoute

Export-ModuleMember -function *-TargetResource
