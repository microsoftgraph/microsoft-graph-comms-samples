$modulePath = Join-Path -Path (Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent) -ChildPath 'Modules'

# Import the Networking Common Modules
Import-Module -Name (Join-Path -Path $modulePath `
        -ChildPath (Join-Path -Path 'NetworkingDsc.Common' `
            -ChildPath 'NetworkingDsc.Common.psm1'))

Import-Module -Name (Join-Path -Path $modulePath -ChildPath 'DscResource.Common')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -DefaultUICulture 'en-US'

<#
    This is an array of all the parameters used by this resource
    It can be used by several of the functions to reduce the amount of code required
    Each element contains 3 properties:
    Name: The parameter name
    Source: The source where the existing parameter can be pulled from
    Type: This is the content type of the paramater (it is either array or string or blank)
    A blank type means it will not be compared
    data ParameterList
    Delimiter: Only required for Profile parameter, because Get-NetFirewall rule doesn't
    return the profile as an array, but a comma delimited string. Setting this value causes
    the functions to first split the parameter into an array.
#>
$script:resourceData = Import-LocalizedData `
    -BaseDirectory $PSScriptRoot `
    -FileName 'DSC_Firewall.data.psd1'
$script:parameterList = $script:resourceData.ParameterList

<#
    .SYNOPSIS
        Returns the current state of the Firewall Rule.

    .PARAMETER Name
        Name of the firewall rule.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        # Name of the Firewall Rule
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name
    )

    $ErrorActionPreference = 'Stop'
    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingFirewallRuleMessage) -f $Name
        ) -join '')

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.FindFirewallRuleMessage) -f $Name
        ) -join '')

    $firewallRule = Get-FirewallRule -Name $Name

    if (-not $firewallRule)
    {
        Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                $($script:localizedData.FirewallRuleDoesNotExistMessage) -f $Name
            ) -join '')
        return @{
            Ensure = 'Absent'
            Name   = $Name
        }
    }

    $properties = Get-FirewallRuleProperty -FirewallRule $firewallRule

    $result = @{
        Ensure = 'Present'
    }

    <#
        Populate the properties for get target resource by looping through
        the parameter array list and adding the values to
    #>
    foreach ($parameter in $script:parameterList)
    {
        if ($parameter.Type -in @('Array', 'ArrayIP'))
        {
            $parameterValue = @(Get-FirewallPropertyValue `
                    -FirewallRule $firewallRule `
                    -Properties $properties `
                    -Parameter $parameter)
            if ($parameter.Delimiter)
            {
                $parameterValue = $parameterValue -split $parameter.Delimiter
            }

            $result += @{
                $parameter.Name = $parameterValue
            }

            Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                    $($script:localizedData.FirewallParameterValueMessage) -f `
                        $Name, $parameter.Name, ($parameterValue -join ',')
                ) -join '')
        }
        else
        {
            $parameterValue = Get-FirewallPropertyValue `
                -FirewallRule $firewallRule `
                -Properties $properties `
                -Parameter $parameter

            $result += @{
                $parameter.Name = $parameterValue
            }

            Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                    $($script:localizedData.FirewallParameterValueMessage) -f `
                        $Name, $parameter.Name, $parameterValue
                ) -join '')
        }
    }
    return $result
}

