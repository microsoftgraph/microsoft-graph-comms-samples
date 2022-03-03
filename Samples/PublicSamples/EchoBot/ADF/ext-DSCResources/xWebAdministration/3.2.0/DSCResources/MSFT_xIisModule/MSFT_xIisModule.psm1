$script:resourceModulePath = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent
$script:modulesFolderPath = Join-Path -Path $script:resourceModulePath -ChildPath 'Modules'
$script:localizationModulePath = Join-Path -Path $script:modulesFolderPath -ChildPath 'xWebAdministration.Common'

Import-Module -Name (Join-Path -Path $script:localizationModulePath -ChildPath 'xWebAdministration.Common.psm1')

# Import Localization Strings
$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_xIisModule'

function Get-TargetResource
{
    <#
    .SYNOPSIS
        This will return a hashtable of results
    #>

    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Path,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [String] $RequestPath,

        [Parameter(Mandatory = $true)]
        [String[]] $Verb,

        [Parameter()]
        [ValidateSet('FastCgiModule')]
        [String] $ModuleType = 'FastCgiModule',

        [Parameter()]
        [String] $SiteName
    )

        Assert-Module

        $currentVerbs = @()
        $Ensure = 'Absent'

        $modulePresent = $false;

        $handler = Get-IisHandler -Name $Name -SiteName $SiteName

        if ($handler )
        {
            $Ensure = 'Present'
            $modulePresent = $true;
        }

        foreach ($thisVerb  in $handler.Verb)
        {
            $currentVerbs += $thisVerb
        }

        $fastCgiSetup = $false

        if ($handler.Modules -eq 'FastCgiModule')
        {
            $fastCgi = Get-WebConfiguration /system.webServer/fastCgi/* `
                        -PSPath (Get-IisSitePath `
                        -SiteName $SiteName) | `
                        Where-Object{$_.FullPath -ieq $handler.ScriptProcessor}
            if ($fastCgi)
            {
                $fastCgiSetup = $true
            }
        }

        Write-Verbose -Message $script:localizedData.VerboseGetTargetResource

        $returnValue = @{
            Path          = $handler.ScriptProcessor
            Name          = $handler.Name
            RequestPath   = $handler.Path
            Verb          = $currentVerbs
            SiteName      = $SiteName
            Ensure        = $Ensure
            ModuleType    = $handler.Modules
            EndPointSetup = $fastCgiSetup
        }

        $returnValue

}

function Set-TargetResource
{
    <#
    .SYNOPSIS
        This will set the desired state
    #>

    [CmdletBinding()]
    param
    (
        [Parameter()]
        [ValidateSet('Present','Absent')]
        [String] $Ensure,

        [Parameter(Mandatory = $true)]
        [String] $Path,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [String] $RequestPath,

        [Parameter(Mandatory = $true)]
        [String[]] $Verb,

        [Parameter()]
        [ValidateSet('FastCgiModule')]
        [String] $ModuleType = 'FastCgiModule',

        [Parameter()]
        [String] $SiteName
    )

    $getParameters = Get-PSBoundParameters -FunctionParameters $PSBoundParameters
    $resourceStatus = Get-TargetResource @GetParameters
    $resourceTests = Test-TargetResourceImpl @PSBoundParameters -ResourceStatus $resourceStatus
    if ($resourceTests.Result)
    {
        return
    }

    if ($Ensure -eq 'Present')
    {
        if ($resourceTests.ModulePresent -and -not $resourceTests.ModuleConfigured)
        {
            Write-Verbose -Message $script:localizedData.VerboseSetTargetRemoveHandler
            Remove-IisHandler
        }

        if (-not $resourceTests.ModulePresent -or -not $resourceTests.ModuleConfigured)
        {
            Write-Verbose -Message $script:localizedData.VerboseSetTargetAddHandler
            Add-webconfiguration /system.webServer/handlers iis:\ -Value @{
                Name = $Name
                Path = $RequestPath
                Verb = $Verb -join ','
                Module = $ModuleType
                ScriptProcessor = $Path
            }
        }

        if (-not $resourceTests.EndPointSetup)
        {
            Write-Verbose -Message $script:localizedData.VerboseSetTargetAddfastCgi
            Add-WebConfiguration /system.webServer/fastCgi iis:\ -Value @{
                FullPath = $Path
            }
        }
    }
    else
    {
        Write-Verbose -Message $script:localizedData.VerboseSetTargetRemoveHandler
        Remove-IisHandler
    }
}

function Test-TargetResource
{
    <#
    .SYNOPSIS
        This tests the desired state. If the state is not correct it will return $false.
        If the state is correct it will return $true
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter()]
        [ValidateSet('Present','Absent')]
        [String] $Ensure,

        [Parameter(Mandatory = $true)]
        [String] $Path,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [String] $RequestPath,

        [Parameter(Mandatory = $true)]
        [String[]] $Verb,

        [Parameter()]
        [ValidateSet('FastCgiModule')]
        [String] $ModuleType = 'FastCgiModule',

        [Parameter()]
        [String] $SiteName
    )

    $getParameters = Get-PSBoundParameters -FunctionParameters $PSBoundParameters
    $resourceStatus = Get-TargetResource @GetParameters

    Write-Verbose -Message $script:localizedData.VerboseTestTargetResource

    return (Test-TargetResourceImpl @PSBoundParameters -ResourceStatus $resourceStatus).Result
}

#region Helper Functions

function Get-PSBoundParameters
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [Hashtable] $FunctionParameters
    )

    [Hashtable] $getParameters = @{}
    foreach ($key in $FunctionParameters.Keys)
    {
        if ($key -ine 'Ensure')
        {
            $getParameters.Add($key, $FunctionParameters.$key) | Out-Null
        }
    }

    return $getParameters
}

function Get-IisSitePath
{
    [OutputType([System.String])]
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [String] $SiteName
    )

    if (-not $SiteName)
    {
        return 'IIS:\'
    }
    else
    {
        return Join-Path 'IIS:\sites\' $SiteName
    }
}

function Get-IisHandler
{
    <#
    .NOTES
        Get a list on IIS handlers
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String] $Name,

        [Parameter()]
        [String] $SiteName
    )

    Write-Verbose -Message ($script:localizedData.VerboseGetIisHandler -f $Name,$SiteName)
    return Get-Webconfiguration -Filter 'System.WebServer/handlers/*' `
                                -PSPath (Get-IisSitePath `
                                -SiteName $SiteName) | `
                                Where-Object{$_.Name -ieq $Name}
}

function Remove-IisHandler
{
    <#
    .NOTES
        Remove an IIS Handler
    #>
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]

        [Parameter()]
        [String] $SiteName
    )

    $handler = Get-IisHandler @PSBoundParameters

    if ($handler)
    {
        Clear-WebConfiguration -PSPath $handler.PSPath `
                               -Filter $handler.ItemXPath `
                               -Location $handler.Location
    }
}

function Test-TargetResourceImpl
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [String] $Path,

        [Parameter(Mandatory = $true)]
        [String] $Name,

        [Parameter(Mandatory = $true)]
        [String] $RequestPath,

        [Parameter(Mandatory = $true)]
        [String[]] $Verb,

        [Parameter()]
        [ValidateSet('FastCgiModule')]
        [String] $ModuleType = 'FastCgiModule',

        [Parameter()]
        [String] $SiteName,

        [Parameter()]
        [ValidateSet('Present','Absent')]
        [String] $Ensure,

        [Parameter(Mandatory = $true)]
        [HashTable] $resourceStatus
    )

    $matchedVerbs = @()
    $mismatchVerbs =@()
    foreach ($thisVerb  in $resourceStatus.Verb)
    {
        if ($Verb -icontains $thisVerb)
        {
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetResourceImplVerb `
                            -f $Verb)
            $matchedVerbs += $thisVerb
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.VerboseTestTargetResourceImplExtraVerb `
                            -f $Verb)
            $mismatchVerbs += $thisVerb
        }
    }

    $modulePresent = $false
    if ($resourceStatus.Name.Length -gt 0)
    {
        $modulePresent = $true
    }

    Write-Verbose -Message ($script:localizedData.VerboseTestTargetResourceImplRequestPath `
                            -f $RequestPath)
    Write-Verbose -Message ($script:localizedData.VerboseTestTargetResourceImplPath `
                            -f $Path)
    Write-Verbose -Message ($script:localizedData.VerboseTestTargetResourceImplresourceStatusRequestPath `
                            -f $($resourceStatus.RequestPath))
    Write-Verbose -Message ($script:localizedData.VerboseTestTargetResourceImplresourceStatusPath `
                            -f $($resourceStatus.Path))

    $moduleConfigured = $false
    if ($modulePresent -and `
        $mismatchVerbs.Count -eq 0 -and `
        $matchedVerbs.Count-eq $Verb.Count -and `
        $resourceStatus.Path -eq $Path -and `
        $resourceStatus.RequestPath -eq $RequestPath)
    {
        $moduleConfigured = $true
    }

    Write-Verbose -Message ($script:localizedData.VerboseTestTargetResourceImplModulePresent `
                            -f $ModulePresent)
    Write-Verbose -Message ($script:localizedData.VerboseTestTargetResourceImplModuleConfigured `
                            -f $ModuleConfigured)
    if ($moduleConfigured -and ($ModuleType -ne 'FastCgiModule' -or $resourceStatus.EndPointSetup))
    {
        return @{
            Result = $true
            ModulePresent = $modulePresent
            ModuleConfigured = $moduleConfigured
        }
    }
    else
    {
        return @{
            Result = $false
            ModulePresent = $modulePresent
            ModuleConfigured = $moduleConfigured
        }
    }
}


#endregion

Export-ModuleMember -Function *-TargetResource
