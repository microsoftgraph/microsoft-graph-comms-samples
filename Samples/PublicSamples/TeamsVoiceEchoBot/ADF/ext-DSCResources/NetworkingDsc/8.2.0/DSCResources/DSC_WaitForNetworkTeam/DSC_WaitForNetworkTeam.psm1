# Suppressed as per PSSA Rule Severity guidelines for unit/integration tests:
# https://github.com/PowerShell/DscResources/blob/master/PSSARuleSeverities.md
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '')]
param ()

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
        Returns the current state of the wait for network team resource.

    .PARAMETER Name
        Specifies the name of the network team to wait for.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingWaitForNetworkTeamStatusMessage -f $Name)
        ) -join '' )


    $null = Get-NetLbfoTeamStatus -Name $Name

    $returnValue = @{
        Name             = $Name
        RetryIntervalSec = $null
        RetryCount       = $null
    }

    return $returnValue
} # function Get-TargetResource

<#
    .SYNOPSIS
        Sets the current state of the wait for network team resource.

    .PARAMETER Name
        Specifies the name of the network team to wait for.

    .PARAMETER RetryIntervalSec
        Specifies the number of seconds to wait for the network team to become available.

    .PARAMETER RetryCount
        The number of times to loop the retry interval while waiting for the network team.
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
        [System.UInt32]
        $RetryIntervalSec = 10,

        [Parameter()]
        [System.UInt32]
        $RetryCount = 60
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.SettingWaitForNetworkTeamStatusMessage -f $Name)
        ) -join '' )

    $lbfoTeamUp = $false

    for ($count = 0; $count -lt $RetryCount; $count++)
    {
        $lbfoTeamStatus = Get-NetLbfoTeamStatus -Name $Name

        if ($lbfoTeamStatus -eq 'Up')
        {
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.NetworkTeamUpMessage -f $Name)
                ) -join '' )

            $lbfoTeamUp = $true
            break
        }
        else
        {
            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.NetworkTeamNotUpRetryingMessage -f $Name, $RetryIntervalSec)
                ) -join '' )

            Start-Sleep -Seconds $RetryIntervalSec
        } # if
    } # for

    if ($lbfoTeamUp -eq $false)
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.NetworkTeamNotUpAfterError -f $Name, $RetryCount)
    } # if
} # function Set-TargetResource

<#
    .SYNOPSIS
        Tests the current state of the wait for network team resource.

    .PARAMETER Name
        Specifies the name of the network team to wait for.

    .PARAMETER RetryIntervalSec
        Specifies the number of seconds to wait for the network team to become available.

    .PARAMETER RetryCount
        The number of times to loop the retry interval while waiting for the network team.
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
        [System.UInt32]
        $RetryIntervalSec = 10,

        [Parameter()]
        [System.UInt32]
        $RetryCount = 60
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestingWaitForNetworkTeamStatusMessage -f $Name)
        ) -join '' )

    $lbfoTeamStatus = Get-NetLbfoTeamStatus -Name $Name

    if ($lbfoTeamStatus -eq 'Up')
    {
        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.NetworkTeamUpMessage -f $Name)
            ) -join '' )

        return $true
    }

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.NetworkTeamNotUpMessage -f $Name)
        ) -join '' )

    return $false
} # function Test-TargetResource

<#
    .SYNOPSIS
        Returns the current status of a network team.
        'Up' indicates that the network team is acive.
        'Degraded' indicates that the network team is not yet
        available.
        If the network team does not exist an exception will be
        thrown.

    .PARAMETER Name
        Specifies the name of the network team to get the status of.
#>

function Get-NetLbfoTeamStatus
{
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name
    )

    try
    {
        $lbfoTeam = Get-NetLbfoTeam -Name $Name

        Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.NetworkTeamFoundMessage -f $Name)
        ) -join '' )

    }
    catch [Microsoft.PowerShell.Cmdletization.Cim.CimJobException]
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.NetworkTeamNotFoundMessage -f $Name)
    }

    return $lbfoTeam.Status
}

Export-ModuleMember -Function *-TargetResource
