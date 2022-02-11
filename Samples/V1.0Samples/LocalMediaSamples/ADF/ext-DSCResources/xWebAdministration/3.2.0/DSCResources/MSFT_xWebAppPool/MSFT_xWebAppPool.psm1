#requires -Version 4.0 -Modules CimCmdlets

$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'
$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'xWebAdministration.Common'

Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'xWebAdministration.Common.psm1')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_xWebAppPool'

# Writable properties except Ensure and Credential.
data PropertyData
{
    @(
        # General
        @{
            Name = 'State'
            Path = 'state'
        }
        @{
            Name = 'autoStart'
            Path = 'autoStart'
        }
        @{
            Name = 'CLRConfigFile'
            Path = 'CLRConfigFile'
        }
        @{
            Name = 'enable32BitAppOnWin64'
            Path = 'enable32BitAppOnWin64'
        }
        @{
            Name = 'enableConfigurationOverride'
            Path = 'enableConfigurationOverride'
        }
        @{
            Name = 'managedPipelineMode'
            Path = 'managedPipelineMode'
        }
        @{
            Name = 'managedRuntimeLoader'
            Path = 'managedRuntimeLoader'
        }
        @{
            Name = 'managedRuntimeVersion'
            Path = 'managedRuntimeVersion'
        }
        @{
            Name = 'passAnonymousToken'
            Path = 'passAnonymousToken'
        }
        @{
            Name = 'startMode'
            Path = 'startMode'
        }
        @{
            Name = 'queueLength'
            Path = 'queueLength'
        }

        # CPU
        @{
            Name = 'cpuAction'
            Path = 'cpu.action'
        }
        @{
            Name = 'cpuLimit'
            Path = 'cpu.limit'
        }
        @{
            Name = 'cpuResetInterval'
            Path = 'cpu.resetInterval'
        }
        @{
            Name = 'cpuSmpAffinitized'
            Path = 'cpu.smpAffinitized'
        }
        @{
            Name = 'cpuSmpProcessorAffinityMask'
            Path = 'cpu.smpProcessorAffinityMask'
        }
        @{
            Name = 'cpuSmpProcessorAffinityMask2'
            Path = 'cpu.smpProcessorAffinityMask2'
        }

        # Process Model
        @{
            Name = 'identityType'
            Path = 'processModel.identityType'
        }
        @{
            Name = 'idleTimeout'
            Path = 'processModel.idleTimeout'
        }
        @{
            Name = 'idleTimeoutAction'
            Path = 'processModel.idleTimeoutAction'
        }
        @{
            Name = 'loadUserProfile'
            Path = 'processModel.loadUserProfile'
        }
        @{
            Name = 'logEventOnProcessModel'
            Path = 'processModel.logEventOnProcessModel'
        }
        @{
            Name = 'logonType'
            Path = 'processModel.logonType'
        }
        @{
            Name = 'manualGroupMembership'
            Path = 'processModel.manualGroupMembership'
        }
        @{
            Name = 'maxProcesses'
            Path = 'processModel.maxProcesses'
        }
        @{
            Name = 'pingingEnabled'
            Path = 'processModel.pingingEnabled'
        }
        @{
            Name = 'pingInterval'
            Path = 'processModel.pingInterval'
        }
        @{
            Name = 'pingResponseTime'
            Path = 'processModel.pingResponseTime'
        }
        @{
            Name = 'setProfileEnvironment'
            Path = 'processModel.setProfileEnvironment'
        }
        @{
            Name = 'shutdownTimeLimit'
            Path = 'processModel.shutdownTimeLimit'
        }
        @{
            Name = 'startupTimeLimit'
            Path = 'processModel.startupTimeLimit'
        }

        # Process Orphaning
        @{
            Name = 'orphanActionExe'
            Path = 'failure.orphanActionExe'
        }
        @{
            Name = 'orphanActionParams'
            Path = 'failure.orphanActionParams'
        }
        @{
            Name = 'orphanWorkerProcess'
            Path = 'failure.orphanWorkerProcess'
        }

        # Rapid-Fail Protection
        @{
            Name = 'loadBalancerCapabilities'
            Path = 'failure.loadBalancerCapabilities'
        }
        @{
            Name = 'rapidFailProtection'
            Path = 'failure.rapidFailProtection'
        }
        @{
            Name = 'rapidFailProtectionInterval'
            Path = 'failure.rapidFailProtectionInterval'
        }
        @{
            Name = 'rapidFailProtectionMaxCrashes'
            Path = 'failure.rapidFailProtectionMaxCrashes'
        }
        @{
            Name = 'autoShutdownExe'
            Path = 'failure.autoShutdownExe'
        }
        @{
            Name = 'autoShutdownParams'
            Path = 'failure.autoShutdownParams'
        }

        # Recycling
        @{
            Name = 'disallowOverlappingRotation'
            Path = 'recycling.disallowOverlappingRotation'
        }
        @{
            Name = 'disallowRotationOnConfigChange'
            Path = 'recycling.disallowRotationOnConfigChange'
        }
        @{
            Name = 'logEventOnRecycle'
            Path = 'recycling.logEventOnRecycle'
        }
        @{
            Name = 'restartMemoryLimit'
            Path = 'recycling.periodicRestart.memory'
        }
        @{
            Name = 'restartPrivateMemoryLimit'
            Path = 'recycling.periodicRestart.privateMemory'
        }
        @{
            Name = 'restartRequestsLimit'
            Path = 'recycling.periodicRestart.requests'
        }
        @{
            Name = 'restartTimeLimit'
            Path = 'recycling.periodicRestart.time'
        }
        @{
            Name = 'restartSchedule'
            Path = 'recycling.periodicRestart.schedule'
        }
    )
}

