# AppReleaseDSC
Invoke App Releases via DSC

PowerShell AZCOPY + App Release DSC __Class based Resource__

This is a DSC Resource for performing File sync tasks with Azure File Shares

[azcopy sync /?](https://docs.microsoft.com/en-us/azure/storage/common/storage-ref-azcopy-sync)

__Requirements__
* PowerShell Version 5.0 +
* Server 2012 +

This is the componentbuild.json metadata file that stores all of your build/release info
You have all of your components in there you update this file when you run your build/release pipeline
You do a git checkout of the file, update the value for the individual component/environment
Then you check it into the git repo, then you copy it to the storage URI
DSC will read this file to know which version to install via the DSC Module.

```json
{
    "ComponentName": {
        "LogHeadersAPI": {
            "D": {
                "DefaultBuild": "0.1.0"
            },
            "Q": {
                "DefaultBuild": "0.1.0"
            },
            "T": {
                "DefaultBuild": "0.1.0"
            },
            "U": {
                "DefaultBuild": "0.1.0"
            },
            "P": {
                "DefaultBuild": "0.1.0"
            }
        }
    }
}
```

I provided a sample with all values passed in, however I would recommend that you set some of the file names as your defaults 
IN the DSC Module, then you don't need to specify them for each component

```powershell
    # sample configuation data

            # Blob copy with Managed Identity - Oauth2
            AppReleaseDSCAppPresent     = @(

                @{
                    ComponentName     = 'LogHeadersAPI'
                    SourcePathBlobURI = 'https://{0}.blob.core.windows.net/builds/'
                    DestinationPath   = 'F:\WEB\'
                    ValidateFileName  = 'CurrentBuild.txt'
                    BuildFileName     = 'F:\Build\ComponentBuild.json'
                }
            )
```


```powershell


Configuration AppServers
{
    Param (
        [String]$StorageAccountName,
        [String]$clientIDGlobal
    )

    node $AllNodes.NodeName
    {
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
                EnvironmentName         = $environment[0]
                Ensure                  = 'Present'
                ManagedIdentityClientID = $clientIDGlobal
                LogDir                  = 'F:\azcopy_logs'
            }
            $dependsonAZCopyDSCDir += @("[AppReleaseDSC]$($AppComponent.ComponentName)")
        }
```

Full sample available here

- DSC Configuration
    - [ADF/ext-DSC/DSC-AppServers.ps1](https://github.com/brwilkinson/AzureDeploymentFramework/blob/main/ADF/ext-DSC/DSC-AppServers.ps1#L448)
- DSC ConfigurationData
    - [ADF/ext-CD/JMP-ConfigurationData.psd1](https://github.com/brwilkinson/AzureDeploymentFramework/blob/main/ADF/ext-CD/API-ConfigurationData.psd1#L182)

## Invoke the resource directly to sync the files

As well as using DSC in Pull Mode, you can also invoke the release as part of your Release Pipeline directly
So you get the benefit of a push or pull model, you can deploy via DSC for initial deployments,
Then you perform updates  via push.


```powershell
$ht = @{
    ComponentName           = 'LogHeadersAPI'
    SourcePath              = 'https://storage01.blob.core.windows.net/builds/'
    DestinationPath         = 'F:\WEB'
    EnvironmentName         = 'D'
    ValidateFileName        = 'CurrentBuild.txt'
    BuildFileName           = 'F:\Build\ComponentBuild.json'
    Ensure                  = 'Present'
    ManagedIdentityClientID = '219fa169-9031-49cc-b4cb-1850bc5bf6b4'
    LogDir                  = 'F:\azcopy_logs'
}

Invoke-DscResource -Name AppReleaseDSC -Method Set -ModuleName AppReleaseDSC -Property $ht -Verbose
