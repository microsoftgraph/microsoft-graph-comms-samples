#Requires -Modules Az.Compute

# Zip up all files
# break
[string] $Artifacts = Get-Item -Path $PSScriptRoot\..
[string] $DSCSourceFolder = $Artifacts + '\ext-DSC'

Import-Module Az.Compute
$dscResourceFolder = $Artifacts + '\ext-DSCResources'
$psModulePath = $env:PSModulePath
$env:PSModulePath = $dscResourceFolder

if (Test-Path $DSCSourceFolder)
{
    Get-ChildItem $DSCSourceFolder -File -Filter '*.ps1' | ForEach-Object {

        $DSCArchiveFilePath = $_.FullName.Substring(0, $_.FullName.Length - 4) + '.zip'
        Publish-AzVMDscConfiguration $_.FullName -OutputArchivePath $DSCArchiveFilePath -Force -Verbose
    }
}

$env:PSModulePath = $psModulePath