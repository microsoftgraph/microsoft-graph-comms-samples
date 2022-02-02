Param (
    [Parameter(Mandatory)]
    [String]$Location,
    [Parameter(Mandatory)]
    [String]$SAName,
    [Parameter(Mandatory)]
    [String]$RGName
)

Write-Output "$('-'*50)"
Write-Output $MyInvocation.MyCommand.Source

Write-Verbose -Message "Global RGName:`t`t $RGName" -Verbose
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

Write-Verbose -Message "Global SAName:`t`t $SAName" -Verbose
if (! (Get-AzStorageAccount -EA SilentlyContinue | Where-Object StorageAccountName -EQ $SAName))
{
    try
    {
        # Create the global storage acounts
        ## Used for File and Blob Storage for assets/artifacts
        New-AzStorageAccount -ResourceGroupName $RGName -Name ($SAName).tolower() -AllowBlobPublicAccess $false `
            -SkuName Standard_RAGRS -Location $Location -Kind StorageV2 -EnableHttpsTrafficOnly $true -ErrorAction Stop
    }
    catch
    {
        Write-Warning $_
        break
    }
}

