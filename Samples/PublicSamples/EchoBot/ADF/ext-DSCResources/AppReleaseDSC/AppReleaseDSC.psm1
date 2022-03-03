# Docs https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azcopy-configure
# Docs https://docs.microsoft.com/en-us/azure/storage/common/storage-ref-azcopy?toc=/azure/storage/blobs/toc.json
# Docs https://docs.microsoft.com/en-us/azure/storage/common/storage-ref-azcopy-list
# Docs 

# Defines the values for the resource's Ensure property.
enum Ensure
{
    # The resource must be absent.    
    Absent
    # The resource must be present.    
    Present
}

# [DscResource()] indicates the class is a DSC resource.
[DscResource()]
class AppReleaseDSC
{
    # The build componentname being released
    [DscProperty(Key)]
    [string]$ComponentName

    # The target environment used to lookup build
    [DscProperty(Key)]
    [string]$EnvironmentName

    # Can be local File or remote blob
    # Can be local directory or remote blob container
    [DscProperty(Key)]
    [string]$SourcePath

    # can be remote blob or local file
    # can be remote blob container or local file directory
    [DscProperty(Key)]
    [string]$DestinationPath

    # The file used to validate the correct buildversion
    [DscProperty(Mandatory)]
    [string]$ValidateFileName = 'CurrentBuild.txt'

    # The file that maintains buildversion state
    [DscProperty(Mandatory)]
    [string]$BuildFileName = 'F:\Build\ComponentBuild.json'

    # Should have 'Storage Blob Data Contributor' or 'Storage Blob Data Reader'
    [DscProperty(Mandatory)]
    [string]$ManagedIdentityClientID

    [DscProperty(Mandatory)]
    [string]$LogDir

    # When deploying new binaries, it will wait this many seconds for the site to be shutdown
    [DscProperty()]
    [string]$DeploySleepWaitSeconds = 30

    # Mandatory indicates the property is required and DSC will guarantee it is set.
    [DscProperty()]
    [Ensure]$Ensure = [Ensure]::Present

    # Tests if the resource is in the desired state.
    # maybe able to enhance the performance if azcopy sync had a log only option
    # https://github.com/Azure/azure-storage-azcopy/issues/1354
    [bool] Test()
    {
        # echo azcopy path
        $azcopy = "$PSScriptRoot\utilities\azcopy_windows_amd64_10.9.0\azcopy.exe"
        $env:AZCOPY_LOG_LOCATION = $this.LogDir

        # # need to write logs in order to track changes, will run as system, so cannot use the default path.
        if (! (Test-Path -Path $this.LogDir))
        {
            try
            {
                mkdir $this.LogDir -Verbose -Force -ErrorAction stop
            }
            catch
            {
                $_
            }
        }
        
        $DestinationDir = Join-Path -Path $this.DestinationPath -ChildPath $this.ComponentName
        if (! (Test-Path -Path $DestinationDir))
        {
            try
            {
                mkdir $DestinationDir -Verbose -Force -ErrorAction stop
            }
            catch
            {
                $_
            }
        }

        # Always azlogin via managed identity
        & $azcopy login --identity --identity-client-id $this.ManagedIdentityClientID
        
        # Always copy source ComponentBuild.json to local via sync from master on Blob
        $Source = $this.SourcePath.TrimEnd('/')
        $BuildFile = $Source + '/' + $this.ComponentName + '/' + (Split-Path -Path $this.BuildFileName -Leaf)
        & $azcopy copy $BuildFile $this.BuildFileName

        # Read the build file to determine which version of the component should be in the current environment
        $RequiredBuild = Get-Content -Path $this.BuildFileName | ConvertFrom-Json |
            ForEach-Object ComponentName | ForEach-Object $this.ComponentName | 
            ForEach-Object $this.EnvironmentName | ForEach-Object DefaultBuild
        
        # Check if the correct build is already installed
        $CurrentBuildFile = Join-Path -Path $this.DestinationPath -ChildPath (Join-Path -Path $this.ComponentName -ChildPath $this.ValidateFileName)
        if (! (Test-Path -Path $CurrentBuildFile))
        {
            return $false
        }
        else 
        {
            $CurrentBuild = Get-Content -Path $CurrentBuildFile
            if ($CurrentBuild -ne $RequiredBuild)
            {
                return $false
            }
            else 
            {
                # Validate files with file count comparison
                # Read source information via list command
                $DesiredBuildFiles = $Source + '/' + $this.ComponentName + '/' + $RequiredBuild
                $Files = & $azcopy list $DesiredBuildFiles --machine-readable

                $Source = $Files | ForEach-Object {
            
                    $null = $_ -match '(?<pre>; Content Length:) (?<length>.+)'
                    if ($matches)
                    {
                        $matches.length
                        $matches.Clear()
                    }
                } | Measure-Object -Sum
        
                [long]$SourceFilesBytes = $Source.Sum
                [long]$SourceFilesCount = $Source.Count
        
                # # Read destination information
                $CurrentBuildFilesDir = Join-Path -Path $this.DestinationPath -ChildPath $this.ComponentName
                $DestinationFiles = Get-ChildItem -Path $CurrentBuildFilesDir -Recurse -File | Measure-Object -Property length -Sum
                [long]$DestinationFilesCount = $DestinationFiles | ForEach-Object Count
                [long]$DestinationFilesBytes = $DestinationFiles | ForEach-Object Sum

                Write-Verbose -Message "Source has ------> [$($SourceFilesBytes)] Bytes files"
                Write-Verbose -Message "Destination has -> [$($DestinationFilesBytes)] Bytes files"
                Write-Verbose -Message "Source has ------> [$($SourceFilesCount)] files"
                Write-Verbose -Message "Destination has -> [$($DestinationFilesCount)] files"
        
                return ( $SourceFilesCount -le ( $DestinationFilesCount -1 ) )   # don't check file sizes for now: -and ($SourceFilesBytes) -le ($DestinationFilesBytes)
            }
        }
    }

