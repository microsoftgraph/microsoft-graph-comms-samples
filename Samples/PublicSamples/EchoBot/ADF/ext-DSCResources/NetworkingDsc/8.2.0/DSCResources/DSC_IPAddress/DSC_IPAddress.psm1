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
        Returns the current state of an IP address assigned to an interface.

    .PARAMETER IPAddress
        The desired IP address.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the IP address should be set.

    .PARAMETER AddressFamily
        IP address family.

    .PARAMETER KeepExistingAddress
        Indicates whether or not existing IP addresses on an interface will be retained.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.String[]]
        $IPAddress,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily = 'IPv4',

        [Parameter()]
        [System.Boolean]
        $KeepExistingAddress = $false
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingIPAddressMessage)
        ) -join '')

    $getNetIPAddressParameters = @{
        InterfaceAlias = $InterfaceAlias
        AddressFamily  = $AddressFamily
    }

    $currentIPAddress = Get-NetIPAddress @getNetIPAddressParameters

    $currentIPAddressWithPrefix = $currentIPAddress |
        Foreach-Object {
            "$($_.IPAddress)/$($_.prefixLength)"
        }

    $returnValue = @{
        IPAddress           = @($currentIPAddressWithPrefix)
        AddressFamily       = $AddressFamily
        InterfaceAlias      = $InterfaceAlias
        KeepExistingAddress = $KeepExistingAddress
    }

    return $returnValue
}

<#
    .SYNOPSIS
        Sets an IP address on an interface.

    .PARAMETER IPAddress
        The desired IP address.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the IP address should be set.

    .PARAMETER AddressFamily
        IP address family.

    .PARAMETER KeepExistingAddress
        Indicates whether or not existing IP addresses on an interface will be retained.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.String[]]
        $IPAddress,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily = 'IPv4',

        [Parameter()]
        [System.Boolean]
        $KeepExistingAddress = $false
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.ApplyingIPAddressMessage)
        ) -join '')

    # Use $AddressFamily to select the IPv4 or IPv6 destination prefix
    $destinationPrefix = '0.0.0.0/0'

    if ($AddressFamily -eq 'IPv6')
    {
        $destinationPrefix = '::/0'
        $prefixLength = 64
    }

    # Get all the default routes - this has to be done in case the IP Address is being Removed
    $getNetRouteParameters = @{
        InterfaceAlias = $InterfaceAlias
        AddressFamily  = $AddressFamily
        ErrorAction    = 'Stop'
    }

    $defaultRoutes = @(Get-NetRoute @getNetRouteParameters).Where(
        {
            $_.DestinationPrefix -eq $destinationPrefix
        }
    )

    <#
        Remove any default routes on the specified interface -- it is important to do
        this *before* removing the IP address, particularly in the case where the IP
        address was auto-configured by DHCP
    #>
    if ($defaultRoutes)
    {
        foreach ($defaultRoute in $defaultRoutes)
        {
            $removeNetRouteParameters = @{
                DestinationPrefix = $defaultRoute.DestinationPrefix
                NextHop           = $defaultRoute.NextHop
                InterfaceIndex    = $defaultRoute.InterfaceIndex
                AddressFamily     = $defaultRoute.AddressFamily
                Confirm           = $false
                ErrorAction       = 'Stop'
            }
            Remove-NetRoute @removeNetRouteParameters
        }
    }

    # Get the current IP Address based on the parameters given.
    $getNetIPAddressParameters = @{
        InterfaceAlias = $InterfaceAlias
        AddressFamily  = $AddressFamily
        ErrorAction    = 'Stop'
    }

    $currentIPs = @(Get-NetIPAddress @getNetIPAddressParameters)

    # Remove any IP addresses on the specified interface
    if ($currentIPs)
    {
        foreach ($currentIP in $currentIPs)
        {
            $removeIP = $false

            if ($currentIP.IPAddress -notin ($IPAddress -replace '\/\S*', '') -and -not $KeepExistingAddress)
            {
                $removeIP = $true
            }
            elseif ($currentIP.IPAddress -in ($IPAddress -replace '\/\S*', ''))
            {
                $existingIP = $IPAddress | Where-Object {
                    $_ -match $currentIP.IPAddress
                }

                if ($existingIP -ne "$($currentIP.IPAddress)/$($currentIP.prefixLength)")
                {
                    $removeIP = $true
                }
            }

            if ($removeIP)
            {
                $removeNetIPAddressParameters = @{
                    IPAddress      = $currentIP.IPAddress
                    InterfaceIndex = $currentIP.InterfaceIndex
                    AddressFamily  = $currentIP.AddressFamily
                    prefixLength   = $currentIP.prefixLength
                    Confirm        = $false
                    ErrorAction    = 'Stop'
                }

                Remove-NetIPAddress @removeNetIPAddressParameters
            }
        }
    }

    $ipAddressObject = Get-IPAddressPrefix -IPAddress $IPAddress -AddressFamily $AddressFamily

    foreach ($singleIP in $ipAddressObject)
    {
        # Build parameter hash table
        $newNetIPAddressParameters = @{
            IPAddress      = $singleIP.IPAddress
            prefixLength   = $singleIP.prefixLength
            InterfaceAlias = $InterfaceAlias
        }

        try
        {
            # Apply the specified IP configuration
            New-NetIPAddress @newNetIPAddressParameters -ErrorAction Stop
        }
        catch [Microsoft.Management.Infrastructure.CimException]
        {
            $verifyNetIPAddressAdapterParam = @{
                IPAddress      = $singleIP.IPAddress
                prefixLength   = $singleIP.prefixLength
            }
            <#
                Setting New-NetIPaddress will throw [Microsoft.Management.Infrastructure.CimException] if
                the IP address is already set. Need to check to make sure the IP is set on correct interface
            #>
            $verifyNetIPAddressAdapter = Get-NetIPAddress @verifyNetIPAddressAdapterParam -ErrorAction SilentlyContinue

            if ($verifyNetIPAddressAdapter.InterfaceAlias -eq $InterfaceAlias)
            {
                # The IP Address is already set on the correct interface
                Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                        $($script:localizedData.IPAddressMatchMessage)
                    ) -join '' )
            }
            else
            {
                Write-Error -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.IPAddressDoesNotMatchInterfaceAliasMessage) -f $InterfaceAlias,$verifyNetIPAddressAdapter.InterfaceAlias
                ) -join '' )
            }
            continue
        }

        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.IPAddressSetStateMessage)
            ) -join '' )
    }
} # Set-TargetResource

