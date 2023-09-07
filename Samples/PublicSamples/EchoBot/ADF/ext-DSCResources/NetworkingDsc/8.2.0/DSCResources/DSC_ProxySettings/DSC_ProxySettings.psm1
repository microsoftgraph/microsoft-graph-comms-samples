$modulePath = Join-Path -Path (Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent) -ChildPath 'Modules'

# Import the Networking Common Modules
Import-Module -Name (Join-Path -Path $modulePath `
        -ChildPath (Join-Path -Path 'NetworkingDsc.Common' `
            -ChildPath 'NetworkingDsc.Common.psm1'))

Import-Module -Name (Join-Path -Path $modulePath -ChildPath 'DscResource.Common')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -DefaultUICulture 'en-US'


# Registry key paths for proxy settings
$script:connectionsRegistryKeyPath = 'SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\Connections'

<#
    .SYNOPSIS
        Returns the current state of the proxy settings for
        the computer.

    .PARAMETER IsSingleInstance
        Specifies the resource is a single instance, the
        value must be 'Yes'. Not used in Get-TargetResource.
#>
function Get-TargetResource
{
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Yes')]
        [System.String]
        $IsSingleInstance
    )

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
        $($script:localizedData.GettingProxySettingsMessage)
        ) -join '')

    $returnValue = @{}

    # Get the registry values in the Connections registry key
    $connectionsRegistryValues = Get-ItemProperty `
        -Path "HKLM:\$($script:connectionsRegistryKeyPath)" `
        -ErrorAction SilentlyContinue

    $proxySettingsRegistryBinary = $null

    if ($connectionsRegistryValues.DefaultConnectionSettings)
    {
        $proxySettingsRegistryBinary = $connectionsRegistryValues.DefaultConnectionSettings
    }
    elseif ($connectionsRegistryValues.SavedLegacySettings)
    {
        $proxySettingsRegistryBinary = $connectionsRegistryValues.SavedLegacySettings
    }

    if ($proxySettingsRegistryBinary)
    {
        $returnValue.Add('Ensure','Present')

        $proxySettings = ConvertFrom-ProxySettingsBinary -ProxySettings $proxySettingsRegistryBinary

        $returnValue.Add('EnableManualProxy',$proxySettings.EnableManualProxy)
        $returnValue.Add('EnableAutoConfiguration',$proxySettings.EnableAutoConfiguration)
        $returnValue.Add('EnableAutoDetection',$proxySettings.EnableAutoDetection)
        $returnValue.Add('ProxyServer',$proxySettings.ProxyServer)
        $returnValue.Add('ProxyServerBypassLocal',$proxySettings.ProxyServerBypassLocal)
        $returnValue.Add('ProxyServerExceptions',$proxySettings.ProxyServerExceptions)
        $returnValue.Add('AutoConfigURL',$proxySettings.AutoConfigURL)
    }
    else
    {
        $returnValue.Add('Ensure','Absent')
    }

    return $returnValue
}

