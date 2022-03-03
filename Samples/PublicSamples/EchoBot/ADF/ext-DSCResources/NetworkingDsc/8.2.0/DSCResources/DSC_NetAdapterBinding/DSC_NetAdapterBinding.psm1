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
        Returns the current state of an Adapter Binding on an interface.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface. Supports the use of '*'.

    .PARAMETER ComponentId
        Specifies the underlying name of the transport or filter in the following
        form - ms_xxxx, such as ms_tcpip.

    .PARAMETER State
        Specifies if the component ID for the Interface should be Enabled or Disabled.
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
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ComponentId,

        [Parameter()]
        [ValidateSet('Enabled', 'Disabled')]
        [System.String]
        $State = 'Enabled'
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingNetAdapterBindingMessage -f `
                    $InterfaceAlias, $ComponentId)
        ) -join '')

    $currentNetAdapterBinding = Get-Binding @PSBoundParameters

    $adapterState = $currentNetAdapterBinding.Enabled |
        Sort-Object -Unique

    if ( $adapterState.Count -eq 2)
    {
        $currentEnabled = 'Mixed'
    }
    elseif ( $adapterState -eq $true )
    {
        $currentEnabled = 'Enabled'
    }
    else
    {
        $currentEnabled = 'Disabled'
    }

    $returnValue = @{
        InterfaceAlias = $InterfaceAlias
        ComponentId    = $ComponentId
        State          = $State
        CurrentState   = $currentEnabled
    }

    return $returnValue
} # Get-TargetResource

<#
    .SYNOPSIS
        Sets the Adapter Binding on a specific interface.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface. Supports the use of '*'.

    .PARAMETER ComponentId
        Specifies the underlying name of the transport or filter in the following
        form - ms_xxxx, such as ms_tcpip.

    .PARAMETER State
        Specifies if the component ID for the Interface should be Enabled or Disabled.
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
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ComponentId,

        [Parameter()]
        [ValidateSet('Enabled', 'Disabled')]
        [System.String]
        $State = 'Enabled'
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.ApplyingNetAdapterBindingMessage -f `
                    $InterfaceAlias, $ComponentId)
        ) -join '')

    $null = Get-Binding @PSBoundParameters

    # Remove the State so we can splat
    $null = $PSBoundParameters.Remove('State')

    if ($State -eq 'Enabled')
    {
        Enable-NetAdapterBinding @PSBoundParameters

        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.NetAdapterBindingEnabledMessage -f `
                        $InterfaceAlias, $ComponentId)
            ) -join '' )
    }
    else
    {
        Disable-NetAdapterBinding @PSBoundParameters

        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.NetAdapterBindingDisabledMessage -f `
                        $InterfaceAlias, $ComponentId)
            ) -join '' )
    } # if
} # Set-TargetResource

<#
    .SYNOPSIS
        Tests the current state of an Adapter Binding on an interface.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface. Supports the use of '*'.

    .PARAMETER ComponentId
        Specifies the underlying name of the transport or filter in the following
        form - ms_xxxx, such as ms_tcpip.

    .PARAMETER State
        Specifies if the component ID for the Interface should be Enabled or Disabled.
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
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ComponentId,

        [Parameter()]
        [ValidateSet('Enabled', 'Disabled')]
        [System.String]
        $State = 'Enabled'
    )

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckingNetAdapterBindingMessage -f `
                    $InterfaceAlias, $ComponentId)
        ) -join '')

    $currentNetAdapterBinding = Get-Binding @PSBoundParameters

    $adapterState = $currentNetAdapterBinding.Enabled |
        Sort-Object -Unique

    if ( $adapterState.Count -eq 2)
    {
        $currentEnabled = 'Mixed'
    }
    elseif ( $adapterState -eq $true )
    {
        $currentEnabled = 'Enabled'
    }
    else
    {
        $currentEnabled = 'Disabled'
    }

    # Test if the binding is in the correct state
    if ($currentEnabled -ne $State)
    {
        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.NetAdapterBindingDoesNotMatchMessage -f `
                        $InterfaceAlias, $ComponentId, $State, $currentEnabled)
            ) -join '' )

        return $false
    }
    else
    {
        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.NetAdapterBindingMatchMessage -f `
                        $InterfaceAlias, $ComponentId)
            ) -join '' )

        return $true
    } # if
} # Test-TargetResource

<#
    .SYNOPSIS
        Ensures the interface and component Id exists and returns the Net Adapter binding object.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface. Supports the use of '*'.

    .PARAMETER ComponentId
        Specifies the underlying name of the transport or filter in the following
        form - ms_xxxx, such as ms_tcpip.

    .PARAMETER State
        Specifies if the component ID for the Interface should be Enabled or Disabled.
#>
function Get-Binding
{
    [CmdletBinding()]
    [OutputType([Microsoft.Management.Infrastructure.CimInstance])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ComponentId,

        [Parameter()]
        [ValidateSet('Enabled', 'Disabled')]
        [System.String]
        $State = 'Enabled'
    )

    if (-not (Get-NetAdapter -Name $InterfaceAlias -ErrorAction SilentlyContinue))
    {
        New-InvalidArgumentException `
            -Message ($script:localizedData.InterfaceNotAvailableError -f $InterfaceAlias) `
            -ArgumentName 'InterfaceAlias'
    } # if

    $binding = Get-NetAdapterBinding `
        -InterfaceAlias $InterfaceAlias `
        -ComponentId $ComponentId `
        -ErrorAction Stop

    return $binding
} # Get-Binding

Export-ModuleMember -function *-TargetResource
