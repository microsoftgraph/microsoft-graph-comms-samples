$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'
$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'xWebAdministration.Common'

Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'xWebAdministration.Common.psm1')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_xWebConfigPropertyCollection'


<#
.SYNOPSIS
    Gets the current value of the target resource property.

.PARAMETER WebsitePath
    Required. Path to website location (IIS or WebAdministration format).

.PARAMETER Filter
    Required. Filter used to locate property collection to update. Use '.' for root.

.PARAMETER CollectionName
    Required. Name of the property collection to update.

.PARAMETER ItemName
    Required. Name of the property collection item to update.

.PARAMETER ItemKeyName
    Required. Name of the key of the property collection item to update.

.PARAMETER ItemKeyValue
    Required. Value of the key of the property collection item to update.

.PARAMETER ItemPropertyName
    Required. Name of the property of the property collection item to update.
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
        $CollectionName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemKeyName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemKeyValue,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemPropertyName
    )
    # Retrieve the values of the existing property collection item if present.
    Write-Verbose `
        -Message ($script:localizedData.VerboseTargetCheckingTarget -f $ItemPropertyName, $CollectionName, $ItemName, $ItemKeyName, $ItemKeyValue, $Filter, $WebsitePath )

    $existingItem = Get-ItemValues `
                        -WebsitePath $WebsitePath `
                        -Filter $Filter `
                        -CollectionName $CollectionName `
                        -ItemName $ItemName `
                        -ItemKeyName $ItemKeyName `
                        -ItemKeyValue $ItemKeyValue

    $result = @{
        WebsitePath = $WebsitePath
        Filter = $Filter
        CollectionName = $CollectionName
        ItemName = $ItemName
        ItemKeyName = $ItemKeyName
        ItemKeyValue = $ItemKeyValue
        ItemPropertyName = $ItemPropertyName
        Ensure = 'Present'
        ItemPropertyValue = $null
    }

    if ($null -eq $existingItem)
    {
        # Property collection item with specified key was not found.
        Write-Verbose `
            -Message ($script:localizedData.VerboseTargetItemNotFound -f $CollectionName, $ItemName, $ItemKeyName, $ItemKeyValue )

        $result.Ensure = 'Absent'
        $result.ItemPropertyValue = $null
    }
    elseif ($existingItem.Keys -notcontains $ItemPropertyName)
    {
        # Property collection item with specified key was found, but property was not present.
        Write-Verbose `
            -Message ($script:localizedData.VerboseTargetPropertyNotFound -f $ItemPropertyName )

        $result.Ensure = 'Absent'
        $result.ItemPropertyValue = $null
    }
    else
    {
        # Property collection item with specified key was found.
        Write-Verbose `
            -Message ($script:localizedData.VerboseTargetPropertyFound -f $ItemPropertyName )

        $result.Ensure = 'Present'
        $result.ItemPropertyValue = $existingItem[$ItemPropertyName].ToString()
    }
    return $result
}

<#
.SYNOPSIS
    Sets the value of the target resource property.

.PARAMETER WebsitePath
    Required. Path to website location (IIS or WebAdministration format).

.PARAMETER Filter
    Required. Filter used to locate property collection to update. Use '.' for root.

.PARAMETER CollectionName
    Required. Name of the property collection to update.

.PARAMETER ItemName
    Required. Name of the property collection item to update.

.PARAMETER ItemKeyName
    Required. Name of the key of the property collection item to update.

.PARAMETER ItemKeyValue
    Required. Value of the key of the property collection item to update.

.PARAMETER ItemPropertyName
    Required. Name of the property of the property collection item to update.