<#
    .SYNOPSIS
        Sets the current state of the proxy settings for
        the computer.

    .PARAMETER IsSingleInstance
        Specifies the resource is a single instance, the
        value must be 'Yes'.

    .PARAMETER Ensure
        Specifies if computer proxy settings should be set.
        Defaults to 'Present'.

    .PARAMETER ConnectionType
        Defines if the proxy settings should be configured
        for default connections, legacy connections or all
        connections. Defaults to 'All'.

    .PARAMETER EnableAutoDetection
        Enable automatic detection of the proxy settings. Defaults
        to 'False'.

    .PARAMETER EnableAutoConfiguration
        Use automatic configuration script for specifying proxy
        settings. Defaults to 'False'.

    .PARAMETER EnableManualProxy
        Use manual proxy server settings. Defaults to 'False'.

    .PARAMETER AutoConfigURL
        The URL of the automatic configuration script to specify
        the proxy settings. Should be specified if 'EnableAutoConfiguration'
        is 'True'.

    .PARAMETER ProxyServer
        The address and port of the manual proxy server to use.
        Should be specified if 'EnableManualProxy' is 'True'.

    .PARAMETER ProxyServerExceptions
        Bypass proxy server for addresses starting with addresses
        in this list.

    .PARAMETER ProxyServerBypassLocal
        Bypass proxy server for local addresses. Defaults to 'False'.
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
        [ValidateSet('Present','Absent')]
        [System.String]
        $Ensure = 'Present',

        [Parameter()]
        [ValidateSet('All','Default','Legacy')]
        [System.String]
        $ConnectionType = 'All',

        [Parameter()]
        [System.Boolean]
        $EnableAutoDetection = $false,

        [Parameter()]
        [System.Boolean]
        $EnableAutoConfiguration = $false,

        [Parameter()]
        [System.Boolean]
        $EnableManualProxy = $false,

        [Parameter()]
        [System.String]
        $AutoConfigURL,

        [Parameter()]
        [System.String]
        $ProxyServer,

        [Parameter()]
        [System.String[]]
        $ProxyServerExceptions = @(),

        [Parameter()]
        [System.Boolean]
        $ProxyServerBypassLocal = $false
    )

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
        $($script:localizedData.ApplyingProxySettingsMessage -f $Ensure)
        ) -join '')

    if ($Ensure -eq 'Absent')
    {
        # Remove all the Proxy Settings
        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.DisablingComputerProxyMessage)
            ) -join '')

        if ($ConnectionType -in ('All','Default'))
        {
            Remove-ItemProperty `
                -Path "HKLM:\$($script:connectionsRegistryKeyPath)" `
                -Name 'DefaultConnectionSettings' `
                -ErrorAction SilentlyContinue
        }

        if ($ConnectionType -in ('All','Legacy'))
        {
            Remove-ItemProperty `
                -Path "HKLM:\$($script:connectionsRegistryKeyPath)" `
                -Name 'SavedLegacySettings' `
                -ErrorAction SilentlyContinue
        }
    }
    else
    {
        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.EnablingComputerProxyMessage)
            ) -join '')

        # Generate the Proxy Settings binary value
        $convertToProxySettingsBinaryParameters = @{} + $PSBoundParameters

        $convertToProxySettingsBinaryParameters.Remove('IsSingleInstance')
        $convertToProxySettingsBinaryParameters.Remove('Ensure')
        $convertToProxySettingsBinaryParameters.Remove('ConnectionType')

        $proxySettings = ConvertTo-ProxySettingsBinary @convertToProxySettingsBinaryParameters

        if ($ConnectionType -in ('All','Default'))
        {
            Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.WritingComputerProxyBinarySettingsMessage -f 'DefaultConnectionSettings',($proxySettings -join ','))
                ) -join '')

            Set-BinaryRegistryValue `
                -Path "HKEY_LOCAL_MACHINE\$($script:connectionsRegistryKeyPath)" `
                -Name 'DefaultConnectionSettings' `
                -Value $proxySettings
        }

        if ($ConnectionType -in ('All','Legacy'))
        {
            Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.WritingComputerProxyBinarySettingsMessage -f 'SavedLegacySettings',($proxySettings -join ','))
                ) -join '')

            Set-BinaryRegistryValue `
                -Path "HKEY_LOCAL_MACHINE\$($script:connectionsRegistryKeyPath)" `
                -Name 'SavedLegacySettings' `
                -Value $proxySettings
        }
    }
} # Set-TargetResource

