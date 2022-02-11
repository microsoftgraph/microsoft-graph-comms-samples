$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'
$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'xWebAdministration.Common'

Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'xWebAdministration.Common.psm1')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_xSslSettings'

<#
        .SYNOPSIS
        This will return a hashtable of results including Name, Bindings, and Ensure
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [ValidateSet('','Ssl','SslNegotiateCert','SslRequireCert','Ssl128')]
        [String[]] $Bindings
    )

    Assert-Module

    $ensure = 'Absent'

    try
    {
        $params = @{
            PSPath   = 'MACHINE/WEBROOT/APPHOST'
            Location = $Name
            Filter   = 'system.webServer/security/access'
            Name     = 'sslFlags'
        }

        $sslSettings = Get-WebConfigurationProperty @params

        # If SSL is configured at all this will be a String else
        # it willl be a configuration object.
        if ($sslSettings.GetType().FullName -eq 'System.String')
        {
            $Bindings = $sslSettings.Split(',')
            $ensure = 'Present'
        }
    }
    catch [Exception]
    {
        $errorMessage = $script:localizedData.UnableToFindConfig
        New-TerminatingError -ErrorId 'UnableToFindConfig'`
                             -ErrorMessage  $errorMessage`
                             -ErrorCategory 'InvalidResult'
    }

    Write-Verbose -Message $script:localizedData.VerboseGetTargetResource

    return @{
        Name = $Name
        Bindings = $Bindings
        Ensure = $ensure
    }
}

<#
        .SYNOPSIS
        This will update the desired state based on the Bindings passed in
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [ValidateSet('','Ssl','SslNegotiateCert','SslRequireCert','Ssl128')]
        [String[]] $Bindings,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [String] $Ensure = 'Present'
    )

    Assert-Module

    if ($Ensure -eq 'Absent' -or $Bindings.toLower().Contains('none'))
    {
        $params = @{
            PSPath   = 'MACHINE/WEBROOT/APPHOST'
            Location = $Name
            Filter   = 'system.webServer/security/access'
            Name     = 'sslFlags'
            Value    = ''
        }

        Write-Verbose -Message ($script:localizedData.SettingsslConfig -f $Name, 'None')
        Set-WebConfigurationProperty @params
    }

    else
    {
        $sslBindings = $Bindings -join ','
        $params = @{
            PSPath   = 'MACHINE/WEBROOT/APPHOST'
            Location = $Name
            Filter   = 'system.webServer/security/access'
            Name     = 'sslFlags'
            Value    = $sslBindings
        }

        Write-Verbose -Message ($script:localizedData.SettingsslConfig -f $Name, $params.Value)
        Set-WebConfigurationProperty @params
    }
}

<#
        .SYNOPSIS
        This tests the desired state. If the state is not correct it will return $false.
        If the state is correct it will return $true
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [ValidateSet('','Ssl','SslNegotiateCert','SslRequireCert','Ssl128')]
        [String[]] $Bindings,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [String] $Ensure = 'Present'
    )

    $sslSettings = Get-TargetResource -Name $Name -Bindings $Bindings

    if ($Ensure -eq 'Present' -and $sslSettings.Ensure -eq 'Present')
    {
        $sslComp = Compare-Object -ReferenceObject $Bindings `
                                  -DifferenceObject $sslSettings.Bindings `
                                  -PassThru
        if ($null -eq $sslComp)
        {
            Write-Verbose -Message ($script:localizedData.sslBindingsCorrect -f $Name)
            return $true;
        }
    }

    if ($Ensure -eq 'Absent' -and $sslSettings.Ensure -eq 'Absent')
    {
        Write-Verbose -Message ($script:localizedData.sslBindingsAbsent -f $Name)
        return $true;
    }

    return $false;
}

Export-ModuleMember -Function *-TargetResource
