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
        Gets the state of the network adapter RDMA.

    .PARAMETER Name
        Specifies the name of network adapter for which RDMA needs
        to be configured.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name
    )

    $configuration = @{
        Name = $Name
    }

    try
    {
        Write-Verbose -Message ($script:localizedData.GetNetAdapterRdmaMessage -f $Name)

        $netAdapterRdma = Get-NetAdapterRdma -Name $Name -ErrorAction Stop
    }
    catch
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.NetAdapterNotFoundError -f $Name)
    }

    if ($netAdapterRdma)
    {
        Write-Verbose -Message ($script:localizedData.CheckNetAdapterRdmaMessage -f $Name)

        $configuration.Add('Enabled', $netAdapterRdma.Enabled)
    }

    return $configuration
}

<#
    .SYNOPSIS
        Sets the state of the network adapter RDMA.

    .PARAMETER Name
        Specifies the name of network adapter for which RDMA needs
        to be configured.

    .PARAMETER Enabled
        Specifies if the RDMA configuration should be enabled or disabled.
        Defaults to $true.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter()]
        [System.Boolean]
        $Enabled = $true
    )

    try
    {
        Write-Verbose -Message ($script:localizedData.GetNetAdapterRdmaMessage -f $Name)

        $netAdapterRdma = Get-NetAdapterRdma -Name $Name -ErrorAction Stop
    }
    catch
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.NetAdapterNotFoundError -f $Name)
    }

    if ($netAdapterRdma)
    {
        Write-Verbose -Message ($script:localizedData.CheckNetAdapterRdmaMessage -f $Name)

        if ($netAdapterRdma.Enabled -ne $Enabled)
        {
            Write-Verbose -Message ($script:localizedData.SetNetAdapterRdmaMessage -f $Name, $Enabled)

            Set-NetAdapterRdma -Name $Name -Enabled $Enabled
        }
    }
}

<#
    .SYNOPSIS
        Tests the state of the network adapter RDMA.

    .PARAMETER Name
        Specifies the name of network adapter for which RDMA needs
        to be configured.

    .PARAMETER Enabled
        Specifies if the RDMA configuration should be enabled or disabled.
        Defaults to $true.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter()]
        [System.Boolean]
        $Enabled = $true
    )

    try
    {
        Write-Verbose -Message ($script:localizedData.GetNetAdapterRdmaMessage -f $Name)

        $netAdapterRdma = Get-NetAdapterRdma -Name $Name -ErrorAction Stop
    }
    catch
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.NetAdapterNotFoundError -f $Name)
    }

    if ($netAdapterRdma)
    {
        Write-Verbose -Message ($script:localizedData.CheckNetAdapterRdmaMessage -f $Name)

        if ($netAdapterRdma.Enabled -ne $Enabled)
        {
            Write-Verbose -Message ($script:localizedData.NetAdapterRdmaDifferentMessage -f $Name)

            return $false
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.NetAdapterRdmaMatchesMessage -f $Name)

            return $true
        }
    }
}
