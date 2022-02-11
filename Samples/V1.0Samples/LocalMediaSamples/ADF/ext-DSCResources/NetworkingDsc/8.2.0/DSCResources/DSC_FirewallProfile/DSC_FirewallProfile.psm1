$modulePath = Join-Path -Path (Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent) -ChildPath 'Modules'

# Import the Networking Common Modules
Import-Module -Name (Join-Path -Path $modulePath `
        -ChildPath (Join-Path -Path 'NetworkingDsc.Common' `
            -ChildPath 'NetworkingDsc.Common.psm1'))

Import-Module -Name (Join-Path -Path $modulePath -ChildPath 'DscResource.Common')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -DefaultUICulture 'en-US'

<#
    This is an array of all the parameters used by this resource.
#>
$resourceData = Import-LocalizedData `
    -BaseDirectory $PSScriptRoot `
    -FileName 'DSC_FirewallProfile.data.psd1'

# This must be a script parameter so that it is accessible
$script:parameterList = $resourceData.ParameterList

<#
    .SYNOPSIS
        Returns the current Firewall Profile.

    .PARAMETER Name
        The name of the firewall profile to configure.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Domain', 'Public', 'Private')]
        [System.String]
        $Name
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingFirewallProfileMessage) `
                -f $Name
        ) -join '' )

    # Get the current Dns Client Global Settings
    $netFirewallProfile = Get-NetFirewallProfile -Name $Name `
        -ErrorAction Stop

    # Generate the return object.
    $returnValue = @{
        Name = $Name
    }

    foreach ($parameter in $script:parameterList)
    {
        $returnValue += @{
            $parameter.Name = $netFirewallProfile.$($parameter.name)
        }
    } # foreach

    return $returnValue
} # Get-TargetResource