.PARAMETER ItemPropertyValue
    Value of the property of the property collection item to update.

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
        $CollectionName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemKeyName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemKeyValue,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemPropertyName,

        [Parameter()]
        [string]
        $ItemPropertyValue,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [string]
        $Ensure = 'Present'
    )
    if ($Ensure -eq 'Present')
    {
        # Retrieve the values of the existing property collection item if present.
        Write-Verbose `
            -Message ($script:localizedData.VerboseTargetCheckingTarget -f $ItemPropertyName, $CollectionName, $ItemName, $ItemKeyName, $ItemKeyValue, $Filter, $WebsitePath )

        $existingItem = Get-ItemValues `
                            -WebsitePath $WebsitePath `
                            -Filter $Filter `
                            -CollectionName $CollectionName `
                            -ItemName $ItemName `
                            -ItemKeyName $ItemKeyName `
                            -ItemKeyValue $ItemKeyValue

        $propertyType = Get-CollectionItemPropertyType -WebsitePath $WebsitePath -Filter "$Filter/$CollectionName" -PropertyName $ItemPropertyName -AddElement $ItemName

        if ($propertyType -match 'Int32|Int64')
        {
            $setItemPropertyValue = Convert-PropertyValue -PropertyType $propertyType -InputValue $ItemPropertyValue
        }
        else
        {
            $setItemPropertyValue = $ItemPropertyValue
        }

        if (-not($existingItem))
        {
            # Property collection item with specified key was not found.
            Write-Verbose `
                -Message ($script:localizedData.VerboseSetTargetAddItem -f $CollectionName, $ItemName, $ItemKeyName, $ItemKeyValue, $ItemPropertyName )

            $filter = "$($Filter)/$($CollectionName)"
            # Use Add- in this case to add the element (including the key/value) and also the specified property name/value.
            Add-WebConfigurationProperty `
                -PSPath $WebsitePath `
                -Filter $filter `
                -Name '.' `
                -Value @{
                    $ItemKeyName = $ItemKeyValue
                    $ItemPropertyName = $setItemPropertyValue
                }
        }
        else
        {
            # Property collection item with specified key was found.
            Write-Verbose `
                -Message ($script:localizedData.VerboseSetTargetEditItem -f $CollectionName, $ItemName, $ItemKeyName, $ItemKeyValue, $ItemPropertyName )

            $filter = "$($Filter)/$($CollectionName)/$($ItemName)[@$($ItemKeyName)='$($ItemKeyValue)']"
            # Use Set- in this case to update the specified property of the element with the specified key/value.
            Set-WebConfigurationProperty `
                -PSPath $WebsitePath `
                -Filter $filter `
                -Name $ItemPropertyName `
                -Value $setItemPropertyValue
        }
    }
    else
    {
        # Remove the specified property from the element with the specified key/value.
        Write-Verbose `
            -Message ($script:localizedData.VerboseSetTargetRemoveItem -f $ItemPropertyName )

        $filter = "$($Filter)/$($CollectionName)"
        Remove-WebConfigurationProperty `
            -PSPath $WebsitePath `
            -Filter $filter `
            -Name '.' `
            -AtElement @{
                $ItemKeyName = $ItemKeyValue
            }
    }
}

<#
.SYNOPSIS
    Tests the value of the target resource property.

.PARAMETER WebsitePath
    Required. Path to website location (IIS or WebAdministration format).

.PARAMETER Filter
    Required. Filter used to locate property collection to update. Use '.' for root.

.PARAMETER CollectionName
    Required. Name of the property collection to update.

.PARAMETER ItemName
    Required. Name of the property collection item to update.

.PARAMETER ItemKeyName
    Required. Name of the key of the property collection item to update.

.PARAMETER ItemKeyValue
    Required. Value of the key of the property collection item to update.

.PARAMETER ItemPropertyName
    Required. Name of the property of the property collection item to update.

