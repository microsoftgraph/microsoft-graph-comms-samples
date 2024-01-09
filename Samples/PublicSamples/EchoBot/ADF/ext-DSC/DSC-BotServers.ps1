$Configuration = 'BotServers'
Configuration $Configuration
{
    Param (
        [String]$DomainName,
        [PSCredential]$AdminCreds,
        [Int]$RetryCount = 30,
        [Int]$RetryIntervalSec = 120,
        [String]$StorageAccountId,
        [String]$AppInsightsConnectionString,
        [String]$Deployment,
        [String]$NetworkID,
        [String]$AppInfo,
        [String]$DataDiskInfo,
        [String]$clientIDLocal,
        [String]$clientIDGlobal,
        [switch]$NoDomainJoin
    )

    Import-DscResource -ModuleName PSDscResources
    Import-DscResource -ModuleName ComputerManagementDsc
    Import-DscResource -ModuleName ActiveDirectoryDSC
    Import-DscResource -ModuleName StorageDsc
    Import-DscResource -ModuleName xWebAdministration
    Import-DscResource -ModuleName xPSDesiredStateConfiguration -Name xRemoteFile, xPackage -ModuleVersion 9.1.0
    Import-DscResource -ModuleName SecurityPolicyDSC
    Import-DscResource -ModuleName xWindowsUpdate
    Import-DscResource -ModuleName xDSCFirewall
    Import-DscResource -ModuleName NetworkingDSC
    Import-DscResource -ModuleName xSystemSecurity
    Import-DscResource -ModuleName AZCOPYDSCDir         # https://github.com/brwilkinson/AZCOPYDSC
    Import-DscResource -ModuleName AppReleaseDSC        # https://github.com/brwilkinson/AppReleaseDSC
    Import-DscResource -ModuleName EnvironmentDSC       # https://github.com/brwilkinson/EnvironmentDSC

    # PowerShell Modules that you want deployed, comment out if not needed
    # Import-DscResource -ModuleName BRWAzure

    # Azure VM Metadata service
    $VMMeta = Invoke-RestMethod -Headers @{'Metadata' = 'true' } -Uri http://169.254.169.254/metadata/instance?api-version=2020-10-01 -Method get
    $Compute = $VMMeta.compute
    $Zone = $Compute.zone
    $NetworkInt = $VMMeta.network.interface
    $SubscriptionId = $Compute.subscriptionId
    $ResourceGroupName = $Compute.resourceGroupName
    

    # Azure VM Metadata service
    $LBMeta = Invoke-RestMethod -Headers @{'Metadata' = 'true' } -Uri http://169.254.169.254/metadata/loadbalancer?api-version=2020-10-01 -Method get
    $LB = $LBMeta.loadbalancer
    
    $prefix = $Deployment.split('-')[0]
    $OrgName = $Deployment.split('-')[1]
    $App = $Deployment.split('-')[2]
    $environment = $Deployment.split('-')[3]

    $StorageAccountName = Split-Path -Path $StorageAccountId -Leaf

    Function IIf
    {
        param($If, $IfTrue, $IfFalse)
        
        If ($If -IsNot 'Boolean') { $_ = $If }
        If ($If) { If ($IfTrue -is 'ScriptBlock') { &$IfTrue } Else { $IfTrue } }
        Else { If ($IfFalse -is 'ScriptBlock') { &$IfFalse } Else { $IfFalse } }
    }
    
    if ($NoDomainJoin)
    {
        [PSCredential]$DomainCreds = $AdminCreds
    }
    else
    {
        $NetBios = $(($DomainName -split '\.')[0])
        [PSCredential]$DomainCreds = [PSCredential]::New( $NetBios + '\' + $(($AdminCreds.UserName -split '\\')[-1]), $AdminCreds.Password )
    }

    $credlookup = @{
        'localadmin'  = $AdminCreds
        'DomainCreds' = $DomainCreds
        'DomainJoin'  = $DomainCreds
        'SQLService'  = $DomainCreds
        'usercreds'   = $AdminCreds
    }

    node $AllNodes.NodeName
    {
        [string]$computername = $Nodename

        Write-Verbose -Message $computername -Verbose

        Write-Verbose -Message "deployment: $deployment" -Verbose

        Write-Verbose -Message "environment: $environment" -Verbose

        LocalConfigurationManager
        {
            ActionAfterReboot    = 'ContinueConfiguration'
            ConfigurationMode    = iif $node.DSCConfigurationMode $node.DSCConfigurationMode 'ApplyAndMonitor'
            RebootNodeIfNeeded   = $True
            AllowModuleOverWrite = $true
        }

        #-------------------------------------------------------------------
        DnsConnectionSuffix $DomainName
        {
            InterfaceAlias                 = '*Ethernet*'
            RegisterThisConnectionsAddress = $true
            ConnectionSpecificSuffix       = $DomainName
            UseSuffixWhenRegistering       = $true
        }

        foreach ($certBinding in $Node.CertificatePortBinding)
        {
            script ('SetSSLCert_' + $certBinding.Name)
            {
                GetScript  = {
                    $certBinding = $using:certBinding
                    $result = netsh http show sslcert ipport=0.0.0.0:$($certBinding.Port)
                    @{
                        name  = $certBinding.Name
                        value = $result
                    }
                }#Get
                SetScript  = {
                    $certBinding = $using:certBinding
                    netsh http add sslcert ipport=0.0.0.0:$($certBinding.Port) certhash=$($env:AppSettings:CertificateThumbprint) appid=$($certBinding.AppId)
                }#Set 
                TestScript = {
                    $certBinding = $using:certBinding
                    $cert = netsh http show sslcert ipport=0.0.0.0:$($certBinding.Port)
                    $result = $cert | Where-Object { $_ -match '(Application ID.+: )(?<AppId>{.+})' }
                    try
                    {
                        if ($Matches.AppId -eq $certBinding.AppId)
                        {
                            $true
                        }
                        else
                        {
                            $false
                        }
                    }
                    catch
                    {
                        Write-Warning "Cert binding for app does not exist [$($certBinding.Name)]"
                        $false
                    }
                }#Test
            }
        }

        #-------------------------------------------------------------------
        TimeZone timezone
        { 
            IsSingleInstance = 'Yes'
            TimeZone         = iif $Node.timezone $Node.timezone 'Eastern Standard Time'
        }

        #-------------------------------------------------------------------
        IEEnhancedSecurityConfiguration DisableIEESC
        {
            Role    = 'Administrators'
            Enabled = IIF $Node.DisableIEESC (-Not $Node.DisableIEESC) $True
        }

        #-------------------------------------------------------------------
        foreach ($EnvVar in $Node.EnvironmentVarSet)
        {
            EnvironmentDSC $EnvVar.Name
            {
                Name                    = $EnvVar.Name
                Prefix                  = $EnvVar.Prefix
                KeyVaultName            = $EnvVar.KVName -f $Deployment
                Ensure                  = 'Present'
                ManagedIdentityClientID = $clientIDLocal
            }
            $dependsonEnvironmentDSC += @("[EnvironmentDSC]$($EnvVar.Name)")
        }

        #-------------------------------------------------------------------
        
        # #Local Policy
        # foreach ($LocalPolicy in $Node.LocalPolicyPresent)
        # {     
        #     $KeyValueName = $LocalPolicy.KeyValueName -replace $StringFilter 
        #     cAdministrativeTemplateSetting $KeyValueName
        #     {
        #         KeyValueName = $LocalPolicy.KeyValueName
        #         PolicyType   = $LocalPolicy.PolicyType
        #         # Data = $LocalPolicy.Data
        #         type         = $LocalPolicy.Type

        #     }
        # }

        #-------------------------------------------------------------------
        foreach ($Capability in $Node.WindowsCapabilityAbsent)
        {
            WindowsCapability $Capability.Name
            {
                Name   = $Capability.Name
                Ensure = 'Absent'
            }
            $dependsonFeatures += @("[WindowsCapability]$Capability")
        }

        #-------------------------------------------------------------------
        # Server
        if ($Node.WindowsFeatureSetPresent)
        {
            WindowsFeatureSet WindowsFeatureSetPresent
            {
                Ensure = 'Present'
                Name   = $Node.Present
            }
        }

        foreach ($Feature in $Node.WindowsFeaturePresent)
        {
            WindowsFeature $Feature
            {
                Name                 = $Feature
                Ensure               = 'Present'
                IncludeAllSubFeature = $true
            }
            $dependsonFeatures += @("[WindowsFeature]$Feature")
        }

        #-------------------------------------------------------------------
        # Client
        if ($Node.WindowsOptionalFeatureSetPresent)
        {
            WindowsOptionalFeatureSet WindowsOptionalFeatureSet
            {
                Ensure = 'Present'
                Name   = $Node.WindowsOptionalFeatureSetPresent
                #Source = $Node.SXSPath
            }
        }

        foreach ($Feature in $Node.WindowsOptionalFeaturePresent)
        {
            WindowsOptionalFeature $Feature
            {
                Name   = $Feature
                Ensure = 'Present'
            }
            $dependsonFeatures += @("[WindowsFeature]$Feature")
        }

        #-------------------------------------------------------------------
        if ($Node.Absent)
        {
            xWindowsFeatureSet WindowsFeatureSetAbsent
            {
                Ensure = 'Absent'
                Name   = $Node.WindowsFeatureSetAbsent
            }
        }

        #-------------------------------------------------------------------
        if ($Node.ServiceSetStopped)
        {
            ServiceSet ServiceSetStopped
            {
                Name  = $Node.ServiceSetStopped
                State = 'Stopped'
            }
        }

        #-------------------------------------------------------------------
        foreach ($disk in $Node.DisksPresent)
        {
            Disk $disk.DriveLetter
            {
                DiskID      = $disk.DiskID
                DriveLetter = $disk.DriveLetter
            }
            $dependsonDisksPresent += @("[Disk]$($disk.DriveLetter)")
        }

        #-------------------------------------------------------------------
        Service WindowsFirewall
        {
            Name        = 'MPSSvc'
            StartupType = 'Automatic'
            State       = 'Running'
        }

        #-------------------------------------------------------------------
        foreach ($FWRule in $Node.FWRules)
        {
            Firewall $FWRule.Name
            {
                Name      = $FWRule.Name
                Action    = 'Allow'
                Direction = 'Inbound'
                LocalPort = $FWRule.LocalPort
                Protocol  = 'TCP'
            }
        }

        # base install above - custom role install
        #-------------------------------------------------------------------

        foreach ($RegistryKey in $Node.RegistryKeyPresent)
        {
            Registry $RegistryKey.ValueName
            {
                Key                  = $RegistryKey.Key
                ValueName            = $RegistryKey.ValueName
                Ensure               = 'Present'
                ValueData            = $RegistryKey.ValueData
                ValueType            = $RegistryKey.ValueType
                Force                = $true
                PsDscRunAsCredential = $AdminCreds
            }
            $dependsonRegistryKey += @("[Registry]$($RegistryKey.ValueName)")
        }

        #-------------------------------------------------------------------
        foreach ($User in $Node.ADUserPresent)
        {
            xADUser $User.UserName
            {
                DomainName                    = $User.DomainName
                UserName                      = $User.Username
                Description                   = $User.Description
                Enabled                       = $True
                Password                      = $UserCreds
                #DomainController = $User.DomainController
                DomainAdministratorCredential = $credlookup['DomainJoin']
            }
            $dependsonUser += @("[xADUser]$($User.Username)")
        }
        
        #-------------------------------------------------------------------
        foreach ($UserRightsAssignment in $Node.UserRightsAssignmentPresent)
        {
            UserRightsAssignment $UserRightsAssignment.policy
            {
                Identity = $UserRightsAssignment.identity
                Policy   = $UserRightsAssignment.policy
            }
            $dependsonUserRightsAssignment += @("[UserRightsAssignment]$($UserRightsAssignment.policy)")
        }

        #-------------------------------------------------------------------
        #To clean up resource names use a regular expression to remove spaces, slashes and colons Etc.
        $StringFilter = '\W', ''

        #-------------------------------------------------------------------
        foreach ($Dir in $Node.DirectoryPresent)
        {
            $Name = $Dir -replace $StringFilter
            File $Name
            {
                DestinationPath      = $Dir
                Type                 = 'Directory'
                PsDscRunAsCredential = $credlookup['DomainCreds']
            }
            $dependsonDir += @("[File]$Name")
        }

        #-------------------------------------------------------------------     
        foreach ($AZCOPYDSCDir in $Node.AZCOPYDSCDirPresentSource)
        {
            $Name = ($AZCOPYDSCDir.SourcePathBlobURI + '_' + $AZCOPYDSCDir.DestinationPath) -replace $StringFilter 
            AZCOPYDSCDir $Name
            {
                SourcePath              = ($AZCOPYDSCDir.SourcePathBlobURI -f $StorageAccountName)
                DestinationPath         = $AZCOPYDSCDir.DestinationPath
                Ensure                  = 'Present'
                ManagedIdentityClientID = $clientIDGlobal
                LogDir                  = 'C:\azcopy_logs'
            }
            $dependsonAZCopyDSCDir += @("[AZCOPYDSCDir]$Name")
        }

        #-------------------------------------------------------------------
        foreach ($RemoteFile in $Node.RemoteFilePresent)
        {
            $Name = ($RemoteFile.DestinationPath + '_' + $RemoteFile.Uri) -replace $StringFilter 
            xRemoteFile $Name
            {
                DestinationPath = $RemoteFile.DestinationPath
                Uri             = $RemoteFile.Uri
                #UserAgent       = [Microsoft.PowerShell.Commands.PSUserAgent]::InternetExplorer
                #Headers         = @{ 'Accept-Language' = 'en-US' }
            }
        }

        #-------------------------------------------------------------------
        foreach ($AppComponent in $Node.AppReleaseDSCAppPresent)
        {
            AppReleaseDSC $AppComponent.ComponentName
            {
                ComponentName           = $AppComponent.ComponentName
                SourcePath              = ($AppComponent.SourcePathBlobURI -f $StorageAccountName)
                DestinationPath         = $AppComponent.DestinationPath
                ValidateFileName        = $AppComponent.ValidateFileName
                BuildFileName           = $AppComponent.BuildFileName
                EnvironmentName         = $environment
                Ensure                  = 'Present'
                ManagedIdentityClientID = $clientIDGlobal
                LogDir                  = 'C:\azcopy_logs'
                DeploySleepWaitSeconds  = $AppComponent.SleepTime
            }
            $dependsonAZCopyDSCDir += @("[AppReleaseDSC]$($AppComponent.ComponentName)")
        }

        #-------------------------------------------------------------------
        # Non PATH envs
        foreach ($EnvironmentVar in $Node.EnvironmentVarPresentVMSS)
        {
            $Name = $EnvironmentVar.Name -replace $StringFilter
            Environment $Name
            {
                Name  = $EnvironmentVar.Name
                Value = $EnvironmentVar.Value -f ($LB.inboundRules | Where-Object backendPort -EQ $EnvironmentVar.BackendPortMatch | ForEach-Object frontendPort)
            }
            $dependsonEnvironmentPathVMSS += @("[Environment]$Name")
        }

        #-------------------------------------------------------------------
        # Non PATH envs
        foreach ($EnvironmentVar in $Node.EnvironmentVarPresent)
        {
            $Name = $EnvironmentVar.Name -replace $StringFilter
            Environment $Name
            {
                Name  = $EnvironmentVar.Name
                Value = $EnvironmentVar.Value
            }
            $dependsonEnvironmentPath += @("[Environment]$Name")
        }

        #-----------------------------------------
        # Add Application Insights Key to Environment Variables
        if ($AppInsightsConnectionString)
        {
            Environment 'AppInsightsConnectionString'
            {
                Name  = 'APPLICATIONINSIGHTS_CONNECTION_STRING'
                Value = $AppInsightsConnectionString
            }
        }

        #-----------------------------------------
        foreach ($WebSite in $Node.WebSiteAbsent)
        {
            $Name = $WebSite.Name -replace ' ', ''
            xWebsite $Name
            {
                Name         = $WebSite.Name
                Ensure       = 'Absent'
                State        = 'Stopped'
                PhysicalPath = 'C:\inetpub\wwwroot'
                DependsOn    = $dependsonFeatures
            }
            $dependsonWebSitesAbsent += @("[xWebsite]$Name")
        }

        #-------------------------------------------------------------------
        foreach ($AppPool in $Node.WebAppPoolPresent)
        { 
            $Name = $AppPool.Name -replace $StringFilter

            xWebAppPool $Name
            {
                Name                  = ($AppPool.Name -f $environment)
                State                 = 'Started'
                autoStart             = $true
                DependsOn             = '[ServiceSet]ServiceSetStarted'
                managedRuntimeVersion = $AppPool.Version
                identityType          = 'SpecificUser'
                Credential            = $credlookup['DomainCreds']
                enable32BitAppOnWin64 = $AppPool.enable32BitAppOnWin64
            }
            $dependsonWebAppPool += @("[xWebAppPool]$Name")
        }

        #-------------------------------------------------------------------
        foreach ($WebSite in $Node.WebSitePresent)
        {
            $Name = $WebSite.Name -replace $StringFilter

            xWebsite $Name
            {
                Name            = ($WebSite.Name -f $environment)
                ApplicationPool = ($WebSite.ApplicationPool -f $environment)
                PhysicalPath    = $Website.PhysicalPath
                State           = 'Started'
                DependsOn       = $dependsonWebAppPools
                BindingInfo     = foreach ($Binding in $WebSite.BindingPresent)
                {
                    MSFT_xWebBindingInformation
                    {  
                        Protocol              = $binding.Protocol
                        Port                  = $binding.Port
                        IPAddress             = $binding.IpAddress
                        HostName              = ($binding.HostHeader -f $prefix, $orgname, $app, $environment)
                        CertificateThumbprint = $env:AppSettings:CertificateThumbprint
                        CertificateStoreName  = 'MY'
                    }
                }
            }
            $dependsonWebSites += @("[xWebsite]$Name")
        }

        #------------------------------------------------------
        foreach ($WebVirtualDirectory in $Node.VirtualDirectoryPresent)
        {
            xWebVirtualDirectory $WebVirtualDirectory.Name
            {
                Name                 = $WebVirtualDirectory.Name
                PhysicalPath         = $WebVirtualDirectory.PhysicalPath
                WebApplication       = $WebVirtualDirectory.WebApplication
                Website              = $WebVirtualDirectory.Website
                PsDscRunAsCredential = $credlookup['DomainCreds']
                Ensure               = 'Present'
                DependsOn            = $dependsonWebSites
            }
            $dependsonWebVirtualDirectory += @("[xWebVirtualDirectory]$($WebVirtualDirectory.name)")
        }

        # set virtual directory creds
        foreach ($WebVirtualDirectory in $Node.VirtualDirectoryPresent)
        {
            $vdname	= $WebVirtualDirectory.Name
            $wsname	= $WebVirtualDirectory.Website
            $pw = $credlookup['DomainCreds'].GetNetworkCredential().Password
            $Domain	= $credlookup['DomainCreds'].GetNetworkCredential().Domain
            $UserName = $credlookup['DomainCreds'].GetNetworkCredential().UserName

            Script $vdname
            {
                DependsOn  = $dependsonWebVirtualDirectory 
                
                GetScript  = {
                    Import-Module -Name 'webadministration'
                    $vd = Get-WebVirtualDirectory -site $using:wsname -Name $vdname
                    @{
                        path         = $vd.path
                        physicalPath = $vd.physicalPath
                        userName     = $vd.userName
                    }
                }#Get
                SetScript  = {
                    Import-Module -Name 'webadministration'
                    Set-ItemProperty -Path "IIS:\Sites\$using:wsname\$using:vdname" -Name userName -Value "$using:domain\$using:UserName"
                    Set-ItemProperty -Path "IIS:\Sites\$using:wsname\$using:vdname" -Name password -Value $using:pw
                }#Set 
                TestScript = {
                    Import-Module -Name 'webadministration'
                    Write-Warning $using:vdname
                    $vd = Get-WebVirtualDirectory -site $using:wsname -Name $using:vdname
                    if ($vd.userName -eq "$using:domain\$using:UserName")
                    {
                        $true
                    }
                    else
                    {
                        $false
                    }
                }#Test
            }#[Script]VirtualDirCreds
        }

        #------------------------------------------------------
        foreach ($WebApplication in $Node.WebApplicationsPresent)
        {
            xWebApplication $WebApplication.Name
            {
                Name         = $WebApplication.Name
                PhysicalPath = $WebApplication.PhysicalPath
                WebAppPool   = $WebApplication.ApplicationPool
                Website      = $WebApplication.Site
                Ensure       = 'Present'
                DependsOn    = $dependsonWebSites
            }
            $dependsonWebApplication += @("[xWebApplication]$($WebApplication.name)")
        }

        #-------------------------------------------------------------------
        # Run and SQL scripts
        foreach ($Script in $Node.SQLServerScriptsPresent)
        {
            $i = $Script.InstanceName -replace $StringFilter
            $Name = $Script.TestFilePath -replace $StringFilter
            SqlScript ($i + $Name)
            {
                InstanceName         = $Script.InstanceName
                SetFilePath          = $Script.SetFilePath
                GetFilePath          = $Script.GetFilePath
                TestFilePath         = $Script.TestFilePath
                PsDscRunAsCredential = $credlookup['SQLService']
            }

            $dependsonSQLServerScripts += @("[xSQLServerScript]$($Name)")
        }

        #-------------------------------------------------------------------
        # install font
        foreach ($Font in $Node.FontsPresent)
        {
            cFont $Font.Name
            {
                Ensure   = 'Present'
                FontFile = $Font.Path
                FontName = $Font.Name
            }
            $dependsonAppxPackage += @("[cAppxPackage]$($Font.Name)")
        }
        

        #-------------------------------------------------------------------
        # install appxpackage
        foreach ($AppxPackage in $Node.AppxPackagePresent)
        {
            $Name = $AppxPackage.Name -replace $StringFilter
            cAppxPackage $Name
            {
                Name           = $AppxPackage.Name
                DependencyPath = $AppxPackage.Dependency
                PackagePath    = $AppxPackage.Path
                # Register       = $AppxPackage.Register
            }
            $dependsonAppxPackage += @("[cAppxPackage]$($Name)")
        }

        #-------------------------------------------------------------------
        # install any appxpackage
        foreach ($AppxProvisionedPackage in $Node.AppxProvisionedPackagePresent)
        {
            $Name = $AppxProvisionedPackage.Name -replace $StringFilter
            cAppxProvisionedPackage $Name
            {
                PackageName           = $AppxProvisionedPackage.Name
                DependencyPackagePath = $AppxProvisionedPackage.Dependency
                PackagePath           = $AppxProvisionedPackage.Path
            }
            $dependsonProvisionedPackage += @("[cAppxProvisionedPackage]$($Name)")
        }

        #-------------------------------------------------------------------
        # install any packages without dependencies
        foreach ($Package in $Node.SoftwarePackagePresent)
        {
            $Name = $Package.Name -replace $StringFilter
            xPackage $Name
            {
                Name      = $Package.Name
                Path      = $Package.Path
                Ensure    = 'Present'
                ProductId = $Package.ProductId
                # PsDscRunAsCredential = $credlookup['DomainCreds']
                DependsOn = $dependsonDirectory
                Arguments = $Package.Arguments
            }

            $dependsonPackage += @("[xPackage]$($Name)")
        }

        #--------------------------------------------------------------------
        # install packages that need to check registry path E.g. .Net frame work
        foreach ($Package in $Node.SoftwarePackagePresentRegKey)
        {
            $Name = $Package.Name -replace $StringFilter
            xPackage $Name
            {
                Name                       = $Package.Name
                Path                       = $Package.Path
                Ensure                     = 'Present'
                ProductId                  = $Package.ProductId
                DependsOn                  = $dependsonDirectory + $dependsonArchive
                Arguments                  = $Package.Arguments
                RunAsCredential            = $credlookup['DomainCreds'] 
                CreateCheckRegValue        = $true 
                InstalledCheckRegHive      = $Package.RegHive
                InstalledCheckRegKey       = $Package.RegKey
                InstalledCheckRegValueName = $Package.RegValueName
                InstalledCheckRegValueData = $Package.RegValueData
            }
            $dependsonPackageRegKey += @("[xPackage]$($Name)")
        }

        #-------------------------------------------------------------------
        if ($Node.WVDInstall)
        {
            WVDDSC RDInfraAgent
            {
                PoolNameSuffix          = $Node.WVDInstall.PoolNameSuffix
                PackagePath             = $Node.WVDInstall.PackagePath
                ManagedIdentityClientID = $AppInfo.ClientID
            }
        }

        #-------------------------------------------------------------------
        # install new services
        foreach ($NewService in $Node.NewServicePresent)
        {
            $Name = $NewService.Name -replace $StringFilter
            Service $Name
            {
                Name        = $NewService.Name
                Path        = $NewService.Path
                Ensure      = 'Present'
                #Credential  = $DomainCreds
                Description = $NewService.Description 
                StartupType = $NewService.StartupType
                State       = $NewService.State
                DependsOn   = $apps 
            }
            $dependsonService += @("[Service]$($Name)")
        }

        #-------------------------------------------------------------------
        if ($Node.ServiceSetStarted)
        {
            ServiceSet ServiceSetStarted
            {
                Name        = $Node.ServiceSetStarted
                State       = 'Running'
                StartupType = 'Automatic'
            }
        }

        #-------------------------------------------------------------------
        Foreach ($DevOpsAgentPool in $node.DevOpsAgentPoolPresent)
        {
            $poolName = $DevOpsAgentPool.poolName -f $Prefix, $OrgName, $App, $environment
                
            DevOpsAgentPool $poolName
            {
                PoolName = $poolName
                PATCred  = $credLookup['DevOpsPAT']
                orgURL   = $DevOpsAgentPool.orgUrl
            }
        }

        #-------------------------------------------------------------------
        Foreach ($DevOpsAgent in $node.DevOpsAgentPresent)
        {
            $agentName = $DevOpsAgent.name -f $Prefix, $OrgName, $App, $environment
            $poolName = $DevOpsAgent.pool -f $Prefix, $OrgName, $App, $environment
            
            DevOpsAgent $agentName
            {
                PoolName     = $poolName
                AgentName    = $agentName
                AgentBase    = $DevOpsAgent.AgentBase
                AgentVersion = $DevOpsAgent.AgentVersion
                orgURL       = $DevOpsAgent.orgUrl
                Ensure       = $DevOpsAgent.Ensure
                PATCred      = $credLookup['DevOpsPAT']
                Credential   = $credLookup[$DevOpsAgent.Credlookup]
            }
        }

        #------------------------------------------------------
        # Reboot after Package Install
        PendingReboot RebootForPackageInstall
        {
            Name                        = 'RebootForPackageInstall'
            DependsOn                   = $dependsonPackage
            SkipComponentBasedServicing = $True
            SkipWindowsUpdate           = $True 
            SkipCcmClientSDK            = $True
            SkipPendingFileRename       = $true 
        }
        #-------------------------------
    }
}#Main

# used for troubleshooting
# F5 loads the configuration and starts the push


#region The following is used for manually running the script, breaks when running as system
if ((whoami) -notmatch 'system' -and !$NotAA)
{
    # Set the location to the DSC extension directory
    if ($psise) { $DSCdir = ($psISE.CurrentFile.FullPath | Split-Path) }
    else { $DSCdir = $psscriptroot }
    Write-Output "DSCDir: $DSCdir"

    if (Test-Path -Path $DSCdir -ErrorAction SilentlyContinue)
    {
        Set-Location -Path $DSCdir -ErrorAction SilentlyContinue
    }
}
elseif (!$NotAA)
{
    Write-Warning -Message 'running as system'
    break
}
else
{
    Write-Warning -Message 'running as mof upload'
    return 'configuration loaded'
}
#endregion

Import-Module $psscriptroot\..\..\bin\DscExtensionHandlerSettingManager.psm1
$ConfigurationArguments = Get-DscExtensionHandlerSettings | ForEach-Object ConfigurationArguments

$AdminCredsPW = ConvertTo-SecureString -String $ConfigurationArguments['AdminCreds'].Password -AsPlainText -Force

$ConfigurationArguments['AdminCreds'] = [pscredential]::new($ConfigurationArguments['AdminCreds'].UserName, $AdminCredsPW)

$Params = @{
    ConfigurationData = '.\*-ConfigurationData.psd1'
    Verbose           = $true
}

# Compile the MOFs
& $Configuration @Params @ConfigurationArguments

# Set the LCM to reboot
Set-DscLocalConfigurationManager -Path .\$Configuration -Force 

# Push the configuration
Start-DscConfiguration -Path .\$Configuration -Wait -Verbose -Force

# delete mofs after push
Get-ChildItem .\$Configuration -Filter *.mof -ea SilentlyContinue | Remove-Item -ea SilentlyContinue