# Properties that are specified as a single comma-separated string containing multiple flags
$script:commaSeparatedStringProperties = @(
    'logEventOnRecycle'
)

function Get-TargetResource
{
    <#
    .SYNOPSIS
        This will return a hashtable of results
    #>

    [CmdletBinding()]
    [OutputType([Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateLength(1, 64)]
        [String] $Name
    )

    Assert-Module

    # XPath -Filter is case-sensitive. Use Where-Object to get the target application pool by name.
    $appPool = Get-WebConfiguration -Filter '/system.applicationHost/applicationPools/add' |
        Where-Object -FilterScript {$_.name -eq $Name}

    $cimCredential = $null

    if ($null -eq $appPool)
    {
        Write-Verbose -Message ($script:localizedData.VerboseAppPoolNotFound -f $Name)

        $ensureResult = 'Absent'
    }
    else
    {
        Write-Verbose -Message ($script:localizedData.VerboseAppPoolFound -f $Name)

        $ensureResult = 'Present'

        if ($appPool.processModel.identityType -eq 'SpecificUser')
        {
            $cimCredential = New-CimInstance -ClientOnly `
                -ClassName MSFT_Credential `
                -Namespace root/microsoft/windows/DesiredStateConfiguration `
                -Property @{
                    UserName = [String]$appPool.processModel.userName
                    Password = [String]$appPool.processModel.password
                }
        }
    }

    $returnValue = @{
        Name = $Name
        Ensure = $ensureResult
        Credential = $cimCredential
    }

    $PropertyData.Where(
        {
            $_.Name -ne 'restartSchedule'
        }
    ).ForEach(
        {
            $property = Get-Property -Object $appPool -PropertyName $_.Path
            $returnValue.Add($_.Name, $property)
        }
    )

    $restartScheduleCurrent = [String[]]@(
        @($appPool.recycling.periodicRestart.schedule.Collection).ForEach('value')
    )

    $returnValue.Add('restartSchedule', $restartScheduleCurrent)

    return $returnValue
}

function Set-TargetResource
{
    <#
    .SYNOPSIS
        This will set the desired state
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateLength(1, 64)]
        [String] $Name,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [String] $Ensure = 'Present',

        [Parameter()]
        [ValidateSet('Started', 'Stopped')]
        [String] $State,

        [Parameter()]
        [Boolean] $autoStart,

        [Parameter()]
        [String] $CLRConfigFile,

        [Parameter()]
        [Boolean] $enable32BitAppOnWin64,

        [Parameter()]
        [Boolean] $enableConfigurationOverride,

        [Parameter()]
        [ValidateSet('Integrated', 'Classic')]
        [String] $managedPipelineMode,

        [Parameter()]
        [String] $managedRuntimeLoader,

        [Parameter()]
        [ValidateSet('v4.0', 'v2.0', '')]
        [String] $managedRuntimeVersion,

        [Parameter()]
        [Boolean] $passAnonymousToken,

        [Parameter()]
        [ValidateSet('OnDemand', 'AlwaysRunning')]
        [String] $startMode,

        [Parameter()]
        [ValidateRange(10, 65535)]
        [UInt32] $queueLength,

        [Parameter()]
        [ValidateSet('NoAction', 'KillW3wp', 'Throttle', 'ThrottleUnderLoad')]
        [String] $cpuAction,

        [Parameter()]
        [ValidateRange(0, 100000)]
        [UInt32] $cpuLimit,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(0, 1440)]$valueInMinutes = [TimeSpan]::Parse($_).TotalMinutes); $?
        })]
        [String] $cpuResetInterval,

        [Parameter()]
        [Boolean] $cpuSmpAffinitized,

        [Parameter()]
        [UInt32] $cpuSmpProcessorAffinityMask,

        [Parameter()]
        [UInt32] $cpuSmpProcessorAffinityMask2,

        [Parameter()]
        [ValidateSet(
                'ApplicationPoolIdentity', 'LocalService', 'LocalSystem',
                'NetworkService', 'SpecificUser'
        )]
        [String] $identityType,

        [Parameter()]
        [System.Management.Automation.PSCredential]
        [System.Management.Automation.Credential()]
        $Credential,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(0, 43200)]$valueInMinutes = [TimeSpan]::Parse($_).TotalMinutes); $?
        })]
        [String] $idleTimeout,

        [Parameter()]
        [ValidateSet('Terminate', 'Suspend')]
        [String] $idleTimeoutAction,

        [Parameter()]
        [Boolean] $loadUserProfile,

        [Parameter()]
        [String] $logEventOnProcessModel,

        [Parameter()]
        [ValidateSet('LogonBatch', 'LogonService')]
        [String] $logonType,

        [Parameter()]
        [Boolean] $manualGroupMembership,

        [Parameter()]
        [ValidateRange(0, 2147483647)]
        [UInt32] $maxProcesses,

        [Parameter()]
        [Boolean] $pingingEnabled,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1, 4294967)]$valueInSeconds = [TimeSpan]::Parse($_).TotalSeconds); $?
        })]
        [String] $pingInterval,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1, 4294967)]$valueInSeconds = [TimeSpan]::Parse($_).TotalSeconds); $?
        })]
        [String] $pingResponseTime,

        [Parameter()]
        [Boolean] $setProfileEnvironment,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1, 4294967)]$valueInSeconds = [TimeSpan]::Parse($_).TotalSeconds); $?
        })]
        [String] $shutdownTimeLimit,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1, 4294967)]$valueInSeconds = [TimeSpan]::Parse($_).TotalSeconds); $?
        })]
        [String] $startupTimeLimit,

        [Parameter()]
        [String] $orphanActionExe,

        [Parameter()]
        [String] $orphanActionParams,

        [Parameter()]
        [Boolean] $orphanWorkerProcess,

        [Parameter()]
        [ValidateSet('HttpLevel', 'TcpLevel')]
        [String] $loadBalancerCapabilities,

        [Parameter()]
        [Boolean] $rapidFailProtection,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1, 144000)]$valueInMinutes = [TimeSpan]::Parse($_).TotalMinutes); $?
        })]
        [String] $rapidFailProtectionInterval,

        [Parameter()]
        [ValidateRange(0, 2147483647)]
        [UInt32] $rapidFailProtectionMaxCrashes,

        [Parameter()]
        [String] $autoShutdownExe,

        [Parameter()]
        [String] $autoShutdownParams,

        [Parameter()]
        [Boolean] $disallowOverlappingRotation,

        [Parameter()]
        [Boolean] $disallowRotationOnConfigChange,

        [Parameter()]
        [String] $logEventOnRecycle,

        [Parameter()]
        [UInt32] $restartMemoryLimit,

        [Parameter()]
        [UInt32] $restartPrivateMemoryLimit,

        [Parameter()]
        [UInt32] $restartRequestsLimit,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(0, 432000)]$valueInMinutes = [TimeSpan]::Parse($_).TotalMinutes); $?
        })]
        [String] $restartTimeLimit,

        [Parameter()]
        [ValidateScript({
            ($_ -eq '') -or
            (& {
                ([ValidateRange(0, 86399)]$valueInSeconds = [TimeSpan]::Parse($_).TotalSeconds); $?
            })
        })]
        [String[]] $restartSchedule
    )

    if (-not $PSCmdlet.ShouldProcess($Name))
    {
        return
    }

    Assert-Module

    $appPool = Get-WebConfiguration -Filter '/system.applicationHost/applicationPools/add' |
        Where-Object -FilterScript {$_.name -eq $Name}

    if ($Ensure -eq 'Present')
    {
        # Create Application Pool
        if ($null -eq $appPool)
        {
            Write-Verbose -Message ($script:localizedData.VerboseAppPoolNotFound -f $Name)
            Write-Verbose -Message ($script:localizedData.VerboseNewAppPool -f $Name)

            $appPool = New-WebAppPool -Name $Name -ErrorAction Stop
        }

        # Set Application Pool Properties
        if ($null -ne $appPool)
        {
            Write-Verbose -Message ($script:localizedData.VerboseAppPoolFound -f $Name)

            $PropertyData.Where(
                {
                    ($_.Name -in $PSBoundParameters.Keys) -and
                    ($_.Name -notin @('State', 'restartSchedule'))
                }
            ).ForEach(
                {
                    $propertyName = $_.Name
                    $propertyPath = $_.Path
                    $property = Get-Property -Object $appPool -PropertyName $propertyPath

                    if (
                        $PSBoundParameters[$propertyName] -ne $property
                    )
                    {
                        Write-Verbose -Message (
                            $script:localizedData.VerboseSetProperty -f $propertyName, $Name
                        )

                        Invoke-AppCmd -ArgumentList 'set', 'apppool', $Name, (
                            '/{0}:{1}' -f $propertyPath, $PSBoundParameters[$propertyName]
                        )
                    }
                }
            )

            if ($PSBoundParameters.ContainsKey('Credential'))
            {
                if ($PSBoundParameters['identityType'] -eq 'SpecificUser')
                {
                    if ($appPool.processModel.userName -ne $Credential.UserName)
                    {
                        Write-Verbose -Message (
                            $script:localizedData.VerboseSetProperty -f 'Credential (userName)', $Name
                        )

                        Invoke-AppCmd -ArgumentList 'set', 'apppool', $Name, (
                            '/processModel.userName:{0}' -f $Credential.UserName
                        )
                    }

                    $clearTextPassword = $Credential.GetNetworkCredential().Password

                    if ($appPool.processModel.password -cne $clearTextPassword)
                    {
                        Write-Verbose -Message (
                            $script:localizedData.VerboseSetProperty -f 'Credential (password)', $Name
                        )

                        Invoke-AppCmd -ArgumentList 'set', 'apppool', $Name, (
                            '/processModel.password:{0}' -f $clearTextPassword
                        )
                    }
                }
                else
                {
                    Write-Verbose -Message ($script:localizedData.VerboseCredentialToBeIgnored)
                }
            }

            # Ensure userName and password are cleared if identityType isn't set to SpecificUser.
            if (
                (
                    (
                        ($PSBoundParameters.ContainsKey('identityType') -eq $true) -and
                        ($PSBoundParameters['identityType'] -ne 'SpecificUser')
                    ) -or
                    (
                        ($PSBoundParameters.ContainsKey('identityType') -eq $false) -and
                        ($appPool.processModel.identityType -ne 'SpecificUser')
                    )
                ) -and
                (
                    ([String]::IsNullOrEmpty($appPool.processModel.userName) -eq $false) -or
                    ([String]::IsNullOrEmpty($appPool.processModel.password) -eq $false)
                )
            )
            {
                Write-Verbose -Message ($script:localizedData.VerboseClearCredential -f $Name)

                Invoke-AppCmd -ArgumentList 'set', 'apppool', $Name, '/processModel.userName:'
                Invoke-AppCmd -ArgumentList 'set', 'apppool', $Name, '/processModel.password:'
            }

            if ($PSBoundParameters.ContainsKey('restartSchedule'))
            {
                # Normalize the restartSchedule array values.
                $restartScheduleDesired = [String[]]@(
                    $restartSchedule.Where(
                        {
                            $_ -ne ''
                        }
                    ).ForEach(
                        {
                            [TimeSpan]::Parse($_).ToString('hh\:mm\:ss')
                        }
                    ) |
                    Select-Object -Unique
                )

                $restartScheduleCurrent = [String[]]@(
                    @($appPool.recycling.periodicRestart.schedule.Collection).ForEach('value')
                )

                Compare-Object -ReferenceObject $restartScheduleDesired `
                    -DifferenceObject $restartScheduleCurrent |
                        ForEach-Object -Process {

                            # Add value
                            if ($_.SideIndicator -eq '<=')
                            {
                                Write-Verbose -Message (
                                    $script:localizedData.VerboseRestartScheduleValueAdd -f
                                        $_.InputObject, $Name
                                )

                                Invoke-AppCmd -ArgumentList 'set', 'apppool', $Name, (
                                    "/+recycling.periodicRestart.schedule.[value='{0}']" -f $_.InputObject
                                )
                            }
                            # Remove value
                            else
                            {
                                Write-Verbose -Message (
                                    $script:localizedData.VerboseRestartScheduleValueRemove -f
                                        $_.InputObject, $Name
                                )

                                Invoke-AppCmd -ArgumentList 'set', 'apppool', $Name, (
                                    "/-recycling.periodicRestart.schedule.[value='{0}']" -f $_.InputObject
                                )
                            }

                        }
            }

            if ($PSBoundParameters.ContainsKey('State') -and $appPool.state -ne $State)
            {
                if ($State -eq 'Started')
                {
                    Write-Verbose -Message ($script:localizedData.VerboseStartAppPool -f $Name)

                    Start-WebAppPool -Name $Name -ErrorAction Stop
                }
                else
                {
                    Write-Verbose -Message ($script:localizedData.VerboseStopAppPool -f $Name)

                    Stop-WebAppPool -Name $Name -ErrorAction Stop
                }
            }
        }
    }
    else
    {
        # Remove Application Pool
        if ($null -ne $appPool)
        {
            Write-Verbose -Message ($script:localizedData.VerboseAppPoolFound -f $Name)

            if ($appPool.state -eq 'Started')
            {
                Write-Verbose -Message ($script:localizedData.VerboseStopAppPool -f $Name)

                Stop-WebAppPool -Name $Name -ErrorAction Stop
            }

            Write-Verbose -Message ($script:localizedData.VerboseRemoveAppPool -f $Name)

            Remove-WebAppPool -Name $Name -ErrorAction Stop
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.VerboseAppPoolNotFound -f $Name)
        }
    }
}

