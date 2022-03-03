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
        Gets the current state of a network adapter.

    .PARAMETER Name
        Specifies the name of the network adapter.

    .PARAMETER State
        Specifies the desired state for the network adapter.
        Not used in Get-TargetResource.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Enabled', 'Disabled')]
        [System.String]
        $State
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $script:localizedData.CheckingNetAdapterMessage
        ) -join '')

    try
    {
        $netAdapter = Get-NetAdapter -Name $Name -ErrorAction Stop
    }
    catch
    {
        Write-Warning -Message ( @(
            "$($MyInvocation.MyCommand): "
            $script:localizedData.NetAdapterNotFoundMessage -f $Name
        ) -join '')
    }

    if ($netAdapter)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.NetAdapterTestingStateMessage -f $Name)
            ) -join '')

        <#
            Using NET_IF_ADMIN_STATUS as documented here:
            https://docs.microsoft.com/en-us/windows/desktop/api/ifdef/ne-ifdef-net_if_admin_status
        #>

        $enabled  = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetAdapter.NET_IF_ADMIN_STATUS]::Up
        $disabled = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.NetAdapter.NET_IF_ADMIN_STATUS]::Down

        $result = @{
            Name  = $Name
            State = switch ($netAdapter.AdminStatus)
            {
                $enabled  { 'Enabled' }
                $disabled { 'Disabled' }
                default   { 'Unsupported' }
            }
        }

        return $result
    }
}

<#
    .SYNOPSIS
        Sets the NetAdapterState resource state.

    .PARAMETER Name
        Specifies the name of the network adapter.

    .PARAMETER State
        Specifies the desired state for the network adapter.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Enabled', 'Disabled')]
        [System.String]
        $State
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $script:localizedData.CheckingNetAdapterMessage
        ) -join '')

    try
    {
        $netAdapter = Get-NetAdapter -Name $Name -ErrorAction Stop
    }
    catch
    {
        Write-Error -Message ( @(
            "$($MyInvocation.MyCommand): "
            $script:localizedData.NetAdapterNotFoundMessage -f $Name
        ) -join '')
    }

    if ($netAdapter)
    {
        try
        {
            if ($State -eq 'Disabled')
            {
                Disable-NetAdapter -Name $Name -Confirm:$false -ErrorAction Stop
            }
            else
            {
                Enable-NetAdapter -Name $Name -ErrorAction Stop
            }
        }
        catch
        {
            Write-Error -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.NetAdapterSetFailedMessage -f $Name, $State, $_)
            ) -join '')
        }
    }
}

<#
    .SYNOPSIS
        Tests if the NetAdapterState resource state is desired state.

    .PARAMETER Name
        Specifies the name of the network adapter.

    .PARAMETER State
        Specifies the state of the network adapter.
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

        [Parameter(Mandatory = $true)]
        [ValidateSet('Enabled', 'Disabled')]
        [System.String]
        $State
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.NetAdapterTestingStateMessage -f $Name)
        ) -join '')

    $currentState = Get-TargetResource @PSBoundParameters

    if ($currentState)
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.NetAdapterStateMessage -f $Name, $currentState.State)
            ) -join '')

        return $currentState.State -eq $State
    }

    return $false
}
