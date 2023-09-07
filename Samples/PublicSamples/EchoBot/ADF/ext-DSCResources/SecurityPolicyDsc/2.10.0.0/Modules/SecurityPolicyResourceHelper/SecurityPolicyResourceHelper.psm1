<#
    .SYNOPSIS
        Retrieves the localized string data based on the machine's culture.
        Falls back to en-US strings if the machine's culture is not supported.

    .PARAMETER ResourceName
        The name of the resource as it appears before '.strings.psd1' of the localized string file.
        For example:
            AuditPolicySubcategory: MSFT_AuditPolicySubcategory
            AuditPolicyOption: MSFT_AuditPolicyOption
#>
function Get-LocalizedData
{
    [OutputType([String])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'resource')]
        [ValidateNotNullOrEmpty()]
        [String]
        $ResourceName,

        [Parameter(Mandatory = $true, ParameterSetName = 'helper')]
        [ValidateNotNullOrEmpty()]
        [String]
        $HelperName
    )

    # With the helper module just update the name and path variables as if it were a resource.
    if ($PSCmdlet.ParameterSetName -eq 'helper')
    {
        $resourceDirectory = $PSScriptRoot
        $ResourceName = $HelperName
    }
    else
    {
        # Step up one additional level to build the correct path to the resource culture.
        $resourceDirectory = Join-Path -Path (Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent) `
                                       -ChildPath "DSCResources\$ResourceName"
    }

    $localizedStringFileLocation = Join-Path -Path $resourceDirectory -ChildPath $PSUICulture

    if (-not (Test-Path -Path $localizedStringFileLocation))
    {
        # Fallback to en-US
        $localizedStringFileLocation = Join-Path -Path $resourceDirectory -ChildPath 'en-US'
    }

    Import-LocalizedData `
        -BindingVariable 'localizedData' `
        -FileName "$ResourceName.strings.psd1" `
        -BaseDirectory $localizedStringFileLocation

    return $localizedData
}

# This must be loaded after the Get-LocalizedData function is created.
$script:localizedData = Get-LocalizedData -HelperName 'SecurityPolicyResourceHelper'

<#
    .SYNOPSIS
        Wrapper around secedit.exe used to make changes
    .PARAMETER InfPath
        Path to an INF file with desired user rights assignment policy configuration
    .PARAMETER SeceditOutput
        Path to secedit log file output
    .EXAMPLE
        Invoke-Secedit -InfPath C:\secedit.inf -SeceditOutput C:\seceditLog.txt
#>
function Invoke-Secedit
{
    [OutputType([void])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $InfPath,

        [Parameter(Mandatory = $true)]
        [System.String]
        $SeceditOutput,

        [Parameter()]
        [System.Management.Automation.SwitchParameter]
        $OverWrite
    )

    $script:localizedData = Get-LocalizedData -HelperName 'SecurityPolicyResourceHelper'

    $tempDB = "$env:TEMP\DscSecedit.sdb"
    $arguments = "/configure /db $tempDB /cfg $InfPath"

    if ($OverWrite)
    {
        $arguments = $arguments + " /overwrite /quiet"
    }

    Write-Verbose "secedit arguments: $arguments"
    Start-Process -FilePath secedit.exe -ArgumentList $arguments -RedirectStandardOutput $seceditOutput -NoNewWindow -Wait
}

<#
    .SYNOPSIS
        Returns security policies configuration settings

    .PARAMETER Area
        Specifies the security areas to be returned

    .NOTES
    General notes
#>
function Get-SecurityPolicy
{
    [OutputType([Hashtable])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet("SECURITYPOLICY","GROUP_MGMT","USER_RIGHTS","REGKEYS","FILESTORE","SERVICES")]
        [System.String]
        $Area,

        [Parameter()]
        [System.String]
        $FilePath
    )

    if ($FilePath)
    {
        $currentSecurityPolicyFilePath = $FilePath
    }
    else
    {
        $currentSecurityPolicyFilePath = Join-Path -Path $env:temp -ChildPath 'SecurityPolicy.inf'

        Write-Debug -Message ($localizedData.EchoDebugInf -f $currentSecurityPolicyFilePath)

        secedit.exe /export /cfg $currentSecurityPolicyFilePath /areas $Area | Out-Null
    }

    $policyConfiguration = @{}
    switch -regex -file $currentSecurityPolicyFilePath
    {
        "^\[(.+)\]" # Section
        {
            $section = $matches[1]
            $policyConfiguration[$section] = @{}
            $CommentCount = 0
        }
        "^(;.*)$" # Comment
        {
            $value = $matches[1]
            $commentCount = $commentCount + 1
            $name = "Comment" + $commentCount
            $policyConfiguration[$section][$name] = $value
        }
        "(.+?)\s*=(.*)" # Key
        {
            $name,$value =  $matches[1..2] -replace "\*"
            $policyConfiguration[$section][$name] = $value
        }
    }

    Switch($Area)
    {
        "USER_RIGHTS"
        {
            $returnValue = @{}
            $privilegeRights = $policyConfiguration.'Privilege Rights'
            foreach ($key in $privilegeRights.keys )
            {
                $policyName = Get-UserRightConstant -Policy $key -Inverse
                $identity = ConvertTo-LocalFriendlyName -Identity $($privilegeRights[$key] -split ",").Trim() -Policy $policyName -Verbose:$VerbosePreference
                $returnValue.Add( $key,$identity )
            }

            continue
        }
        Default
        {
            $returnValue = $policyConfiguration
        }
    }

    return $returnValue
}