<#
    .SYNOPSIS
        Sets the Firewall Profile.

    .PARAMETER Name
        The name of the firewall profile to configure.

    .PARAMETER AllowInboundRules
        Specifies that the firewall blocks inbound traffic.

    .PARAMETER AllowLocalFirewallRules
        Specifies that the local firewall rules should be merged into the effective policy
        along with Group Policy settings.

    .PARAMETER AllowLocalIPsecRules
        Specifies that the local IPsec rules should be merged into the effective policy
        along with Group Policy settings.

    .PARAMETER AllowUnicastResponseToMulticast
        Allows unicast responses to multi-cast traffic.

    .PARAMETER AllowUserApps
        Specifies that traffic from local user applications is allowed through the firewall.

    .PARAMETER AllowUserPorts
        Specifies that traffic is allowed through local user ports.

    .PARAMETER DefaultInboundAction
        Specifies how to filter inbound traffic.

    .PARAMETER DefaultOutboundAction
        Specifies how to filter outbound traffic.

    .PARAMETER DisabledInterfaceAliases
        Specifies a list of interfaces on which firewall settings are excluded.

    .PARAMETER Enabled
        Specifies that devolution is activated.

    .PARAMETER EnableStealthModeForIPsec
        Enables stealth mode for IPsec traffic.

    .PARAMETER LogAllowed
        Specifies how to log the allowed packets in the location specified by the
        LogFileName parameter.

    .PARAMETER LogBlocked
        Specifies how to log the dropped packets in the location specified by the
        LogFileName parameter.

    .PARAMETER LogFileName
        Specifies the path and filename of the file to which Windows Server writes log entries.

    .PARAMETER LogIgnored
        Specifies how to log the ignored packets in the location specified by the LogFileName
        parameter.

    .PARAMETER LogMaxSizeKilobytes
        Specifies the maximum file size of the log, in kilobytes. The acceptable values for
        this parameter are: 1 through 32767.

    .PARAMETER NotifyOnListen
        Allows the notification of listening for inbound connections by a service.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Domain', 'Public', 'Private')]
        [System.String]
        $Name,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowInboundRules,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowLocalFirewallRules,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowLocalIPsecRules,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowUnicastResponseToMulticast,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowUserApps,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowUserPorts,

        [Parameter()]
        [ValidateSet('Block', 'Allow', 'NotConfigured')]
        [System.String]
        $DefaultInboundAction,

        [Parameter()]
        [ValidateSet('Block', 'Allow', 'NotConfigured')]
        [System.String]
        $DefaultOutboundAction,

        [Parameter()]
        [System.String[]]
        $DisabledInterfaceAliases,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $Enabled,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $EnableStealthModeForIPsec,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $LogAllowed,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $LogBlocked,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $LogFileName,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $LogIgnored,

        [Parameter()]
        [ValidateRange(1,32767)]
        [System.Uint64]
        $LogMaxSizeKilobytes,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $NotifyOnListen
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.SettingFirewallProfileMessage) `
                -f $Name
        ) -join '' )

    # Get the current Firewall Profile Settings
    $netFirewallProfile = Get-NetFirewallProfile -Name $Name `
        -ErrorAction Stop

    # Generate a list of parameters that will need to be changed.
    $changeParameters = @{}

    foreach ($parameter in $script:parameterList)
    {
        $parameterSourceValue = $netFirewallProfile.$($parameter.name)
        $parameterNewValue = (Get-Variable -Name ($parameter.name)).Value

        if ($PSBoundParameters.ContainsKey($parameter.Name) `
            -and (Compare-Object -ReferenceObject $parameterSourceValue -DifferenceObject $parameterNewValue -SyncWindow 0))
        {
            $changeParameters += @{
                $($parameter.name) = $parameterNewValue
            }

            Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.FirewallProfileUpdateParameterMessage) `
                    -f $Name,$parameter.Name,$parameterNewValue
                ) -join '' )
        } # if
    } # foreach

    if ($changeParameters.Count -gt 0)
    {
        # Update any parameters that were identified as different
        $null = Set-NetFirewallProfile -Name $Name `
            @ChangeParameters `
            -ErrorAction Stop

        Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.FirewallProfileUpdatedMessage) `
                -f $Name
            ) -join '' )
    } # if
} # Set-TargetResource

<#
    .SYNOPSIS
        Tests the state of Firewall Profile.

    .PARAMETER Name
        The name of the firewall profile to configure.

    .PARAMETER AllowInboundRules
        Specifies that the firewall blocks inbound traffic.

    .PARAMETER AllowLocalFirewallRules
        Specifies that the local firewall rules should be merged into the effective policy
        along with Group Policy settings.

    .PARAMETER AllowLocalIPsecRules
        Specifies that the local IPsec rules should be merged into the effective policy
        along with Group Policy settings.

    .PARAMETER AllowUnicastResponseToMulticast
        Allows unicast responses to multi-cast traffic.

    .PARAMETER AllowUserApps
        Specifies that traffic from local user applications is allowed through the firewall.

    .PARAMETER AllowUserPorts
        Specifies that traffic is allowed through local user ports.

    .PARAMETER DefaultInboundAction
        Specifies how to filter inbound traffic.

    .PARAMETER DefaultOutboundAction
        Specifies how to filter outbound traffic.

    .PARAMETER DisabledInterfaceAliases
        Specifies a list of interfaces on which firewall settings are excluded.

    .PARAMETER Enabled
        Specifies that devolution is activated.

    .PARAMETER EnableStealthModeForIPsec
        Enables stealth mode for IPsec traffic.

    .PARAMETER LogAllowed
        Specifies how to log the allowed packets in the location specified by the
        LogFileName parameter.

    .PARAMETER LogBlocked
        Specifies how to log the dropped packets in the location specified by the
        LogFileName parameter.

    .PARAMETER LogFileName
        Specifies the path and filename of the file to which Windows Server writes log entries.

    .PARAMETER LogIgnored
        Specifies how to log the ignored packets in the location specified by the LogFileName
        parameter.

    .PARAMETER LogMaxSizeKilobytes
        Specifies the maximum file size of the log, in kilobytes. The acceptable values for
        this parameter are: 1 through 32767.

    .PARAMETER NotifyOnListen
        Allows the notification of listening for inbound connections by a service.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Domain', 'Public', 'Private')]
        [System.String]
        $Name,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowInboundRules,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowLocalFirewallRules,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowLocalIPsecRules,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowUnicastResponseToMulticast,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowUserApps,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $AllowUserPorts,

        [Parameter()]
        [ValidateSet('Block', 'Allow', 'NotConfigured')]
        [System.String]
        $DefaultInboundAction,

        [Parameter()]
        [ValidateSet('Block', 'Allow', 'NotConfigured')]
        [System.String]
        $DefaultOutboundAction,

        [Parameter()]
        [System.String[]]
        $DisabledInterfaceAliases,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $Enabled,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $EnableStealthModeForIPsec,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $LogAllowed,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $LogBlocked,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $LogFileName,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $LogIgnored,

        [Parameter()]
        [ValidateRange(1,32767)]
        [System.Uint64]
        $LogMaxSizeKilobytes,

        [Parameter()]
        [ValidateSet('True', 'False', 'NotConfigured')]
        [System.String]
        $NotifyOnListen
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestingFirewallProfileMessage) `
                -f $Name
        ) -join '' )

    # Flag to signal whether settings are correct
    $desiredConfigurationMatch = $true

    # Get the current Dns Client Global Settings
    $netFirewallProfile = Get-NetFirewallProfile -Name $Name `
        -ErrorAction Stop

    # Check each parameter
    foreach ($parameter in $script:parameterList)
    {
        $parameterSourceValue = $netFirewallProfile.$($parameter.name)
        $parameterNewValue = (Get-Variable -Name ($parameter.name)).Value

        if ($PSBoundParameters.ContainsKey($parameter.Name) `
            -and (Compare-Object -ReferenceObject $parameterSourceValue -DifferenceObject $parameterNewValue -SyncWindow 0))
        {
            Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.FirewallProfileParameterNeedsUpdateMessage) `
                    -f $Name,$parameter.Name,$parameterSourceValue,$parameterNewValue
                ) -join '' )

            $desiredConfigurationMatch = $false
        } # if
    } # foreach

    return $desiredConfigurationMatch
} # Test-TargetResource

Export-ModuleMember -Function *-TargetResource
