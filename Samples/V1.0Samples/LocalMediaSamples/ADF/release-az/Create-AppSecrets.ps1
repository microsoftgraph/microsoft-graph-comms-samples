param (
    [Parameter(Mandatory)]
    [String]$OrgName,
    [Parameter(Mandatory)]
    [String]$KVName,
    [Parameter(Mandatory)]
    [String]$Prefix,
    [String]$Environment = 'D1',
    [String]$App = 'BOT',
    [String]$BotName = "Teams AudioVideoPlayback Bot"
)

# Write-Output $PSScriptRoot
Write-Output $MyInvocation.MyCommand.Source

$BaseSecrets = @(
    @{ Name = 'Prefix'; Value = $Prefix },
    @{ Name = 'OrgName'; Value = $OrgName },
    @{ Name = 'App'; Value = $App },
    @{ Name = 'Environment'; Value = $Environment },
    @{ Name = 'BotName'; Value = $BotName }
)

$BaseSecrets | ForEach-Object {

    $Secret = $_.Name
    $Value = $_.Value
    # LocalAdmin Creds
    Write-Verbose -Message "Primary KV Name: [$KVName] Secret for [$Secret]" -Verbose
    if (! (Get-AzKeyVaultSecret -VaultName $KVName -Name $Secret -EA SilentlyContinue))
    {
        try
        {
            $SV = $Value | ConvertTo-SecureString -AsPlainText -Force
            Set-AzKeyVaultSecret -VaultName $KVName -Name $Secret -SecretValue $SV
        }
        catch
        {
            Write-Warning $_
            break
        }
    }
    else 
    {
        Write-Output "`t Primary KV Name: $KVName Secret for [$Secret] Exists!!!`n`n" -Verbose
    }
}

# App Secrets required to be published to the keyvault
$RequiredSecrets = @(
    @{ Name = 'localadmin'; Message = 'Enter the VMSS admin password'; },
    @{ Name = 'AadAppId'; Message = 'Enter the Azure Bot AAD Client Id'; },
    @{ Name = 'AadAppSecret'; Message = 'Enter the Azure Bot AAD Client Secret'; },
    @{ Name = 'ServiceDNSName'; Message = 'Enter the DNS value that will point to the load balancer (ie bot.example.com)'; }
)

$CognitiveServicesSecrets = @(
    @{ Name = 'UseCognitiveServices'; Message = 'Enter all the secrets and settings for Cognitive Services mode'; },
    @{ Name = 'SpeechConfigKey'; Message = 'Enter the Cognitive Services Key'; },
    @{ Name = 'SpeechConfigRegion'; Message = 'Enter the Azure Region for your Cognitive Services (ie centralus, eastus2)'; },
    @{ Name = 'BotLanguage'; Message = 'Enter the language code you want your bot to understand and speak (ie en-US, es-MX, fr-FR)'; }
)

Write-Warning -Message "There are [$($RequiredSecrets.count)] Secrets required, you can enter them now or cancel."
Write-Warning -Message "The secrets used by the BOT App are: [$($RequiredSecrets.Name)]`n`n"

Write-Verbose -Message "Secrets will be added to this Key Vault: [$KVName]" -Verbose
$choices = @(
    @{ Choice="&Yes"; Help="Enter all the secrets now" },
    @{ Choice="&No"; Help="Skip this step to enter the required secrets. Update the secrets directly in KeyVault before deploying." }
)
$options = [System.Management.Automation.Host.ChoiceDescription[]]($choices | ForEach-Object {
    New-Object System.Management.Automation.Host.ChoiceDescription $_.Choice, $_.Help
})
$chosen = $host.ui.PromptForChoice('Keyvault Secrets Setup', "Do you want to enter all the secrets now?", $options, 0)
if ($options[$chosen].Label -eq '&Yes') {
    $RequiredSecrets | ForEach-Object {
        $secretName = $_.Name
        $secretMessage = $_.Message
        if (! (Get-AzKeyVaultSecret -VaultName $KVName -Name $secretName -EA SilentlyContinue))
        {
            try
            {
                if ($secretName -eq "localadmin")
                {
                    while (!$vmPassword)
                    {
                        try
                        {
                            [ValidateNotNullOrEmpty()][ValidatePattern('\W_|\d')][ValidatePattern('(?-i)[a-z]')][ValidatePattern('(?-i)[A-Z]')][Validatelength(8, 72)][string]$vmPassword = Read-Host -Prompt $secretMessage

                        }
                        catch
                        {
                            Write-Verbose "Enter the local admin password with at least 1 uppercase letter, 1 lowercase letter, 1 number and 1 special character. Minimum length of 8." -Verbose
                        }
                    }

                    $settingValue = $vmPassword
                }
                else 
                {
                    $settingValue = Read-Host -Prompt $secretMessage    
                }

                $secretValue = ConvertTo-SecureString -String $settingValue -AsPlainText -Force    
                Set-AzKeyVaultSecret -VaultName $KVName -Name $secretName -SecretValue $secretValue
            }
            catch
            {
                Write-Warning $_
                break
            }
        }
        else 
        {
            Write-Output "`t Primary KV Name: $KVName Secret for [$secretName] Exists!!!`n`n" -Verbose
        }
    }
}