<#
    .SYNOPSIS
        Tests the current state of the proxy settings for
        the computer.

    .PARAMETER IsSingleInstance
        Specifies the resource is a single instance, the
        value must be 'Yes'.

    .PARAMETER Ensure
        Specifies if computer proxy settings should be set.
        Defaults to 'Present'.

    .PARAMETER ConnectionType
        Defines if the proxy settings should be configured
        for default connections, legacy connections or all
        connections. Defaults to 'All'.

    .PARAMETER EnableAutoDetection
        Enable automatic detection of the proxy settings. Defaults
        to 'False'.

    .PARAMETER EnableAutoConfiguration
        Use automatic configuration script for specifying proxy
        settings. Defaults to 'False'.

    .PARAMETER EnableManualProxy
        Use manual proxy server settings. Defaults to 'False'.

    .PARAMETER AutoConfigURL
        The URL of the automatic configuration script to specify
        the proxy settings. Should be specified if 'EnableAutoConfiguration'
        is 'True'.

    .PARAMETER ProxyServer
        The address and port of the manual proxy server to use.
        Should be specified if 'EnableManualProxy' is 'True'.

    .PARAMETER ProxyServerExceptions
        Bypass proxy server for addresses starting with addresses
        in this list.

    .PARAMETER ProxyServerBypassLocal
        Bypass proxy server for local addresses. Defaults to 'False'.
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
        [ValidateSet('Present','Absent')]
        [System.String]
        $Ensure = 'Present',

        [Parameter()]
        [ValidateSet('All','Default','Legacy')]
        [System.String]
        $ConnectionType = 'All',

        [Parameter()]
        [System.Boolean]
        $EnableAutoDetection = $false,

        [Parameter()]
        [System.Boolean]
        $EnableAutoConfiguration = $false,

        [Parameter()]
        [System.Boolean]
        $EnableManualProxy = $false,

        [Parameter()]
        [System.String]
        $AutoConfigURL,

        [Parameter()]
        [System.String]
        $ProxyServer,

        [Parameter()]
        [System.String[]]
        $ProxyServerExceptions = @(),

        [Parameter()]
        [System.Boolean]
        $ProxyServerBypassLocal = $false
    )

    Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
        $($script:localizedData.CheckingProxySettingsMessage -f $Ensure)
        ) -join '')

    [System.Boolean] $desiredConfigurationMatch = $true

    # Get the registry values in the Connections registry key
    $connectionsRegistryValues = Get-ItemProperty `
        -Path "HKLM:\$($script:connectionsRegistryKeyPath)" `
        -ErrorAction SilentlyContinue

    if ($Ensure -eq 'Absent')
    {
        # Check if any of the Proxy Settings need to be removed
        if ($ConnectionType -in ('All','Default'))
        {
            # Does the Default Connection Settings need to be removed?
            if ($connectionsRegistryValues.DefaultConnectionSettings)
            {
                Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                    $($script:localizedData.ComputerProxyBinarySettingsRequiresRemovalMessage -f 'DefaultConnectionSettings')
                    ) -join '')

                $desiredConfigurationMatch = $false
            }
        }

        # Does the Saved Legacy Settings need to be removed?
        if ($ConnectionType -in ('All','Legacy'))
        {
            if ($connectionsRegistryValues.SavedLegacySettings)
            {
                Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                    $($script:localizedData.ComputerProxyBinarySettingsRequiresRemovalMessage -f 'SavedLegacySettings')
                    ) -join '')

                $desiredConfigurationMatch = $false
            }
        }
    }
    else
    {
        $desiredValues = @{} + $PSBoundParameters

        $desiredValues.Remove('IsSingleInstance')
        $desiredValues.Remove('Ensure')
        $desiredValues.Remove('ConnectionType')

        if ($ConnectionType -in ('All','Default'))
        {
            # Check if the Default Connection proxy settings are in the desired state
            Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.CheckingComputerProxyBinarySettingsMessage -f 'DefaultConnectionSettings')
                ) -join '')

            if ($connectionsRegistryValues.DefaultConnectionSettings)
            {
                $defaultConnectionSettings = ConvertFrom-ProxySettingsBinary -ProxySettings $connectionsRegistryValues.DefaultConnectionSettings
            }
            else
            {
                $defaultConnectionSettings = @{}
            }

            $inDesiredState = Test-ProxySettings -CurrentValues $defaultConnectionSettings -DesiredValues $desiredValues

            if (-not $inDesiredState)
            {
                Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                    $($script:localizedData.ComputerProxyBinarySettingsNoMatchMessage -f 'DefaultConnectionSettings')
                    ) -join '')

                $desiredConfigurationMatch = $false
            }
        }

        if ($ConnectionType -in ('All','Legacy'))
        {
            # Check if the Saved Legacy proxy settings are in the desired state
            Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.CheckingComputerProxyBinarySettingsMessage -f 'SavedLegacySettings')
                ) -join '')

            if ($connectionsRegistryValues.SavedLegacySettings)
            {
                $savedLegacySettings = ConvertFrom-ProxySettingsBinary -ProxySettings $connectionsRegistryValues.SavedLegacySettings
            }
            else
            {
                $savedLegacySettings = @{}
            }

            $inDesiredState = Test-ProxySettings -CurrentValues $savedLegacySettings -DesiredValues $desiredValues

            if (-not $inDesiredState)
            {
                Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                    $($script:localizedData.ComputerProxyBinarySettingsNoMatchMessage -f 'SavedLegacySettings')
                    ) -join '')

                $desiredConfigurationMatch = $false
            }
        }
    }

    return $desiredConfigurationMatch
} # Test-TargetResource