<#
    .SYNOPSIS
        Tests the IP address on the interface.

    .PARAMETER IPAddress
        The desired IP address.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the IP address should be set.

    .PARAMETER AddressFamily
        IP address family.

    .PARAMETER KeepExistingAddress
        Indicates whether or not existing IP addresses on an interface will be retained.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.String[]]
        $IPAddress,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily = 'IPv4',

        [Parameter()]
        [System.Boolean]
        $KeepExistingAddress = $false
    )

    # Flag to signal whether settings are correct
    [System.Boolean] $desiredConfigurationMatch = $true

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckingIPAddressMessage)
        ) -join '')

    Assert-ResourceProperty @PSBoundParameters

    <#
        Get the current IP Address based on the parameters given.
        First make sure that adapter is available
    #>
    [System.Boolean] $adapterBindingReady = $false
    [System.DateTime] $startTime = Get-Date

    while (-not $adapterBindingReady -and (((Get-Date) - $startTime).TotalSeconds) -lt 30)
    {
        $getNetIPAddressParameters = @{
            InterfaceAlias = $InterfaceAlias
            AddressFamily  = $AddressFamily
            ErrorAction    = 'SilentlyContinue'
        }

        $currentIPs = @(Get-NetIPAddress @getNetIPAddressParameters)

        if ($currentIPs)
        {
            $adapterBindingReady = $true
        }
        else
        {
            Start-Sleep -Milliseconds 200
        }
    } # while

    $ipAddressObject = Get-IPAddressPrefix -IPAddress $IPAddress -AddressFamily $AddressFamily

    # Test if the IP Address passed is present
    foreach ($singleIP in $ipAddressObject)
    {
        $prefixLength = $singleIP.prefixLength

        if ($singleIP.IPAddress -notin $currentIPs.IPAddress)
        {
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.IPAddressDoesNotMatchMessage) -f $singleIP, $currentIPs.IPAddress
                ) -join '' )

            $desiredConfigurationMatch = $false
        }
        else
        {
            Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                    $($script:localizedData.IPAddressMatchMessage)
                ) -join '')

            # Filter the IP addresses for the IP address to check
            $filterIP = $currentIPs.Where(
                {
                    $_.IPAddress -eq $singleIP.IPAddress
                }
            )

            # Only test the Prefix Length if the IP address is present
            if (-not $filterIP.prefixLength.Equals([System.Byte] $prefixLength))
            {
                Write-Verbose -Message ( @(
                        "$($MyInvocation.MyCommand): "
                        $($script:localizedData.prefixLengthDoesNotMatchMessage) -f $prefixLength, $currentIPs.prefixLength
                    ) -join '' )

                $desiredConfigurationMatch = $false
            }
            else
            {
                Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                        $($script:localizedData.prefixLengthMatchMessage)
                    ) -join '' )
            }
        }
    }
    return $desiredConfigurationMatch
} # Test-TargetResource

