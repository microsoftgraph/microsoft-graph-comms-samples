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
                    Name             = 'AzureSettings:MediaInstanceExternalPort'
                    BackendPortMatch = '8445'
                    Value            = '{0}'
                },
                @{
                    Name             = 'AzureSettings:BotInstanceExternalPort'
                    BackendPortMatch = '9441'
                    Value            = '{0}'
                }
            )

            # default environment variables
            EnvironmentVarPresent       = @(
                @{
                    Name  = 'AzureSettings:BotCallingInternalPort'
                    Value = '9442'
                },
                @{
                    Name  = 'AzureSettings:BotInternalPort'
                    Value = '9441'
                },
                @{
                    Name  = 'AzureSettings:MediaInternalPort'
                    Value = '8445'
                }
            )
            
            EnvironmentVarSet           = @(
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'AadAppId' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'AadAppSecret' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'ServiceDnsName' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'SpeechConfigKey' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'CertificateThumbprint' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'Prefix' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'OrgName' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'App' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'Environment' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'UseCognitiveServices' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'SpeechConfigRegion' },
                @{Prefix = 'AzureSettings:'; KVName = '{0}-kv'; Name = 'BotLanguage' }
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
                    Uri             = 'https://github.com/git-for-windows/git/releases/download/v2.33.1.windows.1/Git-2.33.1-64-bit.exe'
                    DestinationPath = 'F:\Source\GIT\Git-2.33.1-64-bit.exe'
                },
                @{
                    Uri             = 'https://download.visualstudio.microsoft.com/download/pr/571ad766-28d1-4028-9063-0fa32401e78f/5D3D8C6779750F92F3726C70E92F0F8BF92D3AE2ABD43BA28C6306466DE8A144/VC_redist.x64.exe'
                    DestinationPath = 'F:\Source\dotnet\vc_redist.x64.exe'
                },
                @{
                    Uri             = 'https://download.visualstudio.microsoft.com/download/pr/5a50b8ac-2c22-47f1-ba60-70d4257a78fa/d662d2f23b4b523f30e24cbd7e5e651c7c6a712f21f48e032f942dc678f08beb/vs_Community.exe'
                    DestinationPath = 'F:\Source\VisualStudio\vs_community.exe'
                }
            )

            SoftwarePackagePresent      = @(
                @{
                    Name      = 'Git'
                    Path      = 'F:\Source\GIT\Git-2.33.1-64-bit.exe'
                    ProductId = ''
                    Arguments = '/VERYSILENT'
                },
                @{
                    Name      = 'Microsoft Visual C++ 2015-2022 Redistributable (x64) - 14.30.30708'
                    Path      = 'F:\Source\dotnet\VC_redist.x64.exe'
                    ProductId = ''
                    Arguments = '/install /q /norestart'
                }
                @{  
                    Name      = 'Visual Studio Community 2019'
                    Path      = 'F:\Source\VisualStudio\vs_community.exe'
                    ProductId = ''
                    Arguments = '--installPath F:\VisualStudio\2019\Community --addProductLang en-US  --includeRecommended --quiet --wait --norestart' #--config "F:\Source\VisualStudio\.vsconfig"
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
                    Name        = 'EchoBotService'
                    Path        = 'F:\API\EchoBot\EchoBot.WindowsService.exe'
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
