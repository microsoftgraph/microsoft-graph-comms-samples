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
class AZCOPYDSCDir
{
    # Can be local File or remote blob
    # Can be local directory or remote blob container
    [DscProperty(Key)]
    [string]$SourcePath

    # can be remote blob or local file
    # can be remote blob container or local file directory
    [DscProperty(Key)]
    [string]$DestinationPath

    # Should have 'Storage Blob Data Contributor' or 'Storage Blob Data Reader'
    [DscProperty(Mandatory)]
    [string]$ManagedIdentityClientID

    [DscProperty(Mandatory)]
    [string]$LogDir

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
        
        if (! (Test-Path -Path $this.DestinationPath))
        {
            try
            {
                mkdir $this.DestinationPath -Verbose -Force -ErrorAction stop
            }
            catch
            {
                $_
            }
        }

        # Read source information via list command  
        & $azcopy login --identity --identity-client-id $this.ManagedIdentityClientID
        $Files = & $azcopy list $this.SourcePath --machine-readable

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
        $DestinationFiles = Get-ChildItem -Path $this.DestinationPath -Recurse -File | Measure-Object -Property length -Sum
        [long]$DestinationFilesCount = $DestinationFiles | ForEach-Object Count
        [long]$DestinationFilesBytes = $DestinationFiles | ForEach-Object Sum

        Write-Verbose -Message "Source has ------> [$($SourceFilesBytes)] Bytes files"
        Write-Verbose -Message "Destination has -> [$($DestinationFilesBytes)] Bytes files"
        Write-Verbose -Message "Source has ------> [$($SourceFilesCount)] files"
        Write-Verbose -Message "Destination has -> [$($DestinationFilesCount)] files"
        
        return ($SourceFilesCount -le $DestinationFilesCount -and ($SourceFilesBytes) -le ($DestinationFilesBytes))
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

        & $azcopy login --identity --identity-client-id $this.ManagedIdentityClientID
        & $azcopy sync $this.SourcePath $this.DestinationPath --recursive=true
    }

    # Gets the resource's current state.
    [AZCOPYDSCDir] Get()
    {        
        # Return this instance or construct a new instance.
        return $this
    }
}