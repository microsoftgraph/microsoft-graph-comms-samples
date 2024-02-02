#requires -PSEdition Core

Param (
    [parameter(mandatory)]
    [ValidateLength(2, 7)]
    [string]
    $OrgName,
    [parameter(mandatory)]
    $Location,
    [switch]$RunDeployment,
    [switch]$PackageDSC,
    [switch]$RunSetup
)

# disable powershell warnings for session
Set-Item Env:\SuppressAzurePowerShellBreakingChangeWarnings "true"
$base = $PSScriptRoot
$Location = $Location -replace '\W', ''

if ($RunSetup -OR ! (Test-Path -Path $base\ADF\azuredeploy${OrgName}.parameters.json))
{

    Write-Warning -Message "Running Prerequiste Bot Setup"

    # create storage account for release
    & $base\ADF\release-az\Create-StorageAccount.ps1 -OrgName $orgName -Location $Location

    # create keyvault for release + Admin Cred
    & $base\ADF\release-az\Create-KeyVault.ps1 -OrgName $orgName -Location $Location

    # create GitHub secret + Service Principal + give current user access to the above storage account
    & $base\ADF\release-az\Create-GHServicePrincipal.ps1 -OrgName $orgName -Location $Location -AddStorageAccess -CurrentUserStorageAccess

    # create Parameter File and Global File for the deployment
    & $base\ADF\release-az\Create-StageFiles.ps1 -OrgName $orgName -Location $Location

    # upload certificate
    & $base\ADF\release-az\Import-UploadWebCert.ps1 -OrgName $orgName -Location $Location

    # create App Environment Secrets
    & $base\ADF\release-az\Create-AppSecrets.ps1 -OrgName $orgName -Location $Location
}
else
{
    Write-Warning -Message "Setup is complete.`n`n`t To rerun setup use:`t . .\deploy.ps1 -orgName $OrgName -RunSetup`n"
    Write-Warning -Message "To deploy use: `t . .\deploy.ps1 -orgName $OrgName -RunDeployment`n"
    Write-Warning -Message "Param file:`t`t [$base\ADF\azuredeploy${OrgName}.parameters.json]"
    Write-Warning -Message "Infra pipeline file:`t [$base\.github\workflows\app-infra-release-${OrgName}.yml]"
    Write-Warning -Message "App pipeline file:`t [$base\.github\workflows\app-build-${OrgName}.yml]"
}

if ($PackageDSC)
{
    # create zip packages if dsc configuration is updated
    # if DSC-BotServers.ps1 is changed, this needs to be run
    # to repackage the zip file and be uploaded
    & $base\ADF\release-az\Package-DSCConfiguration.ps1
}

if ($RunDeployment)
{
    # deploy manually
    $Params = @{
        OrgName    = $OrgName
        Location   = $Location
        FullUpload = $true
    }
    & $base\ADF\main.ps1 @Params
}
