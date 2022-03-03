$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'
$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'xWebAdministration.Common'

Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'xWebAdministration.Common.psm1')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_xWebConfigProperty'

<#
.SYNOPSIS
    Gets the current value of the target resource property.

.PARAMETER WebsitePath
    Required. Path to website location (IIS or WebAdministration format).

.PARAMETER Filter
    Required. Filter used to locate property to update.

.PARAMETER PropertyName
    Required. Name of the property to update.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $WebsitePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Filter,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $PropertyName
    )
    # Retrieve the value of the existing property if present.
    Write-Verbose `
        -Message ($script:localizedData.VerboseTargetCheckingTarget -f $PropertyName, $Filter, $WebsitePath )

    $existingValue = Get-ItemValue `
                        -WebsitePath $WebsitePath `
                        -Filter $Filter `
                        -PropertyName $PropertyName

    $result = @{
        WebsitePath = $WebsitePath
        Filter = $Filter
        PropertyName = $PropertyName
        Ensure = 'Present'
        Value = $existingValue
    }

    if (-not($existingValue))
    {
        # Property was not found.
        Write-Verbose `
            -Message ($script:localizedData.VerboseTargetPropertyNotFound -f $PropertyName )

        $result.Ensure = 'Absent'
    }
    else
    {
        # Property was found.
        Write-Verbose `
            -Message ($script:localizedData.VerboseTargetPropertyFound -f $PropertyName )
    }

    return $result
}

<#
.SYNOPSIS
    Sets the value of the target resource property.

.PARAMETER WebsitePath
    Required. Path to website location (IIS or WebAdministration format).

.PARAMETER Filter
    Required. Filter used to locate property to update.

.PARAMETER PropertyName
    Required. Name of the property to update.

.PARAMETER Value
    Value of the property to update.

.PARAMETER Ensure
    Present or Absent. Defaults to Present.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $WebsitePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Filter,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $PropertyName,

        [Parameter()]
        [string]
        $Value,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [string]
        $Ensure = 'Present'
    )
    if ($Ensure -eq 'Present')
    {
        # Property needs to be updated.
        Write-Verbose `
            -Message ($script:localizedData.VerboseSetTargetEditItem -f $PropertyName )

        $propertyType = Get-ItemPropertyType -WebsitePath $WebsitePath -Filter $Filter -PropertyName $PropertyName

        if ($propertyType -match 'Int32|Int64')
        {
            $setValue = Convert-PropertyValue -PropertyType $propertyType -InputValue $Value
        }
        else
        {
            $setValue = $Value
        }

        Set-WebConfigurationProperty `
            -Filter $Filter `
            -PSPath $WebsitePath `
            -Name $PropertyName `
            -Value $setValue `
            -WarningAction Stop
    }
    else
    {
        # Property needs to be removed.
        Write-Verbose `
            -Message ($script:localizedData.VerboseSetTargetRemoveItem -f $PropertyName )

        Clear-WebConfiguration `
                -Filter "$($Filter)/@$($PropertyName)" `
                -PSPath $WebsitePath `
                -WarningAction Stop
    }
}

<#
.SYNOPSIS
    Tests the value of the target resource property.

.PARAMETER WebsitePath
    Required. Path to website location (IIS or WebAdministration format).

.PARAMETER Filter
    Required. Filter used to locate property to update.

.PARAMETER PropertyName
    Required. Name of the property to update.

.PARAMETER Value
    Value of the property to update.

.PARAMETER Ensure
    Present or Absent. Defaults to Present.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $WebsitePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Filter,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $PropertyName,

        [Parameter()]
        [string]
        $Value,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [string]
        $Ensure = 'Present'
    )
    # Retrieve the value of the existing property if present.
    Write-Verbose `
        -Message ($script:localizedData.VerboseTargetCheckingTarget -f $PropertyName, $Filter, $WebsitePath )

    $targetResource = Get-TargetResource `
                        -WebsitePath $WebsitePath `
                        -Filter $Filter `
                        -PropertyName $PropertyName

    if ($Ensure -eq 'Present')
    {
        if ( ($null -eq $targetResource.Value) -or ($targetResource.Value.ToString() -ne $Value) )
        {
            # Property was not found or didn't have expected value.
            Write-Verbose `
                -Message ($script:localizedData.VerboseTargetPropertyNotFound -f $PropertyName )

            return $false
        }
    }
    else
    {
        if ( ($null -ne $targetResource.Value) -and ($targetResource.Value.ToString().Length -ne 0 ) )
        {
            # Property was found.
                Write-Verbose `
                -Message ($script:localizedData.VerboseTargetPropertyFound -f $PropertyName )

            return $false
        }
    }

    Write-Verbose `
            -Message ($script:localizedData.VerboseTargetPropertyFound -f $PropertyName)

    return $true
}

# region Helper Functions

<#
.SYNOPSIS
    Gets the current value of the property.

.PARAMETER WebsitePath
    Required. Path to website location (IIS or WebAdministration format).

.PARAMETER Filter
    Required. Filter used to locate property to retrieve.

.PARAMETER PropertyName
    Required. Name of the property to retrieve.
#>
function Get-ItemValue
{
    [CmdletBinding()]
    [OutputType([System.Object])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $WebsitePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Filter,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $PropertyName
    )
    # Retrieve the value of the specified property if present.
    $value = Get-WebConfigurationProperty `
                -PSPath $WebsitePath `
                -Filter $Filter `
                -Name $PropertyName

    # Return the value of the property if located.
    return Get-WebConfigurationPropertyValue -WebConfigurationPropertyObject $value
}

<#
.SYNOPSIS
    Gets the current data type of the property.

.PARAMETER WebsitePath
    Path to website location (IIS or WebAdministration format).

.PARAMETER Filter
    Filter used to locate property to retrieve.

.PARAMETER PropertyName
    Name of the property to retrieve.
#>
function Get-ItemPropertyType
{
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $WebsitePath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Filter,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $PropertyName
    )

    $webConfiguration = Get-WebConfiguration -Filter $Filter -PsPath $WebsitePath

    $property = $webConfiguration.Schema.AttributeSchemas | Where-Object -FilterScript {$_.Name -eq $propertyName}

    return $property.ClrType.Name
}

<#
.SYNOPSIS
    Converts the property from string to appropriate data type.

.PARAMETER PropertyType
    Property type to be converted to.

.PARAMETER InputValue
    Value to be converted.
#>
function Convert-PropertyValue
{
    [CmdletBinding()]
    [OutputType([System.ValueType])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]
        $PropertyType,

        [Parameter(Mandatory = $true)]
        [string]
        $InputValue
    )

    switch ($PropertyType )
    {
        'Int32'
        {
            [Int32] $value = [convert]::ToInt32($InputValue, 10)
        }
        'UInt32'
        {
            [UInt32] $value = [convert]::ToUInt32($InputValue, 10)
        }
        'Int64'
        {
            [Int64] $value = [convert]::ToInt64($InputValue, 10)
        }
    }

    return $value
}

# endregion

Export-ModuleMember -Function *-TargetResource
