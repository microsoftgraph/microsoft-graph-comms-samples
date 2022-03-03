#requires -Version 4.0 -Modules CimCmdlets

$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'
$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'xWebAdministration.Common'

Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'xWebAdministration.Common.psm1')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_xWebSite'

<#
        .SYNOPSYS
            The Get-TargetResource cmdlet is used to fetch the status of role or Website on
            the target machine. It gives the Website info of the requested role/feature on the
            target machine.

        .PARAMETER Name
            Name of the website
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name
    )

    Assert-Module

    $website = Get-Website | Where-Object -FilterScript {$_.Name -eq $Name}

    if ($website.Count -eq 0)
    {
        Write-Verbose -Message ($script:localizedData.VerboseGetTargetAbsent)
        $ensureResult = 'Absent'
    }
    elseif ($website.Count -eq 1)
    {
        Write-Verbose -Message ($script:localizedData.VerboseGetTargetPresent)
        $ensureResult = 'Present'

        $cimBindings = @(ConvertTo-CimBinding -InputObject $website.bindings.Collection)

        $allDefaultPages = @(
            Get-WebConfiguration -Filter '/system.webServer/defaultDocument/files/*' -PSPath "IIS:\Sites\$Name" |
            ForEach-Object -Process {Write-Output -InputObject $_.value}
        )
        $cimAuthentication = Get-AuthenticationInfo -Site $Name
        $websiteAutoStartProviders = (Get-WebConfiguration `
            -filter /system.applicationHost/serviceAutoStartProviders).Collection
        $webConfiguration = $websiteAutoStartProviders | `
                                Where-Object -Property Name -eq -Value $ServiceAutoStartProvider | `
                                Select-Object Name,Type

        [Array] $cimLogCustomFields = ConvertTo-CimLogCustomFields -InputObject $website.logFile.customFields.Collection

        $logFlagsArray = $null
        if ($website.logfile.LogExtFileFlags -is [System.String])
        {
            $logFlagsArray = [System.String[]] $website.logfile.LogExtFileFlags.Split(',')
        }
    }
    # Multiple websites with the same name exist. This is not supported and is an error
    else
    {
        $errorMessage = $script:localizedData.ErrorWebsiteDiscoveryFailure -f $Name
        New-TerminatingError -ErrorId 'WebsiteDiscoveryFailure' `
                             -ErrorMessage $errorMessage `
                             -ErrorCategory 'InvalidResult'
    }

    # Add all website properties to the hash table
    return @{
        Ensure                   = $ensureResult
        Name                     = $Name
        SiteId                   = $website.id
        PhysicalPath             = $website.PhysicalPath
        State                    = $website.State
        ApplicationPool          = $website.ApplicationPool
        BindingInfo              = $cimBindings
        DefaultPage              = $allDefaultPages
        EnabledProtocols         = $website.EnabledProtocols
        ServerAutoStart          = $website.serverAutoStart
        AuthenticationInfo       = $cimAuthentication
        PreloadEnabled           = $website.applicationDefaults.preloadEnabled
        ServiceAutoStartProvider = $website.applicationDefaults.serviceAutoStartProvider
        ServiceAutoStartEnabled  = $website.applicationDefaults.serviceAutoStartEnabled
        ApplicationType          = $webConfiguration.Type
        LogPath                  = $website.logfile.directory
        LogFlags                 = $logFlagsArray
        LogPeriod                = $website.logfile.period
        LogtruncateSize          = $website.logfile.truncateSize
        LoglocalTimeRollover     = $website.logfile.localTimeRollover
        LogFormat                = $website.logfile.logFormat
        LogTargetW3C             = $website.logfile.logTargetW3C
        LogCustomFields          = $cimLogCustomFields
    }
}

<#
        .SYNOPSYS
        The Set-TargetResource cmdlet is used to create, delete or configure a website on the
        target machine.

        .PARAMETER SiteId
            Optional. Specifies the IIS site Id for the web site.

        .PARAMETER PhysicalPath
        Specifies the physical path of the web site. Don't set this if the site will be deployed by an external tool that updates the path.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [String]
        $Ensure = 'Present',

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name,

        # To avoid confusion we use SiteId instead of just Id
        [Parameter()]
        [UInt32]
        $SiteId,

        [Parameter()]
        [String]
        $PhysicalPath,

        [Parameter()]
        [ValidateSet('Started', 'Stopped')]
        [String]
        $State = 'Started',

        # The application pool name must contain between 1 and 64 characters
        [Parameter()]
        [ValidateLength(1, 64)]
        [String]
        $ApplicationPool,

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $BindingInfo,

        [Parameter()]
        [String[]]
        $DefaultPage,

        [Parameter()]
        [String]
        $EnabledProtocols,

        [Parameter()]
        [Boolean]
        $ServerAutoStart,

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
        [String]
        $LogPath,

        [Parameter()]
        [ValidateSet('Date','Time','ClientIP','UserName','SiteName','ComputerName','ServerIP','Method','UriStem','UriQuery','HttpStatus','Win32Status','BytesSent','BytesRecv','TimeTaken','ServerPort','UserAgent','Cookie','Referer','ProtocolVersion','Host','HttpSubStatus')]
        [String[]]
        $LogFlags,

        [Parameter()]
        [ValidateSet('Hourly','Daily','Weekly','Monthly','MaxSize')]
        [String]
        $LogPeriod,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1048576, 4294967295)] $valueAsUInt64 = [UInt64]::Parse($_))
        })]
        [String]
        $LogTruncateSize,

        [Parameter()]
        [Boolean]
        $LoglocalTimeRollover,

        [Parameter()]
        [ValidateSet('IIS','W3C','NCSA')]
        [String]
        $LogFormat,

        [Parameter()]
        [ValidateSet('File','ETW','File,ETW')]
        [String]
        $LogTargetW3C,

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $LogCustomFields
    )

    Assert-Module

    $website = Get-Website | Where-Object -FilterScript {$_.Name -eq $Name}

    if ($Ensure -eq 'Present')
    {
        if ($null -ne $website)
        {
            # Update Site Id if required
            # Note: Set-ItemProperty is case sensitive. only works with id, not Id or ID
            if ($SiteId -gt 0 -and `
                $website.Id -ne $SiteId)
            {
                Set-ItemProperty -Path "IIS:\Sites\$Name" `
                    -Name id `
                    -Value $SiteId `
                    -ErrorAction Stop
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdatedSiteId `
                    -f $Name, $SiteId)
            }

            # Update Physical Path if required
            if ([String]::IsNullOrEmpty($PhysicalPath) -eq $false -and `
                $website.PhysicalPath -ne $PhysicalPath)
            {
                Set-ItemProperty -Path "IIS:\Sites\$Name" `
                                 -Name physicalPath `
                                 -Value $PhysicalPath `
                                 -ErrorAction Stop
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdatedPhysicalPath `
                                        -f $Name, $PhysicalPath)
            }

            # Update Application Pool if required
            if ($PSBoundParameters.ContainsKey('ApplicationPool') -and `
                $website.ApplicationPool -ne $ApplicationPool)
            {
                Set-ItemProperty -Path "IIS:\Sites\$Name" `
                                 -Name applicationPool `
                                 -Value $ApplicationPool `
                                 -ErrorAction Stop
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdatedApplicationPool `
                                        -f $Name, $ApplicationPool)
            }

            # Update Bindings if required
            if ($PSBoundParameters.ContainsKey('BindingInfo') -and `
                $null -ne $BindingInfo)
            {
                if (-not (Test-WebsiteBinding -Name $Name `
                                              -BindingInfo $BindingInfo))
                {
                    Update-WebsiteBinding -Name $Name `
                                          -BindingInfo $BindingInfo
                    Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdatedBindingInfo `
                                            -f $Name)
                }
            }

            # Update Enabled Protocols if required
            if ($PSBoundParameters.ContainsKey('EnabledProtocols') -and `
                $website.EnabledProtocols -ne $EnabledProtocols)
            {
                Set-ItemProperty -Path "IIS:\Sites\$Name" `
                                 -Name enabledProtocols `
                                 -Value $EnabledProtocols `
                                 -ErrorAction Stop
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdatedEnabledProtocols `
                                        -f $Name, $EnabledProtocols)
            }

            # Update Default Pages if required
            if ($PSBoundParameters.ContainsKey('DefaultPage') -and `
                $null -ne $DefaultPage)
            {
                Update-DefaultPage -Name $Name `
                                   -DefaultPage $DefaultPage
            }

            # Update State if required
            if ($PSBoundParameters.ContainsKey('State') -and `
                $website.State -ne $State)
            {
                if ($State -eq 'Started')
                {
                    # Ensure that there are no other running websites with binding information that
                    # will conflict with this website before starting
                    if (-not (Confirm-UniqueBinding -Name $Name -ExcludeStopped))
                    {
                        # Return error and do not start the website
                        $errorMessage = $script:localizedData.ErrorWebsiteBindingConflictOnStart `
                                        -f $Name
                        New-TerminatingError -ErrorId 'WebsiteBindingConflictOnStart' `
                                             -ErrorMessage $errorMessage `
                                             -ErrorCategory 'InvalidResult'
                    }

                    try
                    {
                        Start-Website -Name $Name -ErrorAction Stop
                    }
                    catch
                    {
                        $errorMessage = $script:localizedData.ErrorWebsiteStateFailure `
                                        -f $Name, $_.Exception.Message
                        New-TerminatingError -ErrorId 'WebsiteStateFailure' `
                                             -ErrorMessage $errorMessage `
                                             -ErrorCategory 'InvalidOperation'
                    }
                }
                else
                {
                    try
                    {
                        Stop-Website -Name $Name -ErrorAction Stop
                    }
                    catch
                    {
                        $errorMessage = $script:localizedData.ErrorWebsiteStateFailure `
                                        -f $Name, $_.Exception.Message
                        New-TerminatingError -ErrorId 'WebsiteStateFailure' `
                                             -ErrorMessage $errorMessage `
                                             -ErrorCategory 'InvalidOperation'
                    }
                }

                Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdatedState `
                                        -f $Name, $State)
            }

            # Update ServerAutoStart if required
            if ($PSBoundParameters.ContainsKey('ServerAutoStart') -and `
                ($website.serverAutoStart -ne $ServerAutoStart))
            {
                Set-ItemProperty -Path "IIS:\Sites\$Name" `
                                 -Name serverAutoStart `
                                 -Value $ServerAutoStart `
                                 -ErrorAction Stop
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetWebsiteAutoStartUpdated `
                                        -f $Name)
            }
        }
        # Create website if it does not exist
        else
        {
            try
            {
                $PSBoundParameters.GetEnumerator() | Where-Object -FilterScript {
                    $_.Key -in (Get-Command -Name New-Website `
                                            -Module WebAdministration).Parameters.Keys
                } | ForEach-Object -Begin {
                        $newWebsiteSplat = @{}
                } -Process {
                    $newWebsiteSplat.Add($_.Key, $_.Value)
                }

                # New-WebSite has Id parameter instead of SiteId, so it's getting mapped to Id
                if ($PSBoundParameters.ContainsKey('SiteId'))
                {
                    $newWebsiteSplat.Add('Id', $SiteId)
                } elseif (-not (Get-WebSite)) {
                    # If there are no other websites and SiteId is missing, specify the Id Parameter for the new website.
                    # Otherwise an error can occur on systems running Windows Server 2008 R2.
                    $newWebsiteSplat.Add('Id', 1)
                }

                if ([String]::IsNullOrEmpty($PhysicalPath))
                {
                    # If no physical path is provided run New-Website with -Force flag
                    $website = New-Website @newWebsiteSplat -ErrorAction Stop -Force
                } else {
                    # If physical path is provided don't run New-Website with -Force flag to verify that the path exists
                    $website = New-Website @newWebsiteSplat -ErrorAction Stop
                }

                Write-Verbose -Message ($script:localizedData.VerboseSetTargetWebsiteCreated `
                                        -f $Name)
            }
            catch
            {
                $errorMessage = $script:localizedData.ErrorWebsiteCreationFailure `
                                -f $Name, $_.Exception.Message
                New-TerminatingError -ErrorId 'WebsiteCreationFailure' `
                                     -ErrorMessage $errorMessage `
                                     -ErrorCategory 'InvalidOperation'
            }

            Stop-Website -Name $website.Name -ErrorAction Stop

            # Clear default bindings if new bindings defined and are different
            if ($PSBoundParameters.ContainsKey('BindingInfo') -and `
                $null -ne $BindingInfo)
            {
                if (-not (Test-WebsiteBinding -Name $Name `
                                              -BindingInfo $BindingInfo))
                {
                    Update-WebsiteBinding -Name $Name -BindingInfo $BindingInfo
                    Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdatedBindingInfo `
                                            -f $Name)
                }
            }

            # Update Enabled Protocols if required
            if ($PSBoundParameters.ContainsKey('EnabledProtocols') `
                -and $website.EnabledProtocols `
                -ne $EnabledProtocols)
            {
                Set-ItemProperty -Path "IIS:\Sites\$Name" `
                                 -Name enabledProtocols `
                                 -Value $EnabledProtocols `
                                 -ErrorAction Stop
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdatedEnabledProtocols `
                                        -f $Name, $EnabledProtocols)
            }

            # Update Default Pages if required
            if ($PSBoundParameters.ContainsKey('DefaultPage') -and `
                $null -ne $DefaultPage)
            {
                Update-DefaultPage -Name $Name `
                                   -DefaultPage $DefaultPage
            }

            # Start website if required
            if ($State -eq 'Started')
            {
                # Ensure that there are no other running websites with binding information that
                # will conflict with this website before starting
                if (-not (Confirm-UniqueBinding -Name $Name -ExcludeStopped))
                {
                    # Return error and do not start the website
                    $errorMessage = $script:localizedData.ErrorWebsiteBindingConflictOnStart `
                                    -f $Name
                    New-TerminatingError -ErrorId 'WebsiteBindingConflictOnStart' `
                                         -ErrorMessage $errorMessage `
                                         -ErrorCategory 'InvalidResult'
                }

                try
                {
                    Start-Website -Name $Name -ErrorAction Stop
                    Write-Verbose -Message ($script:localizedData.VerboseSetTargetWebsiteStarted `
                                            -f $Name)
                }
                catch
                {
                    $errorMessage = $script:localizedData.ErrorWebsiteStateFailure `
                                    -f $Name, $_.Exception.Message
                    New-TerminatingError -ErrorId 'WebsiteStateFailure' `
                                         -ErrorMessage $errorMessage `
                                         -ErrorCategory 'InvalidOperation'
                }
            }

            # Update ServerAutoStart if required
            if ($PSBoundParameters.ContainsKey('ServerAutoStart') -and `
                ($website.serverAutoStart -ne $ServerAutoStart))
            {
                Set-ItemProperty -Path "IIS:\Sites\$Name" `
                                 -Name serverAutoStart `
                                 -Value $ServerAutoStart `
                                 -ErrorAction Stop
                Write-Verbose -Message ($script:localizedData.VerboseSetTargetWebsiteAutoStartUpdated `
                                        -f $Name)
            }
        }

        # Set Authentication; if not defined then pass in DefaultAuthenticationInfo
        if ($PSBoundParameters.ContainsKey('AuthenticationInfo') -and `
        (-not (Test-AuthenticationInfo -Site $Name `
                                        -AuthenticationInfo $AuthenticationInfo)))
        {
            Set-AuthenticationInfo -Site $Name `
                                    -AuthenticationInfo $AuthenticationInfo `
                                    -ErrorAction Stop
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetAuthenticationInfoUpdated `
                                    -f $Name)
        }

        # Update Preload if required
        if ($PSBoundParameters.ContainsKey('preloadEnabled') -and `
            ($website.applicationDefaults.preloadEnabled -ne $PreloadEnabled))
        {
            Set-ItemProperty -Path "IIS:\Sites\$Name" `
                            -Name applicationDefaults.preloadEnabled `
                            -Value $PreloadEnabled `
                            -ErrorAction Stop
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetWebsitePreloadUpdated `
                                    -f $Name)
        }

        # Update AutoStart if required
        if ($PSBoundParameters.ContainsKey('ServiceAutoStartEnabled') -and `
            ($website.applicationDefaults.ServiceAutoStartEnabled -ne $ServiceAutoStartEnabled))
        {
            Set-ItemProperty -Path "IIS:\Sites\$Name" `
                                -Name applicationDefaults.serviceAutoStartEnabled `
                                -Value $ServiceAutoStartEnabled `
                                -ErrorAction Stop
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetWebsiteAutoStartUpdated `
                                    -f $Name)
        }

        # Update AutoStartProviders if required
        if ($PSBoundParameters.ContainsKey('ServiceAutoStartProvider') -and `
            ($website.applicationDefaults.ServiceAutoStartProvider `
            -ne $ServiceAutoStartProvider))
        {
            if (-not (Confirm-UniqueServiceAutoStartProviders `
                        -ServiceAutoStartProvider $ServiceAutoStartProvider `
                        -ApplicationType $ApplicationType))
            {
                Add-WebConfiguration -filter /system.applicationHost/serviceAutoStartProviders `
                                        -Value @{
                                            name = $ServiceAutoStartProvider
                                            type = $ApplicationType
                                        } `
                                        -ErrorAction Stop
                Write-Verbose -Message `
                                ($script:localizedData.VerboseSetTargetIISAutoStartProviderUpdated)
            }
            Set-ItemProperty -Path "IIS:\Sites\$Name" `
                                -Name applicationDefaults.serviceAutoStartProvider `
                                -Value $ServiceAutoStartProvider -ErrorAction Stop
            Write-Verbose -Message `
                            ($script:localizedData.VerboseSetTargetServiceAutoStartProviderUpdated `
                            -f $Name)
        }

        # Update LogFormat if Needed
        if ($PSBoundParameters.ContainsKey('LogFormat') -and `
            ($LogFormat -ne $website.logfile.LogFormat))
        {
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdateLogFormat -f $Name)

            # In Windows Server 2008 R2, Set-ItemProperty only accepts index values to the LogFile.LogFormat property
            $site = Get-Item "IIS:\Sites\$Name"
            $site.LogFile.LogFormat = $LogFormat
            $site | Set-Item
        }

        # Update LogTargetW3C if Needed
        if ($PSBoundParameters.ContainsKey('LogTargetW3C') `
            -and $website.logfile.LogTargetW3C `
            -ne $LogTargetW3C)
        {
            Set-ItemProperty -Path "IIS:\Sites\$Name" `
                                -Name logfile.logTargetW3C `
                                -Value $LogTargetW3C `
                                -ErrorAction Stop
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdateLogTargetW3C `
                                    -f $Name, $LogTargetW3C)
        }

        # Update LogFlags if required
        if ($PSBoundParameters.ContainsKey('LogFlags') -and `
            (-not (Compare-LogFlags -Name $Name -LogFlags $LogFlags)))
        {
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdateLogFlags `
                                    -f $Name)

            Set-ItemProperty "IIS:\Sites\$Name" `
                -Name logFile.logFormat -value 'W3C'
            Set-ItemProperty "IIS:\Sites\$Name" `
                -Name logFile.logExtFileFlags -value ($LogFlags -join ',')
        }

        # Update LogPath if required
        if ($PSBoundParameters.ContainsKey('LogPath') -and `
            ($LogPath -ne $website.logfile.directory))
        {
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdateLogPath `
                                    -f $Name)
            Set-ItemProperty -Path "IIS:\Sites\$Name" `
                -Name LogFile.directory -value $LogPath
        }

        # Update LogPeriod if needed
        if ($PSBoundParameters.ContainsKey('LogPeriod') -and `
            ($LogPeriod -ne $website.logfile.period))
        {
            if ($PSBoundParameters.ContainsKey('LogTruncateSize'))
                {
                    Write-Verbose -Message ($script:localizedData.WarningLogPeriod `
                                            -f $Name)
                }

            Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdateLogPeriod)

            # In Windows Server 2008 R2, Set-ItemProperty only accepts index values to the LogFile.Period property
            $site = Get-Item "IIS:\Sites\$Name"
            $site.LogFile.Period = $LogPeriod
            $site | Set-Item
        }

        # Update LogTruncateSize if needed
        if ($PSBoundParameters.ContainsKey('LogTruncateSize') -and `
            ($LogTruncateSize -ne $website.logfile.LogTruncateSize))
        {
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdateLogTruncateSize `
                                    -f $Name)
            Set-ItemProperty -Path "IIS:\Sites\$Name" `
                -Name LogFile.truncateSize -Value $LogTruncateSize
            Set-ItemProperty -Path "IIS:\Sites\$Name" `
                -Name LogFile.period -Value 'MaxSize'
        }

        # Update LoglocalTimeRollover if needed
        if ($PSBoundParameters.ContainsKey('LoglocalTimeRollover') -and `
            ($LoglocalTimeRollover -ne `
                ([System.Convert]::ToBoolean($website.logfile.LocalTimeRollover))))
        {
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdateLoglocalTimeRollover `
                                    -f $Name)
            Set-ItemProperty -Path "IIS:\Sites\$Name" `
                -Name LogFile.localTimeRollover -Value $LoglocalTimeRollover
        }

        # Update LogCustomFields if needed
        if ($PSBoundParameters.ContainsKey('LogCustomFields') -and `
        (-not (Test-LogCustomField -Site $Name -LogCustomField $LogCustomFields)))
        {
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetUpdateLogCustomFields `
                                    -f $Name)
            Set-LogCustomField -Site $Name -LogCustomField $LogCustomFields
        }
    }
    # Remove website
    else
    {
        try
        {
            Remove-Website -Name $Name -ErrorAction Stop
            Write-Verbose -Message ($script:localizedData.VerboseSetTargetWebsiteRemoved `
                                    -f $Name)
        }
        catch
        {
            $errorMessage = $script:localizedData.ErrorWebsiteRemovalFailure `
                            -f $Name, $_.Exception.Message
            New-TerminatingError -ErrorId 'WebsiteRemovalFailure' `
                                 -ErrorMessage $errorMessage `
                                 -ErrorCategory 'InvalidOperation'
        }
    }
}

<#
        .SYNOPSIS
        The Test-TargetResource cmdlet is used to validate if the role or feature is in a state as
        expected in the instance document.

        .PARAMETER SiteId
            Optional. Specifies the IIS site Id for the web site.

#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [String]
        $Ensure = 'Present',

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name,

        [Parameter()]
        [UInt32]
        $SiteId,

        [Parameter()]
        [String]
        $PhysicalPath,

        [Parameter()]
        [ValidateSet('Started', 'Stopped')]
        [String]
        $State = 'Started',

        # The application pool name must contain between 1 and 64 characters
        [Parameter()]
        [ValidateLength(1, 64)]
        [String]
        $ApplicationPool,

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $BindingInfo,

        [Parameter()]
        [String[]]
        $DefaultPage,

        [Parameter()]
        [String]
        $EnabledProtocols,

        [Parameter()]
        [Boolean]
        $ServerAutoStart,

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
        [String]
        $LogPath,

        [Parameter()]
        [ValidateSet('Date','Time','ClientIP','UserName','SiteName','ComputerName','ServerIP','Method','UriStem','UriQuery','HttpStatus','Win32Status','BytesSent','BytesRecv','TimeTaken','ServerPort','UserAgent','Cookie','Referer','ProtocolVersion','Host','HttpSubStatus')]
        [String[]]
        $LogFlags,

        [Parameter()]
        [ValidateSet('Hourly','Daily','Weekly','Monthly','MaxSize')]
        [String]
        $LogPeriod,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1048576, 4294967295)] $valueAsUInt64 = [UInt64]::Parse($_))
        })]
        [String]
        $LogTruncateSize,

        [Parameter()]
        [Boolean]
        $LoglocalTimeRollover,

        [Parameter()]
        [ValidateSet('IIS','W3C','NCSA')]
        [String]
        $LogFormat,

        [Parameter()]
        [ValidateSet('File','ETW','File,ETW')]
        [String]
        $LogTargetW3C,

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $LogCustomFields
    )

    Assert-Module

    $inDesiredState = $true

    $website = Get-Website | Where-Object -FilterScript {$_.Name -eq $Name}

    # Check Ensure
    if (($Ensure -eq 'Present' -and $null -eq $website) -or `
        ($Ensure -eq 'Absent' -and $null -ne $website))
    {
        $inDesiredState = $false
        Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseEnsure `
                                -f $Name)
    }

    # Only check properties if website exists
    if ($Ensure -eq 'Present' -and `
        $null -ne $website)
    {
        # Check Site Id property.
        if ($SiteId -gt 0 -and $website.Id -ne $SiteId)
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseSiteId -f $Name)
        }

        # Check Physical Path property
        if ($PSBoundParameters.ContainsKey('PhysicalPath') -and `
            $website.PhysicalPath -ne $PhysicalPath)
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalsePhysicalPath `
                                    -f $Name)
        }

        # Check State
        if ($PSBoundParameters.ContainsKey('State') -and `
            $website.State -ne $State)
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseState `
                                    -f $Name)
        }

        # Check Application Pool property
        if ($PSBoundParameters.ContainsKey('ApplicationPool') -and `
            $website.ApplicationPool -ne $ApplicationPool)
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseApplicationPool `
                                    -f $Name)
        }

        # Check Binding properties
        if ($PSBoundParameters.ContainsKey('BindingInfo') -and `
            $null -ne $BindingInfo)
        {
            if (-not (Test-WebsiteBinding -Name $Name -BindingInfo $BindingInfo))
            {
                $inDesiredState = $false
                Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseBindingInfo `
                                        -f $Name)
            }
        }

        # Check Enabled Protocols
        if ($PSBoundParameters.ContainsKey('EnabledProtocols') -and `
            $website.EnabledProtocols -ne $EnabledProtocols)
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseEnabledProtocols `
                                    -f $Name)
        }

        # Check Default Pages
        if ($PSBoundParameters.ContainsKey('DefaultPage') -and `
            $null -ne $DefaultPage)
        {
            $allDefaultPages = @(
                Get-WebConfiguration -Filter '/system.webServer/defaultDocument/files/*' `
                                     -PSPath "IIS:\Sites\$Name" |
                ForEach-Object -Process { Write-Output -InputObject $_.value }
            )

            foreach ($page in $DefaultPage)
            {
                if ($allDefaultPages -inotcontains $page)
                {
                    $inDesiredState = $false
                    Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseDefaultPage `
                                            -f $Name)
                }
            }
        }

        #Check ServerAutoStart
        if ($PSBoundParameters.ContainsKey('ServerAutoStart') -and `
            $website.serverAutoStart -ne $ServerAutoStart)
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseAutoStart `
                                    -f $Name)
        }

        #Check AuthenticationInfo
        if ($PSBoundParameters.ContainsKey('AuthenticationInfo') -and `
            (-not (Test-AuthenticationInfo -Site $Name `
                                           -AuthenticationInfo $AuthenticationInfo)))
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseAuthenticationInfo)
        }

        #Check Preload
        if ($PSBoundParameters.ContainsKey('preloadEnabled') -and `
            $website.applicationDefaults.preloadEnabled -ne $PreloadEnabled)
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalsePreload `
                                    -f $Name)
        }

        #Check AutoStartEnabled
        if ($PSBoundParameters.ContainsKey('serviceAutoStartEnabled') -and `
            $website.applicationDefaults.serviceAutoStartEnabled -ne $ServiceAutoStartEnabled)
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseServiceAutoStart `
                                    -f $Name)
        }

        #Check AutoStartProviders
        if ($PSBoundParameters.ContainsKey('serviceAutoStartProvider') -and `
            $website.applicationDefaults.serviceAutoStartProvider -ne $ServiceAutoStartProvider)
        {
            if (-not (Confirm-UniqueServiceAutoStartProviders `
                        -serviceAutoStartProvider $ServiceAutoStartProvider `
                        -ApplicationType $ApplicationType))
            {
                $inDesiredState = $false
                Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseIISAutoStartProvider)
            }
        }

        # Check LogFormat
        if ($PSBoundParameters.ContainsKey('LogFormat'))
        {
            # Warn if LogFlags are passed in and Current LogFormat is not W3C
            if ($PSBoundParameters.ContainsKey('LogFlags') -and `
                $LogFormat -ne 'W3C')
            {
                Write-Verbose -Message ($script:localizedData.WarningIncorrectLogFormat `
                                        -f $Name)
            }

            # Warn if LogFlags are passed in and Desired LogFormat is not W3C
            if ($PSBoundParameters.ContainsKey('LogFlags') -and `
                $website.logfile.LogFormat -ne 'W3C')
            {
                Write-Verbose -Message ($script:localizedData.WarningIncorrectLogFormat `
                                        -f $Name)
            }

            # Check Log Format
            if ($LogFormat -ne $website.logfile.LogFormat)
            {
                $inDesiredState = $false
                Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseLogFormat `
                                        -f $Name)
            }
        }

        # Check LogFlags
        if ($PSBoundParameters.ContainsKey('LogFlags') -and `
            (-not (Compare-LogFlags -Name $Name -LogFlags $LogFlags)))
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseLogFlags)
        }

        # Check LogPath
        if ($PSBoundParameters.ContainsKey('LogPath') -and `
            ($LogPath -ne $website.logfile.directory))
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseLogPath `
                                    -f $Name)
        }

        # Check LogPeriod
        if ($PSBoundParameters.ContainsKey('LogPeriod') -and `
            ($LogPeriod -ne $website.logfile.period))
        {
            if ($PSBoundParameters.ContainsKey('LogTruncateSize'))
            {
                Write-Verbose -Message ($script:localizedData.WarningLogPeriod `
                                        -f $Name)
            }
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseLogPeriod `
                                    -f $Name)
        }

        # Check LogTruncateSize
        if ($PSBoundParameters.ContainsKey('LogTruncateSize') -and `
            ($LogTruncateSize -ne $website.logfile.LogTruncateSize))
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseLogTruncateSize `
                                    -f $Name)
        }

        # Check LoglocalTimeRollover
        if ($PSBoundParameters.ContainsKey('LoglocalTimeRollover') -and `
            ($LoglocalTimeRollover -ne `
            ([System.Convert]::ToBoolean($website.logfile.LocalTimeRollover))))
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseLoglocalTimeRollover `
                                    -f $Name)
        }

        # Check LogTargetW3C
        if ($PSBoundParameters.ContainsKey('LogTargetW3C') -and `
            ($LogTargetW3C -ne $website.logfile.LogTargetW3C))
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseLogTargetW3C `
                                    -f $Name)
        }

        # Check LogCustomFields if needed
        if ($PSBoundParameters.ContainsKey('LogCustomFields') -and `
            (-not (Test-LogCustomField -Site $Name -LogCustomField $LogCustomFields)))
        {
            $inDesiredState = $false
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetUpdateLogCustomFields `
                                    -f $Name)
        }
    }

    if ($inDesiredState -eq $true)
    {
        Write-Verbose -Message ($script:localizedData.VerboseTestTargetTrueResult)
    }
    else
    {
        Write-Verbose -Message ($script:localizedData.VerboseTestTargetFalseResult)
    }

    return $inDesiredState
}

