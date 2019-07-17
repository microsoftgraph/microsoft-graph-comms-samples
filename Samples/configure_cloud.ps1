<#

.SYNOPSIS
Configure the build files in preperation for deployment.

.DESCRIPTION
This script performs a couple tasks to configure the builds.
- Backs up all the relevant files.
- Replaces the configurations in all the relevant files.
- Allows developer to restore the original files from the backups.

.PARAMETER Path
The path where the project is located, this is where the recursive search will begin.

.PARAMETER ServiceDns
Enter your Service DNS name (ex: contoso.cloudapp.net).

.PARAMETER CName
Enter your Service CName (ex: contoso.net).

.PARAMETER CertThumbprint
Provide your certificate thumbprint.

.PARAMETER BotId
Enter your Bot Display Name as in the registration page.

.PARAMETER AppId
Enter your Bot's Microsoft AppId as in the registation page.

.PARAMETER AppSecret
Enter your Bot's Microsoft AppPassword as in the registration page.

.PARAMETER Reset
If set to true, restores the configurations files with the backups.  If no backups exist, nothing will be done.

.EXAMPLE
Set the parameters:
.\configure_cloud.ps1 -p .\AudioVideoPlaybackBot\
.\configure_cloud.ps1 -p .\AudioVideoPlaybackBot\ -dns MeetingJoinBot.cloudapp.net -cn MeetingJoinBot.cloudapp.net -thumb ABC0000000000000000000000000000000000CBA -bid MeetingJoinBot -aid 51e3bc4a-c06e-469e-afaf-6c8caf8e5dd9 -as <secret>

Restore the parameters
.\configure_cloud.ps1 -p .\ -reset true

#>

param(
    [parameter(Mandatory=$true,HelpMessage="The root path to the project you wish to configure.")][alias("p")] $Path,
    [parameter(Mandatory=$false,HelpMessage="Enter your Service DNS name (ex: contoso.cloudapp.net).")][alias("dns")] $ServiceDns,
    [parameter(Mandatory=$false,HelpMessage="Enter your Service CName (ex: contoso.net).")][alias("cn")] $CName,
    [parameter(Mandatory=$false,HelpMessage="Provide your certificate thumbprint.")][alias("thumb")] $CertThumbprint,
    [parameter(Mandatory=$false,HelpMessage="Enter your Bot Display Name from your bot registration portal.")][alias("bid")] $BotName,
    [parameter(Mandatory=$false,HelpMessage="Enter your Bot's Microsoft application id from your bot registration portal.")][alias("aid")] $AppId,
    [parameter(Mandatory=$false,HelpMessage="Enter your Bot's Microsoft application secret from your bot registration portal.")][alias("as")] $AppSecret,
    [switch] $Reset
)

Write-Output 'Microsoft BotBuilder Enterprise SDK - Azure Cloud Configurator'

$Files = "ServiceConfiguration.Cloud.cscfg", "ServiceConfiguration.Local.cscfg", "app.config", "appsettings.json", "cloud.xml", "ServiceManifest.xml", "ApplicationManifest.xml", "AzureDeploy.Parameters.json"
[System.Collections.ArrayList]$FilesToReplace = @()

foreach($file in $Files)
{
    $foundFiles = Get-ChildItem $Path -Recurse $file
    foreach($foundFile in $foundFiles) {
        $count = $FilesToReplace.Add($foundFile)
    }
}

if ($reset)
{
    Write-Output "Resetting configuration settings..."
    foreach($file in $FilesToReplace)
    {
        $fileName = $file.Name
        $backupName = "$fileName.original"
        $backupFile = Join-Path $file.DirectoryName $backupName
        Write-Output "  Found configuration"
        Write-Output "  $($file.FullName)"
        if (Test-Path $backupFile)
        {
            Write-Output "  Resetting $fileName using $backupName"
            Copy-Item $backupFile -Destination $file.FullName
            Remove-Item $backupFile
        }
        else
        {
            Write-Output "  No backup found for $file"
        }
    }
    Write-Output "Reset Complete."
    exit;
}

if (-not $ServiceDns) {
    $ServiceDns = (Read-Host 'Enter your Service DNS name (ex: contoso.cloudapp.net).').Trim()
}

if (-not $CName) {
    $CName = (Read-Host 'Enter your Service CName (ex: contoso.net).').Trim()
}

if (-not $CertThumbprint) {
    $CertThumbprint = (Read-Host 'Provide your certificate thumbprint.').Trim()
}

if (-not $BotName) {
    $BotName = (Read-Host 'Enter your Bot Display Name from your bot registration portal.').Trim()
}

if (-not $AppId) {
    $AppId = (Read-Host "Enter your Bot's Microsoft application id from your bot registration portal.").Trim()
}

if (-not $AppSecret) {
    $AppSecret = (Read-Host "Enter your Bot's Microsoft application secret from your bot registration portal.").Trim()
}

function ReplaceInFile ($file, [string]$pattern, [string]$replaceWith) {
    $fileName = $file.Name
    Write-Output "  Replacing $pattern with $replaceWith in $fileName"

    (Get-Content $file.FullName).replace($pattern, $replaceWith) | Set-Content $file.FullName
}

Write-Output ""
Write-Output "Updating configuration files..."

foreach($file in $FilesToReplace)
{
    $fileName = $file.Name
    $backupName = "$fileName.original"
    $backupFile = Join-Path $file.DirectoryName $backupName
    Write-Output "  Found configuration"
    Write-Output "  $($file.FullName)"

    if (-not (Test-Path $backupFile)) {
        Write-Output "  Backing up $fileName with $backupName"
        Copy-Item $file.FullName -Destination $backupFile
    }

    Copy-Item $backupFile -Destination $file.FullName
    ReplaceInFile $file "%ServiceDns%" $ServiceDns
    ReplaceInFile $file "%CName%" $CName
    ReplaceInFile $file "ABC0000000000000000000000000000000000CBA" $CertThumbprint
    ReplaceInFile $file "%BotName%" $BotName
    ReplaceInFile $file "%BotNameLower%" $BotName.ToLower()
    ReplaceInFile $file "%AppId%" $AppId
    ReplaceInFile $file "%AppSecret%" $AppSecret
}

Write-Output "Update Complete."
