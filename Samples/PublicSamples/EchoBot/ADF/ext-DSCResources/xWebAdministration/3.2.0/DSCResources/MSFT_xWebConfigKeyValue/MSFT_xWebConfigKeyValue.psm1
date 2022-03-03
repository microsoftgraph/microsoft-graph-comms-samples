$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'
$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'xWebAdministration.Common'

Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'xWebAdministration.Common.psm1')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_xWebConfigKeyValue'

<#
    .SYNOPSIS
        Gets the value of the specified key in the config file
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String] $WebsitePath,

        [Parameter(Mandatory = $true)]
        [ValidateSet('AppSettings')]
        [System.String] $ConfigSection,

        [Parameter(Mandatory = $true)]
        [String] $Key
    )

    Write-Verbose `
        -Message ($script:localizedData.VerboseGetTargetCheckingTarget -f $Key, $ConfigSection, $WebsitePath )

    $existingValue = Get-ItemValue `
                        -Key $Key `
                        -IsAttribute $false `
                        -WebsitePath $WebsitePath `
                        -ConfigSection $ConfigSection

    if ( $null -eq $existingValue )
    {
        Write-Verbose `
            -Message ($script:localizedData.VerboseGetTargetAttributeCheck -f $Key )

        $existingValue = Get-ItemValue `
                            -Key $Key `
                            -IsAttribute $true `
                            -WebsitePath $WebsitePath `
                            -ConfigSection $ConfigSection
    }

    if ( $existingValue.Length -eq 0 )
    {
        Write-Verbose `
            -Message ($script:localizedData.VerboseGetTargetKeyNotFound -f $Key )

         return @{
             Ensure = 'Absent'
             Key = $Key
             Value = $existingValue
        }
    }

    Write-Verbose `
        -Message ($script:localizedData.VerboseGetTargetKeyFound -f $Key )

    return @{
        Ensure = 'Present'
        Key = $Key
        Value = $existingValue
    }
}

<#
    .SYNOPSIS
        Sets the value of the specified key in the config file
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String] $WebsitePath,

        [Parameter(Mandatory = $true)]
        [ValidateSet('AppSettings')]
        [System.String] $ConfigSection,

        [Parameter(Mandatory = $true)]
        [String] $Key,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [System.String] $Ensure = 'Present',

        [Parameter()]
        [String] $Value,

        [Parameter()]
        [System.Boolean] $IsAttribute
    )

    if ($Ensure -eq 'Present')
    {
        Write-Verbose `
            -Message ($script:localizedData.VerboseSetTargetCheckingKey -f $Key )

        $existingValue = Get-ItemValue `
                            -Key $Key `
                            -IsAttribute $IsAttribute `
                            -WebsitePath $WebsitePath `
                            -ConfigSection $ConfigSection

        if ( (-not $IsAttribute -and ($null -eq $existingValue) ) `
                -or ( $IsAttribute -and ($existingValue.Length -eq 0) ) )
        {
            Write-Verbose `
                -Message ($script:localizedData.VerboseSetTargetAddItem -f $Key )

            Add-Item `
                -Key $Key `
                -Value $Value `
                -IsAttribute $IsAttribute `
                -WebsitePath $WebsitePath `
                -ConfigSection $ConfigSection
        }
        else
        {
            $propertyName = 'value'

            if ( $IsAttribute )
            {
                $propertyName = $Key
            }

            Write-Verbose `
                -Message ($script:localizedData.VerboseSetTargetEditItem -f $Key )

            Edit-Item `
                -PropertyName $propertyName `
                -OldValue $Key `
                -NewValue $Value `
                -IsAttribute $IsAttribute `
                -WebsitePath $WebsitePath `
                -ConfigSection $ConfigSection
        }
    }
    else
    {
        Write-Verbose `
            -Message ($script:localizedData.VerboseSetTargetRemoveItem -f $Key )

        Remove-Item `
            -Key $Key `
            -IsAttribute $IsAttribute `
            -WebsitePath $WebsitePath `
            -ConfigSection $ConfigSection
    }
}

