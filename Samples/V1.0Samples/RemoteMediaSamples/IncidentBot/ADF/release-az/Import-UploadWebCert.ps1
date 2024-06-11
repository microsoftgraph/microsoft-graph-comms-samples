param (
    [String]$Environment = 'D1',
    [String]$App = 'BOT',
    [Parameter(Mandatory)]
    [String]$Location,
    [Parameter(Mandatory)]
    [String]$OrgName
)

Write-Output "$('-'*50)"
Write-Output $MyInvocation.MyCommand.Source

$LocationLookup = Get-Content -Path $PSScriptRoot\..\bicep\global\region.json | ConvertFrom-Json
$Prefix = $LocationLookup.$Location.Prefix

# Azure Keyvault Info
[String]$KVName = "${Prefix}-${OrgName}-${App}-${Environment}-kv".tolower()

# TLS Cert
Write-Verbose -Message "Primary KV Name:`t $KVName certificate]" -Verbose
if (! (Get-AzKeyVaultCertificate -VaultName $KVName -Name WildcardCert -EA SilentlyContinue))
{
    try
    {
        $PfxFilePath = Read-Host -Prompt "Enter the PFX Cert File Path"
        # trim quotes if the user enters quotes
        $PfxFilePathTrimmed = $PfxFilePath.Trim('"').Trim("'")
        
        if (! (Test-Path -Path $PfxFilePathTrimmed))
        {
            Throw "PFX File not found [$PfxFilePathTrimmed]"
        }

        $PW = Read-Host -AsSecureString -Prompt "Enter the certificate password"
        Import-AzKeyVaultCertificate -FilePath $PfxFilePathTrimmed -Name WildcardCert -VaultName $KVName -Password $PW -OutVariable kvcert

        $Thumbprint = ConvertTo-SecureString -String $kvcert.Thumbprint -AsPlainText -Force
        Set-AzKeyVaultSecret -VaultName $KVName -Name 'CertificateThumbprint' -SecretValue $Thumbprint
    }
    catch
    {
        Write-Warning $_
        break
    }
}
else
{
    Write-Warning "Certificate exists:`t [WildcardCert]"
}
