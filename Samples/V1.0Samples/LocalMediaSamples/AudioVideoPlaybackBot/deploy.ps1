#requires -PSEdition Core

Param (
    [parameter(mandatory)]
    [ValidateLength(2, 7)]
    $OrgName,
    [parameter(mandatory)]
    $Location,
    [String]$Environment = 'D1',
    [String]$App = 'avb',
    [switch]$RunDeployment,
    [switch]$PackageDSC,
    [switch]$RunSetup,
    [String]$ComponentName = 'AVB'
)

# disable powershell warnings for session
Set-Item Env:\SuppressAzurePowerShellBreakingChangeWarnings "true"
$base = $PSScriptRoot
$Location = $Location -replace '\W', ''

if ($RunSetup -OR ! (Test-Path -Path $base\ADF\azuredeploy${OrgName}.parameters.json))
{

    Write-Warning -Message "Running Prerequiste Bot Setup"
    
    $LocationLookup = Get-Content -Path $PSScriptRoot\..\ADF\bicep\global\region.json | ConvertFrom-Json
    $Prefix = $LocationLookup.$Location.Prefix

    $SAName1 = "${Prefix}${OrgName}${App}${Environment}saglobal".tolower()
    $RGName1 = "${Prefix}-${OrgName}-${App}-RG-${Environment}"
    $KVName = "${Prefix}-${OrgName}-${App}-${Environment}-kv".tolower()

    # create storage account for release
    & $base\..\ADF\release-az\Create-StorageAccount.ps1 -Location $Location -SAName $SAName1 -RGName $RGName1
    
    # create keyvault for release + Admin Cred
    & $base\..\ADF\release-az\Create-KeyVault.ps1 -Location $Location -KVName $KVName -RGName $RGName1

    # create GitHub secret + Service Principal + give current user access to the above storage account
    & $base\..\ADF\release-az\Create-GHServicePrincipal.ps1 -SAName $SAName1 -OrgName $OrgName -Location $Location -AddStorageAccess -CurrentUserStorageAccess

    # create Parameter File and Global File for the deployment
    & $base\..\ADF\release-az\Create-StageFiles.ps1 -OrgName $OrgName -SAName $SAName1 -Location $Location -ComponentName $ComponentName -App $App

    # upload certificate
    & $base\..\ADF\release-az\Import-UploadWebCert.ps1 -OrgName $OrgName -KVName $KVName -Location $Location

    # create App Environment Secrets
    & $base\..\ADF\release-az\Create-AppSecrets.ps1 -OrgName $OrgName -Prefix $Prefix -KVName $KVName -Environment $Environment -App $App -BotName "Teams AudioVideoPlayback Bot"
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