<#
    .SYNOPSIS
        Create, update or delete the Firewall Rule.

    .PARAMETER Name
        Name of the firewall rule.

    .PARAMETER DisplayName
        Localized, user-facing name of the firewall rule being created.

    .PARAMETER Group
        Name of the firewall group where we want to put the firewall rule.

    .PARAMETER Ensure
        Ensure that the firewall rule exists.

    .PARAMETER Enabled
        Enable or Disable the supplied configuration.

    .PARAMETER Action
        Allow or Block the supplied configuration.

    .PARAMETER Profile
        Specifies one or more profiles to which the rule is assigned.

    .PARAMETER Direction
        Direction of the connection.

    .PARAMETER RemotePort
        Specific port used for filter. Specified by port number, range, or keyword.

    .PARAMETER LocalPort
        Local port used for the filter.

    .PARAMETER Protocol
        Specific protocol for filter. Specified by name, number, or range.

    .PARAMETER Description
        Documentation for the rule.

    .PARAMETER Program
        Path and filename of the program for which the rule is applied.

    .PARAMETER Service
        Specifies the short name of a Windows service to which the firewall rule applies.

    .PARAMETER Authentication
        Specifies that authentication is required on firewall rules.

    .PARAMETER Encryption
        Specifies that encryption in authentication is required on firewall rules.

    .PARAMETER InterfaceAlias
        Specifies the alias of the interface that applies to the traffic.

    .PARAMETER InterfaceType
        Specifies that only network connections made through the indicated interface types are subject
        to the requirements of this rule.

    .PARAMETER LocalAddress
        Specifies that network packets with matching IP addresses match this rule. This parameter value
        is the first end point of an IPsec rule and specifies the computers that are subject to the
        requirements of this rule. This parameter value is an IPv4 or IPv6 address, hostname, subnet,
        range, or the following keyword: Any.

    .PARAMETER LocalUser
        Specifies the principals to which network traffic this firewall rule applies. The principals,
        represented by security identifiers (SIDs) in the security descriptor definition language (SDDL)
        string, are services, users, application containers, or any SID to which network traffic is
        associated.

    .PARAMETER Package
        Specifies the Windows Store application to which the firewall rule applies. This parameter is
        specified as a security identifier (SID).

    .PARAMETER Platform
        Specifies which version of Windows the associated rule applies.

    .PARAMETER RemoteAddress
        Specifies that network packets with matching IP addresses match this rule. This parameter value
        is the second end point of an IPsec rule and specifies the computers that are subject to the
        requirements of this rule. This parameter value is an IPv4 or IPv6 address, hostname, subnet,
        range, or the following keyword: Any

    .PARAMETER RemoteMachine
        Specifies that matching IPsec rules of the indicated computer accounts are created. This
        parameter specifies that only network packets that are authenticated as incoming from or
        outgoing to a computer identified in the list of computer accounts (SID) match this rule.
        This parameter value is specified as an SDDL string.

    .PARAMETER RemoteUser
        Specifies that matching IPsec rules of the indicated user accounts are created. This parameter
        specifies that only network packets that are authenticated as incoming from or outgoing to a
        user identified in the list of user accounts match this rule. This parameter value is specified
        as an SDDL string.

    .PARAMETER DynamicTransport
        Specifies a dynamic transport.

    .PARAMETER EdgeTraversalPolicy
        Specifies that matching firewall rules of the indicated edge traversal policy are created.

    .PARAMETER IcmpType
        Specifies the ICMP type codes.

    .PARAMETER LocalOnlyMapping
        Indicates that matching firewall rules of the indicated value are created.

    .PARAMETER LooseSourceMapping
        Indicates that matching firewall rules of the indicated value are created.

    .PARAMETER OverrideBlockRules
        Indicates that matching network traffic that would otherwise be blocked are allowed.

    .PARAMETER Owner
        Specifies that matching firewall rules of the indicated owner are created.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $DisplayName,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Group,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [String]
        $Ensure = 'Present',

        [Parameter()]
        [ValidateSet('True', 'False')]
        [String]
        $Enabled,

        [Parameter()]
        [ValidateSet('NotConfigured', 'Allow', 'Block')]
        [String]
        $Action,

        [Parameter()]
        [String[]]
        $Profile,

        [Parameter()]
        [ValidateSet('Inbound', 'Outbound')]
        [String]
        $Direction,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $RemotePort,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $LocalPort,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Protocol,

        [Parameter()]
        [String]
        $Description,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Program,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Service,

        [Parameter()]
        [ValidateSet('NotRequired', 'Required', 'NoEncap')]
        [String]
        $Authentication,

        [Parameter()]
        [ValidateSet('NotRequired', 'Required', 'Dynamic')]
        [String]
        $Encryption,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $InterfaceAlias,

        [Parameter()]
        [ValidateSet('Any', 'Wired', 'Wireless', 'RemoteAccess')]
        [String]
        $InterfaceType,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $LocalAddress,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $LocalUser,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Package,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $Platform,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $RemoteAddress,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $RemoteMachine,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $RemoteUser,

        [Parameter()]
        [ValidateSet('Any', 'ProximityApps', 'ProximitySharing', 'WifiDirectPrinting', 'WifiDirectDisplay', 'WifiDirectDevices')]
        [String]
        $DynamicTransport,

        [Parameter()]
        [ValidateSet('Block', 'Allow', 'DeferToUser', 'DeferToApp')]
        [String]
        $EdgeTraversalPolicy,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $IcmpType,

        [Parameter()]
        [Boolean]
        $LocalOnlyMapping,

        [Parameter()]
        [Boolean]
        $LooseSourceMapping,

        [Parameter()]
        [Boolean]
        $OverrideBlockRules,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Owner
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.ApplyingFirewallRuleMessage) -f $Name
        ) -join '')

    # Remove any parameters not used in Splats
    $null = $PSBoundParameters.Remove('Ensure')

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.FindFirewallRuleMessage) -f $Name
        ) -join '')
    $firewallRule = Get-FirewallRule -Name $Name

    $exists = ($null -ne $firewallRule)

    if ($Ensure -eq 'Present')
    {
        Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                $($script:localizedData.FirewallRuleShouldExistMessage) -f $Name, $Ensure
            ) -join '')

        if ($exists)
        {
            Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                    $($script:localizedData.FirewallRuleShouldExistAndDoesMessage) -f $Name
                ) -join '')

            Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                    $($script:localizedData.CheckFirewallRuleParametersMessage) -f $Name
                ) -join '')

            if (-not (Test-RuleProperties -FirewallRule $firewallRule @PSBoundParameters))
            {
                Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                        $($script:localizedData.UpdatingExistingFirewallMessage) -f $Name
                    ) -join '')

                # If the Group is being changed the the rule needs to be recreated
                if ($PSBoundParameters.ContainsKey('Group') `
                        -and ($Group -ne $FirewallRule.Group))
                {
                    Remove-NetFirewallRule -Name  (ConvertTo-FirewallRuleNameEscapedString -Name $Name)

                    <#
                        Merge the existing rule values into the PSBoundParameters
                        so that it can be splatted.
                    #>
                    $properties = Get-FirewallRuleProperty -FirewallRule $firewallRule

                    <#
                        Loop through each possible property and if it is not passed as a parameter
                        then set the PSBoundParameter property to the exiting rule value.
                    #>
                    foreach ($parameter in $ParametersList)
                    {
                        if (-not $PSBoundParameters.ContainsKey($parameter.Name))
                        {
                            $parameterValue = Get-FirewallPropertyValue `
                                -FirewallRule $firewallRule `
                                -Properties $properties `
                                -Parameter $parameter

                            if ($ParameterValue)
                            {
                                $null = $PSBoundParameters.Add($parameter.Name, $ParameterValue)
                            }
                        }
                    }

                    New-NetFirewallRule @PSBoundParameters
                }
                else
                {
                    # Group is a lookup key parameter that cannot be used in conjunction with Name
                    $null = $PSBoundParameters.Remove('Group')

                    <#
                        If the DisplayName is provided then need to remove it
                        And change it to NewDisplayName if it is different.
                    #>
                    if ($PSBoundParameters.ContainsKey('DisplayName'))
                    {
                        $null = $PSBoundParameters.Remove('DisplayName')
                        if ($DisplayName -ne $FirewallRule.DisplayName)
                        {
                            $null = $PSBoundParameters.Add('NewDisplayName', $DisplayName)
                        }
                    }

                    # Escape firewall rule name to ensure that wildcard update is not used
                    $PSBoundParameters['Name'] = ConvertTo-FirewallRuleNameEscapedString -Name $Name

                    # Set the existing Firewall rule based on specified parameters
                    Set-NetFirewallRule @PSBoundParameters
                }
            }
        }
        else
        {
            Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                    $($script:localizedData.FirewallRuleShouldExistAndDoesNotMessage) -f $Name
                ) -join '')

            # Set any default parameter values
            if (-not $DisplayName)
            {
                if (-not $PSBoundParameters.ContainsKey('DisplayName'))
                {
                    $null = $PSBoundParameters.Add('DisplayName', $Name)
                }
                else
                {
                    $PSBoundParameters.DisplayName = $Name
                }
            }

            # Add the new Firewall rule based on specified parameters
            New-NetFirewallRule @PSBoundParameters
        }
    }
    else
    {
        Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                $($script:localizedData.FirewallRuleShouldNotExistMessage) -f $Name, $Ensure
            ) -join '')

        if ($exists)
        {
            Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                    $($script:localizedData.FirewallRuleShouldNotExistButDoesMessage) -f $Name
                ) -join '')

            # Remove the existing Firewall rule
            Remove-NetFirewallRule -Name (ConvertTo-FirewallRuleNameEscapedString -Name $Name)
        }
        else
        {
            Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                    $($script:localizedData.FirewallRuleShouldNotExistAndDoesNotMessage) -f $Name
                ) -join '')
            # Do Nothing
        }
    }
}

