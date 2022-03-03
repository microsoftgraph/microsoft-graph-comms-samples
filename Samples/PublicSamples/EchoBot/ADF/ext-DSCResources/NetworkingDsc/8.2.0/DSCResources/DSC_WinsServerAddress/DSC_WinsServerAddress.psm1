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
        Returns the current WINS Server Addresses for an interface.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the WINS server address is set.

    .PARAMETER Address
        The desired WINS Server address(es). Exclude to remove existing servers.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $InterfaceAlias
    )

    Assert-ResourceProperty @PSBoundParameters

    Write-Verbose -Message "$($MyInvocation.MyCommand): $($script:localizedData.GettingWinsServerAddressesMessage)"

    # Get the current WINS Server Addresses based on the parameters given.
    $currentAddress = [string[]]@(Get-WinsClientServerStaticAddress -InterfaceAlias $InterfaceAlias -ErrorAction Stop)

    $returnValue = @{
        InterfaceAlias = $InterfaceAlias
        Address        = $currentAddress
    }

    return $returnValue
}

<#
    .SYNOPSIS
        Sets the WINS Server Address(es) for an interface.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the WINS server address is set.

    .PARAMETER Address
        The desired WINS Server address(es). Exclude to remove existing servers.
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

        [Parameter()]
        [AllowEmptyCollection()]
        [System.String[]]
        $Address
    )

    Assert-ResourceProperty @PSBoundParameters

    Write-Verbose -Message "$($MyInvocation.MyCommand): $($script:localizedData.ApplyingWinsServerAddressesMessage)"

    Set-WinsClientServerStaticAddress -InterfaceAlias $InterfaceAlias -Address $Address -ErrorAction Stop

}

<#
    .SYNOPSIS
        Tests the current state of a WINS Server Address for an interface.

    .PARAMETER InterfaceAlias
        Alias of the network interface for which the WINS server address is set.

    .PARAMETER Address
        The desired WINS Server address(es). Exclude to remove existing servers.
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

        [Parameter()]
        [AllowEmptyCollection()]
        [System.String[]]
        $Address
    )

    Write-Verbose -Message "$($MyInvocation.MyCommand): $($script:localizedData.CheckingWinsServerAddressesMessage)"

    Assert-ResourceProperty @PSBoundParameters

    $currentState = Get-TargetResource -InterfaceAlias $InterfaceAlias
    $desiredState = $PSBoundParameters

    $result = Test-DscParameterState -CurrentValues $currentState -DesiredValues $desiredState

    if ($result)
    {
        Write-Verbose -Message "$($MyInvocation.MyCommand): $($script:localizedData.WinsServersSetCorrectlyMessage)"
    }
    else
    {
        $message = "$($MyInvocation.MyCommand): $($script:localizedData.WinsServersNotCorrectMessage -f
        ($currentState.Address -join ', '), ($desiredState.Address -join ', '))"
        Write-Verbose -Message $message
    }

    return $result
}

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
        [AllowEmptyCollection()]
        [System.String[]]
        $Address
    )

    if (-not (Get-NetAdapter | Where-Object Name -EQ $InterfaceAlias))
    {
        New-InvalidArgumentException `
            -Message ($script:localizedData.InterfaceNotAvailableError -f $InterfaceAlias) `
            -ArgumentName 'InterfaceAlias'
    }

    foreach ($ip in $Address)
    {
        if (-not ([System.Net.IPAddress]::TryParse($ip, [ref]0)))
        {
            New-InvalidArgumentException `
                -Message ($script:localizedData.AddressFormatError -f $ip)
                -ArgumentName 'Address'
        }
    }
}

Export-ModuleMember -Function *-TargetResource