function Test-TargetResource
{
    <#
    .SYNOPSIS
        This tests the desired state. If the state is not correct it will return $false.
        If the state is correct it will return $true
    #>

    [OutputType([Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateLength(1, 64)]
        [String] $Name,

        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [String] $Ensure = 'Present',

        [Parameter()]
        [ValidateSet('Started', 'Stopped')]
        [String] $State,

        [Parameter()]
        [Boolean] $autoStart,

        [Parameter()]
        [String] $CLRConfigFile,

        [Parameter()]
        [Boolean] $enable32BitAppOnWin64,

        [Parameter()]
        [Boolean] $enableConfigurationOverride,

        [Parameter()]
        [ValidateSet('Integrated', 'Classic')]
        [String] $managedPipelineMode,

        [Parameter()]
        [String] $managedRuntimeLoader,

        [Parameter()]
        [ValidateSet('v4.0', 'v2.0', '')]
        [String] $managedRuntimeVersion,

        [Parameter()]
        [Boolean] $passAnonymousToken,

        [Parameter()]
        [ValidateSet('OnDemand', 'AlwaysRunning')]
        [String] $startMode,

        [Parameter()]
        [ValidateRange(10, 65535)]
        [UInt32] $queueLength,

        [Parameter()]
        [ValidateSet('NoAction', 'KillW3wp', 'Throttle', 'ThrottleUnderLoad')]
        [String] $cpuAction,

        [Parameter()]
        [ValidateRange(0, 100000)]
        [UInt32] $cpuLimit,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(0, 1440)]$valueInMinutes = [TimeSpan]::Parse($_).TotalMinutes); $?
        })]
        [String] $cpuResetInterval,

        [Parameter()]
        [Boolean] $cpuSmpAffinitized,

        [Parameter()]
        [UInt32] $cpuSmpProcessorAffinityMask,

        [Parameter()]
        [UInt32] $cpuSmpProcessorAffinityMask2,

        [Parameter()]
        [ValidateSet(
                'ApplicationPoolIdentity', 'LocalService', 'LocalSystem',
                'NetworkService', 'SpecificUser'
        )]
        [String] $identityType,

        [Parameter()]
        [System.Management.Automation.PSCredential]
        [System.Management.Automation.Credential()]
        $Credential,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(0, 43200)]$valueInMinutes = [TimeSpan]::Parse($_).TotalMinutes); $?
        })]
        [String] $idleTimeout,

        [Parameter()]
        [ValidateSet('Terminate', 'Suspend')]
        [String] $idleTimeoutAction,

        [Parameter()]
        [Boolean] $loadUserProfile,

        [Parameter()]
        [String] $logEventOnProcessModel,

        [Parameter()]
        [ValidateSet('LogonBatch', 'LogonService')]
        [String] $logonType,

        [Parameter()]
        [Boolean] $manualGroupMembership,

        [Parameter()]
        [ValidateRange(0, 2147483647)]
        [UInt32] $maxProcesses,

        [Parameter()]
        [Boolean] $pingingEnabled,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1, 4294967)]$valueInSeconds = [TimeSpan]::Parse($_).TotalSeconds); $?
        })]
        [String] $pingInterval,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1, 4294967)]$valueInSeconds = [TimeSpan]::Parse($_).TotalSeconds); $?
        })]
        [String] $pingResponseTime,

        [Parameter()]
        [Boolean] $setProfileEnvironment,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1, 4294967)]$valueInSeconds = [TimeSpan]::Parse($_).TotalSeconds); $?
        })]
        [String] $shutdownTimeLimit,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1, 4294967)]$valueInSeconds = [TimeSpan]::Parse($_).TotalSeconds); $?
        })]
        [String] $startupTimeLimit,

        [Parameter()]
        [String] $orphanActionExe,

        [Parameter()]
        [String] $orphanActionParams,

        [Parameter()]
        [Boolean] $orphanWorkerProcess,

        [Parameter()]
        [ValidateSet('HttpLevel', 'TcpLevel')]
        [String] $loadBalancerCapabilities,

        [Parameter()]
        [Boolean] $rapidFailProtection,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(1, 144000)]$valueInMinutes = [TimeSpan]::Parse($_).TotalMinutes); $?
        })]
        [String] $rapidFailProtectionInterval,

        [Parameter()]
        [ValidateRange(0, 2147483647)]
        [UInt32] $rapidFailProtectionMaxCrashes,

        [Parameter()]
        [String] $autoShutdownExe,

        [Parameter()]
        [String] $autoShutdownParams,

        [Parameter()]
        [Boolean] $disallowOverlappingRotation,

        [Parameter()]
        [Boolean] $disallowRotationOnConfigChange,

        [Parameter()]
        [String] $logEventOnRecycle,

        [Parameter()]
        [UInt32] $restartMemoryLimit,

        [Parameter()]
        [UInt32] $restartPrivateMemoryLimit,

        [Parameter()]
        [UInt32] $restartRequestsLimit,

        [Parameter()]
        [ValidateScript({
            ([ValidateRange(0, 432000)]$valueInMinutes = [TimeSpan]::Parse($_).TotalMinutes); $?
        })]
        [String] $restartTimeLimit,

        [Parameter()]
        [ValidateScript({
            ($_ -eq '') -or
            (& {
                ([ValidateRange(0, 86399)]$valueInSeconds = [TimeSpan]::Parse($_).TotalSeconds); $?
            })
        })]
        [String[]] $restartSchedule
    )

    Assert-Module

    $inDesiredState = $true

    $appPool = Get-WebConfiguration -Filter '/system.applicationHost/applicationPools/add' |
        Where-Object -FilterScript {$_.name -eq $Name}

    if (
        ($Ensure -eq 'Absent' -and $null -ne $appPool) -or
        ($Ensure -eq 'Present' -and $null -eq $appPool)
    )
    {
        $inDesiredState = $false

        if ($null -ne $appPool)
        {
            Write-Verbose -Message ($script:localizedData.VerboseAppPoolFound -f $Name)
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.VerboseAppPoolNotFound -f $Name)
        }

        Write-Verbose -Message ($script:localizedData.VerboseEnsureNotInDesiredState -f $Name)
    }

    if ($Ensure -eq 'Present' -and $null -ne $appPool)
    {
        Write-Verbose -Message ($script:localizedData.VerboseAppPoolFound -f $Name)

        $PropertyData.Where(
            {
                ($_.Name -in $PSBoundParameters.Keys) -and
                ($_.Name -ne 'restartSchedule')
            }
        ).ForEach(
            {
                $propertyName = $_.Name
                $propertyPath = $_.Path
                $property = Get-Property -Object $appPool -PropertyName $propertyPath

                # First check if the property is a single comma-separated string containing multiple flags, split and compare membership if so
                if ($propertyName -in $script:commaSeparatedStringProperties)
                {
                    $currentPropertyCollection = $property.Split(',')
                    $expectedPropertyCollection = $PSBoundParameters[$propertyName].Split(',')

                    $compareResult = @(Compare-Object -ReferenceObject $currentPropertyCollection -DifferenceObject $expectedPropertyCollection)
                    if ($compareResult.Length -ne 0)
                    {
                        Write-Verbose -Message (
                            $script:localizedData.VerbosePropertyNotInDesiredState -f $propertyName, $Name
                        )

                        $inDesiredState = $false
                    }
                }
                elseif (
                    $PSBoundParameters[$propertyName] -ne $property
                )
                {
                    Write-Verbose -Message (
                        $script:localizedData.VerbosePropertyNotInDesiredState -f $propertyName, $Name
                    )

                    $inDesiredState = $false
                }
            }
        )

        if ($PSBoundParameters.ContainsKey('Credential'))
        {
            if ($PSBoundParameters['identityType'] -eq 'SpecificUser')
            {
                if ($appPool.processModel.userName -ne $Credential.UserName)
                {
                    Write-Verbose -Message (
                        $script:localizedData.VerbosePropertyNotInDesiredState -f
                            'Credential (userName)', $Name
                    )

                    $inDesiredState = $false
                }

                $clearTextPassword = $Credential.GetNetworkCredential().Password

                if ($appPool.processModel.password -cne $clearTextPassword)
                {
                    Write-Verbose -Message (
                        $script:localizedData.VerbosePropertyNotInDesiredState -f
                            'Credential (password)', $Name
                    )

                    $inDesiredState = $false
                }
            }
            else
            {
                Write-Verbose -Message ($script:localizedData.VerboseCredentialToBeIgnored)
            }
        }

        # Ensure userName and password are cleared if identityType isn't set to SpecificUser.
        if (
            (
                (
                    ($PSBoundParameters.ContainsKey('identityType') -eq $true) -and
                    ($PSBoundParameters['identityType'] -ne 'SpecificUser')
                ) -or
                (
                    ($PSBoundParameters.ContainsKey('identityType') -eq $false) -and
                    ($appPool.processModel.identityType -ne 'SpecificUser')
                )
            ) -and
            (
                ([String]::IsNullOrEmpty($appPool.processModel.userName) -eq $false) -or
                ([String]::IsNullOrEmpty($appPool.processModel.password) -eq $false)
            )
        )
        {
            Write-Verbose -Message ($script:localizedData.VerboseCredentialToBeCleared -f $Name)

            $inDesiredState = $false
        }

        if ($PSBoundParameters.ContainsKey('restartSchedule'))
        {
            # Normalize the restartSchedule array values.
            $restartScheduleDesired = [String[]]@(
                $restartSchedule.Where(
                    {
                        $_ -ne ''
                    }
                ).ForEach(
                    {
                        [TimeSpan]::Parse($_).ToString('hh\:mm\:ss')
                    }
                ) |
                Select-Object -Unique
            )

            $restartScheduleCurrent = [String[]]@(
                @($appPool.recycling.periodicRestart.schedule.Collection).ForEach('value')
            )

            if (
                Compare-Object -ReferenceObject $restartScheduleDesired `
                    -DifferenceObject $restartScheduleCurrent
            )
            {
                Write-Verbose -Message (
                    $script:localizedData.VerbosePropertyNotInDesiredState -f 'restartSchedule', $Name
                )

                $inDesiredState = $false
            }
        }
    }

    if ($inDesiredState -eq $true)
    {
        Write-Verbose -Message ($script:localizedData.VerboseResourceInDesiredState)
    }
    else
    {
        Write-Verbose -Message ($script:localizedData.VerboseResourceNotInDesiredState)
    }

    return $inDesiredState
}

#region Helper Functions

function Get-Property
{
    param
    (
        [Parameter()]
        [object] $Object,

        [Parameter()]
        [string] $PropertyName)

    $parts = $PropertyName.Split('.')
    $firstPart = $parts[0]

    $value = $Object.$firstPart
    if ($parts.Count -gt 1)
    {
        $newParts = @()
        1..($parts.Count -1) | ForEach-Object {
            $newParts += $parts[$_]
        }

        $newName = ($newParts -join '.')
        return Get-Property -Object $value -PropertyName $newName
    }
    else
    {
        return $value
    }
}

<#
    .SYNOPSIS
        Runs appcmd.exe - if there's an error then the application will terminate

    .PARAMETER ArgumentList
        Optional list of string arguments to be passed into appcmd.exe

#>
function Invoke-AppCmd
{
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [String[]] $ArgumentList
    )

    <#
            This is a local preference for the function which will terminate
            the program if there's an error invoking appcmd.exe
    #>
    $ErrorActionPreference = 'Stop'

    $appcmdFilePath = "$env:SystemRoot\System32\inetsrv\appcmd.exe"

    $appcmdResult = $(& $appcmdFilePath $ArgumentList)
    Write-Verbose -Message $($appcmdResult).ToString()

    if ($LASTEXITCODE -ne 0)
    {
        $errorMessage = $script:localizedData.ErrorAppCmdNonZeroExitCode -f $LASTEXITCODE

        New-TerminatingError -ErrorId 'ErrorAppCmdNonZeroExitCode' `
            -ErrorMessage $errorMessage `
            -ErrorCategory 'InvalidResult'
    }
}

#endregion Helper Functions

Export-ModuleMember -Function *-TargetResource
