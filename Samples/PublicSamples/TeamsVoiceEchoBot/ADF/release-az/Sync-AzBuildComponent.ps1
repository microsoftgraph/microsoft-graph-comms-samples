
#requires -Module Az.Storage
#requires -Module Az.Accounts

<#
.SYNOPSIS
    Stage Files from a Build on Azure Storage (Blob Container), for Deployment
.DESCRIPTION
    Stage Files from a Build on Azure Storage (Blob Container), for Deployment. Primarily used for a PULL mode deployment where a Server can retrieve new builds via Desired State Configuration.
.EXAMPLE
    Sync-AzBuildComponent -ComponentName WebAPI -BuildName 5.3 -BasePath "F:\Builds\WebAPI"

    Sync a local Build
.EXAMPLE
    Sync-AzBuildComponent -ComponentName $(ComponentName) -BuildName $(Build.BuildNumber) -BasePath "$(System.ArtifactsDirectory)/_$(ComponentName)/$(ComponentName)"

    As seen in an Azure DevOps pipeline
.INPUTS
    Inputs (if any)
.OUTPUTS
    Output (if any)
.NOTES
    Updated 04/03/2021 
        - Moved the script from using Storage File Shares to Storage Blob Containers

    Updated 02/16/2021 
        - Added to Function instead of Script
        - Added examples
        - Updated to work with the newer AZ.Storage Module tested with 3.2.1
#>

Param (
    [String]$BuildName = '4.2',
    [String]$ComponentName = 'EchoBot',
    [String]$BasePath = 'D:\Builds',
    [String]$Environment = 'D1',
    [String]$App = 'BOT',
    [Parameter(Mandatory)]
    [String]$Location,
    [Parameter(Mandatory)]
    [String]$OrgName
)

$LocationLookup = Get-Content -Path $PSScriptRoot\..\bicep\global\region.json | ConvertFrom-Json
$Prefix = $LocationLookup.$Location.Prefix

# Azure Blob Container Info
[String]$SAName = "${Prefix}${OrgName}${App}${Environment}saglobal".tolower()
[String]$ContainerName = 'builds'

# Get context using Oauth
$Context = New-AzStorageContext -StorageAccountName $SAName -UseConnectedAccount

$StorageContainerParams = @{
    Container = $ContainerName
    Context   = $Context
}

# *Builds/<ComponentName>/<BuildName>
# need to pass this in
$CurrentFolder = (Get-Item -Path $BasePath\$ComponentName\$BuildName ).FullName

# Copy up the files and capture a list of the files URI's
$SourceFiles = Get-ChildItem -Path $BasePath\$ComponentName\$BuildName -File -Recurse | ForEach-Object {
    $path = $_.FullName.Substring($Currentfolder.Length + 1).Replace('\', '/')
    Write-Output -InputObject "$ComponentName/$BuildName/$path"
    $b = Set-AzStorageBlobContent @StorageContainerParams -File $_.FullName -Blob $ComponentName\$BuildName\$Path -Verbose -Force
}

# Find all of the files in the share including subfolders
$Path = "$ComponentName/$BuildName/*"
$DestinationFiles = Get-AzStorageBlob @StorageContainerParams -Blob $Path | ForEach-Object Name

# Compare the new files that were uploaded to the files already on the share
# these should be deleted from the Azure Blob Container
$FilestoRemove = Compare-Object -ReferenceObject $DestinationFiles -DifferenceObject $SourceFiles -IncludeEqual | 
    Where-Object SideIndicator -EQ '<=' | ForEach-Object InputObject

# Remove the old Files
$FilestoRemove | ForEach-Object {
    Write-Verbose "Removing: [$_]" -Verbose
    Remove-AzStorageBlob @StorageContainerParams -Blob "$_" -Verbose
}