<#
    .SYNOPSIS
    Test if Firewall Rule is in the required state.

    .PARAMETER Name
        Name of the firewall rule.

    .PARAMETER DisplayName
        Localized, user-facing name of the firewall rule being created.

    .PARAMETER Group
        Name of the firewall group where we want to put the firewall rule.

    .PARAMETER Ensure
        Ensure that the firewall rule exists.

    .PARAMETER Enabled
        Enable or Disable the supplied configuration.

    .PARAMETER Action
        Allow or Block the supplied configuration.

    .PARAMETER Profile
        Specifies one or more profiles to which the rule is assigned.

    .PARAMETER Direction
        Direction of the connection.

    .PARAMETER RemotePort
        Specific port used for filter. Specified by port number, range, or keyword.

    .PARAMETER LocalPort
        Local port used for the filter.

    .PARAMETER Protocol
        Specific protocol for filter. Specified by name, number, or range.

    .PARAMETER Description
        Documentation for the rule.

    .PARAMETER Program
        Path and filename of the program for which the rule is applied.

    .PARAMETER Service
        Specifies the short name of a Windows service to which the firewall rule applies.

    .PARAMETER Authentication
        Specifies that authentication is required on firewall rules.

    .PARAMETER Encryption
        Specifies that encryption in authentication is required on firewall rules.

    .PARAMETER InterfaceAlias
        Specifies the alias of the interface that applies to the traffic.

    .PARAMETER InterfaceType
        Specifies that only network connections made through the indicated interface types are subject
        to the requirements of this rule.

    .PARAMETER LocalAddress
        Specifies that network packets with matching IP addresses match this rule. This parameter value
        is the first end point of an IPsec rule and specifies the computers that are subject to the
        requirements of this rule. This parameter value is an IPv4 or IPv6 address, hostname, subnet,
        range, or the following keyword: Any.

    .PARAMETER LocalUser
        Specifies the principals to which network traffic this firewall rule applies. The principals,
        represented by security identifiers (SIDs) in the security descriptor definition language (SDDL)
        string, are services, users, application containers, or any SID to which network traffic is
        associated.

    .PARAMETER Package
        Specifies the Windows Store application to which the firewall rule applies. This parameter is
        specified as a security identifier (SID).

    .PARAMETER Platform
        Specifies which version of Windows the associated rule applies.

    .PARAMETER RemoteAddress
        Specifies that network packets with matching IP addresses match this rule. This parameter value
        is the second end point of an IPsec rule and specifies the computers that are subject to the
        requirements of this rule. This parameter value is an IPv4 or IPv6 address, hostname, subnet,
        range, or the following keyword: Any

    .PARAMETER RemoteMachine
        Specifies that matching IPsec rules of the indicated computer accounts are created. This
        parameter specifies that only network packets that are authenticated as incoming from or
        outgoing to a computer identified in the list of computer accounts (SID) match this rule.
        This parameter value is specified as an SDDL string.

    .PARAMETER RemoteUser
        Specifies that matching IPsec rules of the indicated user accounts are created. This parameter
        specifies that only network packets that are authenticated as incoming from or outgoing to a
        user identified in the list of user accounts match this rule. This parameter value is specified
        as an SDDL string.

    .PARAMETER DynamicTransport
        Specifies a dynamic transport.

    .PARAMETER EdgeTraversalPolicy
        Specifies that matching firewall rules of the indicated edge traversal policy are created.

    .PARAMETER IcmpType
        Specifies the ICMP type codes.

    .PARAMETER LocalOnlyMapping
        Indicates that matching firewall rules of the indicated value are created.

    .PARAMETER LooseSourceMapping
        Indicates that matching firewall rules of the indicated value are created.

    .PARAMETER OverrideBlockRules
        Indicates that matching network traffic that would otherwise be blocked are allowed.

    .PARAMETER Owner
        Specifies that matching firewall rules of the indicated owner are created.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $DisplayName,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Group,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [String]
        $Ensure = 'Present',

        [Parameter()]
        [ValidateSet('True', 'False')]
        [String]
        $Enabled,

        [Parameter()]
        [ValidateSet('NotConfigured', 'Allow', 'Block')]
        [String]
        $Action,

        [Parameter()]
        [String[]]
        $Profile,

        [Parameter()]
        [ValidateSet('Inbound', 'Outbound')]
        [String]
        $Direction,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $RemotePort,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $LocalPort,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Protocol,

        [Parameter()]
        [String]
        $Description,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Program,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Service,

        [Parameter()]
        [ValidateSet('NotRequired', 'Required', 'NoEncap')]
        [String]
        $Authentication,

        [Parameter()]
        [ValidateSet('NotRequired', 'Required', 'Dynamic')]
        [String]
        $Encryption,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $InterfaceAlias,

        [Parameter()]
        [ValidateSet('Any', 'Wired', 'Wireless', 'RemoteAccess')]
        [String]
        $InterfaceType,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $LocalAddress,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $LocalUser,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Package,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $Platform,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $RemoteAddress,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $RemoteMachine,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $RemoteUser,

        [Parameter()]
        [ValidateSet('Any', 'ProximityApps', 'ProximitySharing', 'WifiDirectPrinting', 'WifiDirectDisplay', 'WifiDirectDevices')]
        [String]
        $DynamicTransport,

        [Parameter()]
        [ValidateSet('Block', 'Allow', 'DeferToUser', 'DeferToApp')]
        [String]
        $EdgeTraversalPolicy,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $IcmpType,

        [Parameter()]
        [Boolean]
        $LocalOnlyMapping,

        [Parameter()]
        [Boolean]
        $LooseSourceMapping,

        [Parameter()]
        [Boolean]
        $OverrideBlockRules,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $Owner
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckingFirewallRuleMessage) -f $Name
        ) -join '')

    # Remove any parameters not used in Splats
    $null = $PSBoundParameters.Remove('Ensure')

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.FindFirewallRuleMessage) -f $Name
        ) -join '')

    $firewallRule = Get-FirewallRule -Name $Name

    $exists = ($null -ne $firewallRule)

    if (-not $exists)
    {
        Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                $($script:localizedData.FirewallRuleDoesNotExistMessage) -f $Name
            ) -join '')

        # Returns whether complies with $Ensure
        $returnValue = ($false -eq ($Ensure -eq 'Present'))

        Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                $($script:localizedData.CheckingFirewallReturningMessage) -f $Name, $returnValue
            ) -join '')

        return $returnValue
    }

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckFirewallRuleParametersMessage) -f $Name
        ) -join '')

    $desiredConfigurationMatch = Test-RuleProperties -FirewallRule $firewallRule @PSBoundParameters

    # Returns whether or not $exists complies with $Ensure
    $returnValue = ($desiredConfigurationMatch -and $exists -eq ($Ensure -eq 'Present'))

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.CheckingFirewallReturningMessage) -f $Name, $returnValue
        ) -join '')

    return $returnValue
}

