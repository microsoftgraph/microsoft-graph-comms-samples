param (
    [String]$Environment = 'D1',
    [String]$App = 'BOT',
    [Parameter(Mandatory)]
    [String]$Location,
    [Parameter(Mandatory)]
    [String]$OrgName,
    [string]$RoleName = 'Key Vault Administrator',
    
    # Default to false for lab
    [switch]$EnablePurgeProtection
)

Write-Output "$('-'*50)"
Write-Output $MyInvocation.MyCommand.Source

$LocationLookup = Get-Content -Path $PSScriptRoot\..\bicep\global\region.json | ConvertFrom-Json
$Prefix = $LocationLookup.$Location.Prefix

# Azure Blob Container Info
[String]$KVName = "${Prefix}-${OrgName}-${App}-${Environment}-kv".tolower()
[String]$RGName = "${Prefix}-${OrgName}-${App}-RG-${Environment}"


# Primary RG
Write-Verbose -Message "KeyVault RGName:`t $RGName" -Verbose
if (! (Get-AzResourceGroup -Name $RGName -EA SilentlyContinue))
{
    try
    {
        New-AzResourceGroup -Name $RGName -Location $Location -ErrorAction stop
    }
    catch
    {
        Write-Warning $_
        break
    }
}

# Primary KV
Write-Verbose -Message "KeyVault Name:`t`t $KVName" -Verbose
if (! (Get-AzKeyVault -Name $KVName -EA SilentlyContinue))
{
    try
    {
        New-AzKeyVault -Name $KVName -ResourceGroupName $RGName -Location $Location `
            -EnabledForDeployment -EnabledForTemplateDeployment -EnablePurgeProtection:$EnablePurgeProtection `
            -EnableRbacAuthorization -Sku Standard -ErrorAction Stop
    }
    catch
    {
        Write-Warning $_
        break
    }
}

# Primary KV RBAC
Write-Verbose -Message "Primary KV Name:`t $KVName RBAC for KV Contributor" -Verbose
if (Get-AzKeyVault -Name $KVName -EA SilentlyContinue)
{
    try
    {
        $CurrentUserId = Get-AzContext | ForEach-Object account | ForEach-Object Id
        if (! (Get-AzRoleAssignment -ResourceGroupName $RGName -SignInName $CurrentUserId -RoleDefinitionName $RoleName))
        {
            New-AzRoleAssignment -ResourceGroupName $RGName -SignInName $CurrentUserId -RoleDefinitionName $RoleName -Verbose
        }
    }
    catch
    {
        Write-Warning $_
        break
    }
}

# # LocalAdmin Creds
# Write-Verbose -Message "Primary KV Name:`t $KVName Secret for [localadmin]" -Verbose
# if (! (Get-AzKeyVaultSecret -VaultName $KVName -Name localadmin -EA SilentlyContinue))
# {
#     try
#     {
#         Write-Warning -Message 'vmss Username is: [botadmin]'
#         $vmAdminPassword = (Read-Host -AsSecureString -Prompt 'Enter the vmss password')
#         Set-AzKeyVaultSecret -VaultName $KVName -Name localadmin -SecretValue $vmAdminPassword
#     }
#     catch
#     {
#         Write-Warning $_
#         break
#     }
# }