#region Helper Functions

<#
        .SYNOPSIS
        Helper function used to validate that the logflags status.
        Returns False if the loglfags do not match and true if they do

        .PARAMETER LogFlags
        Specifies flags to check

        .PARAMETER Name
        Specifies website to check the flags on
#>
function Compare-LogFlags
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String[]]
        [ValidateSet('Date','Time','ClientIP','UserName','SiteName','ComputerName','ServerIP','Method','UriStem','UriQuery','HttpStatus','Win32Status','BytesSent','BytesRecv','TimeTaken','ServerPort','UserAgent','Cookie','Referer','ProtocolVersion','Host','HttpSubStatus')]
        $LogFlags,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name

    )

    $currentLogFlags = (Get-Website -Name $Name).logfile.logExtFileFlags -split ',' | Sort-Object
    $proposedLogFlags = $LogFlags -split ',' | Sort-Object

    if (Compare-Object -ReferenceObject $currentLogFlags -DifferenceObject $proposedLogFlags)
    {
        return $false
    }

    return $true

}

<#
        .SYNOPSIS
        Helper function used to validate that the website's binding information is unique to other
        websites. Returns False if at least one of the bindings is already assigned to another
        website.

        .PARAMETER Name
        Specifies the name of the website.

        .PARAMETER ExcludeStopped
        Omits stopped websites.

        .NOTES
        This function tests standard ('http' and 'https') bindings only.
        It is technically possible to assign identical non-standard bindings (such as 'net.tcp')
        to different websites.