.PARAMETER ItemPropertyValue
    Value of the property of the property collection item to update.

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
        $CollectionName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemKeyName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemKeyValue,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemPropertyName,

        [Parameter()]
        [string]
        $ItemPropertyValue,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [string]
        $Ensure = 'Present'
    )
    # Retrieve the values of the existing property collection item if present.
    Write-Verbose `
        -Message ($script:localizedData.VerboseTargetCheckingTarget -f $ItemPropertyName, $CollectionName, $ItemName, $ItemKeyName, $ItemKeyValue, $Filter, $WebsitePath )

    $existingItem = Get-ItemValues `
                        -WebsitePath $WebsitePath `
                        -Filter $Filter `
                        -CollectionName $CollectionName `
                        -ItemName $ItemName `
                        -ItemKeyName $ItemKeyName `
                        -ItemKeyValue $ItemKeyValue

    if ($Ensure -eq 'Present')
    {
        if ($null -eq $existingItem)
        {
            # Property collection item with specified key was not found.
            Write-Verbose `
                -Message ($script:localizedData.VerboseTargetItemNotFound -f $CollectionName, $ItemName, $ItemKeyName, $ItemKeyValue )

            return $false
        }
        if ($existingItem.Keys -notcontains $ItemPropertyName)
        {
            # Property collection item with specified key was found, but property was not present.
            Write-Verbose `
                -Message ($script:localizedData.VerboseTargetPropertyNotFound -f $ItemPropertyName )

            return $false
        }
        if ($existingItem[$ItemPropertyName].ToString() -ne $ItemPropertyValue)
        {
            # Property collection item with specified key was found, but property did not have expected value.
            Write-Verbose `
                -Message ($script:localizedData.VerboseTestTargetPropertyValueNotFound -f $ItemPropertyName )

            return $false
        }
        # Property collection item with specified key was found & had expected value.
        Write-Verbose `
            -Message ($script:localizedData.VerboseTargetPropertyFound -f $ItemPropertyName )

        return $true
    }
    else
    {
        if ( ($null -ne $existingItem) -and ($existingItem.Keys -contains $ItemPropertyName) )
        {
            # Property collection item with specified key was found & property was present.
            Write-Verbose `
                -Message ($script:localizedData.VerboseTargetPropertyFound -f $ItemPropertyName )

            return $false
        }
        # Property collection item with specified key was either not found or property was not present.
        Write-Verbose `
            -Message ($script:localizedData.VerboseTargetPropertyNotFound -f $ItemPropertyName )

        return $true
    }
}

# region Helper Functions

<#
.SYNOPSIS
    Gets the current values of the property collection item.

.PARAMETER WebsitePath
    Required. Path to website location (IIS or WebAdministration format).

.PARAMETER Filter
    Required. Filter used to locate property collection to retrieve. Use '.' for root.

.PARAMETER CollectionName
    Required. Name of the property collection to retrieve.

.PARAMETER ItemName
    Required. Name of the property collection item to retrieve.

.PARAMETER ItemKeyName
    Required. Name of the key of the property collection item to retrieve.

.PARAMETER ItemKeyValue
    Required. Value of the key of the property collection item to retrieve.
#>
function Get-ItemValues
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
        $CollectionName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemKeyName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ItemKeyValue
    )
    # Construct the complete filter we'll use to locate the collection item with the specified key/value in the property collection, then retrieve it if we can.
    $filter = "$($Filter)/$($CollectionName)/$($ItemName)[@$($ItemKeyName)='$($ItemKeyValue)']"

    $item = Get-WebConfigurationProperty `
                -PSPath $WebsitePath `
                -Filter $filter `
                -Name "." `
                -ErrorAction SilentlyContinue

    if ($item)
    {
        # If the property collection item exists, construct & return a hashtable containing the current values of all non-key properties.
        $result = @{}
        $item.Attributes.ForEach({ if ($_.Name -ne $ItemKeyName) { $result.Add($_.Name, $_.Value) } })
        return $result
    }
    return $null
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

.PARAMETER AddElement
    Name of the Add Element to retrieve schema from.
#>
function Get-CollectionItemPropertyType
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
        $PropertyName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $AddElement
    )

    $webConfiguration = Get-WebConfiguration -Filter $Filter -PsPath $WebsitePath

    $addElementSchema = Get-AddElementSchema -AddElement $AddElement -WebConfiguration $webConfiguration

    $property = $addElementSchema | Where-Object -FilterScript {$_.Name -eq $PropertyName}

    return $property.ClrType.Name
}

<#
.SYNOPSIS
    Gets the current data type of the property.

.PARAMETER AddElement
    Name of the Add Element to retrieve schema from.

.PARAMETER WebConfiguration
    Web configuration Element to retrieve the schema from.

#>
function Get-AddElementSchema
{
    [CmdletBinding()]
    [OutputType([Microsoft.IIs.PowerShell.Framework.ConfigurationAttributeSchema])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]
        $AddElement,

        [Parameter(Mandatory = $true)]
        [object]
        $WebConfiguration
    )

    $addElementSchema = $WebConfiguration.Schema.CollectionSchema.GetAddElementSchema($AddElement)

    return $addElementSchema.AttributeSchemas
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

    switch ($PropertyType)
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
        'UInt64'
        {
            [UInt64] $value = [convert]::ToUInt64($InputValue, 10)
        }
    }

    return $value
}

# endregion

Export-ModuleMember -Function *-TargetResource