<#
    .SYNOPSIS
        Tests if the properties in the supplied Firewall Rule match the expected parameters passed.

    .PARAMETER FirewallRule
        The firewall rule object to compare the properties of.

    .PARAMETER Name
        Name of the firewall rule.

    .PARAMETER DisplayName
        Localized, user-facing name of the firewall rule being created.

    .PARAMETER Group
        Name of the firewall group where we want to put the firewall rule.

    .PARAMETER Ensure
        Ensure that the firewall rule exists.

    .PARAMETER Enabled
        Enable or Disable the supplied configuration.

    .PARAMETER Action
        Allow or Block the supplied configuration.

    .PARAMETER Profile
        Specifies one or more profiles to which the rule is assigned.

    .PARAMETER Direction
        Direction of the connection.

    .PARAMETER RemotePort
        Specific port used for filter. Specified by port number, range, or keyword.

    .PARAMETER LocalPort
        Local port used for the filter.

    .PARAMETER Protocol
        Specific protocol for filter. Specified by name, number, or range.

    .PARAMETER Description
        Documentation for the rule.

    .PARAMETER Program
        Path and filename of the program for which the rule is applied.

    .PARAMETER Service
        Specifies the short name of a Windows service to which the firewall rule applies.

    .PARAMETER Authentication
        Specifies that authentication is required on firewall rules.

    .PARAMETER Encryption
        Specifies that encryption in authentication is required on firewall rules.

    .PARAMETER InterfaceAlias
        Specifies the alias of the interface that applies to the traffic.

    .PARAMETER InterfaceType
        Specifies that only network connections made through the indicated interface types are subject
        to the requirements of this rule.

    .PARAMETER LocalAddress
        Specifies that network packets with matching IP addresses match this rule. This parameter value
        is the first end point of an IPsec rule and specifies the computers that are subject to the
        requirements of this rule. This parameter value is an IPv4 or IPv6 address, hostname, subnet,
        range, or the following keyword: Any.

    .PARAMETER LocalUser
        Specifies the principals to which network traffic this firewall rule applies. The principals,
        represented by security identifiers (SIDs) in the security descriptor definition language (SDDL)
        string, are services, users, application containers, or any SID to which network traffic is
        associated.

    .PARAMETER Package
        Specifies the Windows Store application to which the firewall rule applies. This parameter is
        specified as a security identifier (SID).

    .PARAMETER Platform
        Specifies which version of Windows the associated rule applies.

    .PARAMETER RemoteAddress
        Specifies that network packets with matching IP addresses match this rule. This parameter value
        is the second end point of an IPsec rule and specifies the computers that are subject to the
        requirements of this rule. This parameter value is an IPv4 or IPv6 address, hostname, subnet,
        range, or the following keyword: Any

    .PARAMETER RemoteMachine
        Specifies that matching IPsec rules of the indicated computer accounts are created. This
        parameter specifies that only network packets that are authenticated as incoming from or
        outgoing to a computer identified in the list of computer accounts (SID) match this rule.
        This parameter value is specified as an SDDL string.

    .PARAMETER RemoteUser
        Specifies that matching IPsec rules of the indicated user accounts are created. This parameter
        specifies that only network packets that are authenticated as incoming from or outgoing to a
        user identified in the list of user accounts match this rule. This parameter value is specified
        as an SDDL string.

    .PARAMETER DynamicTransport
        Specifies a dynamic transport.

    .PARAMETER EdgeTraversalPolicy
        Specifies that matching firewall rules of the indicated edge traversal policy are created.

    .PARAMETER IcmpType
        Specifies the ICMP type codes.

    .PARAMETER LocalOnlyMapping
        Indicates that matching firewall rules of the indicated value are created.

    .PARAMETER LooseSourceMapping
        Indicates that matching firewall rules of the indicated value are created.

    .PARAMETER OverrideBlockRules
        Indicates that matching network traffic that would otherwise be blocked are allowed.

    .PARAMETER Owner
        Specifies that matching firewall rules of the indicated owner are created.
