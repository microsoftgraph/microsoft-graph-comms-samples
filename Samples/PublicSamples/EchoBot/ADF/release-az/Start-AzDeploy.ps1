<#
.SYNOPSIS
    Short description
.DESCRIPTION
    Long description
.EXAMPLE
    PS C:\> <example usage>
    Explanation of what the example does
.INPUTS
    Inputs (if any)
.OUTPUTS
    Output (if any)
.NOTES
    General notes
#>

Function global:AzDeploy
{
    [CmdletBinding()]
    param (
        # The ADF root directory
        [string] $Artifacts = (Get-Item -Path $PSScriptRoot | ForEach-Object Parent | ForEach-Object FullName),
        
        # Used for DSC Configurations
        [string] $DSCSourceFolder = 'ext-DSC',

        # Used DSC Resources if compiling DSC to zip
        [string] $DSCResourceFolder = 'DSCResources',
        
        [alias('DP', 'Deployment')]
        [string] $Environment = 'D1',

        [validateset('BOT')]
        [alias('AppName')]
        [string] $App = 'BOT',

        [string] $OrgName,

        [String] $Prefix = 'ACU1',

        [alias('TF')]
        [string] $TemplateFile = 'main.bicep',

        [alias('TP')]
        [string] $TemplateParameterFile = "azuredeploy-${OrgName}.parameters.json",

        [alias('ComputerName')]
        [string] $CN = '.',

        [string] $StorageAccountName,

        # Optional, you can use keyvault, update reference in
        # ADF\azuredeploy-OrgName.parameters.json for the 'vmAdminPassword' parameter
        [securestring] $vmAdminPassword,

        # When deploying VM's, this is a subset of AppServers e.g. AppServers, SQLServers, ADPrimary
        [string] $DeploymentName = ($Prefix + '-' + $OrgName + '-' + $App + '-' + $Environment + '-EchoBot'),

        [switch] $FullUpload,

        [validateset('RG', 'SUB', 'MG', 'TENANT')]
        [string]$Scope = 'SUB',

        [switch] $WhatIf,

        [switch] $PackageDSC,

        [validateset('ResourceIdOnly', 'FullResourcePayloads')]
        [String] $WhatIfFormat = 'ResourceIdOnly'
    )

    $TemplateFile = "$Artifacts/$TemplateFile"
    $TemplateParameterFile = "$Artifacts/$TemplateParameterFile"
    $DSCSourceFolder = "$Artifacts/$DSCSourceFolder"
    $DSCResourceFolder = "$Artifacts/$DSCResourceFolder"

    if (! (Test-Path -Path $TemplateParameterFile))
    {
        throw "Cannot find Parameter file: [$TemplateParameterFile], run deploy.ps1 to create"
    }

    # Read in the Prefix Lookup for the Region.
    $PrefixLookup = Get-Content $Artifacts/bicep/global/prefix.json | ConvertFrom-Json
    $ResourceGroupLocation = $PrefixLookup | ForEach-Object $Prefix | ForEach-Object location
    $ResourceGroupName = $prefix + '-' + $OrgName + '-' + $App + '-RG-' + $Environment

    #region Global Settings file + Optional Regional Settings file
    $GlobalGlobal = Get-Content -Path $Artifacts/Global-Global.json | ConvertFrom-Json -Depth 10 | ForEach-Object Global
    # Convert any objects back to string so they are not deserialized
    $GlobalGlobal | Get-Member -MemberType NoteProperty | ForEach-Object {

        if ($_.Definition -match 'PSCustomObject')
        {
            $Object = $_.Name
            $String = $GlobalGlobal.$Object | ConvertTo-Json -Compress -Depth 10
            $GlobalGlobal.$Object = $String
        }
    }
    
    $Global = @{}
    $GlobalGlobal | Get-Member -MemberType NoteProperty | ForEach-Object {
        $Property = $_.Name
        $Global[$Property] = $GlobalGlobal.$Property
    }
    $Global['CN'] = $CN
    #endregion

    #region Only needed for extensions such as DSC or Script extension
    $StorageAccount = Get-AzStorageAccount | Where-Object StorageAccountName -EQ $StorageAccountName
    $StorageContainerName = "$Prefix-$App-stageartifacts-$env:USERNAME".ToLowerInvariant()
    $TemplateURIBase = $StorageAccount.Context.BlobEndPoint + $StorageContainerName
    Write-Verbose "Storage Account is: [$StorageAccountName] and container is: [$StorageContainerName]" -Verbose

    if (!($StorageAccount))
    {
        throw 'Please run the deploy.ps1 or Create-StorageAccount.ps1'
    }

    $SASParams = @{
        Container  = $StorageContainerName
        Context    = $StorageAccount.Context
        Permission = 'r'
        ExpiryTime = (Get-Date).AddHours(40)
    }
    $queryString = (New-AzStorageContainerSASToken @SASParams).Substring(1)
    $Global['_artifactsLocation'] = $TemplateURIBase
    $Global['_artifactsLocationSasToken'] = "?${queryString}"
    $Global['OrgName'] = $OrgName
    $Global['AppName'] = $App
    $Global['SAName'] = $StorageAccountName
    $Global['GlobalRGName'] = $ResourceGroupName

    # Create the storage container only if it doesn't already exist
    if ( -not (Get-AzStorageContainer -Name $StorageContainerName -Context $StorageAccount.Context -Verbose -ErrorAction SilentlyContinue))
    {
        $c = New-AzStorageContainer -Name $StorageContainerName -Context $StorageAccount.Context -ErrorAction SilentlyContinue *>&1
    }

    if ( -not $FullUpload )
    {
        if ($PackageDSC)
        {
            $Include = @(
                "$Artifacts\ext-DSC\"
            )
            # Create DSC configuration archive only for the files that changed
            git -C $DSCSourceFolder diff --diff-filter d --name-only $Include |
                Where-Object { $_ -match 'ps1$' } | ForEach-Object {
                
                    # ignore errors on git diff for deleted files
                    $File = Get-Item -EA Ignore -Path (Join-Path -ChildPath $_ -Path (Split-Path -Path $Artifacts))
                    if ($File)
                    {
                        $DSCArchiveFilePath = $File.FullName.Substring(0, $File.FullName.Length - 4) + '.zip'
                        Publish-AzVMDscConfiguration $File.FullName -OutputArchivePath $DSCArchiveFilePath -Force -Verbose
                    }
                    else 
                    {
                        Write-Verbose -Message "File not found, assume deleted, will not upload [$_]"
                    }
                }
        }

        # Upload only files that changes since last git add, i.e. only for the files that changed, 
        # use -fullupload to upload ALL files
        # only look in the 3 templates directories for uploading files
        $Include = @(
            "$Artifacts\ext-DSC\",
            "$Artifacts\ext-CD\"
        )
        git -C $Artifacts diff --diff-filter d --name-only $Include | ForEach-Object {
                
            # ignore errors on git diff for deleted files
            # added --diff-filter above, so likely don't need this anymore, will leave it anyway
            $File = Get-Item -EA Ignore -Path (Join-Path -ChildPath $_ -Path (Split-Path -Path $Artifacts))
            if ($File)
            {
                $StorageParams = @{
                    File      = $File.FullName
                    Blob      = $File.FullName.Substring($Artifacts.length + 1)
                    Container = $StorageContainerName
                    Context   = $StorageAccount.Context
                    Force     = $true
                }
                Set-AzStorageBlobContent @StorageParams | Select-Object Name, Length, LastModified
            }
            else 
            {
                Write-Verbose -Message "File not found, assume deleted, will not upload [$_]"
            }
        }
        Start-Sleep -Seconds 2
    }
    else
    {
        if ((Test-Path $DSCSourceFolder) -and $PackageDSC)
        {
            Get-ChildItem $DSCSourceFolder -File -Filter '*.ps1' | ForEach-Object {

                $DSCArchiveFilePath = $_.FullName.Substring(0, $_.FullName.Length - 4) + '.zip'
                Publish-AzVMDscConfiguration $_.FullName -OutputArchivePath $DSCArchiveFilePath -Force -Verbose
            }
        }
            
        $Include = @(
            # no longer uploading any templates only extensions
            'ext-DSC', 'ext-CD'
        )
        Get-ChildItem -Path $Artifacts -Include $Include -Recurse -Directory |
            Get-ChildItem -File -Include *.json, *.zip, *.psd1, *.sh, *.ps1 | ForEach-Object {
                #    $_.FullName.Substring($Artifacts.length)
                $StorageParams = @{
                    File      = $_.FullName
                    Blob      = $_.FullName.Substring($Artifacts.length + 1 )
                    Container = $StorageContainerName
                    Context   = $StorageAccount.Context
                    Force     = $true
                }
                Set-AzStorageBlobContent @StorageParams
            } | Select-Object Name, Length, LastModified
    }
    #endregion

    $TemplateArgs = @{ }
    $OptionalParameters = @{ }
    $OptionalParameters['Global'] = $Global
    $OptionalParameters['Environment'] = $Environment.substring(0, 1)
    $OptionalParameters['DeploymentID'] = $Environment.substring(1, 1)

    if ($vmAdminPassword)
    {
        $OptionalParameters['vmAdminPassword'] = $vmAdminPassword
    }

    Write-Warning -Message "Using parameter file: [$TemplateParameterFile]"
    $TemplateArgs['TemplateParameterFile'] = $TemplateParameterFile

    Write-Warning -Message "Using template file: [$TemplateFile]"
    $TemplateFile = Get-Item -Path $TemplateFile | ForEach-Object FullName

    Write-Warning -Message "Using template File: [$TemplateFile]"
    $TemplateArgs['TemplateFile'] = $TemplateFile

    $OptionalParameters.getenumerator() | ForEach-Object {
        Write-Verbose $_.Key -Verbose
        Write-Warning $_.Value
    }

    $TemplateArgs.getenumerator() | Where-Object Key -NE 'queryString' | ForEach-Object {
        Write-Verbose $_.Key -Verbose
        Write-Warning $_.Value
    }

    $Common = @{
        Name          = $DeploymentName
        Location      = $ResourceGroupLocation
        Verbose       = $true
        ErrorAction   = 'SilentlyContinue'
        ErrorVariable = 'e'
    }

    switch ($Scope)
    {
        # Tenant
        'TENANT'
        {
            Write-Output 'T0'
            if ($WhatIf)
            {
                $Common.Remove('Name')
                $Common['ResultFormat'] = $WhatIfFormat
                Get-AzTenantDeploymentWhatIfResult @Common @TemplateArgs @OptionalParameters
            }
            else 
            {
                $global:r = New-AzTenantDeployment @Common @TemplateArgs @OptionalParameters
            }
        }

        # ManagementGroup
        'MG'
        {
            Write-Output 'M0'
            $MGName = Get-AzManagementGroup | Where-Object displayname -EQ 'Root Management Group' | ForEach-Object Name
            if ($WhatIf)
            {
                $Common.Remove('Name')
                $Common['ResultFormat'] = $WhatIfFormat
                Get-AzManagementGroupDeploymentWhatIfResult @Common @TemplateArgs @OptionalParameters -ManagementGroupId $MGName
            }
            else 
            {
                $global:r = New-AzManagementGroupDeployment @Common @TemplateArgs @OptionalParameters -ManagementGroupId $MGName
            }
        }

        # Subscription
        'SUB'
        {
            if ($WhatIf)
            {
                $Common.Remove('Name')
                $Common['ResultFormat'] = $WhatIfFormat
                Get-AzDeploymentWhatIfResult @Common @TemplateArgs @OptionalParameters
            }
            else 
            {
                $global:r = New-AzDeployment @Common @TemplateArgs @OptionalParameters
            }
        }

        # ResourceGroup
        'RG'
        {
            $Common.Remove('Location')
            $Common['ResourceGroupName'] = $ResourceGroupName
            if ($WhatIf)
            {
                $Common.Remove('Name')
                $Common['ResultFormat'] = $WhatIfFormat
                Get-AzResourceGroupDeploymentWhatIfResult @Common @TemplateArgs @OptionalParameters
            }
            else 
            {
                $global:r = New-AzResourceGroupDeployment @Common @TemplateArgs @OptionalParameters
            }
        }
    }

    $Properties = 'ResourceGroupName', 'DeploymentName', 'ProvisioningState', 'Timestamp', 'Mode', 'CorrelationId'
    $r | Select-Object -Property $Properties | Format-Table -AutoSize
    $global:e = $e
    $e
} # Start-AzDeploy