<#
    .SYNOPSIS
        Parses an INF file produced by 'secedit.exe /export' and returns an object of identites assigned to a user rights assignment policy
    .PARAMETER FilePath
        Path to an INF file
    .EXAMPLE
        Get-UserRightsAssignment -FilePath C:\seceditOutput.inf
#>
function Get-UserRightsAssignment
{
    [OutputType([Hashtable])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $FilePath
    )

    $policyConfiguration = @{}
    switch -regex -file $FilePath
    {
        "^\[(.+)\]" # Section
        {
            $section = $matches[1]
            $policyConfiguration[$section] = @{}
            $CommentCount = 0
        }
        "^(;.*)$" # Comment
        {
            $value = $matches[1]
            $commentCount = $commentCount + 1
            $name = "Comment" + $commentCount
            $policyConfiguration[$section][$name] = $value
        }
        "(.+?)\s*=(.*)" # Key
        {
            $name,$value =  $matches[1..2] -replace "\*"
            $policyConfiguration[$section][$name] = @(ConvertTo-LocalFriendlyName -Identity $($value -split ','))
        }
    }

    return $policyConfiguration
}

<#
    .SYNOPSIS
        Resolves username or SID to a NTAccount friendly name so desired and actual idnetities can be compared

    .PARAMETER Identity
        An Identity in the form of a friendly name (testUser1,contoso\testUser1) or SID

    .EXAMPLE
        PS C:\> ConvertTo-LocalFriendlyName testuser1
        Server1\TestUser1

        This example demonstrats converting a username without a domain name specified

    .EXAMPLE
        PS C:\> ConvertTo-LocalFriendlyName -Identity S-1-5-21-3084257389-385233670-139165443-1001
        Server1\TestUser1

        This example demonstrats converting a SID to a frendlyname
#>
function ConvertTo-LocalFriendlyName
{
    [OutPutType([string])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true,ValueFromPipeline=$true)]
        [System.String[]]
        $Identity,

        [Parameter()]
        [System.String]
        $Policy,

        [Parameter()]
        [System.String]
        $Scope = 'Get'
    )

    $friendlyNames = @()
    foreach ($id in $Identity)
    {
        $id = ( $id -replace "\*" ).Trim()
        if ($null -ne $id -and $id -match '^(S-[0-9-]{3,})')
        {
            # if id is a SID convert to a NTAccount
            $friendlyNames += ConvertTo-NTAccount -SID $id -Policy $Policy -Scope $Scope -Verbose:$VerbosePreference
        }
        else
        {
            # if id is an friendly name convert it to a sid and then to an NTAccount
            $sidResult = ConvertTo-Sid -Identity $id -Scope $Scope -Verbose:$VerbosePreference

            if ($sidResult -isnot [System.Security.Principal.SecurityIdentifier])
            {
                continue
            }

            $friendlyNames += ConvertTo-NTAccount -SID $sidResult.Value -Policy $Policy -Scope $Scope
        }
    }

    return $friendlyNames
}

<#
    .SYNOPSIS
        Tests if the provided Identity is null
    .PARAMETER Identity
        The identity string to test
#>
function Test-IdentityIsNull
{
    [OutputType([bool])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [AllowEmptyString()]
        [AllowNull()]
        [System.String[]]
        $Identity
    )

    if ( $null -eq $Identity -or [System.String]::IsNullOrWhiteSpace($Identity) )
    {
        return $true
    }
    else
    {
        return $false
    }
}

<#
    .SYNOPSIS
        Convert a SID to a common friendly name
    .PARAMETER SID
        SID of an identity being converted