#>
function Confirm-UniqueBinding
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name,

        [Parameter()]
        [Switch]
        $ExcludeStopped
    )

    $website = Get-Website | Where-Object -FilterScript { $_.Name -eq $Name }

    if (-not $website)
    {
        $errorMessage = $script:localizedData.ErrorWebsiteNotFound `
                        -f $Name
        New-TerminatingError -ErrorId 'WebsiteNotFound' `
                             -ErrorMessage $errorMessage `
                             -ErrorCategory 'InvalidResult'
    }

    $referenceObject = @(
        $website.bindings.Collection |
        Where-Object -FilterScript { $_.protocol -in @('http', 'https') } |
        ConvertTo-WebBinding -Verbose:$false
    )

    if ($ExcludeStopped)
    {
        $otherWebsiteFilter = { $_.Name -ne $website.Name -and $_.State -ne 'Stopped' }
    }
    else
    {
        $otherWebsiteFilter = { $_.Name -ne $website.Name }
    }

    $differenceObject = @(
        Get-Website |
        Where-Object -FilterScript $otherWebsiteFilter |
        ForEach-Object -Process { $_.bindings.Collection } |
        Where-Object -FilterScript { $_.protocol -in @('http', 'https') } |
        ConvertTo-WebBinding -Verbose:$false
    )

    # Assume that bindings are unique
    $result = $true

    $compareSplat = @{
        ReferenceObject  = $referenceObject
        DifferenceObject = $differenceObject
        Property         = @('protocol', 'bindingInformation')
        ExcludeDifferent = $true
        IncludeEqual     = $true
    }

    if (Compare-Object @compareSplat)
    {
        $result = $false
    }

    return $result
}

<#
        .SYNOPSIS
        Helper function used to validate that the AutoStartProviders is unique to other websites.
        returns False if the AutoStartProviders exist.

        .PARAMETER ServiceAutoStartProvider
        Specifies the name of the AutoStartProviders.

        .PARAMETER ApplicationType
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
        [String]
        $ServiceAutoStartProvider,

        [Parameter(Mandatory = $true)]
        [String]
        $ApplicationType
    )

    $websiteASP = (Get-WebConfiguration `
                   -filter /system.applicationHost/serviceAutoStartProviders).Collection

    $existingObject = $websiteASP | `
        Where-Object -Property Name -eq -Value $ServiceAutoStartProvider | `
        Select-Object Name,Type

    $proposedObject = New-Object -TypeName PSObject -Property @{
        name = $ServiceAutoStartProvider
        type = $ApplicationType
    }

    if (-not $existingObject)
    {
        return $false
    }

    if (-not (Compare-Object -ReferenceObject $existingObject `
                            -DifferenceObject $proposedObject `
                            -Property name))
    {
        if (Compare-Object -ReferenceObject $existingObject `
                          -DifferenceObject $proposedObject `
                          -Property type)
        {
            $errorMessage = $script:localizedData.ErrorWebsiteTestAutoStartProviderFailure
            New-TerminatingError -ErrorId 'ErrorWebsiteTestAutoStartProviderFailure' `
                                 -ErrorMessage $errorMessage `
                                 -ErrorCategory 'InvalidResult'`
        }
    }

    return $true

}

<#
        .SYNOPSIS
        Converts IIS <binding> elements to instances of the MSFT_xWebBindingInformation CIM class.
#>
function ConvertTo-CimBinding
{
    [CmdletBinding()]
    [OutputType([Microsoft.Management.Infrastructure.CimInstance])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [AllowEmptyCollection()]
        [AllowNull()]
        [Object[]]
        $InputObject
    )

    begin
    {
        $cimClassName = 'MSFT_xWebBindingInformation'
        $cimNamespace = 'root/microsoft/Windows/DesiredStateConfiguration'
    }

    process
    {
        foreach ($binding in $InputObject)
        {
            [Hashtable]$cimProperties = @{
                Protocol           = [String]$binding.protocol
                BindingInformation = [String]$binding.bindingInformation
            }

            if ($binding.Protocol -in @('http', 'https'))
            {
                # Extract IPv6 address
                if ($binding.bindingInformation -match '^\[(.*?)\]\:(.*?)\:(.*?)$')
                {
                    $IPAddress = $Matches[1]
                    $Port      = $Matches[2]
                    $HostName  = $Matches[3]
                }
                else
                {
                    $IPAddress, $Port, $HostName = $binding.bindingInformation -split '\:'
                }

                if ([String]::IsNullOrEmpty($IPAddress))
                {
                    $IPAddress = '*'
                }

                $cimProperties.Add('IPAddress', [String]$IPAddress)
                $cimProperties.Add('Port',      [UInt16]$Port)
                $cimProperties.Add('HostName',  [String]$HostName)
            }
            else
            {
                $cimProperties.Add('IPAddress', [String]::Empty)
                $cimProperties.Add('Port',      [UInt16]::MinValue)
                $cimProperties.Add('HostName',  [String]::Empty)
            }

            if ([Environment]::OSVersion.Version -ge '6.2')
            {
                $cimProperties.Add('SslFlags', [String]$binding.sslFlags)
            }

            $cimProperties.Add('CertificateThumbprint', [String]$binding.certificateHash)
            $cimProperties.Add('CertificateStoreName',  [String]$binding.certificateStoreName)

            New-CimInstance -ClassName $cimClassName `
                            -Namespace $cimNamespace `
                            -Property $cimProperties `
                            -ClientOnly
        }
    }
}

