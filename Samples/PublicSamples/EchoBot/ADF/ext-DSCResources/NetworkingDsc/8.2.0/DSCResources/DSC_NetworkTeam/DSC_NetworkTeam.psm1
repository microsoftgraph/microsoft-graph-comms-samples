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
        Returns the current state of a Network Team.

    .PARAMETER Name
        Specifies the name of the network team to create.

    .PARAMETER TeamMembers
        Specifies the network interfaces that should be a part of the network team.
        This is a comma-separated list.
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
        [System.String[]]
        $TeamMembers
    )

    $configuration = @{
        Name        = $Name
        TeamMembers = $TeamMembers
        Ensure      = 'Absent'
    }

    Write-Verbose -Message ($script:localizedData.GetTeamInfo -f $Name)
    $networkTeam = Get-NetLBFOTeam -Name $Name -ErrorAction SilentlyContinue

    if ($networkTeam)
    {
        Write-Verbose -Message ($script:localizedData.FoundTeam -f $Name)
        $configuration.Add('LoadBalancingAlgorithm', $networkTeam.LoadBalancingAlgorithm)
        $configuration.Add('TeamingMode', $networkTeam.TeamingMode)
        $configuration.Ensure = 'Present'

        if ($null -eq (Compare-Object -ReferenceObject $TeamMembers -DifferenceObject $networkTeam.Members))
        {
            Write-Verbose -Message ($script:localizedData.TeamMembersMatch -f $Name)
        }
        else
        {
            $configuration.TeamMembers = $networkTeam.Members
            Write-Verbose -Message ($script:localizedData.TeamMembersNotMatch -f $Name)
        }
    }
    else
    {
        Write-Verbose -Message ($script:localizedData.TeamNotFound -f $Name)
    }

    return $configuration
}

