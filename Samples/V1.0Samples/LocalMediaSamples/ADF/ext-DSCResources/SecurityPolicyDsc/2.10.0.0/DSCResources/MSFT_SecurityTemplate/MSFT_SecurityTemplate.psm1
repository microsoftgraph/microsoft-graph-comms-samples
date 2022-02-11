$resourceModuleRootPath = Split-Path -Path (Split-Path $PSScriptRoot -Parent) -Parent
$modulesRootPath = Join-Path -Path $resourceModuleRootPath -ChildPath 'Modules'
Import-Module -Name (Join-Path -Path $modulesRootPath  `
              -ChildPath 'SecurityPolicyResourceHelper\SecurityPolicyResourceHelper.psm1') `
              -Force


$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_SecurityTemplate'

<#
    .SYNOPSIS
        Gets the path of the current policy template.
    .PARAMETER Path
        Not used in Get-TargetResource.
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Yes')]
        [String]
        $IsSingleInstance,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $Path
    )

    Write-Verbose -Message ($script:localizedData.GettingPolicyTemplate)
    $securityCmdlets = Get-Module -Name SecurityCmdlets -ListAvailable
    $currentUserRightsInf = ([System.IO.Path]::GetTempFileName()).Replace('tmp','inf')

    if ($securityCmdlets)
    {
        Backup-SecurityPolicy -Path $currentUserRightsInf
        $templateFileName = Format-SecurityPolicyFile -Path $currentUserRightsInf
    }
    else
    {
        Get-SecurityTemplate -Path $currentUserRightsInf | Out-Null
        $templateFileName = $currentUserRightsInf
    }

    $returnValue = @{
        Path = [System.String]$templateFileName
        IsSingleInstance = 'Yes'
    }

    $returnValue
}

<#
    .SYNOPSIS
        Gets the path of the desired policy template.
    .PARAMETER Path
        Specifies the path to the policy template.
#>
function Set-TargetResource
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Yes')]
        [String]
        $IsSingleInstance,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $Path
    )

    $securityCmdlets = Get-Module -Name SecurityCmdlets -ListAvailable

    if ($securityCmdlets)
    {
        Restore-SecurityPolicy -Path $Path
    }
    else
    {
        $secEditOutput = "$env:TEMP\Secedit-OutPut.txt"

        Invoke-Secedit -InfPath $Path -SecEditOutput $seceditOutput
    }
    # Verify secedit command was successful
    $testSuccuess = Test-TargetResource @PSBoundParameters

    if ($testSuccuess -eq $true)
    {
        Write-Verbose -Message ($script:localizedData.TaskSuccess)
    }
    else
    {
        Write-Error -Message ($script:localizedData.TaskFail)
    }
}

<#
    .SYNOPSIS
        Gets the path of the desired policy template.
    .PARAMETER Path
        Specifies the path to the policy template.
#>
function Test-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Yes')]
        [String]
        $IsSingleInstance,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $Path
    )

    $securityCmdlets = Get-Module -Name SecurityCmdlets -ListAvailable
    $fileExists = Test-Path -Path $Path

    if ($fileExists -eq $false)
    {
        throw ($script:localizedData.PathNotFound) -f $Path
    }

    if ($securityCmdlets)
    {
        $currentUserRightsInf = Join-Path -Path $env:temp -ChildPath 'SecurityPolicy.inf'
        Backup-SecurityPolicy -Path $currentUserRightsInf
    }


    $desiredPolicies = (Get-SecurityPolicy -FilePath $Path -Area 'USER_RIGHTS')
    $currentPolicies = (Get-SecurityPolicy -Area 'USER_RIGHTS')

    $policyNames = $desiredPolicies.keys

    foreach ($policy in $policyNames)
    {
        $policiesMatch = $false

        if ($null -eq $currentPolicies[$policy] -or $null -eq $desiredPolicies[$policy])
        {
            $policiesMatch = [String]::IsNullOrEmpty($currentPolicies[$policy]) -and [String]::IsNullOrEmpty($desiredPolicies[$policy])
        }
        else
        {
            $policiesMatch = $null -eq ( Compare-Object -ReferenceObject ($currentPolicies[$policy]).Trim() -DifferenceObject ($desiredPolicies[$policy]).Trim() )
        }

        if(-not $policiesMatch)
        {
            Write-Verbose -Message ($script:localizedData.NotDesiredState -f $Policy)
            return $false
        }
    }

    # If the code made it this far all policies must be in a desired state
    return $true
}

<#
    .SYNOPSIS
        Removes the other security areas from policy template file so only settings for user rights assignments are returned.
    .PARAMETER Path
        Specifies the path to the template file to be parsed.
#>
function Format-SecurityPolicyFile
{
    [OutputType([String])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Path
    )

    $outputPath = ([System.IO.Path]::GetTempFileName()).Replace('tmp','inf')
    $content = Get-Content -Path $Path

    $endOfFileMatch = Select-String -Path $Path -Pattern "Revision=1" -SimpleMatch

    $startOfFile = $privilegeRightIndex.LineNumber -1
    $endOfFile = $endOfFileMatch.LineNumber

    $content[$startOfFile..$endOfFile] | Out-File -FilePath $outputPath

    $outputPath
}

<#
    .SYNOPSIS
        Invokes secedit.exe to create an INF file of the current policies.
    .PARAMETER Path
        The path to the export INF file that will be created.
#>
function Get-SecurityTemplate
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Path
    )

    secedit.exe /export /cfg $Path /areas "USER_Rights"
}

Export-ModuleMember -Function *-TargetResource