<#
        .SYNOPSIS
        Converts instances of the MSFT_xWebBindingInformation CIM class to the IIS <binding>
        element representation.

        .LINK
        https://www.iis.net/configreference/system.applicationhost/sites/site/bindings/binding
#>
function ConvertTo-WebBinding
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [AllowEmptyCollection()]
        [AllowNull()]
        [Object[]]
        $InputObject
    )
    process
    {
        foreach ($binding in $InputObject)
        {
            $outputObject = @{
                protocol = $binding.Protocol
            }

            if ($binding -is [Microsoft.Management.Infrastructure.CimInstance])
            {
                if ($binding.Protocol -in @('http', 'https'))
                {
                    if (-not [String]::IsNullOrEmpty($binding.BindingInformation))
                    {
                        if (-not [String]::IsNullOrEmpty($binding.IPAddress) -or
                            -not [String]::IsNullOrEmpty($binding.Port) -or
                            -not [String]::IsNullOrEmpty($binding.HostName)
                        )
                        {
                            $isJoinRequired = $true
                            Write-Verbose -Message `
                                ($script:localizedData.VerboseConvertToWebBindingIgnoreBindingInformation `
                                -f $binding.Protocol)
                        }
                        else
                        {
                            $isJoinRequired = $false
                        }
                    }
                    else
                    {
                        $isJoinRequired = $true
                    }

                    # Construct the bindingInformation attribute
                    if ($isJoinRequired -eq $true)
                    {
                        $ipAddressString = Format-IPAddressString -InputString $binding.IPAddress `
                                                                   -ErrorAction Stop

                        if ([String]::IsNullOrEmpty($binding.Port))
                        {
                            switch ($binding.Protocol)
                            {
                                'http'  { $portNumberString = '80' }
                                'https' { $portNumberString = '443' }
                            }

                            Write-Verbose -Message `
                                ($script:localizedData.VerboseConvertToWebBindingDefaultPort `
                                -f $binding.Protocol, $portNumberString)
                        }
                        else
                        {
                            if (Test-PortNumber -InputString $binding.Port)
                            {
                                $portNumberString = $binding.Port
                            }
                            else
                            {
                                $errorMessage = $script:localizedData.ErrorWebBindingInvalidPort `
                                                -f $binding.Port
                                New-TerminatingError -ErrorId 'WebBindingInvalidPort' `
                                                     -ErrorMessage $errorMessage `
                                                     -ErrorCategory 'InvalidArgument'
                            }
                        }

                        $bindingInformation = $ipAddressString, `
                                              $portNumberString, `
                                              $binding.HostName -join ':'
                        $outputObject.Add('bindingInformation', [String]$bindingInformation)
                    }
                    else
                    {
                        $outputObject.Add('bindingInformation', [String]$binding.BindingInformation)
                    }
                }
                else
                {
                    if ([String]::IsNullOrEmpty($binding.BindingInformation))
                    {
                        $errorMessage = $script:localizedData.ErrorWebBindingMissingBindingInformation `
                                        -f $binding.Protocol
                        New-TerminatingError -ErrorId 'WebBindingMissingBindingInformation' `
                                             -ErrorMessage $errorMessage `
                                             -ErrorCategory 'InvalidArgument'
                    }
                    else
                    {
                        $outputObject.Add('bindingInformation', [String]$binding.BindingInformation)
                    }
                }

                # SSL-related properties
                if ($binding.Protocol -eq 'https')
                {
                    if ([String]::IsNullOrEmpty($binding.CertificateThumbprint))
                    {
                        if ($Binding.CertificateSubject)
                        {
                            if ($binding.CertificateSubject.substring(0,3) -ne 'CN=')
                            {
                                $binding.CertificateSubject = "CN=$($Binding.CertificateSubject)"
                            }
                            $FindCertificateSplat = @{
                                Subject = $Binding.CertificateSubject
                            }
                        }
                        else
                        {
                            $errorMessage = $script:localizedData.ErrorWebBindingMissingCertificateThumbprint `
                                            -f $binding.Protocol
                            New-TerminatingError -ErrorId 'WebBindingMissingCertificateThumbprint' `
                                                -ErrorMessage $errorMessage `
                                                -ErrorCategory 'InvalidArgument'
                        }
                    }

                    if ([String]::IsNullOrEmpty($binding.CertificateStoreName))
                    {
                        $certificateStoreName = 'MY'
                        Write-Verbose -Message `
                            ($script:localizedData.VerboseConvertToWebBindingDefaultCertificateStoreName `
                            -f $certificateStoreName)
                    }
                    else
                    {
                        $certificateStoreName = $binding.CertificateStoreName
                    }

                    $certificateHash = $null
                    if ($FindCertificateSplat)
                    {
                        $FindCertificateSplat.Add('Store',$CertificateStoreName)
                        $Certificate = Find-Certificate @FindCertificateSplat | Select-Object -First 1
                        if ($Certificate)
                        {
                            $certificateHash = $Certificate.Thumbprint
                        }
                        else
                        {
                            $errorMessage = $script:localizedData.ErrorWebBindingInvalidCertificateSubject `
                                            -f $binding.CertificateSubject, $binding.CertificateStoreName
                            New-TerminatingError -ErrorId 'WebBindingInvalidCertificateSubject' `
                                                -ErrorMessage $errorMessage `
                                                -ErrorCategory 'InvalidArgument'
                        }
                    }

                    # Remove the Left-to-Right Mark character
                    if ($certificateHash)
                    {
                        $certificateHash = $certificateHash -replace '^\u200E'
                    }
                    else
                    {
                        $certificateHash = $binding.CertificateThumbprint -replace '^\u200E'
                    }

                    $outputObject.Add('certificateHash',      [String]$certificateHash)
                    $outputObject.Add('certificateStoreName', [String]$certificateStoreName)

                    if ([Environment]::OSVersion.Version -ge '6.2')
                    {
                        $sslFlags = [Int64]$binding.SslFlags

                        if ($sslFlags -in @(1, 3) -and [String]::IsNullOrEmpty($binding.HostName))
                        {
                            $errorMessage = $script:localizedData.ErrorWebBindingMissingSniHostName
                            New-TerminatingError -ErrorId 'WebBindingMissingSniHostName' `
                                                 -ErrorMessage $errorMessage `
                                                 -ErrorCategory 'InvalidArgument'
                        }

                        $outputObject.Add('sslFlags', $sslFlags)
                    }
                }
                else
                {
                    # Ignore SSL-related properties for non-SSL bindings
                    $outputObject.Add('certificateHash',      [String]::Empty)
                    $outputObject.Add('certificateStoreName', [String]::Empty)

                    if ([Environment]::OSVersion.Version -ge '6.2')
                    {
                        $outputObject.Add('sslFlags', [Int64]0)
                    }
                }
            }
            else
            {
                <#
                        WebAdministration can throw the following exception if there are non-standard
                        bindings (such as 'net.tcp'): 'The data is invalid.
                        (Exception from HRESULT: 0x8007000D)'

                        Steps to reproduce:
                        1) Add 'net.tcp' binding
                        2) Execute {Get-Website | `
                                ForEach-Object {$_.bindings.Collection} | `
                                Select-Object *}

                        Workaround is to create a new custom object and use dot notation to
                        access binding properties.
                #>

                $outputObject.Add('bindingInformation',   [String]$binding.bindingInformation)
                $outputObject.Add('certificateHash',      [String]$binding.certificateHash)
                $outputObject.Add('certificateStoreName', [String]$binding.certificateStoreName)

                if ([Environment]::OSVersion.Version -ge '6.2')
                {
                    $outputObject.Add('sslFlags', [Int64]$binding.sslFlags)
                }
            }

            Write-Output -InputObject ([PSCustomObject]$outputObject)
        }
    }
}

<#
        .SYNOPSIS
        Converts IIS custom log field collection to instances of the MSFT_xLogCustomFieldInformation CIM class.
#>
function ConvertTo-CimLogCustomFields
{
    [CmdletBinding()]
    [OutputType([Microsoft.Management.Infrastructure.CimInstance[]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [AllowNull()]
        [Object[]]
        $InputObject
    )

    $cimClassName = 'MSFT_xLogCustomFieldInformation'
    $cimNamespace = 'root/microsoft/Windows/DesiredStateConfiguration'
    $cimCollection = New-Object -TypeName 'System.Collections.ObjectModel.Collection`1[Microsoft.Management.Infrastructure.CimInstance]'

    foreach ($customField in $InputObject)
    {
        $cimProperties = @{
            LogFieldName = $customField.LogFieldName
            SourceName   = $customField.SourceName
            SourceType   = $customField.SourceType
        }

        $cimCollection += (New-CimInstance -ClassName $cimClassName `
                        -Namespace $cimNamespace `
                        -Property $cimProperties `
                        -ClientOnly)
    }

    return $cimCollection
}

<#
        .SYNOPSYS
        Formats the input IP address string for use in the bindingInformation attribute.
#>
function Format-IPAddressString
{
    [CmdletBinding()]
    [OutputType([String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [AllowNull()]
        [String]
        $InputString
    )

    if ([String]::IsNullOrEmpty($InputString) -or $InputString -eq '*')
    {
        $outputString = '*'
    }
    else
    {
        try
        {
            $ipAddress = [IPAddress]::Parse($InputString)

            switch ($ipAddress.AddressFamily)
            {
                'InterNetwork'
                {
                    $outputString = $ipAddress.IPAddressToString
                }
                'InterNetworkV6'
                {
                    $outputString = '[{0}]' -f $ipAddress.IPAddressToString
                }
            }
        }
        catch
        {
            $errorMessage = $script:localizedData.ErrorWebBindingInvalidIPAddress `
                            -f $InputString, $_.Exception.Message
            New-TerminatingError -ErrorId 'WebBindingInvalidIPAddress' `
                                 -ErrorMessage $errorMessage `
                                 -ErrorCategory 'InvalidArgument'
        }
    }

    return $outputString
}

<#
        .SYNOPSIS
        Helper function used to validate that the authenticationProperties for an Application.

        .PARAMETER Site
        Specifies the name of the Website.
#>
function Get-AuthenticationInfo
{
    [CmdletBinding()]
    [OutputType([Microsoft.Management.Infrastructure.CimInstance])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]$Site
    )

    $authenticationProperties = @{}
    foreach ($type in @('Anonymous', 'Basic', 'Digest', 'Windows'))
    {
        $authenticationProperties[$type] = [Boolean](Test-AuthenticationEnabled -Site $Site `
                                                                               -Type $type)
    }

    return New-CimInstance `
            -ClassName MSFT_xWebAuthenticationInformation `
            -ClientOnly -Property $authenticationProperties `
            -NameSpace 'root\microsoft\windows\desiredstateconfiguration'
}

<#
        .SYNOPSIS
        Helper function used to build a default CimInstance for AuthenticationInformation
#>
function Get-DefaultAuthenticationInfo
{
    New-CimInstance -ClassName MSFT_xWebAuthenticationInformation `
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
        Helper function used to set authenticationProperties for an Application

        .PARAMETER Site
        Specifies the name of the Website.

        .PARAMETER Type
        Specifies the type of Authentication.
        Limited to the set: ('Anonymous','Basic','Digest','Windows')

        .PARAMETER Enabled
        Whether the Authentication is enabled or not.
#>
function Set-Authentication
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]$Site,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Anonymous','Basic','Digest','Windows')]
        [String]$Type,

        [Parameter()]
        [Boolean]$Enabled
    )

    Set-WebConfigurationProperty `
        -Filter /system.WebServer/security/authentication/${Type}Authentication `
        -Name enabled `
        -Value $Enabled `
        -Location $Site
}

<#
        .SYNOPSIS
        Helper function used to validate that the authenticationProperties for an Application.

        .PARAMETER Site
        Specifies the name of the Website.

        .PARAMETER AuthenticationInfo
        A CimInstance of what state the AuthenticationInfo should be.
#>
function Set-AuthenticationInfo
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]$Site,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Microsoft.Management.Infrastructure.CimInstance]$AuthenticationInfo
    )

    foreach ($type in @('Anonymous', 'Basic', 'Digest', 'Windows'))
    {
        $enabled = ($AuthenticationInfo.CimInstanceProperties[$type].Value -eq $true)
        Set-Authentication -Site $Site -Type $type -Enabled $enabled
    }
}

<#
        .SYNOPSIS
        Helper function used to set the LogCustomField for a website.

        .PARAMETER Site
        Specifies the name of the Website.

        .PARAMETER LogCustomField
        A CimInstance collection of what the LogCustomField should be.
#>
function Set-LogCustomField
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]
        $Site,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $LogCustomField
    )

    $setCustomFields = @()
    foreach ($customField in $LogCustomField)
    {
        $setCustomFields += @{
            logFieldName = $customField.LogFieldName
            sourceName = $customField.SourceName
            sourceType = $customField.SourceType
        }
    }

    # The second Set-WebConfigurationProperty is to handle an edge case where logfile.customFields is not updated correctly.  May be caused by a possible bug in the IIS provider
    for ($i = 1; $i -le 2; $i++)
    {
        Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter "system.applicationHost/sites/site[@name='$Site']/logFile/customFields" -Name "." -Value $setCustomFields
    }
}

<#
        .SYNOPSIS
        Helper function used to test the authenticationProperties state for an Application.
        Will return that value which will either [String]True or [String]False

        .PARAMETER Site
        Specifies the name of the Website.

        .PARAMETER Type
        Specifies the type of Authentication.
        Limited to the set: ('Anonymous','Basic','Digest','Windows').
#>
function Test-AuthenticationEnabled
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]$Site,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Anonymous','Basic','Digest','Windows')]
        [String]$Type
    )


    $prop = Get-WebConfigurationProperty `
        -Filter /system.WebServer/security/authentication/${Type}Authentication `
        -Name enabled `
        -Location $Site

    return $prop.Value
}

<#
        .SYNOPSIS
        Helper function used to test the authenticationProperties state for an Application.
        Will return that result for use in Test-TargetResource. Uses Test-AuthenticationEnabled
        to determine this. First incorrect result will break this function out.

        .PARAMETER Site
        Specifies the name of the Website.

        .PARAMETER AuthenticationInfo
        A CimInstance of what state the AuthenticationInfo should be.
#>
function Test-AuthenticationInfo
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]$Site,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [Microsoft.Management.Infrastructure.CimInstance]$AuthenticationInfo
    )

    $result = $true

    foreach ($type in @('Anonymous', 'Basic', 'Digest', 'Windows'))
    {
        $expected = $AuthenticationInfo.CimInstanceProperties[$type].Value
        $actual = Test-AuthenticationEnabled -Site $Site -Type $type
        if ($expected -ne $actual)
        {
            $result = $false
            break
        }
    }

    return $result
}