<#
    .SYNOPSIS
        Sets a binary value in a Registry Key.

    .PARAMETER Path
        The path to the registry key containing the value.

    .PARAMETER Name
        The name of the registry value.

    .PARAMETER Value
        The value to put into the binary registry value.
#>
function Set-BinaryRegistryValue
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Path,

        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter(Mandatory = $true)]
        [System.Byte[]]
        $Value
    )

    $null = [Microsoft.Win32.Registry]::SetValue($Path, $Name, $Value, 'Binary')
}

<#
    .SYNOPSIS
        Checks if the current proxy setting values are in the desired
        state. Returns $true if in the desired state.

    .PARAMETER CurrentValues
        An object containing the current values of the Proxy Settings.

    .PARAMETER DesiredValues
        An object containing the desired values of the Proxy Settings.
#>
function Test-ProxySettings
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.Collections.Hashtable]
        $CurrentValues,

        [Parameter(Mandatory = $true)]
        [System.Object]
        $DesiredValues
    )

    [System.Boolean] $inState = $true

    $proxySettingsToCompare = @(
        'EnableManualProxy'
        'EnableAutoConfiguration'
        'EnableAutoDetection'
        'ProxyServer'
        'ProxyServerBypassLocal'
        'AutoConfigURL'
    )

    # Test the string and boolean values
    foreach ($proxySetting in $proxySettingsToCompare)
    {
        if ($DesiredValues.ContainsKey($proxySetting) -and ($DesiredValues.$proxySetting -ne $CurrentValues.$proxySetting))
        {
            Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
                $($script:localizedData.ProxySettingMismatchMessage -f $proxySetting,$CurrentValues.$proxySetting,$DesiredValues.$proxySetting)
                ) -join '')

            $inState = $false
        }
    }

    # Test the array value
    if ($DesiredValues.ContainsKey('ProxyServerExceptions') `
        -and $CurrentValues.ProxyServerExceptions `
        -and @(Compare-Object `
            -ReferenceObject $DesiredValues.ProxyServerExceptions `
            -DifferenceObject $CurrentValues.ProxyServerExceptions).Count -gt 0)
    {
        Write-Verbose -Message ( @("$($MyInvocation.MyCommand): "
            $($script:localizedData.ProxySettingMismatchMessage -f 'ProxyServerExceptions',($CurrentValues.ProxyServerExceptions -join ';'),($DesiredValues.ProxyServerExceptions -join ';'))
            ) -join '')

        $inState = $false
    }

    return $inState
}

<#
    .SYNOPSIS
        Get the length of a string in the format of an array
        of hexidecimal format strings.

    .PARAMETER Value
        The string to return the length for.
