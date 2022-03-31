param (
    [String]$Environment = 'D1',
    [String]$App = 'BOT',
    [Parameter(Mandatory)]
    [String]$OrgName,
    [String]$Location,
    [String]$ComponentName = 'EchoBot',
    [String]$MetaDataFileName = 'componentBuild.json'
)

Write-Output "$('-'*50)"
Write-Output $MyInvocation.MyCommand.Source

$LocationLookup = Get-Content -Path $PSScriptRoot\..\bicep\global\region.json | ConvertFrom-Json
$Prefix = $LocationLookup.$Location.Prefix
$BranchName = git branch --show-current ? git branch --show-current : "main"

$filestocopy = @(
    @{
        SourcePath      = "$PSScriptRoot\..\templates\azuredeploy-OrgName.parameters.json"
        DestinationPath = "$PSScriptRoot\..\azuredeploy-${OrgName}.parameters.json"
        TokenstoReplace = $null
    }

    @{
        SourcePath      = "$PSScriptRoot\..\templates\echobot-build-OrgName.yml"
        DestinationPath = "$PSScriptRoot\..\..\..\..\..\.github\workflows\echobot-build-${OrgName}.yml"
        TokenstoReplace = @(
            @{ Name = '{OrgName}'; Value = $OrgName },
            @{ Name = '{Location}'; Value = $Location },
            @{ Name = '{BranchName}'; Value = $BranchName }
        )
    }

    @{
        SourcePath      = "$PSScriptRoot\..\templates\echobot-infra-OrgName.yml"
        DestinationPath = "$PSScriptRoot\..\..\..\..\..\.github\workflows\echobot-infra-${OrgName}.yml"
        TokenstoReplace = @(
            @{ Name = '{OrgName}'; Value = $OrgName },
            @{ Name = '{Location}'; Value = $Location },
            @{ Name = '{BranchName}'; Value = $BranchName }
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
[String]$SAName = "${Prefix}${OrgName}${App}${Environment}saglobal".tolower()
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
        Write-Warning $_
        break
    }
}