<#
        .SYNOPSIS
        Validates the desired binding information (i.e. no duplicate IP address, port, and
        host name combinations).
#>
function Test-BindingInfo
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $BindingInfo
    )

    $isValid = $true

    try
    {
        # Normalize the input (helper functions will perform additional validations)
        $bindings = @(ConvertTo-WebBinding -InputObject $bindingInfo | ConvertTo-CimBinding)
        $standardBindings = @($bindings | `
                                Where-Object -FilterScript {$_.Protocol -in @('http', 'https')})
        $nonStandardBindings = @($bindings | `
                                 Where-Object -FilterScript {$_.Protocol -notin @('http', 'https')})

        if ($standardBindings.Count -ne 0)
        {
            # IP address, port, and host name combination must be unique
            if (($standardBindings | Group-Object -Property IPAddress, Port, HostName) | Where-Object -FilterScript {$_.Count -ne 1})
            {
                $isValid = $false
                Write-Verbose -Message `
                    ($script:localizedData.VerboseTestBindingInfoSameIPAddressPortHostName)
            }

            # A single port cannot be simultaneously specified for bindings with different protocols
            foreach ($groupByPort in ($standardBindings | Group-Object -Property Port))
            {
                if (($groupByPort.Group | Group-Object -Property Protocol).Length -ne 1)
                {
                    $isValid = $false
                    Write-Verbose -Message `
                        ($script:localizedData.VerboseTestBindingInfoSamePortDifferentProtocol)
                    break
                }
            }
        }

        if ($nonStandardBindings.Count -ne 0)
        {
            if (($nonStandardBindings | `
                Group-Object -Property Protocol, BindingInformation) | `
                Where-Object -FilterScript {$_.Count -ne 1})
            {
                $isValid = $false
                Write-Verbose -Message `
                    ($script:localizedData.VerboseTestBindingInfoSameProtocolBindingInformation)
            }
        }
    }
    catch
    {
        $isValid = $false
        Write-Verbose -Message ($script:localizedData.VerboseTestBindingInfoInvalidCatch `
                                -f $_.Exception.Message)
    }

    return $isValid
}

