$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'
$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'xWebAdministration.Common'

Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'xWebAdministration.Common.psm1')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_xIisMimeTypeMapping'

Set-Variable ConstDefaultConfigurationPath -Option Constant -Value 'MACHINE/WEBROOT/APPHOST' -Scope Script
Set-Variable ConstSectionNode              -Option Constant -Value 'system.webServer/staticContent' -Scope Script

<#
    .SYNOPSIS
        This will return a hashtable of results.

    .PARAMETER ConfigurationPath
        This can be either an IIS configuration path in the format computername/webroot/apphost, or the IIS module path in this format IIS:\sites\Default Web Site.

    .PARAMETER Extension
        The file extension to map such as .html or .xml.

    .PARAMETER MimeType
        The MIME type to map that extension to such as text/html.

    .PARAMETER Ensure
        Ensures that the MIME type mapping is Present or Absent.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [String]
        $ConfigurationPath,

        [Parameter(Mandatory = $true)]
        [String]
        $Extension,

        [Parameter(Mandatory = $true)]
        [String]
        $MimeType,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Present', 'Absent')]
        [String]
        $Ensure
    )

    # Check if WebAdministration module is present for IIS cmdlets
    Assert-Module

    if (!$ConfigurationPath)
    {
        $ConfigurationPath = $ConstDefaultConfigurationPath
    }

    $currentMimeTypeMapping = Get-Mapping -ConfigurationPath $ConfigurationPath -Extension $Extension -Type $MimeType

    if ($null -eq $currentMimeTypeMapping)
    {
        Write-Verbose -Message $script:localizedData.VerboseGetTargetAbsent
        return @{
            Ensure            = 'Absent'
            ConfigurationPath = $ConfigurationPath
            Extension         = $Extension
            MimeType          = $MimeType
        }
    }
    else
    {
        Write-Verbose -Message $script:localizedData.VerboseGetTargetPresent
        return @{
            Ensure            = 'Present'
            ConfigurationPath = $ConfigurationPath
            Extension         = $currentMimeTypeMapping.fileExtension
            MimeType          = $currentMimeTypeMapping.mimeType
        }
    }
}

<#
    .SYNOPSIS
        This will set the desired state.

    .PARAMETER ConfigurationPath
        This can be either an IIS configuration path in the format computername/webroot/apphost, or the IIS module path in this format IIS:\sites\Default Web Site.

    .PARAMETER Extension
        The file extension to map such as .html or .xml.

    .PARAMETER MimeType
        The MIME type to map that extension to such as text/html.

    .PARAMETER Ensure
        Ensures that the MIME type mapping is Present or Absent.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [String]
        $ConfigurationPath,

        [Parameter(Mandatory = $true)]
        [String]
        $Extension,

        [Parameter(Mandatory = $true)]
        [String]
        $MimeType,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Present', 'Absent')]
        [String]
        $Ensure
    )

    Assert-Module

    if (!$ConfigurationPath)
    {
        $ConfigurationPath = $ConstDefaultConfigurationPath
    }

    if ($Ensure -eq 'Present')
    {
        # add the MimeType
        Add-WebConfigurationProperty -PSPath $ConfigurationPath `
                                     -Filter $ConstSectionNode `
                                     -Name '.' `
                                     -Value @{
                                         fileExtension = "$Extension"
                                         mimeType = "$MimeType"
                                     }
        Write-Verbose -Message ($script:localizedData.AddingType -f $MimeType,$Extension)
    }
    else
    {
        # remove the MimeType
        Remove-WebConfigurationProperty -PSPath $ConfigurationPath `
                                        -Filter $ConstSectionNode `
                                        -Name '.' `
                                        -AtElement @{
                                            fileExtension = "$Extension"
                                        }
        Write-Verbose -Message ($script:localizedData.RemovingType -f $MimeType,$Extension)
    }
}

<#
    .SYNOPSIS
        This tests the desired state. If the state is not correct it will return $false.
        If the state is correct it will return $true

    .PARAMETER ConfigurationPath
        This can be either an IIS configuration path in the format computername/webroot/apphost, or the IIS module path in this format IIS:\sites\Default Web Site.

    .PARAMETER Extension
        The file extension to map such as .html or .xml.

    .PARAMETER MimeType
        The MIME type to map that extension to such as text/html.

    .PARAMETER Ensure
        Ensures that the MIME type mapping is Present or Absent.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [String]
        $ConfigurationPath,

        [Parameter(Mandatory = $true)]
        [String]
        $Extension,

        [Parameter(Mandatory = $true)]
        [String]
        $MimeType,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Present', 'Absent')]
        [String]
        $Ensure
    )

    Assert-Module

    if (!$ConfigurationPath)
    {
        $ConfigurationPath = $ConstDefaultConfigurationPath
    }

    $desiredConfigurationMatch = $true

    $currentMimeTypeMapping = Get-Mapping -ConfigurationPath $ConfigurationPath -Extension $Extension -Type $MimeType

    if ($null -ne $currentMimeTypeMapping -and $Ensure -eq 'Present')
    {
        Write-Verbose -Message ($script:localizedData.TypeExists -f $MimeType,$Extension)
    }
    elseif ($null -eq $currentMimeTypeMapping -and $Ensure -eq 'Absent')
    {
        Write-Verbose -Message ($script:localizedData.TypeNotPresent -f $MimeType,$Extension)
    }
    else
    {
        $desiredConfigurationMatch = $false
    }

    return $desiredConfigurationMatch
}

#region Helper Functions

<#
    .PARAMETER ConfigurationPath
        This can be either an IIS configuration path in the format computername/webroot/apphost, or the IIS module path in this format IIS:\sites\Default Web Site.

    .PARAMETER Extension
        The file extension to map such as .html or .xml.

    .PARAMETER Type
        The MIME type to map that extension to such as text/html.
#>
function Get-Mapping
{
    [CmdletBinding()]
    [OutputType([PSObject])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [String]
        $ConfigurationPath,

        [Parameter(Mandatory = $true)]
        [String]
        $Extension,

        [Parameter(Mandatory = $true)]
        [String]
        $Type
    )

    $filter = "$ConstSectionNode/mimeMap[@fileExtension='{0}' and @mimeType='{1}']" -f $Extension, $Type

    return Get-WebConfiguration -PSPath $ConfigurationPath -Filter $filter
}

#endregion

Export-ModuleMember -Function *-TargetResource