    # Sets the desired state of the resource.
    [void] Set()
    {
        # echo azcopy path
        $azcopy = "$PSScriptRoot\utilities\azcopy_windows_amd64_10.9.0\azcopy.exe"
        
        $env:AZCOPY_LOG_LOCATION = $this.LogDir

        # need to write logs in order to track changes, will run as system, so cannot use the default path.
        if (! (Test-Path -Path $this.LogDir))
        {
            try
            {
                mkdir $this.LogDir -Verbose -Force -ErrorAction stop
            }
            catch
            {
                $_
            }
        }

        # Validate files with file count comparison
        # Read source information via list command
        # Read the build file to determine which version of the component should be in the current environment
        $RequiredBuild = Get-Content -Path $this.BuildFileName | ConvertFrom-Json |
            ForEach-Object ComponentName | ForEach-Object $this.ComponentName |
            ForEach-Object $this.EnvironmentName | ForEach-Object DefaultBuild

        $Source = $this.SourcePath.TrimEnd('/')
        $DesiredBuildFiles = $Source + '/' + $this.ComponentName + '/' + $RequiredBuild
        $CurrentBuildFilesDir = Join-Path -Path $this.DestinationPath -ChildPath $this.ComponentName

        # attempt to unlock binaries for ASP.NET apps
        # https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/app-offline?view=aspnetcore-5.0
        New-Item -Path $CurrentBuildFilesDir -Name "app_offline.htm" -ItemType "file" -Verbose
        Start-Sleep -Seconds $this.DeploySleepWaitSeconds

        & $azcopy login --identity --identity-client-id $this.ManagedIdentityClientID
        & $azcopy sync $DesiredBuildFiles $CurrentBuildFilesDir --recursive=true --delete-destination true

        # Update the ValidateFile with the latest build
        $CurrentBuildFile = Join-Path -Path $this.DestinationPath -ChildPath (Join-Path -Path $this.ComponentName -ChildPath $this.ValidateFileName)
        Set-Content -Path $CurrentBuildFile -Value $RequiredBuild -Force -Verbose
        
        # azcopy can remove this file on sync so this file may already be deleted
        Remove-Item -Path $CurrentBuildFilesDir\app_offline.htm -verbose -ErrorAction SilentlyContinue
    }

    # Gets the resource's current state.
    [AppReleaseDSC] Get()
    {
        # Return this instance or construct a new instance.
        return $this
    }
}