<#
        .SYNOPSIS
        Validates that an input string represents a valid port number.
        The port number must be a positive integer between 1 and 65535.
#>
function Test-PortNumber
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [AllowNull()]
        [String]
        $InputString
    )

    try
    {
        $isValid = [UInt16]$InputString -ne 0
    }
    catch
    {
        $isValid = $false
    }

    return $isValid
}

<#
        .SYNOPSIS
        Helper function used to validate and compare website bindings of current to desired.
        Returns True if bindings do not need to be updated.
#>
function Test-WebsiteBinding
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name,

        [Parameter(Mandatory = $true)]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $BindingInfo
    )

    $inDesiredState = $true

    # Ensure that desired binding information is valid (i.e. no duplicate IP address, port, and
    # host name combinations).
    if (-not (Test-BindingInfo -BindingInfo $BindingInfo))
    {
        $errorMessage = $script:localizedData.ErrorWebsiteBindingInputInvalidation `
                        -f $Name
        New-TerminatingError -ErrorId 'WebsiteBindingInputInvalidation' `
                             -ErrorMessage $errorMessage `
                             -ErrorCategory 'InvalidResult'
    }

    try
    {
        $website = Get-Website | Where-Object -FilterScript {$_.Name -eq $Name}

        # Normalize binding objects to ensure they have the same representation
        $currentBindings = @(ConvertTo-WebBinding -InputObject $website.bindings.Collection `
                                                   -Verbose:$false)
        $desiredBindings = @(ConvertTo-WebBinding -InputObject $BindingInfo `
                                                  -Verbose:$false)

        $propertiesToCompare = 'protocol', `
                               'bindingInformation', `
                               'certificateHash', `
                               'certificateStoreName'

        # The sslFlags attribute was added in IIS 8.0.
        # This check is needed for backwards compatibility with Windows Server 2008 R2.
        if ([Environment]::OSVersion.Version -ge '6.2')
        {
            $propertiesToCompare += 'sslFlags'
        }

        if (Compare-Object -ReferenceObject $currentBindings `
                           -DifferenceObject $desiredBindings `
                           -Property $propertiesToCompare)
        {
            $inDesiredState = $false
        }
    }
    catch
    {
        $errorMessage = $script:localizedData.ErrorWebsiteCompareFailure `
                         -f $Name, $_.Exception.Message
        New-TerminatingError -ErrorId 'WebsiteCompareFailure' `
                             -ErrorMessage $errorMessage `
                             -ErrorCategory 'InvalidResult'
    }

    return $inDesiredState
}

<#
        .SYNOPSIS
        Helper function used to test the LogCustomField state for a website.

        .PARAMETER Site
        Specifies the name of the Website.

        .PARAMETER LogCustomField
        A CimInstance collection of what state the LogCustomField should be.
#>
function Test-LogCustomField
{
    [CmdletBinding()]
    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]
        $Site,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $LogCustomField
    )

    $inDesiredSate = $true

    foreach ($customField in $LogCustomField)
    {
        $filterString = "/system.applicationHost/sites/site[@name='{0}']/logFile/customFields/add[@logFieldName='{1}']" -f $Site, $customField.LogFieldName
        $presentCustomField = Get-WebConfigurationProperty -Filter $filterString -Name "."

        if ($presentCustomField)
        {
            $sourceNameMatch = $customField.SourceName -eq $presentCustomField.SourceName
            $sourceTypeMatch = $customField.SourceType -eq $presentCustomField.sourceType
            if (-not ($sourceNameMatch -and $sourceTypeMatch))
            {
                $inDesiredSate = $false
            }
        }
        else
        {
            $inDesiredSate = $false
        }
    }

    return $inDesiredSate
}

<#
        .SYNOPSIS
        Helper function used to update default pages of website.
#>
function Update-DefaultPage
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [String]
        $Name,

        [Parameter(Mandatory = $true)]
        [String[]]
        $DefaultPage
    )

    $allDefaultPages = @(
        Get-WebConfiguration -Filter '/system.webServer/defaultDocument/files/*' `
                             -PSPath "IIS:\Sites\$Name" |
        ForEach-Object -Process { Write-Output -InputObject $_.value }
    )

    foreach ($page in $DefaultPage)
    {
        if ($allDefaultPages -inotcontains $page)
        {
            Add-WebConfiguration -Filter '/system.webServer/defaultDocument/files' `
                                 -PSPath "IIS:\Sites\$Name" `
                                 -Value @{
                                     value = $page
                                 }
            Write-Verbose -Message ($script:localizedData.VerboseUpdateDefaultPageUpdated `
                                    -f $Name, $page)
        }
    }
}

<#
    .SYNOPSIS
        Updates website bindings.
#>
function Update-WebsiteBinding
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name,

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $BindingInfo
    )

    # Use Get-WebConfiguration instead of Get-Website to retrieve XPath of the target website.
    # XPath -Filter is case-sensitive. Use Where-Object to get the target website by name.
    $website = Get-WebConfiguration -Filter '/system.applicationHost/sites/site' |
        Where-Object -FilterScript {$_.Name -eq $Name}

    if (-not $website)
    {
        $errorMessage = $script:localizedData.ErrorWebsiteNotFound `
                        -f $Name
        New-TerminatingError -ErrorId 'WebsiteNotFound' `
                             -ErrorMessage $errorMessage `
                             -ErrorCategory 'InvalidResult'
    }

    ConvertTo-WebBinding -InputObject $BindingInfo -ErrorAction Stop |
    ForEach-Object -Begin {
        Clear-WebConfiguration -Filter "$($website.ItemXPath)/bindings" -Force -ErrorAction Stop
    } -Process {

        $properties = $_

        try
        {
            Add-WebConfiguration -Filter "$($website.ItemXPath)/bindings" -Value @{
                protocol = $properties.protocol
                bindingInformation = $properties.bindingInformation
            } -Force -ErrorAction Stop
        }
        catch
        {
            $errorMessage = $script:localizedData.ErrorWebsiteBindingUpdateFailure `
                            -f $Name, $_.Exception.Message
            New-TerminatingError -ErrorId 'WebsiteBindingUpdateFailure' `
                                 -ErrorMessage $errorMessage `
                                 -ErrorCategory 'InvalidResult'
        }

        if ($properties.protocol -eq 'https')
        {
            if ([Environment]::OSVersion.Version -ge '6.2')
            {
                try
                {
                    Set-WebConfigurationProperty `
                        -Filter "$($website.ItemXPath)/bindings/binding[last()]" `
                        -Name sslFlags `
                        -Value $properties.sslFlags `
                        -Force `
                        -ErrorAction Stop
                }
                catch
                {
                    $errorMessage = $script:localizedData.ErrorWebsiteBindingUpdateFailure `
                                    -f $Name, $_.Exception.Message
                    New-TerminatingError `
                        -ErrorId 'WebsiteBindingUpdateFailure' `
                        -ErrorMessage $errorMessage `
                        -ErrorCategory 'InvalidResult'
                }
            }

            try
            {
                $binding = Get-WebConfiguration `
                            -Filter "$($website.ItemXPath)/bindings/binding[last()]" `
                            -ErrorAction Stop
                $binding.AddSslCertificate($properties.certificateHash, `
                                           $properties.certificateStoreName)
            }
            catch
            {
                $errorMessage = $script:localizedData.ErrorWebBindingCertificate `
                                -f $properties.certificateHash, $_.Exception.Message
                New-TerminatingError `
                    -ErrorId 'WebBindingCertificate' `
                    -ErrorMessage $errorMessage `
                    -ErrorCategory 'InvalidOperation'
            }
        }
    }
}

#endregion

Export-ModuleMember -Function *-TargetResource