#>
function Test-RuleProperties
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        $FirewallRule,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name,

        [Parameter()]
        [String]
        $DisplayName,

        [Parameter()]
        [String]
        $Group,

        [Parameter()]
        [String]
        $DisplayGroup,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [String]
        $Ensure = 'Present',

        [Parameter()]
        [ValidateSet('True', 'False')]
        [String]
        $Enabled,

        [Parameter()]
        [ValidateSet('NotConfigured', 'Allow', 'Block')]
        [String]
        $Action,

        [Parameter()]
        [String[]]
        $Profile,

        [Parameter()]
        [ValidateSet('Inbound', 'Outbound')]
        [String]
        $Direction,

        [Parameter()]
        [String[]]
        $RemotePort,

        [Parameter()]
        [String[]]
        $LocalPort,

        [Parameter()]
        [String]
        $Protocol,

        [Parameter()]
        [String]
        $Description,

        [Parameter()]
        [String]
        $Program,

        [Parameter()]
        [String]
        $Service,

        [Parameter()]
        [ValidateSet('NotRequired', 'Required', 'NoEncap')]
        [String]
        $Authentication,

        [Parameter()]
        [ValidateSet('NotRequired', 'Required', 'Dynamic')]
        [String]
        $Encryption,

        [Parameter()]
        [String[]]
        $InterfaceAlias,

        [Parameter()]
        [ValidateSet('Any', 'Wired', 'Wireless', 'RemoteAccess')]
        [String]
        $InterfaceType,

        [Parameter()]
        [String[]]
        $LocalAddress,

        [Parameter()]
        [String]
        $LocalUser,

        [Parameter()]
        [String]
        $Package,

        [Parameter()]
        [String[]]
        $Platform,

        [Parameter()]
        [String[]]
        $RemoteAddress,

        [Parameter()]
        [String]
        $RemoteMachine,

        [Parameter()]
        [String]
        $RemoteUser,

        [Parameter()]
        [ValidateSet('Any', 'ProximityApps', 'ProximitySharing', 'WifiDirectPrinting', 'WifiDirectDisplay', 'WifiDirectDevices')]
        [String]
        $DynamicTransport,

        [Parameter()]
        [ValidateSet('Block', 'Allow', 'DeferToUser', 'DeferToApp')]
        [String]
        $EdgeTraversalPolicy,

        [Parameter()]
        [String[]]
        $IcmpType,

        [Parameter()]
        [Boolean]
        $LocalOnlyMapping,

        [Parameter()]
        [Boolean]
        $LooseSourceMapping,

        [Parameter()]
        [Boolean]
        $OverrideBlockRules,

        [Parameter()]
        [String]
        $Owner
    )

    $properties = Get-FirewallRuleProperty -FirewallRule $FirewallRule
    $desiredConfigurationMatch = $true

    <#
        Loop through the $script:parameterList array and compare the source
        with the value of each parameter. If different then set $desiredConfigurationMatch
        to false.
    #>
    foreach ($parameter in $script:parameterList)
    {
        $parameterValue = Get-FirewallPropertyValue `
            -FirewallRule $firewallRule `
            -Properties $properties `
            -Parameter $parameter

        $parameterNew = (Get-Variable -Name ($parameter.Name)).Value

        switch -Wildcard ($parameter.Type)
        {
            'String'
            {
                # Perform a plain string comparison.
                if ($parameterNew -and ($parameterValue -ne $parameterNew))
                {
                    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                            $($script:localizedData.PropertyNoMatchMessage) `
                                -f $parameter.Name, $parameterValue, $parameterNew
                        ) -join '')

                    $desiredConfigurationMatch = $false
                }
            }

            'Boolean'
            {
                # Perform a boolean comparison.
                if ($parameterNew -and ($parameterValue -ne $parameterNew))
                {
                    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                            $($script:localizedData.PropertyNoMatchMessage) `
                                -f $parameter.Name, $parameterValue, $parameterNew
                        ) -join '')
                    $desiredConfigurationMatch = $false
                }
            }

            'Array*'
            {
                # Array comparison uses Compare-Object
                if ($null -eq $parameterValue)
                {
                    $parameterValue = @()
                }

                if ($parameter.Delimiter)
                {
                    $parameterValue = $parameterValue -split $parameter.Delimiter
                }

                if ($parameter.Type -eq 'ArrayIP')
                {
                    <#
                        IPArray comparison uses Compare-Object, except needs to convert any IP addresses
                        that use CIDR notation to use Subnet Mask notification because this is the
                        format that the Get-NetFirewallAddressFilter will return the IP addresses in
                        even if they were set using CIDR notation.
                    #>
                    if ($null -ne $parameterNew)
                    {
                        $parameterNew = Convert-CIDRToSubhetMask -Address $parameterNew
                    }
                }

                if ($parameterNew `
                        -and ((Compare-Object `
                                -ReferenceObject $parameterValue `
                                -DifferenceObject $parameterNew).Count -ne 0))
                {
                    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                            $($script:localizedData.PropertyNoMatchMessage) `
                                -f $parameter.Name, ($parameterValue -join ','), ($parameterNew -join ',')
                        ) -join '')
                    $desiredConfigurationMatch = $false
                }
            }
        }
    }

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestFirewallRuleReturningMessage) -f $Name, $desiredConfigurationMatch
        ) -join '')
    return $desiredConfigurationMatch
}

<#
    .SYNOPSIS
        Returns a Firewall object matching the specified name.

    .PARAMETER Name
        The name of the Firewall Rule to Retrieve.
#>
function Get-FirewallRule
{
    [CmdletBinding()]
    [OutputType([Microsoft.Management.Infrastructure.CimInstance])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name
    )

    $firewallRule = @(Get-NetFirewallRule -Name (ConvertTo-FirewallRuleNameEscapedString -Name $Name) -ErrorAction SilentlyContinue)

    if (-not $firewallRule)
    {
        Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                $($script:localizedData.FirewallRuleNotFoundMessage) -f $Name
            ) -join '')
        return $null
    }

    <#
        If more than one rule is returned for a name, then throw an exception
        because this should not be possible.
    #>
    if ($firewallRule.Count -gt 1)
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.RuleNotUniqueError -f $firewallRule.Count, $Name)
    }

    # The array will only contain a single rule so only return the first one (not the array)
    return $firewallRule[0]
}

<#
    .SYNOPSIS
        Returns a Hashtable containing the component Firewall objects for the specified Firewall Rule.

    .PARAMETER FirewallRule
        The firewall rule object to pull the additional firewall objects for.
#>
function Get-FirewallRuleProperty
{
    [CmdletBinding()]
    [OutputType([HashTable])]
    param
    (
        [Parameter(Mandatory = $true)]
        $FirewallRule
    )

    Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
            $($script:localizedData.GetAllPropertiesMessage)
        ) -join '')

    return @{
        AddressFilters       = @(Get-NetFirewallAddressFilter -AssociatedNetFirewallRule $FirewallRule)
        ApplicationFilters   = @(Get-NetFirewallApplicationFilter -AssociatedNetFirewallRule $FirewallRule)
        InterfaceFilters     = @(Get-NetFirewallInterfaceFilter -AssociatedNetFirewallRule $FirewallRule)
        InterfaceTypeFilters = @(Get-NetFirewallInterfaceTypeFilter -AssociatedNetFirewallRule $FirewallRule)
        PortFilters          = @(Get-NetFirewallPortFilter -AssociatedNetFirewallRule $FirewallRule)
        Profile              = @(Get-NetFirewallProfile -AssociatedNetFirewallRule $FirewallRule)
        SecurityFilters      = @(Get-NetFirewallSecurityFilter -AssociatedNetFirewallRule $FirewallRule)
        ServiceFilters       = @(Get-NetFirewallServiceFilter -AssociatedNetFirewallRule $FirewallRule)
    }
}

<#
    .SYNOPSIS
        Looks up a Firewall Property value using the specified parameterList entry.

    .PARAMETER FirewallRule
        The firewall rule object to pull the property from.

    .PARAMETER Properties
        The additional firewall objects to pull the property from.

    .PARAMETER Parameter
        The entry from the ParameterList table used to retireve the parameter for.
#>
function Get-FirewallPropertyValue
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        $FirewallRule,

        [Parameter(Mandatory = $true)]
        $Properties,

        [Parameter(Mandatory = $true)]
        $Parameter
    )

    if ($Parameter.Property)
    {
        return (Get-Variable `
                -Name ($Parameter.Variable)).value.$($Parameter.Property).$($Parameter.Name)
    }
    else
    {
        return (Get-Variable `
                -Name ($Parameter.Variable)).value.$($Parameter.Name)
    }
}

<#
    .SYNOPSIS
        Convert Firewall Rule name to Escape Wildcard Characters.

        It will append '[', ']' and '*' with a backtick.

    .PARAMETER Name
        The firewall rule name to escape.
#>
function ConvertTo-FirewallRuleNameEscapedString
{
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        $Name
    )

    return $Name.Replace('[','`[').Replace(']','`]').Replace('*','`*')
}

Export-ModuleMember -Function *-TargetResource
