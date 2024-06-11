
# use powershell

#Requires -Module AZ.Accounts

<#
.SYNOPSIS
    Create Azure AD Service Principal and the GH Secret for the workflow deployment
.DESCRIPTION
    Create Azure AD Service Principal and the GH Secret for the workflow deployment
.EXAMPLE
    . .\Scripts\Create-GhActionsSecret.ps1 -rgName ACU1-WBC-HAA-RG-G1 -RoleName 'Storage Blob Data Contributor'

#>

param (
    [int]$SecretExpiryYears = 1,
    [Parameter(Mandatory)]
    [string]$OrgName,
    [Parameter(Mandatory)]
    [string]$Location,
    [string]$App = 'BOT',
    [string]$Environment = 'D1',
    [switch]$AddStorageAccess,
    [switch]$CurrentUserStorageAccess,
    [string]$RoleName = 'Owner',
    [string]$StorageRole = 'Storage Blob Data Contributor',
    [switch]$ForceNewPrincipal
)

# Write-Output $PSScriptRoot
Write-Output $MyInvocation.MyCommand.Source

$useGhCli = $true
if (Get-Command gh)
{
    Write-Warning -Message "$(gh --version | Select-Object -First 1)`n"
    $ghAuthStatus = gh auth status *>&1
    if (! ($ghAuthStatus -notmatch "not logged into")) {
        Write-Warning -Message "You are not logged into gh cli"
        $loginToGhCli = Read-Host -Prompt "Do you want to login to gh cli? `n[No] will continue setup and you will need to upload the secret to your github repo manually. [Y/N]"
        if ($loginToGhCli -eq 'y') {
            gh auth login    
        }
        else {
            $useGhCli = $false
        }
    }
}
else 
{
    Write-Warning -Message "You do not have github cli installed"
    # do you want to continue add the secret to github manually?
    $continueWithoutGhCli = Read-Host -Prompt "Do you want to continue and add the secret to your github repo manually? [Y/N]"
    if ($continueWithoutGhCli -eq 'y') {
        $useGhCli = $false
    }
    else {
        throw 'please install GH.exe to create GH secret [https://github.com/cli/cli/releases/latest]'    
    }
}

$repo = git config --get remote.origin.url
if ($repo)
{
    Write-Warning "Your local repo is:`t $($repo)`n"
    $GHProject = ( $repo | Split-Path -Leaf ) -replace '.git', ''
}
else 
{
    throw 'please set location to a Git repo for which to create the secret'
}

# Runs under Service Principal that is owner
$context = Get-AzContext
$Tenant = $Context.Tenant.Id
$SubscriptionID = $Context.Subscription.Id

Write-Warning "Your context is:`n $($Context | Format-List -Property Name,Account,Subscription,Tenant | Out-String)"

if ($Context)
{
    Write-Verbose -Message "Setting SP RBAC on      : [$RoleName] on [$($context.Subscription.Name)] [$($context.Subscription.Id)]" -Verbose
    $SecretName = 'AZURE_CREDENTIALS_{0}_BOT' -f $OrgName
    $ServicePrincipalName = "GH_${GHProject}_{0}_BOT" -f $OrgName

    Write-Verbose -Message "Creating GH Secret Name : [$($SecretName)] in [$($GHProject)] git Secrets" -Verbose
    Write-Verbose -Message "Creating Azure AD SP    : [$($ServicePrincipalName)]" -Verbose
}
else 
{
    throw 'please select the correct Azure Account Context'
}

$LocationLookup = Get-Content -Path $PSScriptRoot\..\bicep\global\region.json | ConvertFrom-Json
$Prefix = $LocationLookup.$Location.Prefix

# Azure Blob Container Info
$SAName = "${Prefix}${OrgName}${App}${Environment}saglobal".tolower()
Write-Warning "Storage Account Name is : [$SAName]"
$storageId = Get-AzStorageAccount | Where-Object StorageAccountName -EQ $SAName | ForEach-Object id

if ($CurrentUserStorageAccess)
{
    # Add storage permissions for current user
    $CurrentUserId = Get-AzContext | ForEach-Object account | ForEach-Object Id
    if (! (Get-AzRoleAssignment -Scope $storageId -SignInName $CurrentUserId -RoleDefinitionName $StorageRole -Verbose))
    {
        New-AzRoleAssignment -Scope $storageId -SignInName $CurrentUserId -RoleDefinitionName $StorageRole -Verbose
    }
    else
    {
        Write-Warning -Message "Current User `t`t : [$CurrentUserId] already has role [$StorageRole] on Storage Account [$SAName]"
    }
}

#region Create the Service Principal in Azure AD
$appID = Get-AzADApplication -DisplayName $ServicePrincipalName
if (! $appID -or $ForceNewPrincipal)
{
    # Create Service Principal
    New-AzADServicePrincipal -DisplayName $ServicePrincipalName -OutVariable sp -EndDate (Get-Date).AddYears($SecretExpiryYears) -Role $RoleName # -Scope $Scope
    
    if ($AddStorageAccess)
    {
        # Add storage permissions for service principal
        if (! (Get-AzRoleAssignment -Scope $storageId -ObjectId $sp[0].Id -RoleDefinitionName $StorageRole -Verbose))
        {
            New-AzRoleAssignment -Scope $storageId -ObjectId $sp[0].Id -RoleDefinitionName $StorageRole -Verbose
        }
        else 
        {
            Write-Warning -Message "Current SP [$ServicePrincipalName] already has role [$StorageRole] on Storage Account [$SAName]"
        }
    }

    # Only set the GH Secret the first time

    $secret = [ordered]@{
        clientId                         = $SP.AppId
        clientSecret                     = [System.Net.NetworkCredential]::new('', $SP.PasswordCredentials.SecretText).Password
        tenantId                         = $Tenant
        subscriptionId                   = $SubscriptionID
        'activeDirectoryEndpointUrl'     = 'https://login.microsoftonline.com'
        'resourceManagerEndpointUrl'     = 'https://management.azure.com/'
        'activeDirectoryGraphResourceId' = 'https://graph.windows.net/'
        'sqlManagementEndpointUrl'       = 'https://management.core.windows.net:8443/'
        'galleryEndpointUrl'             = 'https://gallery.azure.com/'
        'managementEndpointUrl'          = 'https://management.core.windows.net/'
    } | ConvertTo-Json

    if ($useGhCli) {
        #  https://cli.github.com/manual/
        $secret | gh secret set $SecretName -R $repo
    }
    else {
        Write-Warning "Copy this output and create a new secret in your GitHub repo with name: $SecretName"
    }
    $secret
}
else
{
    Write-Warning "Principal already exists: [$ServicePrincipalName]"
    #Get-AzADServicePrincipal -DisplayName $ServicePrincipalName -OutVariable sp
}
#endregion



<#  Sample output.

#>