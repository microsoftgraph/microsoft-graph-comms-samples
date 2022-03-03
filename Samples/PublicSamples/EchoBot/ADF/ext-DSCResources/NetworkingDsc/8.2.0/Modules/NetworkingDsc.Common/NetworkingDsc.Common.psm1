$modulePath = Join-Path -Path (Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent) -ChildPath 'Modules'

Import-Module -Name (Join-Path -Path $modulePath -ChildPath 'DscResource.Common')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -DefaultUICulture 'en-US'

<#
    .SYNOPSIS
        Converts any IP Addresses containing CIDR notation filters in an array to use Subnet Mask
        notation.

    .PARAMETER Address
        The array of addresses to that need to be converted.
#>
function Convert-CIDRToSubhetMask
{
    [CmdletBinding()]
    [OutputType([ Microsoft.Management.Infrastructure.CimInstance])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]
        $Address
    )

    $results = @()

    foreach ($entry in $Address)
    {
        if (-not $entry.Contains(':') -and -not $entry.Contains('-'))
        {
            $entrySplit = $entry -split '/'

            if (-not [String]::IsNullOrEmpty($entrySplit[1]))
            {
                # There was a / so this contains a Subnet Mask or CIDR
                $prefix = $entrySplit[0]
                $postfix = $entrySplit[1]

                if ($postfix -match '^[0-9]*$')
                {
                    # The postfix contains CIDR notation so convert this to Subnet Mask
                    $cidr = [System.Int32] $postfix
                    $subnetMaskInt64 = ([convert]::ToInt64(('1' * $cidr + '0' * (32 - $cidr)), 2))
                    $subnetMask = @(
                        ([math]::Truncate($subnetMaskInt64 / 16777216))
                        ([math]::Truncate(($subnetMaskInt64 % 16777216) / 65536))
                        ([math]::Truncate(($subnetMaskInt64 % 65536) / 256))
                        ([math]::Truncate($subnetMaskInt64 % 256))
                    )
                }
                else
                {
                    $subnetMask = $postfix -split '\.'
                }

                <#
                        Apply the Subnet Mast to the IP Address so that we end up with a correctly
                        masked IP Address that will match what the Firewall rule returns.
                #>
                $maskedIp = $prefix -split '\.'

                for ([System.Int32] $Octet = 0; $octet -lt 4; $octet++)
                {
                    $maskedIp[$Octet] = $maskedIp[$octet] -band $SubnetMask[$octet]
                }

                $entry = '{0}/{1}' -f ($maskedIp -join '.'), ($subnetMask -join '.')
            }
        }

        $results += $entry
    }

    return $results
} # Convert-CIDRToSubhetMask

<#
    .SYNOPSIS
        This function will find a network adapter based on the provided
        search parameters.

    .PARAMETER Name
        This is the name of network adapter to find.

    .PARAMETER PhysicalMediaType
        This is the media type of the network adapter to find.

    .PARAMETER Status
        This is the status of the network adapter to find.

    .PARAMETER MacAddress
        This is the MAC address of the network adapter to find.

    .PARAMETER InterfaceDescription
        This is the interface description of the network adapter to find.

    .PARAMETER InterfaceIndex
        This is the interface index of the network adapter to find.

    .PARAMETER InterfaceGuid
        This is the interface GUID of the network adapter to find.

    .PARAMETER DriverDescription
        This is the driver description of the network adapter.

    .PARAMETER InterfaceNumber
        This is the interface number of the network adapter if more than one
        are returned by the parameters.

    .PARAMETER IgnoreMultipleMatchingAdapters
        This switch will suppress an error occurring if more than one matching
        adapter matches the parameters passed.
