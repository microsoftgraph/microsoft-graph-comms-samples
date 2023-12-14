#
# ConfigurationData.psd1
#

@{
    AllNodes = @(
        @{
            NodeName                    = 'LocalHost'
            PSDscAllowPlainTextPassword = $true
            PSDscAllowDomainUser        = $true

            DisksPresent                = @{DriveLetter = 'F'; DiskID = '2' }

            ServiceSetStopped           = 'ShellHWDetection'

            # IncludesAllSubfeatures
            WindowsFeaturePresent2      = 'RSAT'

            # given this is for a lab and load test, just always pull down the latest App config
            DSCConfigurationMode        = 'ApplyAndAutoCorrect'

            DisableIEESC                = $True

            FWRules                     = @(
                @{
                    Name      = 'EchoBot'
                    LocalPort = ('8445', '9442', '9441')
                }
            )
            
            DirectoryPresent            = @(
                'F:\Source\InstallLogs', 'F:\API\EchoBot', 'F:\Build\EchoBot'
            )
            
            # Port Mappings from NAT Pools on Azure Load Balancer
            # Dynamically set from Azure Metadata Service
            EnvironmentVarPresentVMSS   = @(
                @{
                    Name             = 'AppSettings:MediaInstanceExternalPort'
                    BackendPortMatch = '8445'
                    Value            = '{0}'
                },
                @{
                    Name             = 'AppSettings:BotInstanceExternalPort'
                    BackendPortMatch = '9441'
                    Value            = '{0}'
                }
            )

            # default environment variables
            EnvironmentVarPresent       = @(
                @{
                    Name  = 'AppSettings:BotCallingInternalPort'
                    Value = '9442'
                },
                @{
                    Name  = 'AppSettings:BotInternalPort'
                    Value = '9441'
                },
                @{
                    Name  = 'AppSettings:MediaInternalPort'
                    Value = '8445'
                }
            )
            
            EnvironmentVarSet           = @(
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'AadAppId' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'AadAppSecret' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'ServiceDnsName' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'SpeechConfigKey' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'CertificateThumbprint' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'Prefix' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'OrgName' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'App' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'Environment' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'UseCognitiveServices' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'SpeechConfigRegion' },
                @{Prefix = 'AppSettings:'; KVName = '{0}-kv'; Name = 'BotLanguage' }
            )

            # Blob copy with Managed Identity - Oauth2
            # Commented out for Lab, using RemoteFilePresent instead
            # AZCOPYDSCDirPresentSource  = @(
            #     @{
            #         SourcePathBlobURI = 'https://{0}.blob.core.windows.net/source/GIT/'
            #         DestinationPath   = 'F:\Source\GIT\'
            #     },
            #     @{
            #         SourcePathBlobURI = 'https://{0}.blob.core.windows.net/source/dotnet/'
            #         DestinationPath   = 'F:\Source\dotnet\'
            #     },
            #     @{
            #         SourcePathBlobURI = 'https://{0}.blob.core.windows.net/source/VisualStudio/'
            #         DestinationPath   = 'F:\Source\VisualStudio\'
            #     }
            # )

            # this downloads the files each time, you can use AZCOPYDSCDirPresentSource above
            # As an alternative if you stage the files
            RemoteFilePresent           = @(
                @{
                    Uri             = 'https://github.com/git-for-windows/git/releases/download/v2.42.0.windows.2/Git-2.42.0.2-64-bit.exe'
                    DestinationPath = 'F:\Source\GIT\Git-2.42.0.2-64-bit.exe'
                },
                @{
                    Uri             = 'https://aka.ms/vs/17/release/vc_redist.x64.exe'
                    DestinationPath = 'F:\Source\dotnet\vc_redist.x64.exe'
                },
                @{
                    Uri             = 'https://aka.ms/vs/17/release/vs_enterprise.exe'
                    DestinationPath = 'F:\Source\VisualStudio\vs_enterprise.exe'
                }
            )

            SoftwarePackagePresent      = @(
                @{
                    Name      = 'Git'
                    Path      = 'F:\Source\GIT\Git-2.42.0.2-64-bit.exe'
                    ProductId = ''
                    Arguments = '/VERYSILENT'
                },
                @{
                    Name      = 'Microsoft Visual C++ 2015-2022 Redistributable (x64) - 14.38.33130'
                    Path      = 'F:\Source\dotnet\vc_redist.x64.exe'
                    ProductId = ''
                    Arguments = '/install /q /norestart'
                },
                @{  
                    Name      = 'Visual Studio Enterprise 2022'
                    Path      = 'F:\Source\VisualStudio\vs_enterprise.exe'
                    ProductId = ''
                    Arguments = '--installPath F:\VisualStudio\2022\Enterprise --addProductLang en-US --add Microsoft.VisualStudio.Workload.ManagedDesktop --includeRecommended --quiet --wait --norestart' #--config "F:\Source\VisualStudio\.vsconfig"
                }
            )

            # Blob copy with Managed Identity - Oauth2
            AppReleaseDSCAppPresent     = @(
                @{
                    ComponentName     = 'EchoBot'
                    SourcePathBlobURI = 'https://{0}.blob.core.windows.net/builds/'
                    DestinationPath   = 'F:\API\'
                    ValidateFileName  = 'CurrentBuild.txt'
                    BuildFileName     = 'F:\Build\EchoBot\componentBuild.json'
                    SleepTime         = '10'
                }
            )

            NewServicePresent           = @(
                @{
                    Name        = 'Echo Bot Service'
                    Path        = 'F:\API\EchoBot\EchoBot.exe'
                    State       = 'Running'
                    StartupType = 'Automatic'
                    Description = 'Echo Bot Service'
                }
            )

            CertificatePortBinding      = @(
                @{
                    Name  = 'MediaControlPlane'
                    Port  = '8445'
                    AppId = '{7c64d8a0-4cbb-42b6-85a8-de0e00f6a9c6}'
                },
                @{
                    Name  = 'BotCalling'
                    Port  = '9442'
                    AppId = '{7c64d8a0-4cbb-42b6-85a8-de0e00f6a9c6}'
                },
                @{
                    Name  = 'BotNotification'
                    Port  = '9441'
                    AppId = '{7c64d8a0-4cbb-42b6-85a8-de0e00f6a9c6}'
                }
            )
        }
    )
}