<#
    .SYNOPSIS
        Check the IP Address details are valid and do not conflict with Address family.
        Also checks the prefix length and ensures the interface exists.
        If any problems are detected an exception will be thrown.

    .PARAMETER IPAddress
        The desired IP address.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the IP address should be set.

    .PARAMETER AddressFamily
        IP address family.

    .PARAMETER KeepExistingAddress
        Indicates whether or not existing IP addresses on an interface will be retained.
#>
function Assert-ResourceProperty
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]
        $IPAddress,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias,

        [Parameter()]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily = 'IPv4',

        [Parameter()]
        [System.Boolean]
        $KeepExistingAddress = $false
    )

    $prefixLengthArray = ($IPAddress -split '/')[1]

    if ($prefixLengthArray.Count -ne $IPAddress.Count)
    {
        # Return the prefix length of each IP address specified
        $prefixLengthArray = $IPAddress | Foreach-Object {
            if ($_ -match '\/\d{1,3}')
            {
                ($_ -split '/')[1]
            }
            else
            {
                if ($_.split('.')[0] -in (0..127))
                {
                    $prefixLength = 8
                }
                elseif ($_.split('.')[0] -in (128..191))
                {
                    $prefixLength = 16
                }
                elseif ($_.split('.')[0] -in (192..223))
                {
                    $prefixLength = 24
                }
                if ($AddressFamily -eq 'IPv6')
                {
                    $prefixLength = 64
                }
                $prefixLength
            }
        }
    }

    if (-not (Get-NetAdapter | Where-Object -Property Name -EQ $InterfaceAlias ))
    {
        New-InvalidArgumentException `
            -Message $($($script:localizedData.InterfaceNotAvailableError) -f $InterfaceAlias) `
            -ArgumentName 'InterfaceAlias'
    }

    foreach ($singleIPAddress in $IPAddress)
    {
        $singleIP = ($singleIPAddress -split '/')[0]

        Assert-IPAddress -Address $singleIP -AddressFamily $AddressFamily
    }

    foreach ($prefixLength in $prefixLengthArray)
    {
        $prefixLength = [uint32]::Parse($prefixLength)

        if ((
                ($AddressFamily -eq 'IPv4') `
                    -and (($prefixLength -lt [uint32]0) -or ($prefixLength -gt [uint32]32))
            ) -or (
                ($AddressFamily -eq 'IPv6') `
                    -and (($prefixLength -lt [uint32]0) -or ($prefixLength -gt [uint32]128))
            ))
        {
            New-InvalidArgumentException `
                -Message $($($script:localizedData.PrefixLengthError) -f $prefixLength, $AddressFamily) `
                -ArgumentName 'IPAddress'
        }
    }
} # Assert-ResourceProperty

Export-ModuleMember -function *-TargetResource