<#
    .SYNOPSIS
        Adds, updates or removes a Network Team.

    .PARAMETER Name
        Specifies the name of the network team to create.

    .PARAMETER TeamMembers
        Specifies the network interfaces that should be a part of the network team.
        This is a comma-separated list.

    .PARAMETER TeamingMode
        Specifies the teaming mode configuration.

    .PARAMETER LoadBalancingAlgorithm
        Specifies the load balancing algorithm for the network team.

    .PARAMETER Ensure
        Specifies if the network team should be created or deleted.
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
        [System.String[]]
        $TeamMembers,

        [Parameter()]
        [ValidateSet('SwitchIndependent', 'LACP', 'Static')]
        [System.String]
        $TeamingMode = 'SwitchIndependent',

        [Parameter()]
        [ValidateSet('Dynamic', 'HyperVPort', 'IPAddresses', 'MacAddresses', 'TransportPorts')]
        [System.String]
        $LoadBalancingAlgorithm = 'HyperVPort',

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present'
    )

    Write-Verbose -Message ($script:localizedData.GetTeamInfo -f $Name)

    $networkTeam = Get-NetLBFOTeam -Name $Name -ErrorAction SilentlyContinue

    if ($Ensure -eq 'Present')
    {
        if ($networkTeam)
        {
            Write-Verbose -Message ($script:localizedData.FoundTeam -f $Name)

            $setArguments = @{
                Name = $Name
            }

            if ($networkTeam.loadBalancingAlgorithm -ne $LoadBalancingAlgorithm)
            {
                Write-Verbose -Message ($script:localizedData.LoadBalancingAlgorithmDifferent -f $LoadBalancingAlgorithm)

                $setArguments.Add('LoadBalancingAlgorithm', $LoadBalancingAlgorithm)
                $isNetModifyRequired = $true
            }

            if ($networkTeam.TeamingMode -ne $TeamingMode)
            {
                Write-Verbose -Message ($script:localizedData.TeamingModeDifferent -f $TeamingMode)

                $setArguments.Add('TeamingMode', $TeamingMode)
                $isNetModifyRequired = $true
            }

            if ($isNetModifyRequired)
            {
                Write-Verbose -Message ($script:localizedData.ModifyTeam -f $Name)

                Set-NetLbfoTeam @setArguments -ErrorAction Stop -Confirm:$false
            }

            $netTeamMembers = Compare-Object `
                -ReferenceObject $TeamMembers `
                -DifferenceObject $networkTeam.Members

            if ($null -ne $netTeamMembers)
            {
                Write-Verbose -Message ($script:localizedData.MembersDifferent -f $Name)

                $membersToRemove = ($netTeamMembers | Where-Object -FilterScript {
                        $_.SideIndicator -eq '=>'
                    }).InputObject

                if ($membersToRemove)
                {
                    Write-Verbose -Message ($script:localizedData.RemovingMembers -f ($membersToRemove -join ','))

                    $null = Remove-NetLbfoTeamMember -Name $membersToRemove `
                        -Team $Name `
                        -ErrorAction Stop `
                        -Confirm:$false
                }

                $membersToAdd = ($netTeamMembers | Where-Object -FilterScript {
                        $_.SideIndicator -eq '<='
                    }).InputObject

                if ($membersToAdd)
                {
                    Write-Verbose -Message ($script:localizedData.AddingMembers -f ($membersToAdd -join ','))

                    $null = Add-NetLbfoTeamMember -Name $membersToAdd `
                        -Team $Name `
                        -ErrorAction Stop `
                        -Confirm:$false
                }
            }
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.CreateTeam -f $Name)

            try
            {
                $null = New-NetLbfoTeam `
                    -Name $Name `
                    -TeamMembers $teamMembers `
                    -TeamingMode $TeamingMode `
                    -LoadBalancingAlgorithm $loadBalancingAlgorithm `
                    -ErrorAction Stop `
                    -Confirm:$false

                Write-Verbose -Message $script:localizedData.CreatedNetTeam
            }

            catch
            {
                New-InvalidOperationException `
                    -Message ($script:localizedData.failedToCreateTeam -f $_.Exception.Message)
            }
        }
    }
    else
    {
        Write-Verbose -Message ($script:localizedData.RemoveTeam -f $Name)

        $null = Remove-NetLbfoTeam -Name $name -ErrorAction Stop -Confirm:$false
    }
}

<#
    .SYNOPSIS
        Tests is a specified Network Team is in the correct state.

    .PARAMETER Name
        Specifies the name of the network team to create.

    .PARAMETER TeamMembers
        Specifies the network interfaces that should be a part of the network team.
        This is a comma-separated list.

    .PARAMETER TeamingMode
        Specifies the teaming mode configuration.

    .PARAMETER LoadBalancingAlgorithm
        Specifies the load balancing algorithm for the network team.

    .PARAMETER Ensure
        Specifies if the network team should be created or deleted.
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
        [System.String[]]
        $TeamMembers,

        [Parameter()]
        [ValidateSet('SwitchIndependent', 'LACP', 'Static')]
        [System.String]
        $TeamingMode = 'SwitchIndependent',

        [Parameter()]
        [ValidateSet('Dynamic', 'HyperVPort', 'IPAddresses', 'MacAddresses', 'TransportPorts')]
        [System.String]
        $LoadBalancingAlgorithm = 'HyperVPort',

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present'
    )

    Write-Verbose -Message ($script:localizedData.GetTeamInfo -f $Name)

    $networkTeam = Get-NetLbfoTeam -Name $Name -ErrorAction SilentlyContinue

    if ($ensure -eq 'Present')
    {
        if ($networkTeam)
        {
            Write-Verbose -Message ($script:localizedData.FoundTeam -f $Name)

            if (
                ($networkTeam.LoadBalancingAlgorithm -eq $LoadBalancingAlgorithm) -and
                ($networkTeam.teamingMode -eq $TeamingMode) -and
                ($null -eq (Compare-Object -ReferenceObject $TeamMembers -DifferenceObject $networkTeam.Members))
            )
            {
                Write-Verbose -Message ($script:localizedData.TeamExistsNoAction -f $Name)

                return $true
            }
            else
            {
                Write-Verbose -Message ($script:localizedData.TeamExistsWithDifferentConfig -f $Name)

                return $false
            }
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.TeamDoesNotExistShouldCreate -f $Name)

            return $false
        }
    }
    else
    {
        if ($networkTeam)
        {
            Write-Verbose -Message ($script:localizedData.TeamExistsShouldRemove -f $Name)

            return $false
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.TeamDoesNotExistNoAction -f $Name)

            return $true
        }
    }
}
