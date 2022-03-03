$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'
$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'xWebAdministration.Common'

Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'xWebAdministration.Common.psm1')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_xWebApplication'

<#
.SYNOPSIS
    This will return a hashtable of results
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Website,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [String] $WebAppPool,

        [Parameter(Mandatory = $true)]
        [String] $PhysicalPath
    )

    Assert-Module

    $webApplication = Get-WebApplication -Site $Website -Name $Name
    $cimAuthentication = Get-AuthenticationInfo -Site $Website -Name $Name
    $currentSslFlags = (Get-SslFlags -Location "${Website}/${Name}")

    $Ensure = 'Absent'

    if ($webApplication.Count -eq 1)
    {
        $Ensure = 'Present'
    }

    Write-Verbose -Message $script:localizedData.VerboseGetTargetResource

    $returnValue = @{
        Website                  = $Website
        Name                     = $Name
        WebAppPool               = $webApplication.applicationPool
        PhysicalPath             = $webApplication.PhysicalPath
        AuthenticationInfo       = $cimAuthentication
        SslFlags                 = [Array]$currentSslFlags
        PreloadEnabled           = $webApplication.preloadEnabled
        ServiceAutoStartProvider = $webApplication.serviceAutoStartProvider
        ServiceAutoStartEnabled  = $webApplication.serviceAutoStartEnabled
        EnabledProtocols         = [Array]$webApplication.EnabledProtocols
        Ensure                   = $Ensure
    }

    return $returnValue

}

    <#
    .SYNOPSIS
        This will set the desired state
    #>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Website,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [String] $WebAppPool,

        [Parameter(Mandatory = $true)]
        [String] $PhysicalPath,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [String] $Ensure = 'Present',

        [Parameter()]
        [AllowEmptyString()]
        [ValidateSet('','Ssl','SslNegotiateCert','SslRequireCert','Ssl128')]
        [String[]]$SslFlags = '',

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance]
        $AuthenticationInfo,

        [Parameter()]
        [Boolean]
        $PreloadEnabled,

        [Parameter()]
        [Boolean]
        $ServiceAutoStartEnabled,

        [Parameter()]
        [String]
        $ServiceAutoStartProvider,

        [Parameter()]
        [String]
        $ApplicationType,

        [Parameter()]
        [ValidateSet('http','https','net.tcp','net.msmq','net.pipe')]
        [String[]] $EnabledProtocols
    )

    Assert-Module

    if ($Ensure -eq 'Present')
    {
            $webApplication = Get-WebApplication -Site $Website -Name $Name

            if ($AuthenticationInfo -eq $null)
            {
                $AuthenticationInfo = Get-DefaultAuthenticationInfo
            }

            if ($webApplication.count -eq 0)
            {
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetPresent -f $Name)
                New-WebApplication -Site $Website -Name $Name `
                                   -PhysicalPath $PhysicalPath `
                                   -ApplicationPool $WebAppPool
                $webApplication = Get-WebApplication -Site $Website -Name $Name
            }

            # Update Physical Path if required
            if (($PSBoundParameters.ContainsKey('PhysicalPath') -and `
                $webApplication.physicalPath -ne $PhysicalPath))
            {
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetPhysicalPath -f $Name)
                #Note: read this before touching the next line of code:
                #      https://github.com/PowerShell/xWebAdministration/issues/222
                Set-WebConfigurationProperty `
                    -Filter "$($webApplication.ItemXPath)/virtualDirectory[@path='/']" `
                    -Name physicalPath `
                    -Value $PhysicalPath
            }

            # Update AppPool if required
            if ($PSBoundParameters.ContainsKey('WebAppPool') -and `
                ($webApplication.applicationPool -ne $WebAppPool))
            {
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetWebAppPool -f $Name)
                #Note: read this before touching the next line of code:
                #      https://github.com/PowerShell/xWebAdministration/issues/222
                Set-WebConfigurationProperty `
                    -Filter $webApplication.ItemXPath `
                    -Name applicationPool `
                    -Value $WebAppPool
            }

            # Update SslFlags if required
            if ($PSBoundParameters.ContainsKey('SslFlags') -and (-not (Test-SslFlags -Location "${Website}/${Name}" -SslFlags $SslFlags)))
            {
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetSslFlags -f $Name)
                $params = @{
                    PSPath   = 'MACHINE/WEBROOT/APPHOST'
                    Location = "${Website}/${Name}"
                    Filter   = 'system.webServer/security/access'
                    Name     = 'sslFlags'
                    Value    = ($sslflags -join ',')
                }
                Set-WebConfigurationProperty @params
            }

            # Set Authentication; if not defined then pass in DefaultAuthenticationInfo
            if ($PSBoundParameters.ContainsKey('AuthenticationInfo') -and `
                (-not (Test-AuthenticationInfo -Site $Website `
                                               -Name $Name `
                                               -AuthenticationInfo $AuthenticationInfo)))
            {
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetAuthenticationInfo -f $Name)
                Set-AuthenticationInfo -Site $Website `
                                       -Name $Name `
                                       -AuthenticationInfo $AuthenticationInfo `
                                       -ErrorAction Stop `
                                       -Verbose
            }

            # Update Preload if required
            if ($PSBoundParameters.ContainsKey('preloadEnabled') -and `
                $webApplication.preloadEnabled -ne $PreloadEnabled)
            {
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetPreload -f $Name)
                Set-ItemProperty -Path "IIS:\Sites\$Website\$Name" `
                                 -Name preloadEnabled `
                                 -Value $preloadEnabled `
                                 -ErrorAction Stop
            }

            # Update AutoStart if required
            if ($PSBoundParameters.ContainsKey('ServiceAutoStartEnabled') -and `
                $webApplication.serviceAutoStartEnabled -ne $ServiceAutoStartEnabled)
            {
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetAutostart -f $Name)
                Set-ItemProperty -Path "IIS:\Sites\$Website\$Name" `
                                 -Name serviceAutoStartEnabled `
                                 -Value $serviceAutoStartEnabled `
                                 -ErrorAction Stop
            }

            # Update AutoStartProviders if required
            if ($PSBoundParameters.ContainsKey('ServiceAutoStartProvider') -and `
                $webApplication.serviceAutoStartProvider -ne $ServiceAutoStartProvider)
            {
                if (-not (Confirm-UniqueServiceAutoStartProviders `
                            -ServiceAutoStartProvider $ServiceAutoStartProvider `
                            -ApplicationType $ApplicationType))
                {
                    Write-Verbose -Message ($script:localizedData.VerboseSetTargetIISAutoStartProviders)
                    Add-WebConfiguration `
                        -filter /system.applicationHost/serviceAutoStartProviders `
                        -Value @{
                            name = $ServiceAutoStartProvider
                            type=$ApplicationType
                        } `
                        -ErrorAction Stop
                }
                Write-Verbose -Message `
                    ($script:localizedData.VerboseSetTargetWebApplicationAutoStartProviders -f $Name)
                Set-ItemProperty -Path "IIS:\Sites\$Website\$Name" `
                                 -Name serviceAutoStartProvider `
                                 -Value $ServiceAutoStartProvider `
                                 -ErrorAction Stop
            }

            # Update EnabledProtocols if required
            if ($PSBoundParameters.ContainsKey('EnabledProtocols') -and `
            (-not(Confirm-UniqueEnabledProtocols `
                            -ExistingProtocols $webApplication.EnabledProtocols `
                            -ProposedProtocols $EnabledProtocols )))
            {
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetEnabledProtocols -f $Name)
                # Make input bindings which are an array, into a string
                $stringafiedEnabledProtocols = $EnabledProtocols -join ','
                Set-ItemProperty -Path "IIS:\Sites\$Website\$Name" `
                                 -Name 'enabledProtocols' `
                                 -Value $stringafiedEnabledProtocols `
                                 -ErrorAction Stop
            }
    }

    if ($Ensure -eq 'Absent')
    {
        Write-Verbose -Message ($script:localizedData.VerboseSetTargetAbsent -f $Name)
        Remove-WebApplication -Site $Website -Name $Name
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
        [String] $Website,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [String] $WebAppPool,

        [Parameter(Mandatory = $true)]
        [String] $PhysicalPath,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [String] $Ensure = 'Present',

        [Parameter()]
        [AllowEmptyString()]
        [ValidateSet('','Ssl','SslNegotiateCert','SslRequireCert','Ssl128')]
        [String[]]$SslFlags = '',

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance]
        $AuthenticationInfo,

        [Parameter()]
        [Boolean]
        $preloadEnabled,

        [Parameter()]
        [Boolean]
        $serviceAutoStartEnabled,

        [Parameter()]
        [String]
        $serviceAutoStartProvider,

        [Parameter()]
        [String]
        $ApplicationType,

        [Parameter()]
        [ValidateSet('http','https','net.tcp','net.msmq','net.pipe')]
        [String[]] $EnabledProtocols
    )

    Assert-Module

    $webApplication = Get-WebApplication -Site $Website -Name $Name

    if ($AuthenticationInfo -eq $null)
    {
        $AuthenticationInfo = Get-DefaultAuthenticationInfo
    }

    if ($webApplication.count -eq 0 -and $Ensure -eq 'Present')
    {
        Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseAbsent -f $Name)
        return $false
    }

    if ($webApplication.count -eq 1 -and $Ensure -eq 'Absent')
    {
        Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalsePresent -f $Name)
        return $false
    }

    if ($webApplication.count -eq 1 -and $Ensure -eq 'Present')
    {
        #Check Physical Path
        if ($webApplication.physicalPath -ne $PhysicalPath)
        {
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalsePhysicalPath -f $Name)
            return $false
        }

        #Check AppPool
        if ($webApplication.applicationPool -ne $WebAppPool)
        {
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseWebAppPool -f $Name)
            return $false
        }

        #Check SslFlags
        if ($PSBoundParameters.ContainsKey('SslFlags') -and (-not (Test-SslFlags -Location "${Website}/${Name}" -SslFlags $SslFlags)))
        {
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseSslFlags -f $Name)
            return $false
        }

        #Check AuthenticationInfo
        if ($PSBoundParameters.ContainsKey('AuthenticationInfo') -and `
            (-not (Test-AuthenticationInfo -Site $Website `
                                           -Name $Name `
                                           -AuthenticationInfo $AuthenticationInfo)))
        {
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseAuthenticationInfo `
                                    -f $Name)
            return $false
        }

        #Check Preload
        if ($PSBoundParameters.ContainsKey('preloadEnabled') -and `
            $webApplication.preloadEnabled -ne $PreloadEnabled)
        {
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalsePreload -f $Name)
            return $false
        }

        #Check AutoStartEnabled
        if ($PSBoundParameters.ContainsKey('ServiceAutoStartEnabled') -and `
            $webApplication.serviceAutoStartEnabled -ne $ServiceAutoStartEnabled)
        {
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseAutostart -f $Name)
            return $false
        }

        #Check AutoStartProviders
        if ($PSBoundParameters.ContainsKey('ServiceAutoStartProvider') -and `
            $webApplication.serviceAutoStartProvider -ne $ServiceAutoStartProvider)
        {
            if (-not (Confirm-UniqueServiceAutoStartProviders `
                        -serviceAutoStartProvider $ServiceAutoStartProvider `
                        -ApplicationType $ApplicationType))
            {
                Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseIISAutoStartProviders)
                return $false
            }
            Write-Verbose -Message `
                ($script:localizedData.VerboseTestTargetFalseWebApplicationAutoStartProviders -f $Name)
            return $false
        }

        # Update EnabledProtocols if required
        if ($PSBoundParameters.ContainsKey('EnabledProtocols') -and `
            (-not(Confirm-UniqueEnabledProtocols `
                            -ExistingProtocols $webApplication.EnabledProtocols `
                            -ProposedProtocols $EnabledProtocols )))
        {
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseEnabledProtocols `
                                    -f $Name)
            return $false
        }
    }

    return $true
}

<#
.SYNOPSIS
    Helper function used to validate that the EnabledProtocols are unique.
    Returns $false if EnabledProtocols are not unique and $true if they are
.PARAMETER ExistingProtocols
    Specifies existing SMTP bindings
.PARAMETER ProposedProtocols
    Specifies desired SMTP bindings.
.NOTES
    ExistingProtocols is a String whereas ProposedProtocols is an array of Strings
    so we need to do some extra work in comparing them
#>
function Confirm-UniqueEnabledProtocols
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [String] $ExistingProtocols,

        [Parameter(Mandatory = $true)]
        [String[]] $ProposedProtocols
    )

    $inputToCheck = @()
    foreach ($proposedProtocol in $ProposedProtocols)
    {
        $inputToCheck += $proposedProtocol
    }

    $existingProtocolsToCheck = $existingProtocols -split ','

    $existingToCheck = @()
    foreach ($existingProtocol in $existingProtocolsToCheck)
    {
        $existingToCheck += $existingProtocol.Trim()
    }

    $sortedExistingProtocols = $existingToCheck | Sort-Object -Unique
    $sortedInputProtocols = $inputToCheck | Sort-Object -Unique


    if (Compare-Object -ReferenceObject $sortedExistingProtocols `
                       -DifferenceObject $sortedInputProtocols `
                       -PassThru)
    {
        return $false
    }

    return $true
}

#region Helper Functions

<#
.SYNOPSIS
    Helper function used to validate that the AutoStartProviders is unique to other
    websites. Returns False if the AutoStartProviders exist.
.PARAMETER serviceAutoStartProvider
    Specifies the name of the AutoStartProviders.
.PARAMETER ExcludeStopped
    Specifies the name of the Application Type for the AutoStartProvider.
.NOTES
    This tests for the existance of a AutoStartProviders which is globally assigned.
    As AutoStartProviders need to be uniquely named it will check for this and error out if
    attempting to add a duplicatly named AutoStartProvider.
    Name is passed in to bubble to any error messages during the test.
#>
function Confirm-UniqueServiceAutoStartProviders
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $ServiceAutoStartProvider,

        [Parameter(Mandatory = $true)]
        [String] $ApplicationType
    )

    $WebSiteAutoStartProviders = (Get-WebConfiguration `
                            -filter /system.applicationHost/serviceAutoStartProviders).Collection

    $ExistingObject = $WebSiteAutoStartProviders | `
        Where-Object -Property Name -eq -Value $serviceAutoStartProvider | `
        Select-Object Name,Type

    $ProposedObject = @(New-Object -TypeName PSObject -Property @{
        name   = $ServiceAutoStartProvider
        type   = $ApplicationType
    })

    if (-not $ExistingObject)
        {
            return $false
        }

    if (-not (Compare-Object -ReferenceObject $ExistingObject `
                            -DifferenceObject $ProposedObject `
                            -Property name))
        {
            if (Compare-Object -ReferenceObject $ExistingObject `
                              -DifferenceObject $ProposedObject `
                              -Property type)
                {
                    $ErrorMessage = $script:localizedData.ErrorWebApplicationTestAutoStartProviderFailure
                    New-TerminatingError `
                        -ErrorId 'ErrorWebApplicationTestAutoStartProviderFailure' `
                        -ErrorMessage $ErrorMessage `
                        -ErrorCategory 'InvalidResult'
                }
        }

    return $true

}

<#
.SYNOPSIS
    Helper function used to validate that the authenticationProperties for an Application.
.PARAMETER Site
    Specifies the name of the Website.
.PARAMETER Name
    Specifies the name of the Application.
#>
function Get-AuthenticationInfo
{
    [CmdletBinding()]
    [OutputType([Microsoft.Management.Infrastructure.CimInstance])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Site,

        [Parameter(Mandatory = $true)]
        [String] $Name
    )

    $authenticationProperties = @{}
    foreach ($type in @('Anonymous', 'Basic', 'Digest', 'Windows'))
    {
        $authenticationProperties[$type] = [Boolean](Test-AuthenticationEnabled -Site $Site `
                                                                               -Name $Name `
                                                                               -Type $type)
    }

    return New-CimInstance `
            -ClassName MSFT_xWebApplicationAuthenticationInformation `
            -ClientOnly -Property $authenticationProperties `
            -NameSpace 'root\microsoft\windows\desiredstateconfiguration'

}

<#
.SYNOPSIS
    Helper function used to build a default CimInstance for AuthenticationInformation
#>
function Get-DefaultAuthenticationInfo
{
    New-CimInstance -ClassName MSFT_xWebApplicationAuthenticationInformation `
        -ClientOnly `
        -Property @{
            Anonymous = $false
            Basic = $false
            Digest = $false
            Windows = $false
        } `
        -NameSpace 'root\microsoft\windows\desiredstateconfiguration'
}

<#
.SYNOPSIS
    Helper function used to return the SSLFlags on an Application.
.PARAMETER Location
    Specifies the path in the IIS: PSDrive to the Application
#>
function Get-SslFlags
{
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Location
    )

    $SslFlags = Get-WebConfiguration `
                -PSPath IIS:\Sites `
                -Location $Location `
                -Filter 'system.webserver/security/access' | `
                 ForEach-Object { $_.sslFlags }

    if ($null -eq $SslFlags)
    {
        return [String]::Empty
    }

    return $SslFlags
}

<#
.SYNOPSIS
    Helper function used to set authenticationProperties for an Application.
.PARAMETER Site
    Specifies the name of the Website.
.PARAMETER Name
    Specifies the name of the Application.
.PARAMETER Type
    Specifies the type of Authentication,
Limited to the set: ('Anonymous','Basic','Digest','Windows').
.PARAMETER Enabled
    Whether the Authentication is enabled or not.
#>
function Set-Authentication
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Site,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Anonymous','Basic','Digest','Windows')]
        [String] $Type,

        [Parameter()]
        [Boolean] $Enabled
    )

    Set-WebConfigurationProperty `
        -Filter /system.WebServer/security/authentication/${Type}Authentication `
        -Name enabled `
        -Value $Enabled `
        -Location "${Site}/${Name}"
}

<#
.SYNOPSIS
    Helper function used to validate that the authenticationProperties for an Application.
.PARAMETER Site
    Specifies the name of the Website.
.PARAMETER Name
    Specifies the name of the Application.
.PARAMETER AuthenticationInfo
    A CimInstance of what state the AuthenticationInfo should be.
#>
function Set-AuthenticationInfo
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Site,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Microsoft.Management.Infrastructure.CimInstance] $AuthenticationInfo
    )

    foreach ($type in @('Anonymous', 'Basic', 'Digest', 'Windows'))
    {
        $enabled = ($AuthenticationInfo.CimInstanceProperties[$type].Value -eq $true)
        Set-Authentication -Site $Site `
                           -Name $Name `
                           -Type $type `
                           -Enabled $enabled
    }
}