#>
function Find-NetworkAdapter
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter()]
        [System.String]
        $Name,

        [Parameter()]
        [System.String]
        $PhysicalMediaType,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [ValidateSet('Up', 'Disconnected', 'Disabled')]
        [System.String]
        $Status = 'Up',

        [Parameter()]
        [System.String]
        $MacAddress,

        [Parameter()]
        [System.String]
        $InterfaceDescription,

        [Parameter()]
        [System.UInt32]
        $InterfaceIndex,

        [Parameter()]
        [System.String]
        $InterfaceGuid,

        [Parameter()]
        [System.String]
        $DriverDescription,

        [Parameter()]
        [System.UInt32]
        $InterfaceNumber = 1,

        [Parameter()]
        [System.Boolean]
        $IgnoreMultipleMatchingAdapters = $false
    )

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.FindingNetAdapterMessage)
        ) -join '')

    $adapterFilters = @()

    if ($PSBoundParameters.ContainsKey('Name'))
    {
        $adapterFilters += @('($_.Name -eq $Name)')
    } # if

    if ($PSBoundParameters.ContainsKey('PhysicalMediaType'))
    {
        $adapterFilters += @('($_.PhysicalMediaType -eq $PhysicalMediaType)')
    } # if

    if ($PSBoundParameters.ContainsKey('Status'))
    {
        $adapterFilters += @('($_.Status -eq $Status)')
    } # if

    if ($PSBoundParameters.ContainsKey('MacAddress'))
    {
        $adapterFilters += @('($_.MacAddress -eq $MacAddress)')
    } # if

    if ($PSBoundParameters.ContainsKey('InterfaceDescription'))
    {
        $adapterFilters += @('($_.InterfaceDescription -eq $InterfaceDescription)')
    } # if

    if ($PSBoundParameters.ContainsKey('InterfaceIndex'))
    {
        $adapterFilters += @('($_.InterfaceIndex -eq $InterfaceIndex)')
    } # if

    if ($PSBoundParameters.ContainsKey('InterfaceGuid'))
    {
        $adapterFilters += @('($_.InterfaceGuid -eq $InterfaceGuid)')
    } # if

    if ($PSBoundParameters.ContainsKey('DriverDescription'))
    {
        $adapterFilters += @('($_.DriverDescription -eq $DriverDescription)')
    } # if

    if ($adapterFilters.Count -eq 0)
    {
        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.AllNetAdaptersFoundMessage)
            ) -join '')

        $matchingAdapters = @(Get-NetAdapter)
    }
    else
    {
        # Join all the filters together
        $adapterFilterScript = '(' + ($adapterFilters -join ' -and ') + ')'
        $matchingAdapters = @(Get-NetAdapter |
            Where-Object -FilterScript ([ScriptBlock]::Create($adapterFilterScript)))
    }

    # Were any adapters found matching the criteria?
    if ($matchingAdapters.Count -eq 0)
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.NetAdapterNotFoundError)

        # Return a null so that ErrorAction SilentlyContinue works correctly
        return $null
    }
    else
    {
        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.NetAdapterFoundMessage -f $matchingAdapters.Count)
            ) -join '')

        if ($matchingAdapters.Count -gt 1)
        {
            if ($IgnoreMultipleMatchingAdapters)
            {
                # Was the number of matching adapters found matching the adapter number?
                if (($InterfaceNumber -gt 1) -and ($InterfaceNumber -gt $matchingAdapters.Count))
                {
                    New-InvalidOperationException `
                        -Message ($script:localizedData.InvalidNetAdapterNumberError `
                            -f $matchingAdapters.Count, $InterfaceNumber)

                    # Return a null so that ErrorAction SilentlyContinue works correctly
                    return $null
                } # if
            }
            else
            {
                New-InvalidOperationException `
                    -Message ($script:localizedData.MultipleMatchingNetAdapterFound `
                        -f $matchingAdapters.Count)

                # Return a null so that ErrorAction SilentlyContinue works correctly
                return $null
            } # if
        } # if
    } # if

    # Identify the exact adapter from the adapters that match
    $exactAdapter = $matchingAdapters[$InterfaceNumber - 1]

    $returnValue = [PSCustomObject] @{
        Name                 = $exactAdapter.Name
        PhysicalMediaType    = $exactAdapter.PhysicalMediaType
        Status               = $exactAdapter.Status
        MacAddress           = $exactAdapter.MacAddress
        InterfaceDescription = $exactAdapter.InterfaceDescription
        InterfaceIndex       = $exactAdapter.InterfaceIndex
        InterfaceGuid        = $exactAdapter.InterfaceGuid
        MatchingAdapterCount = $matchingAdapters.Count
    }

    return $returnValue
} # Find-NetworkAdapter

