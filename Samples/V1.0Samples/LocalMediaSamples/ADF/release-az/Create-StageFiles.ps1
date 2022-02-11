param (
    [Parameter(Mandatory)]
    [String]$OrgName,
    [Parameter(Mandatory)]
    [String]$SAName,
    [String]$Location,
    [String]$App = 'avb',
    [String]$ComponentName = 'AVB',
    [String]$MetaDataFileName = 'componentBuild.json'
)

Write-Output "$('-'*50)"
Write-Output $MyInvocation.MyCommand.Source

#$LocationLookup = Get-Content -Path $PSScriptRoot\..\bicep\global\region.json | ConvertFrom-Json
#$Prefix = $LocationLookup.$Location.Prefix

$filestocopy = @(
    @{
        SourcePath      = "$PSScriptRoot\..\templates\azuredeploy.parameters.json"
        DestinationPath = "$PSScriptRoot\..\azuredeploy${OrgName}-${App}.parameters.json"
        TokenstoReplace = $null
    }

    @{
        SourcePath      = "$PSScriptRoot\..\templates\app-build-OrgName.yml"
        DestinationPath = "$PSScriptRoot\..\..\..\..\..\.github\workflows\app-build-${OrgName}-${App}.yml"
        TokenstoReplace = @(
            @{ Name = '{OrgName}'; Value = $OrgName },
            @{ Name = '{Location}'; Value = $Location }
        )
    }

    @{
        SourcePath      = "$PSScriptRoot\..\templates\app-infra-release-OrgName.yml"
        DestinationPath = "$PSScriptRoot\..\..\..\..\..\.github\workflows\app-infra-release-${OrgName}-${App}.yml"
        TokenstoReplace = @(
            @{ Name = '{OrgName}'; Value = $OrgName },
            @{ Name = '{Location}'; Value = $Location }
        )
    }
)

$filestocopy | Foreach {

    if (! (Test-Path -Path $_.DestinationPath))
    {
        Copy-Item -Path $_.SourcePath -Destination $_.DestinationPath
    }

    $destinationPath = $_.DestinationPath
    $_.TokenstoReplace | ForEach-Object {
        if ($_.Name -and (Select-String -Pattern $_.Name -Path $destinationPath))
        {
            ((Get-Content -Path $destinationPath -Raw) -replace $_.Name,$_.Value) | Set-Content -Path $destinationPath
        }
    }
}

# Stage meta data file on storage used for app releases
$Context = New-AzStorageContext -StorageAccountName $SAName -UseConnectedAccount
[String]$ContainerName = 'builds'
$StorageContainerParams = @{
    Container = $ContainerName
    Context   = $Context
}

Write-Verbose -Message "Global SAName:`t`t [$SAName] Container is: [$ContainerName]" -Verbose
if (! (Get-AzStorageContainer @StorageContainerParams -EA 0))
{
    try
    {
        # Create the storage blob Containers
        New-AzStorageContainer @StorageContainerParams -ErrorAction Stop
    }
    catch
    {
        Write-Verbose -Message "Blew up trying to create stroage container"
        Write-Warning $_
        break
    }
}

if (! (Get-AzStorageBlob @StorageContainerParams -Blob $ComponentName/$MetaDataFileName -EA 0))
{
    try
    {
        # Copy up the metadata file
        $Item = Get-Item -Path $PSScriptRoot\..\templates\$MetaDataFileName
        Set-AzStorageBlobContent @StorageContainerParams -File $item.FullName -Blob $ComponentName/$MetaDataFileName -Verbose -Force
    }
    catch
    {
        Write-Verbose -Message "Blew up trying to set stroage container blob content"
        Write-Warning $_
        break
    }
}