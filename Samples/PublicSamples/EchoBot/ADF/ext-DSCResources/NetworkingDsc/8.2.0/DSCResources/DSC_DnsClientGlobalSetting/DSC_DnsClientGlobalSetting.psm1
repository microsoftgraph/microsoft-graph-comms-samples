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
    -FileName 'DSC_DnsClientGlobalSetting.data.psd1'

# This must be a script parameter so that it is accessible
$script:parameterList = $resourceData.ParameterList

<#
    .SYNOPSIS
        Returns the current DNS Client Global Settings.

    .PARAMETER IsSingleInstance
        Specifies the resource is a single instance, the value must be 'Yes'.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Yes')]
        [System.String]
        $IsSingleInstance
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.GettingDnsClientGlobalSettingMessage)
        ) -join '' )

    # Get the current Dns Client Global Settings
    $dnsClientGlobalSetting = Get-DnsClientGlobalSetting `
        -ErrorAction Stop

    # Generate the return object.
    $returnValue = @{
        IsSingleInstance = 'Yes'
    }

    foreach ($parameter in $script:parameterList)
    {
        $returnValue += @{
            $parameter.Name = $dnsClientGlobalSetting.$($parameter.name)
        }
    } # foreach

    return $returnValue
} # Get-TargetResource

<#
    .SYNOPSIS
        Sets the DNS Client Global Settings.

    .PARAMETER IsSingleInstance
        Specifies the resource is a single instance, the value must be 'Yes'.

    .PARAMETER SuffixSearchList
        Specifies a list of global suffixes that can be used in the specified order by the DNS client
        for resolving the IP address of the computer name.

    .PARAMETER UseDevolution.
        Specifies that devolution is activated.

    .PARAMETER DevolutionLevel
        Specifies the number of labels up to which devolution should occur.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Yes')]
        [System.String]
        $IsSingleInstance,

        [Parameter()]
        [System.String[]]
        $SuffixSearchList,

        [Parameter()]
        [System.Boolean]
        $UseDevolution,

        [Parameter()]
        [System.Uint32]
        $DevolutionLevel
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.SettingDnsClientGlobalSettingMessage)
        ) -join '' )

    # Get the current Dns Client Global Settings
    $dnsClientGlobalSetting = Get-DnsClientGlobalSetting `
        -ErrorAction Stop

    # Generate a list of parameters that will need to be changed.
    $changeParameters = @{}

    foreach ($parameter in $script:parameterList)
    {
        $parameterSourceValue = $dnsClientGlobalSetting.$($parameter.name)
        $parameterNewValue = (Get-Variable -Name ($parameter.name)).Value

        if ($PSBoundParameters.ContainsKey($parameter.Name) `
                -and (Compare-Object -ReferenceObject $parameterSourceValue -DifferenceObject $parameterNewValue -SyncWindow 0))
        {
            $changeParameters += @{
                $($parameter.name) = $parameterNewValue
            }

            Write-Verbose -Message ( @(
                    "$($MyInvocation.MyCommand): "
                    $($script:localizedData.DnsClientGlobalSettingUpdateParameterMessage) `
                        -f $parameter.Name,($parameterNewValue -join ',')
                ) -join '' )
        } # if
    } # foreach

    if ($changeParameters.Count -gt 0)
    {
        # Update any parameters that were identified as different
        $null = Set-DnsClientGlobalSetting `
            @ChangeParameters `
            -ErrorAction Stop

        Write-Verbose -Message ( @(
                "$($MyInvocation.MyCommand): "
                $($script:localizedData.DnsClientGlobalSettingUpdatedMessage)
            ) -join '' )
    } # if
} # Set-TargetResource

<#
    .SYNOPSIS
        Tests the state of DNS Client Global Settings.

    .PARAMETER IsSingleInstance
        Specifies the resource is a single instance, the value must be 'Yes'.

    .PARAMETER SuffixSearchList
        Specifies a list of global suffixes that can be used in the specified order by the DNS client
        for resolving the IP address of the computer name.

    .PARAMETER UseDevolution.
        Specifies that devolution is activated.

    .PARAMETER DevolutionLevel
        Specifies the number of labels up to which devolution should occur.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Yes')]
        [System.String]
        $IsSingleInstance,

        [Parameter()]
        [System.String[]]
        $SuffixSearchList,

        [Parameter()]
        [System.Boolean]
        $UseDevolution,

        [Parameter()]
        [System.Uint32]
        $DevolutionLevel
    )

    Write-Verbose -Message ( @(
            "$($MyInvocation.MyCommand): "
            $($script:localizedData.TestingDnsClientGlobalSettingMessage)
        ) -join '' )

    # Flag to signal whether settings are correct
    $desiredConfigurationMatch = $true

    # Get the current Dns Client Global Settings
    $dnsClientGlobalSetting = Get-DnsClientGlobalSetting `
        -ErrorAction Stop

    # Check each parameter
    foreach ($parameter in $script:parameterList)
    {
        $parameterSourceValue = $dnsClientGlobalSetting.$($parameter.name)
        $parameterNewValue = (Get-Variable -Name ($parameter.name)).Value
        $parameterValueMatch = $true

        switch ($parameter.Type)
        {
            'Integer'
            {
                # Perform a plain integer comparison.
                if ($PSBoundParameters.ContainsKey($parameter.Name) -and $parameterSourceValue -ne $parameterNewValue)
                {
                    $parameterValueMatch = $false
                }
            }

            'Boolean'
            {
                # Perform a boolean comparison.
                if ($PSBoundParameters.ContainsKey($parameter.Name) -and $parameterSourceValue -ne $parameterNewValue)
                {
                    $parameterValueMatch = $false
                }
            }

            'Array'
            {
                # Array comparison uses Compare-Object
                if ([System.String]::IsNullOrEmpty($parameterSourceValue))
                {
                    $parameterSourceValue = @()
                }

                if ([System.String]::IsNullOrEmpty($parameterNewValue))
                {
                    $parameterNewValue = @()
                }

                if ($PSBoundParameters.ContainsKey($parameter.Name) `
                        -and ((Compare-Object `
                                -ReferenceObject $parameterSourceValue `
                                -DifferenceObject $parameterNewValue -SyncWindow 0).Count -ne 0))
                {
                    $parameterValueMatch = $false
                }
            }
        }
        if ($parameterValueMatch -eq $false)
        {
            Write-Verbose -Message ( @( "$($MyInvocation.MyCommand): "
                    $($script:localizedData.DnsClientGlobalSettingParameterNeedsUpdateMessage) `
                        -f $parameter.Name, ($parameterSourceValue -join ','), ($parameterNewValue -join ',')
                ) -join '')
            $desiredConfigurationMatch = $false
        }
    } # foreach

    return $desiredConfigurationMatch
} # Test-TargetResource

Export-ModuleMember -Function *-TargetResource