<#
.SYNOPSIS
    Helper function used to test the authenticationProperties state for an Application.
    Will return that value which will either [String]True or [String]False
.PARAMETER Site
    Specifies the name of the Website.
.PARAMETER Name
    Specifies the name of the Application.
.PARAMETER Type
    Specifies the type of Authentication,
    limited to the set: ('Anonymous','Basic','Digest','Windows').
#>

function Test-AuthenticationEnabled
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Site,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Anonymous','Basic','Digest','Windows')]
        [String] $Type
    )


    $prop = Get-WebConfigurationProperty `
            -Filter /system.WebServer/security/authentication/${Type}Authentication `
            -Name enabled `
            -Location "${Site}/${Name}"

    return $prop.Value

}

<#
.SYNOPSIS
    Helper function used to test the authenticationProperties state for an Application.
    Will return that result which will either [boolean]$True or [boolean]$False for use in
    Test-TargetResource.
    Uses Test-AuthenticationEnabled to determine this. First incorrect result will break
    this function out.
.PARAMETER Site
    Specifies the name of the Website.
.PARAMETER Name
    Specifies the name of the Application.
.PARAMETER AuthenticationInfo
    A CimInstance of what state the AuthenticationInfo should be.
#>

function Test-AuthenticationInfo
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Site,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [Microsoft.Management.Infrastructure.CimInstance] $AuthenticationInfo
    )

    foreach ($type in @('Anonymous', 'Basic', 'Digest', 'Windows'))
    {
        $expected = $AuthenticationInfo.CimInstanceProperties[$type].Value
        $actual = Test-AuthenticationEnabled -Site $Site `
                                             -Name $Name `
                                             -Type $type
        if ($expected -ne $actual)
        {
            return $false
        }
    }

    return $true

}

<#
.SYNOPSIS
    Helper function used to test the SSLFlags on an Application.
    Will return $true if they match and $false if they do not.
.PARAMETER SslFlags
    Specifies the SslFlags to Test
.PARAMETER Location
    Specifies the path in the IIS: PSDrive to the Application
#>
function Test-SslFlags
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter()]
        [AllowEmptyString()]
        [ValidateSet('','Ssl','SslNegotiateCert','SslRequireCert','Ssl128')]
        [String[]] $SslFlags = '',

        [Parameter(Mandatory = $true)]
        [String] $Location
    )

    $CurrentSslFlags =  Get-SslFlags -Location $Location

    if (Compare-Object -ReferenceObject $CurrentSslFlags `
                        -DifferenceObject $SslFlags)
    {
        return $false
    }

    return $true
}

#endregion

Export-ModuleMember -Function *-TargetResource
