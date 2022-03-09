param (
    [parameter(mandatory)]
    [string]$OrgName,

    [string]$Location = 'centralus',

    [switch]$FullUpload,

    [switch]$PackageDSC
)

$base = $PSScriptRoot
$LocationLookup = Get-Content -Path $base/bicep/global/region.json | ConvertFrom-Json
$Prefix = $LocationLookup.$Location.Prefix
Write-Warning -Message "Prefix [$Prefix] Org [$OrgName] App [AVB] Environment [D1]"
[String]$SAName = "${Prefix}${OrgName}avbd1saglobal".tolower()

$Context = Get-AzContext | Select-Object Name, Account, Environment, Subscription, Tenant
if ($Context)
{
    Write-Verbose -Message "Deploying to:`n $($Context | Format-List | Out-String)" -Verbose
    
    . $base\release-az\Start-AzDeploy.ps1
    
    $Params = @{
        OrgName            = $OrgName
        Prefix             = $Prefix
        StorageAccountName = $SAName
        PackageDSC         = $PackageDSC
    }
    AzDeploy @Params -FullUpload:$FullUpload
}