<#
    .SYNOPSIS
        Returns the DNS Client Server static address that are assigned to a network
        adapter. This is required because Get-DnsClientServerAddress always returns
        the currently assigned server addresses whether regardless if they were
        assigned as static or by DHCP.

        The only way that could be found to do this is to query the registry.

    .PARAMETER InterfaceAlias
        Alias of the network interface to get the static DNS Server addresses from.

    .PARAMETER AddressFamily
        IP address family.
#>
function Get-DnsClientServerStaticAddress
{
    [CmdletBinding()]
    [OutputType([System.String[]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily
    )

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingDNSServerStaticAddressMessage) -f $AddressFamily, $InterfaceAlias
        ) -join '')

    # Look up the interface Guid
    $adapter = Get-NetAdapter `
        -InterfaceAlias $InterfaceAlias `
        -ErrorAction SilentlyContinue

    if (-not $adapter)
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.InterfaceAliasNotFoundError `
                -f $InterfaceAlias)

        # Return null to support ErrorAction Silently Continue
        return $null
    } # if

    $interfaceGuid = $adapter.InterfaceGuid.ToLower()

    if ($AddressFamily -eq 'IPv4')
    {
        $interfaceRegKeyPath = "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\$interfaceGuid\"
    }
    else
    {
        $interfaceRegKeyPath = "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\$interfaceGuid\"
    } # if

    $interfaceInformation = Get-ItemProperty `
        -Path $interfaceRegKeyPath `
        -ErrorAction SilentlyContinue
    $nameServerAddressString = $interfaceInformation.NameServer

    # Are any statically assigned addresses for this adapter?
    if ([System.String]::IsNullOrWhiteSpace($nameServerAddressString))
    {
        # Static DNS Server addresses not found so return empty array
        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.DNSServerStaticAddressNotSetMessage) -f $AddressFamily, $InterfaceAlias
            ) -join '')

        return $null
    }
    else
    {
        # Static DNS Server addresses found so split them into an array using comma
        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.DNSServerStaticAddressFoundMessage) -f $AddressFamily, $InterfaceAlias, $nameServerAddressString
            ) -join '')

        return @($nameServerAddressString -split ',')
    } # if
} # Get-DnsClientServerStaticAddress

<#
    .SYNOPSIS
        Returns the WINS Client Server static address that are assigned to a network
        adapter. The CIM class Win32_NetworkAdapterConfiguration unfortunately only supports
        the primary and secondary WINS server. The registry gives more flexibility.

    .PARAMETER InterfaceAlias
        Alias of the network interface to get the static WINS Server addresses from.
#>
function Get-WinsClientServerStaticAddress
{
    [CmdletBinding()]
    [OutputType([System.String[]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias
    )

    Write-Verbose -Message ("$($MyInvocation.MyCommand): $($script:localizedData.GettingWinsServerStaticAddressMessage -f $InterfaceAlias)")

    # Look up the interface Guid
    $adapter = Get-NetAdapter -InterfaceAlias $InterfaceAlias -ErrorAction SilentlyContinue

    if (-not $adapter)
    {
        New-InvalidOperationException -Message ($script:localizedData.InterfaceAliasNotFoundError -f $InterfaceAlias)

        # Return null to support ErrorAction Silently Continue
        return $null
    }

    $interfaceGuid = $adapter.InterfaceGuid.ToLower()

    $interfaceRegKeyPath = "HKLM:\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces\Tcpip_$interfaceGuid\"

    $interfaceInformation = Get-ItemProperty -Path $interfaceRegKeyPath -ErrorAction SilentlyContinue
    $nameServerAddressString = $interfaceInformation.NameServerList

    # Are any statically assigned addresses for this adapter?
    if (-not $nameServerAddressString)
    {
        # Static DNS Server addresses not found so return empty array
        Write-Verbose -Message ("$($MyInvocation.MyCommand): $($script:localizedData.WinsServerStaticAddressNotSetMessage -f $InterfaceAlias)")
        return $null
    }
    else
    {
        # Static DNS Server addresses found so split them into an array using comma
        Write-Verbose -Message ("$($MyInvocation.MyCommand): $($script:localizedData.WinsServerStaticAddressFoundMessage -f
        $InterfaceAlias, ($nameServerAddressString -join ','))")

        return $nameServerAddressString
    }
} # Get-WinsClientServerStaticAddress

<#
    .SYNOPSIS
        Sets the WINS Client Server static address on a network adapter. The CIM class
        Win32_NetworkAdapterConfiguration unfortunately only supports the primary and
        secondary WINS server. The registry gives more flexibility.

    .PARAMETER InterfaceAlias
        Alias of the network interface to set the static WINS Server addresses on.
#>
function Set-WinsClientServerStaticAddress
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.String[]]
        $Address
    )

    Write-Verbose -Message ("$($MyInvocation.MyCommand): $($script:localizedData.SettingWinsServerStaticAddressMessage -f $InterfaceAlias, ($Address -join ', '))")

    # Look up the interface Guid
    $adapter = Get-NetAdapter -InterfaceAlias $InterfaceAlias -ErrorAction SilentlyContinue

    if (-not $adapter)
    {
        New-InvalidOperationException -Message ($script:localizedData.InterfaceAliasNotFoundError -f $InterfaceAlias)
    }

    $interfaceGuid = $adapter.InterfaceGuid.ToLower()

    $interfaceRegKeyPath = "HKLM:\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces\Tcpip_$interfaceGuid\"

    Set-ItemProperty -Path $interfaceRegKeyPath -Name NameServerList -Value $Address

} # Set-WinsClientServerStaticAddress

<#
    .SYNOPSIS
        Gets the IP Address prefix from a provided IP Address in CIDR notation.

    .PARAMETER IPAddress
        IP Address to get prefix for, can be in CIDR notation.

    .PARAMETER AddressFamily
        Address family for provided IP Address, defaults to IPv4.

#>
function Get-IPAddressPrefix
{
    [cmdletbinding()]
    param
    (
        [Parameter(Mandatory = $true,
            ValueFromPipeline)]
        [System.String[]]
        $IPAddress,

        [Parameter()]
        [ValidateSet('IPv4', 'IPv6')]
        [System.String]
        $AddressFamily = 'IPv4'
    )

    process
    {
        foreach ($singleIP in $IPAddress)
        {
            $prefixLength = ($singleIP -split '/')[1]

            if (-not ($prefixLength) -and $AddressFamily -eq 'IPv4')
            {
                if ($singleIP.split('.')[0] -in (0..127))
                {
                    $prefixLength = 8
                }
                elseif ($singleIP.split('.')[0] -in (128..191))
                {
                    $prefixLength = 16
                }
                elseif ($singleIP.split('.')[0] -in (192..223))
                {
                    $prefixLength = 24
                }
            }
            elseif (-not ($prefixLength) -and $AddressFamily -eq 'IPv6')
            {
                $prefixLength = 64
            }

            [PSCustomObject]@{
                IPAddress    = $singleIP.split('/')[0]
                prefixLength = $prefixLength
            }
        }
    }
}

<#
.SYNOPSIS
    Returns a filter string for the net adapter CIM instances. Wildcards supported.

.PARAMETER InterfaceAlias
    Specifies the alias of a network interface. Supports the use of '*' or '%'.
#>
function Format-Win32NetworkAdapterFilterByNetConnectionId
{
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $InterfaceAlias
    )

    if ($InterfaceAlias.Contains('*'))
    {
        $InterfaceAlias = $InterfaceAlias.Replace('*','%')
    }

    if ($InterfaceAlias.Contains('%'))
    {
        $operator = ' LIKE '
    }
    else
    {
        $operator = '='
    }

    $returnNetAdapaterFilter = 'NetConnectionID{0}"{1}"' -f $operator, $InterfaceAlias

    return $returnNetAdapaterFilter
}

Export-ModuleMember -Function @(
    'Convert-CIDRToSubhetMask'
    'Find-NetworkAdapter'
    'Get-DnsClientServerStaticAddress'
    'Get-WinsClientServerStaticAddress'
    'Set-WinsClientServerStaticAddress'
    'Get-IPAddressPrefix'
    'Format-Win32NetworkAdapterFilterByNetConnectionId'
)