<#
    .SYNOPSIS
        Tests the value of the specified key in the config file
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String] $WebsitePath,

        [Parameter(Mandatory = $true)]
        [ValidateSet('AppSettings')]
        [System.String] $ConfigSection,

        [Parameter(Mandatory = $true)]
        [String] $Key,

        [Parameter()]
        [String] $Value,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [System.String] $Ensure = 'Present',

        [Parameter()]
        [System.Boolean] $IsAttribute
    )

    if ( -not $PSBoundParameters.ContainsKey('IsAttribute') )
    {
        $IsAttribute = $false
    }

   Write-Verbose `
        -Message ($script:localizedData.VerboseTestTargetCheckingTarget -f $Key, $ConfigSection, $WebsitePath )

    $existingValue = Get-ItemValue `
                        -Key $Key `
                        -IsAttribute $IsAttribute `
                        -WebsitePath $WebsitePath `
                        -ConfigSection $ConfigSection

    if ( $Ensure -eq 'Present' )
    {
        if ( ( $null -eq $existingValue ) -or ( $existingValue -ne $Value ) `
                -or ($existingValue.Length -eq 0) )
        {
            Write-Verbose `
                -Message ($script:localizedData.VerboseTestTargetKeyNotFound -f $Key )
            return $false
        }
    }
    else
    {
        if ( ( $null -ne $existingValue ) -or ( $existingValue.Length -ne 0 ) )
        {
             Write-Verbose `
                -Message ($script:localizedData.VerboseTestTargetKeyNotFound -f $Key )

            return $false
        }
    }

    Write-Verbose `
            -Message ($script:localizedData.VerboseTestTargetKeyWasFound -f $Key)

    return $true
}

# region Helper Functions

function Add-Item
{
    param
    (
        [Parameter()]
        [string] $Key,

        [Parameter()]
        [string] $Value,

        [Parameter()]
        [Boolean] $isAttribute,

        [Parameter()]
        [string] $WebsitePath,

        [Parameter()]
        [string] $ConfigSection
    )

    $itemCollection = @{
        Key   = $Key;
        Value = $Value;
    }

    if ( -not $isAttribute )
    {
        Add-WebConfigurationProperty `
            -Filter $ConfigSection `
            -Name '.' `
            -Value $itemCollection `
            -PSPath $WebsitePath
    }
    else
    {
        Set-WebConfigurationProperty `
            -Filter $ConfigSection `
            -PSPath $WebsitePath `
            -Name $Key `
            -Value $Value `
            -WarningAction Stop
    }
}

function Edit-Item
{
    param
    (
        [Parameter()]
        [string] $PropertyName,

        [Parameter()]
        [string] $OldValue,

        [Parameter()]
        [string] $NewValue,

        [Parameter()]
        [Boolean] $IsAttribute,

        [Parameter()]
        [string] $WebsitePath,

        [Parameter()]
        [string] $ConfigSection
    )

    if ( -not $IsAttribute )
    {
        $filter = "$ConfigSection/add[@key=`'$OldValue`']"

        Set-WebConfigurationProperty -Filter $filter `
            -PSPath $WebsitePath `
            -Name $PropertyName `
            -Value $NewValue `
            -WarningAction Stop
    }
    else
    {
        Set-WebConfigurationProperty `
            -Filter $ConfigSection `
            -PSPath $WebsitePath `
            -Name $PropertyName `
            -Value $NewValue `
            -WarningAction Stop
    }
}

function Remove-Item
{
    param
    (
        [Parameter()]
        [string] $Key,

        [Parameter()]
        [Boolean] $IsAttribute,

        [Parameter()]
        [string] $WebsitePath,

        [Parameter()]
        [string] $ConfigSection
    )

    if ( -not $isAttribute )
    {
        $filter = "$ConfigSection/add[@key=`'$key`']"
        Clear-WebConfiguration `
            -Filter $filter `
            -PSPath $WebsitePath `
            -WarningAction Stop
    }
    else
    {
        $filter = "$ConfigSection/@$key"

        <#
            This is a workaround to ensure if appSettings has no collection
            and we try to delete the only attribute, the entire node is not deleted.
            if we try removing the only attribute even if there is one collection item,
            the node is preserved.
        #>
        Add-Item `
            -Key 'dummyKey' `
            -Value 'dummyValue' `
            -IsAttribute $false `
            -WebsitePath $WebsitePath `
            -ConfigSection $ConfigSection

        Clear-WebConfiguration `
            -Filter $filter `
            -PSPath $WebsitePath `
            -WarningAction Stop

        Remove-Item `
            -Key 'dummyKey' `
            -IsAttribute $false `
            -WebsitePath $WebsitePath `
            -ConfigSection $ConfigSection
    }
}

function Get-ItemValue
{
    param
    (
        [Parameter()]
        [string] $Key,

        [Parameter()]
        [Boolean] $isAttribute,

        [Parameter()]
        [string] $WebsitePath,

        # If this is null $value.Value will be null
        [Parameter()]
        [string] $ConfigSection
    )

    if (-not $isAttribute)
    {
        $filter = "$ConfigSection/add[@key=`'$key`']"
        $value = Get-WebConfigurationProperty `
                    -Filter $filter `
                    -Name 'value' `
                    -PSPath $WebsitePath
    }
    else
    {
        $value = Get-WebConfigurationProperty `
                    -Filter $ConfigSection `
                    -Name "$Key" `
                    -PSPath $WebsitePath
    }

    return $value.Value
}

# endregion

Export-ModuleMember -Function *-TargetResource
