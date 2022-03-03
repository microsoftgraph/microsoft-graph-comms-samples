$modulePath = Join-Path -Path (Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent) -ChildPath 'Modules'

# Import the Networking Common Modules
Import-Module -Name (Join-Path -Path $modulePath `
        -ChildPath (Join-Path -Path 'NetworkingDsc.Common' `
            -ChildPath 'NetworkingDsc.Common.psm1'))

Import-Module -Name (Join-Path -Path $modulePath -ChildPath 'DscResource.Common')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -DefaultUICulture 'en-US'

#region check NetBiosSetting enum loaded, if not load
try
{
    [void][System.Reflection.Assembly]::GetAssembly([NetBiosSetting])
}
catch
{
    Add-Type -TypeDefinition @'
    public enum NetBiosSetting
    {
       Default,
       Enable,
       Disable
    }
'@
}
#endregion

<#
    .SYNOPSIS
        Returns the current state of the Net Bios on an interface.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface. Supports the use of '*' and '%'.

    .PARAMETER Setting
        Specifies if NetBIOS should be enabled or disabled or obtained from
        the DHCP server (Default). If static IP, Enable NetBIOS.

        Parameter value is ignored.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Default', 'Enable', 'Disable')]
        [System.String]
        $Setting
    )

    Write-Verbose -Message ($script:localizedData.GettingNetBiosSettingMessage -f $InterfaceAlias)

    $win32NetworkAdapterFilter = Format-Win32NetworkAdapterFilterByNetConnectionId -InterfaceAlias $InterfaceAlias

    $netAdapter = Get-CimInstance `
        -ClassName Win32_NetworkAdapter `
        -Filter $win32NetworkAdapterFilter

    if ($netAdapter)
    {
        Write-Verbose -Message ($script:localizedData.InterfaceDetectedMessage -f $InterfaceAlias, ($netAdapter.InterfaceIndex -Join ','))
    }
    else
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.InterfaceNotFoundError -f $InterfaceAlias)
    }

    <#
        If a wildcard was specified for the InterfaceAlias then
        more than one adapter may be returned. If more than one
        adapter is returned then the NetBiosSetting value should
        be returned for the first adapter that does not match
        the desired value. This is to ensure that when testing
        the resource state it will return a mismatch if any adapters
        don't have the correct setting.
    #>
    foreach ($netAdapterItem in $netAdapter)
    {
        $netAdapterConfig = $netAdapterItem | Get-CimAssociatedInstance `
            -ResultClassName Win32_NetworkAdapterConfiguration `
            -ErrorAction Stop

        $tcpipNetbiosOptions = $netAdapterConfig.TcpipNetbiosOptions

        if ($tcpipNetbiosOptions)
        {
            $interfaceSetting = $([NetBiosSetting].GetEnumValues()[$tcpipNetbiosOptions])
        }
        else
        {
            $interfaceSetting = 'Default'
        }

        Write-Verbose -Message ($script:localizedData.CurrentNetBiosSettingMessage -f $netAdapterItem.Name, $interfaceSetting)

        if ($interfaceSetting -ne $Setting)
        {
            $Setting = $interfaceSetting
            break
        }
    }

    return @{
        InterfaceAlias = $InterfaceAlias
        Setting        = $Setting
    }
}

<#
    .SYNOPSIS
        Sets the state of the Net Bios on an interface.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface. Supports the use of '*' and '%'.

    .PARAMETER Setting
        Specifies if NetBIOS should be enabled or disabled or obtained from
        the DHCP server (Default). If static IP, Enable NetBIOS.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Default', 'Enable', 'Disable')]
        [System.String]
        $Setting
    )

    Write-Verbose -Message ($script:localizedData.SettingNetBiosSettingMessage -f $InterfaceAlias)

    $win32NetworkAdapterFilter = Format-Win32NetworkAdapterFilterByNetConnectionId -InterfaceAlias $InterfaceAlias

    $netAdapter = Get-CimInstance `
        -ClassName Win32_NetworkAdapter `
        -Filter $win32NetworkAdapterFilter

    if ($netAdapter)
    {
        Write-Verbose -Message ($script:localizedData.InterfaceDetectedMessage -f $InterfaceAlias, ($netAdapter.InterfaceIndex -Join ','))
    }
    else
    {
        New-InvalidOperationException `
            -Message ($script:localizedData.InterfaceNotFoundError -f $InterfaceAlias)
    }

    foreach ($netAdapterItem in $netAdapter)
    {
        $netAdapterConfig = $netAdapterItem | Get-CimAssociatedInstance `
            -ResultClassName Win32_NetworkAdapterConfiguration `
            -ErrorAction Stop

        if ($Setting -eq [NetBiosSetting]::Default)
        {
            Write-Verbose -Message ($script:localizedData.ResetToDefaultMessage -f $netAdapterItem.Name)

            # If DHCP is not enabled, SetTcpipNetbios CIM Method won't take 0 so overwrite registry entry instead.
            $setItemPropertyParameters = @{
                Path  = "HKLM:\SYSTEM\CurrentControlSet\Services\NetBT\Parameters\Interfaces\Tcpip_$($NetAdapterConfig.SettingID)"
                Name  = 'NetbiosOptions'
                Value = 0
            }
            $null = Set-ItemProperty @setItemPropertyParameters
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.SetNetBiosMessage -f $netAdapterItem.Name, $Setting)

            $result = $netAdapterConfig |
                Invoke-CimMethod `
                    -MethodName SetTcpipNetbios `
                    -ErrorAction Stop `
                    -Arguments @{
                        TcpipNetbiosOptions = [uint32][NetBiosSetting]::$Setting.value__
                    }

            if ($result.ReturnValue -ne 0)
            {
                New-InvalidOperationException `
                    -Message ($script:localizedData.FailedUpdatingNetBiosError -f $netAdapterItem.Name, $result.ReturnValue, $Setting)
            }
        }
    }
}

<#
    .SYNOPSIS
        Tests the current state the Net Bios on an interface.

    .PARAMETER InterfaceAlias
        Specifies the alias of a network interface. Supports the use of '*' and '%'.

    .PARAMETER Setting
        Specifies if NetBIOS should be enabled or disabled or obtained from
        the DHCP server (Default). If static IP, Enable NetBIOS.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $InterfaceAlias,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Default', 'Enable', 'Disable')]
        [System.String]
        $Setting
    )

    Write-Verbose -Message ($script:localizedData.TestingNetBiosSettingMessage -f $InterfaceAlias)

    $currentState = Get-TargetResource @PSBoundParameters

    return Test-DscParameterState -CurrentValues $currentState -DesiredValues $PSBoundParameters
}

Export-ModuleMember -Function *-TargetResource