#>
function Get-StringLengthInHexBytes
{
    [CmdletBinding()]
    [OutputType([System.Object[]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [System.String]
        $Value
    )

    $hex = '{0:x8}' -f $Value.Length
    $stringLength = @()
    $stringLength += @('0x' + $hex.Substring(6,2))
    $stringLength += @('0x' + $hex.Substring(4,2))
    $stringLength += @('0x' + $hex.Substring(2,2))
    $stringLength += @('0x' + $hex.Substring(0,2))

    return $stringLength
}

<#
    .SYNOPSIS
        Gets an int32 from 4 little endian bytes containing in a
        byte array.

    .PARAMETER Bytes
        The bytes containing the little endian int32.
#>
function Get-Int32FromByteArray
{
    [CmdletBinding()]
    [OutputType([System.Int32])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.Byte[]]
        $Byte,

        [Parameter(Mandatory = $true)]
        [System.Int32]
        $StartByte
    )

    $value = [System.Int32] 0
    $value += [System.Int32] $Byte[$StartByte]
    $value += [System.Int32] $Byte[$StartByte + 1] -shl 8
    $value += [System.Int32] $Byte[$StartByte + 2] -shl 16
    $value += [System.Int32] $Byte[$StartByte + 3] -shl 24

    return $value
}

<#
    .SYNOPSIS
        Convert the proxy settings parameters to a Byte Array that
        can be used to populate the DefaultConnectionSettings and
        SavedLegacySettings registry settings.

    .PARAMETER EnableAutoDetection
        Enable automatic detection of the proxy settings. Defaults
        to 'False'.

    .PARAMETER EnableAutoConfiguration
        Use automatic configuration script for specifying proxy
        settings. Defaults to 'False'.

    .PARAMETER EnableManualProxy
        Use manual proxy server settings. Defaults to 'False'.

    .PARAMETER AutoConfigURL
        The URL of the automatic configuration script to specify
        the proxy settings. Should be specified if 'EnableAutoConfiguration'
        is 'True'.

    .PARAMETER ProxyServer
        The address and port of the manual proxy server to use.
        Should be specified if 'EnableManualProxy' is 'True'.

    .PARAMETER ProxyServerExceptions
        Bypass proxy server for addresses starting with addresses
        in this list.

    .PARAMETER ProxyServerBypassLocal
        Bypass proxy server for local addresses. Defaults to 'False'.
#>
function ConvertTo-ProxySettingsBinary
{
    [CmdletBinding()]
    [OutputType([System.Byte[]])]
    param
    (
        [Parameter()]
        [System.Boolean]
        $EnableAutoDetection = $false,

        [Parameter()]
        [System.Boolean]
        $EnableAutoConfiguration = $false,

        [Parameter()]
        [System.Boolean]
        $EnableManualProxy = $false,

        [Parameter()]
        [System.String]
        $AutoConfigURL,

        [Parameter()]
        [System.String]
        $ProxyServer,

        [Parameter()]
        [System.String[]]
        $ProxyServerExceptions = @(),

        [Parameter()]
        [System.Boolean]
        $ProxyServerBypassLocal = $false
    )

    [System.Byte[]] $proxySettings = @(0x46, 0x0, 0x0, 0x0, 0x8, 0x0, 0x0, 0x0, 0x1, 0x0, 0x0, 0x0)

    if ($EnableManualProxy)
    {
        $proxySettings[8] = $proxySettings[8] + 2
    }

    if ($EnableAutoConfiguration)
    {
        $proxySettings[8] = $proxySettings[8] + 4
    }

    if ($EnableAutoDetection)
    {
        $proxySettings[8] = $proxySettings[8] + 8
    }

    if ($PSBoundParameters.ContainsKey('ProxyServer'))
    {
        $proxySettings += Get-StringLengthInHexBytes -Value $ProxyServer
        $proxySettings += [Byte[]][Char[]] $ProxyServer
    }
    else
    {
        $proxySettings += @(0x0, 0x0, 0x0, 0x0)
    }

    if ($ProxyServerBypassLocal -eq $true)
    {
        $ProxyServerExceptions += @('<local>')
    }

    if ($ProxyServerExceptions.Count -gt 0)
    {
        $proxyServerExceptionsString = $ProxyServerExceptions -join ';'
        $proxySettings += Get-StringLengthInHexBytes -Value $proxyServerExceptionsString
        $proxySettings += [Byte[]][Char[]] $proxyServerExceptionsString
    }
    else
    {
        $proxySettings += @(0x0, 0x0, 0x0, 0x0)
    }

    if ($PSBoundParameters.ContainsKey('AutoConfigURL'))
    {
        $proxySettings += Get-StringLengthInHexBytes -Value $AutoConfigURL
        $proxySettings += [Byte[]][Char[]] $AutoConfigURL
    }

    $proxySettings += @(0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0)
    $proxySettings += @(0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0)

    return [System.Byte[]] $proxySettings
}

<#
    .SYNOPSIS
        Convert from a Byte Array pulled from the proxy settings
        DefaultConnectionSettings and SavedLegacySettings in the
        registry into an object.

    .PARAMETER ProxySettings
        The binary extracted from the registry key
        DefaultConnectionSettings or SavedLegacySettings.

#>
function ConvertFrom-ProxySettingsBinary
{
    [CmdletBinding()]
    [OutputType([PSObject])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.Byte[]]
        $ProxySettings
    )

    $proxyParameters = @{}

    if ($ProxySettings.Count -gt 0)
    {
        # Do a smoke test on the binary to check it looks valid
        if ($ProxySettings[0] -ne 0x46)
        {
            New-InvalidOperationException `
                -Message ($script:localizedData.ProxySettingsBinaryInvalidError -f $ProxySettings[0])
        }

        # Figure out the proxy settings that are enabled
        $proxyBits = $ProxySettings[8]

        $enableManualProxy = $false
        $enableAutoConfiguration = $false
        $enableAutoDetection = $false

        if (($proxyBits -band 0x2) -gt 0)
        {
            $enableManualProxy = $true
        }

        if (($proxyBits -band 0x4) -gt 0)
        {
            $enableAutoConfiguration = $true
        }

        if (($proxyBits -band 0x8) -gt 0)
        {
            $enableAutoDetection = $true
        }

        $proxyParameters.Add('EnableManualProxy',$enableManualProxy)
        $proxyParameters.Add('EnableAutoConfiguration',$enableAutoConfiguration)
        $proxyParameters.Add('EnableAutoDetection',$enableAutoDetection)

        $stringPointer = 12

        # Extract the Proxy Server string
        $proxyServer = ''
        $stringLength = Get-Int32FromByteArray -Byte $ProxySettings -StartByte $stringPointer
        $stringPointer += 4

        if ($stringLength -gt 0)
        {
            $stringBytes = New-Object -TypeName Byte[] -ArgumentList $stringLength
            $null = [System.Buffer]::BlockCopy($ProxySettings,$stringPointer,$stringBytes,0,$stringLength)
            $proxyServer = [System.Text.Encoding]::ASCII.GetString($stringBytes)
            $stringPointer += $stringLength
        }

        $proxyParameters.Add('ProxyServer',$proxyServer)

        # Extract the Proxy Server Exceptions string
        $proxyServerExceptions = @()
        $stringLength = Get-Int32FromByteArray -Byte $ProxySettings -StartByte $stringPointer
        $stringPointer += 4

        if ($stringLength -gt 0)
        {
            $stringBytes = New-Object -TypeName Byte[] -ArgumentList $stringLength
            $null = [System.Buffer]::BlockCopy($ProxySettings,$stringPointer,$stringBytes,0,$stringLength)
            $proxyServerExceptionsString = [System.Text.Encoding]::ASCII.GetString($stringBytes)
            $stringPointer += $stringLength
            $proxyServerExceptions = [System.String[]] ($proxyServerExceptionsString -split ';')
        }

        if ($proxyServerExceptions.Contains('<local>'))
        {
            $proxyServerExceptions = $proxyServerExceptions | Where-Object -FilterScript { $_ -ne '<local>' }
            $proxyParameters.Add('ProxyServerBypassLocal',$true)
        }
        else
        {
            $proxyParameters.Add('ProxyServerBypassLocal',$false)
        }

        $proxyParameters.Add('ProxyServerExceptions',$proxyServerExceptions)

        # Extract the Auto Config URL string
        $autoConfigURL = ''
        $stringLength = Get-Int32FromByteArray -Byte $ProxySettings -StartByte $stringPointer
        $stringPointer += 4

        if ($stringLength -gt 0)
        {
            $stringBytes = New-Object -TypeName Byte[] -ArgumentList $stringLength
            $null = [System.Buffer]::BlockCopy($ProxySettings,$stringPointer,$stringBytes,0,$stringLength)
            $autoConfigURL = [System.Text.Encoding]::ASCII.GetString($stringBytes)
            $stringPointer += $stringLength
        }

        $proxyParameters.Add('AutoConfigURL',$autoConfigURL)
    }

    return [PSObject] $proxyParameters
}

Export-ModuleMember -function *-TargetResource
