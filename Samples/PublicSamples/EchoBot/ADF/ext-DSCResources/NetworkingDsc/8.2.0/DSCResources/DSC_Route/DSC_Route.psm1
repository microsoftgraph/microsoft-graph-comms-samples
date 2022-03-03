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
        Returns the current state of a Route for an interface.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface.

    .PARAMETER AddressFamily
        Specify the IP address family.

    .PARAMETER DestinationPrefix
        Specifies a destination prefix of an IP route.
        A destination prefix consists of an IP address prefix
        and a prefix length, separated by a slash (/).

    .PARAMETER NextHop
        Specifies the next hop for the IP route.
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

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $DestinationPrefix,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $NextHop
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingRouteMessage) `
                -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
        ) -join '' )

    # Lookup the existing Route
    $route = Get-Route @PSBoundParameters

    $returnValue = @{
        InterfaceAlias    = $InterfaceAlias
        AddressFamily     = $AddressFamily
        DestinationPrefix = $DestinationPrefix
        NextHop           = $NextHop
    }

    if ($route)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.RouteExistsMessage) `
                    -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
            ) -join '' )

        $returnValue += @{
            Ensure            = 'Present'
            RouteMetric       = [System.Uint16] $route.RouteMetric
            Publish           = $route.Publish
            PreferredLifetime = [System.Double] $route.PreferredLifetime.TotalSeconds
        }
    }
    else
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.RouteDoesNotExistMessage) `
                    -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
            ) -join '' )

        $returnValue += @{
            Ensure = 'Absent'
        }
    }

    return $returnValue
} # Get-TargetResource

<#
    .SYNOPSIS
        Sets a Route for an interface.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface.

    .PARAMETER AddressFamily
        Specify the IP address family.

    .PARAMETER DestinationPrefix
        Specifies a destination prefix of an IP route.
        A destination prefix consists of an IP address prefix
        and a prefix length, separated by a slash (/).

    .PARAMETER NextHop
        Specifies the next hop for the IP route.

    .PARAMETER Ensure
        Specifies whether the route should exist.
        Defaults to 'Present'.

    .PARAMETER RouteMetric
        Specifies an integer route metric for an IP route.
        Defaults to 256.

    .PARAMETER Publish
        Specifies the publish setting of an IP route.
        Defaults to 'No'.

    .PARAMETER PreferredLifetime
        Specifies a preferred lifetime in seconds of an IP route.
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

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $DestinationPrefix,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $NextHop,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present',

        [Parameter()]
        [System.Uint16]
        $RouteMetric = 256,

        [Parameter()]
        [ValidateSet('No', 'Yes', 'Age')]
        [System.String]
        $Publish = 'No',

        [Parameter()]
        [System.Double]
        $PreferredLifetime
    )

    Write-Verbose -Message ( @(
        "$($MyInvocation.MyCommand): "
        $($script:localizedData.SettingRouteMessage) `
            -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
    ) -join '' )

    # Remove any parameters that can't be splatted.
    $null = $PSBoundParameters.Remove('Ensure')

    # Lookup the existing Route
    $route = Get-Route @PSBoundParameters

    if ($Ensure -eq 'Present')
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.EnsureRouteExistsMessage) `
                    -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
            ) -join '' )

        if ($route)
        {
            # The Route exists - update it
            Set-NetRoute @PSBoundParameters `
                -Confirm:$false `
                -ErrorAction Stop

            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.RouteUpdatedMessage) `
                        -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
                ) -join '' )
        }
        else
        {
            # The Route does not exit - create it
            New-NetRoute @PSBoundParameters `
                -ErrorAction Stop

            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.RouteCreatedMessage) `
                        -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
                ) -join '' )
        }
    }
    else
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.EnsureRouteDoesNotExistMessage) `
                    -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
            ) -join '' )

        if ($route)
        {
            <#
                The Route exists - remove it
                Use the parameters passed to Set-TargetResource to delete the appropriate route.
                Clear the Publish and PreferredLifetime parameters so they aren't passed to the
                Remove-NetRoute cmdlet.
            #>

            $null = $PSBoundParameters.Remove('Publish')
            $null = $PSBoundParameters.Remove('PreferredLifetime')

            Remove-NetRoute @PSBoundParameters `
                -Confirm:$false `
                -ErrorAction Stop

            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.RouteRemovedMessage) `
                        -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
                ) -join '' )
        } # if
    } # if
} # Set-TargetResource

