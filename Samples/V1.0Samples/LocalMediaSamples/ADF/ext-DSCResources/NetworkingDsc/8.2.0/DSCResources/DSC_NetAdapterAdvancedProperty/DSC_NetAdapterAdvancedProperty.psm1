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
        Gets the current value of an advanced property.

    .PARAMETER NetworkAdapterName
        Specifies the name of the network adapter to set the advanced property for.

    .PARAMETER RegistryKeyword
        Specifies the registry keyword that should be in desired state.

    .PARAMETER RegistryValue
        Specifies the value of the registry keyword.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $NetworkAdapterName,

        [Parameter(Mandatory = $true)]
        [System.String]
        $RegistryKeyword,

        [Parameter(Mandatory = $true)]
        [System.String]
        $RegistryValue
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $script:localizedData.CheckingNetAdapterMessage
        ) -join '')

    try
    {
        $netAdapterAdvancedProperty = Get-NetAdapterAdvancedProperty `
            -Name $networkAdapterName `
            -RegistryKeyword $RegistryKeyword `
            -ErrorAction Stop
    }
    catch
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.NetAdapterNotFoundMessage)
    }

    if ($netAdapterAdvancedProperty)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.NetAdapterTestingStateMessage -f $NetworkAdapterName, $RegistryKeyword)
            ) -join '')

        $result = @{
            NetworkAdapterName = $NetworkAdapterName
            RegistryKeyword    = $RegistryKeyword
            DisplayValue       = $netAdapterAdvancedProperty.DisplayValue
            RegistryValue      = $netAdapterAdvancedProperty.RegistryValue
        }

        return $result
    }
}

<#
    .SYNOPSIS
        Sets the current value of an advanced property.

    .PARAMETER NetworkAdapterName
        Specifies the name of the network adapter to set the advanced property for.

    .PARAMETER RegistryKeyword
        Specifies the registry keyword that should be in desired state.

    .PARAMETER RegistryValue
        Specifies the value of the registry keyword.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $NetworkAdapterName,

        [Parameter(Mandatory = $true)]
        [System.String]
        $RegistryKeyword,

        [Parameter(Mandatory = $true)]
        [System.String]
        $RegistryValue
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $script:localizedData.CheckingNetAdapterMessage
        ) -join '')

    try
    {
        $netAdapterAdvancedProperty = Get-NetAdapterAdvancedProperty `
            -Name $networkAdapterName `
            -RegistryKeyword $RegistryKeyword `
            -ErrorAction Stop
    }
    catch
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.NetAdapterNotFoundMessage)
    }

    if ($netAdapterAdvancedProperty)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.NetAdapterTestingStateMessage -f $NetworkAdapterName, $RegistryKeyword)
            ) -join '')

        if ($RegistryValue -ne $netAdapterAdvancedProperty.RegistryValue)
        {
            $netadapterRegistryValue = $netAdapterAdvancedProperty.RegistryValue
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.NetAdapterApplyingChangesMessage -f `
                            $NetworkAdapterName, $RegistryKeyword, "$netadapterRegistryValue", $RegistryValue )
                ) -join '')

            Set-NetAdapterAdvancedProperty `
                -RegistryValue $RegistryValue `
                -Name $networkAdapterName `
                -RegistryKeyword $RegistryKeyword
        }
    }
}

<#
    .SYNOPSIS
        Tests the current value of an advanced property.

    .PARAMETER NetworkAdapterName
        Specifies the name of the network adapter to set the advanced property for.

    .PARAMETER RegistryKeyword
        Specifies the registry keyword that should be in desired state.

    .PARAMETER RegistryValue
        Specifies the value of the registry keyword.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $NetworkAdapterName,

        [Parameter(Mandatory = $true)]
        [System.String]
        $RegistryKeyword,

        [Parameter(Mandatory = $true)]
        [System.String]
        $RegistryValue
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $script:localizedData.CheckingNetAdapterMessage
        ) -join '')

    try
    {
        $netAdapterAdvancedProperty = Get-NetAdapterAdvancedProperty `
            -Name $networkAdapterName `
            -RegistryKeyword $RegistryKeyword `
            -ErrorAction Stop
    }
    catch
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.NetAdapterNotFoundMessage)
    }

    if ($netAdapterAdvancedProperty)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $script:localizedData.NetAdapterTestingStateMessage -f `
                    $NetworkAdapterName, $RegistryKeyword
            ) -join '')

        if ($RegistryValue -eq $netAdapterAdvancedProperty.RegistryValue)
        {
            return $true
        }
        else
        {
            return $false
        }
    }
}