#>
function ConvertTo-NTAccount
{
    [OutPutType([string])]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true,ValueFromPipeline=$true)]
        [System.Security.Principal.SecurityIdentifier[]]
        $SID,

        [Parameter()]
        [System.String]
        $Scope = 'Get',

        [Parameter()]
        [System.String]
        $Policy
    )

    $result = @()
    foreach ($id in $SID)
    {
        $id = ( $id -replace "\*" ).Trim()

        $sidId = [System.Security.Principal.SecurityIdentifier]$id
        try
        {
            $result += $sidId.Translate([System.Security.Principal.NTAccount]).value
        }
        catch
        {
            if ($Scope -eq 'Get')
            {
                Write-Verbose -Message ($script:localizedData.ErrorSidTranslation -f $sidId, $Policy)
                $result += $sidId.Value
            }
            else
            {
                throw "$($script:localizedData.ErrorSidTranslation -f $sidId, $Policy)"
            }
        }
    }

    return $result
}

<#
    .SYNOPSIS
        Converts an identity to a SID to verify it's a valid account

    .PARAMETER Identity
        Specifies the identity to convert

    .NOTES
        General notes
#>
function ConvertTo-Sid
{
    [OutputType([System.Security.Principal.SecurityIdentifier])]
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [System.String]
        $Identity,

        [Parameter()]
        [System.String]
        $Scope = 'Get'
    )

    $id = [System.Security.Principal.NTAccount]$Identity
    try
    {
        $result = $id.Translate([System.Security.Principal.SecurityIdentifier])
    }
    catch
    {
        if ($Scope -eq 'Get')
        {
            Write-Verbose -Message ($script:localizedData.ErrorIdToSid -f $Identity)
            $result = $id
        }
        else
        {
            throw "$($script:localizedData.ErrorIdToSid -f $Identity)"
        }
    }

    return $result
}


<#
    .SYNOPSIS
        Retrieves the Security Option Data to map the policy name and values as they appear in the Security Template Snap-in

    .PARAMETER FilePath
        Path to the file containing the Security Option Data
#>
function Get-PolicyOptionData
{
    [OutputType([hashtable])]
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.DesiredStateConfiguration.ArgumentToConfigurationDataTransformation()]
        [hashtable]
        $FilePath
    )
    return $FilePath
}

<#
    .SYNOPSIS
        Returns all the set-able parameters in the SecurityOption resource
#>
function Get-PolicyOptionList
{
    [OutputType([array])]
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)]
        [string]
        $ModuleName
    )

    $commonParameters = @( 'Name' )
    $commonParameters += [System.Management.Automation.PSCmdlet]::CommonParameters
    $commonParameters += [System.Management.Automation.PSCmdlet]::OptionalCommonParameters
    $moduleParameters = ( Get-Command -Name "Set-TargetResource" -Module $ModuleName ).Parameters.Keys |
        Where-Object -FilterScript { $PSItem -notin $commonParameters }

    return $moduleParameters
}

<#
    .SYNOPSIS
        Creates the INF file content that contains the security option configurations

    .PARAMETER SystemAccessPolicies
        Specifies the security options that pertain to [System Access] policies

    .PARAMETER RegistryPolicies
        Specifies the security opions that are managed via [Registry Values]
#>
function Add-PolicyOption
{
    [OutputType([System.Object[]])]
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [Collections.ArrayList]
        $SystemAccessPolicies,

        [Parameter()]
        [Collections.ArrayList]
        $RegistryPolicies,

        [Parameter()]
        [Collections.ArrayList]
        $KerberosPolicies
    )

    # insert the appropriate INI section
    if ( [string]::IsNullOrWhiteSpace( $RegistryPolicies ) -eq $false )
    {
        $RegistryPolicies.Insert(0,'[Registry Values]')
    }

    if ( [string]::IsNullOrWhiteSpace( $SystemAccessPolicies ) -eq $false )
    {
        $SystemAccessPolicies.Insert(0,'[System Access]')
    }

    if ( [string]::IsNullOrWhiteSpace( $KerberosPolicies ) -eq $false )
    {
        $KerberosPolicies.Insert(0,'[Kerberos Policy]')
    }

    $iniTemplate = @(
        "[Unicode]"
        "Unicode=yes"
        $systemAccessPolicies
        "[Version]"
        'signature="$CHICAGO$"'
        "Revision=1"
        $KerberosPolicies
        $registryPolicies
    )

    return $iniTemplate
}

<#
    .SYNOPSIS
        Converts policy names that match the GUI to the abbreviated names used by secedit.exe
    .PARAMETER Policy
        Name of the policy to get friendly name for.
#>
function Get-UserRightConstant
{
    [OutputType([string])]
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Policy,

        [Parameter()]
        [Switch]
        $Inverse
    )

    $friendlyNames = Get-Content -Path $PSScriptRoot\UserRightsFriendlyNameConversions.psd1 -Raw |
    ConvertFrom-StringData

    if ($Inverse)
    {
        $result = $friendlyNames.GetEnumerator() | Where-Object -FilterScript { $_.Value -eq $Policy }
        return $result.Key
    }

    return $friendlyNames[$Policy]
}