<#
    .SYNOPSIS
        Tests the state of a Route on an interface.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface.

    .PARAMETER AddressFamily
        Specify the IP address family.

    .PARAMETER DestinationPrefix
        Specifies a destination prefix of an IP route.
        A destination prefix consists of an IP address prefix
        and a prefix length, separated by a slash (/).

    .PARAMETER NextHop
        Specifies the next hop for the IP route.

    .PARAMETER Ensure
        Specifies whether the route should exist.
        Defaults to 'Present'.

    .PARAMETER RouteMetric
        Specifies an integer route metric for an IP route.
        Defaults to 256.

    .PARAMETER Publish
        Specifies the publish setting of an IP route.
        Defaults to 'No'.

    .PARAMETER PreferredLifetime
        Specifies a preferred lifetime in seconds of an IP route.
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

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $DestinationPrefix,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $NextHop,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present',

        [Parameter()]
        [System.Uint16]
        $RouteMetric = 256,

        [Parameter()]
        [ValidateSet('No', 'Yes', 'Age')]
        [System.String]
        $Publish = 'No',

        [Parameter()]
        [System.Double]
        $PreferredLifetime
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestingRouteMessage) `
                -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
        ) -join '' )

    # Flag to signal whether settings are correct
    [System.Boolean] $desiredConfigurationMatch = $true

    # Remove any parameters that can't be splatted.
    $null = $PSBoundParameters.Remove('Ensure')

    # Check the parameters
    Assert-ResourceProperty @PSBoundParameters

    # Lookup the existing Route
    $route = Get-Route @PSBoundParameters

    if ($Ensure -eq 'Present')
    {
        # The route should exist
        if ($route)
        {
            # The route exists and does - but check the parameters
            if (($PSBoundParameters.ContainsKey('RouteMetric')) `
                    -and ($route.RouteMetric -ne $RouteMetric))
            {
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.RoutePropertyNeedsUpdateMessage) `
                            -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop, 'RouteMetric' `
                    ) -join '' )

                $desiredConfigurationMatch = $false
            }

            if (($PSBoundParameters.ContainsKey('Publish')) `
                    -and ($route.Publish -ne $Publish))
            {
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.RoutePropertyNeedsUpdateMessage) `
                            -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop, 'Publish' `
                    ) -join '' )

                $desiredConfigurationMatch = $false
            }

            if (($PSBoundParameters.ContainsKey('PreferredLifetime')) `
                    -and ($route.PreferredLifetime.TotalSeconds -ne $PreferredLifetime))
            {
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.RoutePropertyNeedsUpdateMessage) `
                            -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop, 'PreferredLifetime' `
                    ) -join '' )

                $desiredConfigurationMatch = $false
            }
        }
        else
        {
            # The route doesn't exist but should
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.RouteDoesNotExistButShouldMessage) `
                        -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
                ) -join '' )

            $desiredConfigurationMatch = $false
        }
    }
    else
    {
        # The route should not exist
        if ($route)
        {
            # The route exists but should not
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.RouteExistsButShouldNotMessage) `
                        -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
                ) -join '' )

            $desiredConfigurationMatch = $false
        }
        else
        {
            # The route does not exist and should not
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.RouteDoesNotExistAndShouldNotMessage) `
                        -f $AddressFamily, $InterfaceAlias, $DestinationPrefix, $NextHop `
                ) -join '' )
        }
    } # if

    return $desiredConfigurationMatch
} # Test-TargetResource

<#
    .SYNOPSIS
        This function looks up the route using the parameters and returns
        it. If the route is not found $null is returned.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface.

    .PARAMETER AddressFamily
        Specify the IP address family.

    .PARAMETER DestinationPrefix
        Specifies a destination prefix of an IP route.
        A destination prefix consists of an IP address prefix
        and a prefix length, separated by a slash (/).

    .PARAMETER NextHop
        Specifies the next hop for the IP route.

    .PARAMETER Ensure
        Specifies whether the route should exist.
        Defaults to 'Present'.

    .PARAMETER RouteMetric
        Specifies an integer route metric for an IP route.
        Defaults to 256.

    .PARAMETER Publish
        Specifies the publish setting of an IP route.
        Defaults to 'No'.

    .PARAMETER PreferredLifetime
        Specifies a preferred lifetime in seconds of an IP route.
#>
function Get-Route
{
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

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $DestinationPrefix,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $NextHop,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present',

        [Parameter()]
        [System.Uint16]
        $RouteMetric = 256,

        [Parameter()]
        [ValidateSet('No', 'Yes', 'Age')]
        [System.String]
        $Publish = 'No',

        [Parameter()]
        [System.Double]
        $PreferredLifetime
    )

    try
    {
        $route = Get-NetRoute `
            -InterfaceAlias $InterfaceAlias `
            -AddressFamily $AddressFamily `
            -DestinationPrefix $DestinationPrefix `
            -NextHop $NextHop `
            -ErrorAction Stop
    }
    catch [Microsoft.PowerShell.Cmdletization.Cim.CimJobException]
    {
        $route = $null
    }
    catch
    {
        throw $_
    }

    return $route
}

<#
    .SYNOPSIS
        This function validates the parameters passed. Called by Test-Resource.
        Will throw an error if any parameters are invalid.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface.

    .PARAMETER AddressFamily
        Specify the IP address family.

    .PARAMETER DestinationPrefix
        Specifies a destination prefix of an IP route.
        A destination prefix consists of an IP address prefix
        and a prefix length, separated by a slash (/).

    .PARAMETER NextHop
        Specifies the next hop for the IP route.

    .PARAMETER Ensure
        Specifies whether the route should exist.
        Defaults to 'Present'.

    .PARAMETER RouteMetric
        Specifies an integer route metric for an IP route.
        Defaults to 256.

    .PARAMETER Publish
        Specifies the publish setting of an IP route.
        Defaults to 'No'.

    .PARAMETER PreferredLifetime
        Specifies a preferred lifetime in seconds of an IP route.
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

        [Parameter(Mandatory = $true)]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $DestinationPrefix,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $NextHop,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present',

        [Parameter()]
        [System.Uint16]
        $RouteMetric = 256,

        [Parameter()]
        [ValidateSet('No', 'Yes', 'Age')]
        [System.String]
        $Publish = 'No',

        [Parameter()]
        [System.Double]
        $PreferredLifetime
    )

    # Validate the Adapter exists
    if (-not (Get-NetAdapter | Where-Object -Property Name -EQ $InterfaceAlias ))
    {
        New-InvalidArgumentException `
            -Message $($($script:localizedData.InterfaceNotAvailableError) -f $InterfaceAlias) `
            -ArgumentName 'InterfaceAlias'
    }

    # Validate the DestinationPrefix Parameter
    $components = $DestinationPrefix -split '/'
    $prefix = $components[0]

    Assert-IPAddress -Address $prefix -AddressFamily $AddressFamily

    # Validate the NextHop Parameter
    Assert-IPAddress -Address $NextHop -AddressFamily $AddressFamily
}

Export-ModuleMember -Function *-TargetResource
