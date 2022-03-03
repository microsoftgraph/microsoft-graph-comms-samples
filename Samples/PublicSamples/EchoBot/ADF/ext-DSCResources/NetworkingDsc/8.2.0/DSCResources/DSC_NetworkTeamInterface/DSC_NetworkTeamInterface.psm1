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
        Returns the current state of a network team interface in a Network Team.

    .PARAMETER Name
        Specifies the name of the network team interface to create.

    .PARAMETER TeamName
        Specifies the name of the network team on which this particular interface should exist.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter(Mandatory = $true)]
        [System.String]
        $TeamName
    )

    $configuration = @{
        Name     = $Name
        TeamName = $TeamName
    }

    Write-Verbose -Message ($script:localizedData.GetTeamNicInfo -f $Name)

    $getNetLbfoTeamNicParameters = @{
        Name        = $Name
        Team        = $TeamName
        ErrorAction = 'SilentlyContinue'
    }
    $teamNic = Get-NetLbfoTeamNic @getNetLbfoTeamNicParameters

    if ($teamNic)
    {
        Write-Verbose -Message ($script:localizedData.FoundTeamNic -f $Name)

        $configuration.Add('VlanId', $teamNic.VlanId)
        $configuration.Add('Ensure', 'Present')
    }
    else
    {
        Write-Verbose -Message ($script:localizedData.TeamNicNotFound -f $Name)

        $configuration.Add('Ensure', 'Absent')
    }

    return $configuration
}

<#
    .SYNOPSIS
        Adds, updates or removes a network team interface from a Network Team.

    .PARAMETER Name
        Specifies the name of the network team interface to create.

    .PARAMETER TeamName
        Specifies the name of the network team on which this particular interface should exist.

    .PARAMETER VlanId
        Specifies VlanId to be set on network team interface.

    .PARAMETER Ensure
        Specifies if the network team interface should be created or deleted.
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
        [System.String]
        $TeamName,

        [Parameter()]
        [System.UInt32]
        $VlanId,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present'
    )

    Write-Verbose -Message ($script:localizedData.GetTeamNicInfo -f $Name)

    $getNetLbfoTeamNicParameters = @{
        Name        = $Name
        Team        = $TeamName
        ErrorAction = 'SilentlyContinue'
    }
    $teamNic = Get-NetLbfoTeamNic @getNetLbfoTeamNicParameters

    if ($Ensure -eq 'Present')
    {
        if ($teamNic)
        {
            Write-Verbose -Message ($script:localizedData.FoundTeamNic -f $Name)

            if ($teamNic.VlanId -ne $VlanId)
            {
                Write-Verbose -Message ($script:localizedData.TeamNicVlanMismatch -f $VlanId)

                $isNetModifyRequired = $true
            }

            if ($isNetModifyRequired)
            {
                Write-Verbose -Message ($script:localizedData.ModifyTeamNic -f $Name)

                if ($VlanId -eq 0)
                {
                    $setNetLbfoTeamNicParameters = @{
                        Name        = $Name
                        Team        = $TeamName
                        Default     = $true
                        ErrorAction = 'Stop'
                        Confirm     = $false
                    }
                    Set-NetLbfoTeamNic @setNetLbfoTeamNicParameters
                }
                else
                {
                    <#
                        Required in case of primary interface, whose name gets changed
                        to include VLAN ID, if specified
                    #>
                    $setNetLbfoTeamNicParameters = @{
                        Name        = $Name
                        Team        = $TeamName
                        VlanId      = $VlanId
                        ErrorAction = 'Stop'
                        Confirm     = $false
                    }
                    $renameNetAdapterParameters = @{
                        NewName     = $Name
                        ErrorAction = 'SilentlyContinue'
                        Confirm     = $false
                    }
                    $null = Set-NetLbfoTeamNic @setNetLbfoTeamNicParameters |
                        Rename-NetAdapter @renameNetAdapterParameters
                }
            }
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.CreateTeamNic -f $Name)

            if ($VlanId -ne 0)
            {
                $addNetLbfoTeamNicParameters = @{
                    Name        = $Name
                    Team        = $TeamName
                    VlanId      = $VlanId
                    ErrorAction = 'Stop'
                    Confirm     = $false
                }
                $null = Add-NetLbfoTeamNic @addNetLbfoTeamNicParameters

                Write-Verbose -Message ($script:localizedData.CreatedNetTeamNic -f $Name)
            }
            else
            {
                New-InvalidOperationException `
                    -Message ($script:localizedData.FailedToCreateTeamNic)
            }
        }
    }
    else
    {
        Write-Verbose -Message ($script:localizedData.RemoveTeamNic -f $Name)

        $removeNetLbfoTeamNicParameters = @{
            Team        = $teamNic.Team
            VlanId      = $teamNic.VlanId
            ErrorAction = 'Stop'
            Confirm     = $false
        }
        $null = Remove-NetLbfoTeamNic @removeNetLbfoTeamNicParameters
    }
}

<#
    .SYNOPSIS
        Tests is a specified Network Team Interface is in the correct state.

    .PARAMETER Name
        Specifies the name of the network team interface to create.

    .PARAMETER TeamName
        Specifies the name of the network team on which this particular interface should exist.

    .PARAMETER VlanId
        Specifies VlanId to be set on network team interface.

    .PARAMETER Ensure
        Specifies if the network team interface should be created or deleted.
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
        [System.String]
        $TeamName,

        [Parameter()]
        [System.UInt32]
        $VlanId,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present'
    )

    Write-Verbose -Message ($script:localizedData.GetTeamNicInfo -f $Name)

    $getNetLbfoTeamNicParameters = @{
        Name        = $Name
        Team        = $TeamName
        ErrorAction = 'SilentlyContinue'
    }
    $teamNic = Get-NetLbfoTeamNic @getNetLbfoTeamNicParameters

    if ($VlanId -eq 0)
    {
        $VlanValue = $null
    }
    else
    {
        $VlanValue = $VlanId
    }

    if ($Ensure -eq 'Present')
    {
        if ($teamNic)
        {
            Write-Verbose -Message ($script:localizedData.FoundTeamNic -f $Name)

            if ($teamNic.VlanId -eq $VlanValue)
            {
                Write-Verbose -Message ($script:localizedData.TeamNicExistsNoAction -f $Name)

                return $true
            }
            else
            {
                Write-Verbose -Message ($script:localizedData.TeamNicExistsWithDifferentConfig -f $Name)

                return $false
            }
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.TeamNicDoesNotExistShouldCreate -f $Name)

            return $false
        }
    }
    else
    {
        if ($teamNic)
        {
            Write-Verbose -Message ($script:localizedData.TeamNicExistsShouldRemove -f $Name)

            return $false
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.TeamNicExistsNoAction -f $Name)

            return $true
        }
    }
}

Export-ModuleMember -Function *-TargetResource
