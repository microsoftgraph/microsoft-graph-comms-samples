$resourceModuleRootPath = Split-Path -Path (Split-Path $PSScriptRoot -Parent) -Parent
$modulesRootPath = Join-Path -Path $resourceModuleRootPath -ChildPath 'Modules'
Import-Module -Name (Join-Path -Path $modulesRootPath  `
              -ChildPath 'SecurityPolicyResourceHelper\SecurityPolicyResourceHelper.psm1') `
              -Force

$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_AccountPolicy'

<#
    .SYNOPSIS
        Retreives the current account policy configuration
#>
function Get-TargetResource
{
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name
    )

    $returnValue = @{}
    $currentSecurityPolicy = Get-SecurityPolicy -Area SECURITYPOLICY
    $accountPolicyData = Get-PolicyOptionData -FilePath $("$PSScriptRoot\AccountPolicyData.psd1").Normalize()
    $accountPolicyList = Get-PolicyOptionList -ModuleName MSFT_AccountPolicy

    foreach ($accountPolicy in $accountPolicyList)
    {
        Write-Verbose -Message $accountPolicy
        $section = $accountPolicyData.$accountPolicy.Section
        Write-Verbose -Message ($script:localizedData.Section -f $section)
        $valueName = $accountPolicyData.$accountPolicy.Value
        Write-Verbose -Message ($script:localizedData.Value -f $valueName)
        $options = $accountPolicyData.$accountPolicy.Option
        Write-Verbose -Message ($script:localizedData.Option -f $($options -join ','))
        $currentValue = $currentSecurityPolicy.$section.$valueName
        Write-Verbose -Message ($script:localizedData.RawValue -f $($currentValue -join ','))

        if ($options.keys -eq 'String')
        {
            $stringValue = ($currentValue -split ',')[-1]
            $resultValue = ($stringValue -replace '"').Trim()

            if ($resultValue -eq -1 -and $accountPolicy -eq 'Maximum_Password_Age')
            {
                $resultValue = 0
            }
        }
        else
        {
            Write-Verbose -Message ($script:localizedData.RetrievingValue -f $valueName)
            if ($currentSecurityPolicy.$section.keys -contains $valueName)
            {
                $resultValue = ($accountPolicyData.$accountPolicy.Option.GetEnumerator() |
                    Where-Object -Property Value -eq $currentValue.Trim()).Name
            }
            else
            {
                $resultValue = $null
            }
        }
        $returnValue.Add($accountPolicy, $resultValue)
    }
    return $returnValue
}

<#
    .SYNOPSIS
        Sets the specified account policy
#>
function Set-TargetResource
{
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingPlainTextForPassword", "")]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingUserNameAndPassWordParams", "")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter()]
        [ValidateRange(0, 24)]
        [System.UInt32]
        $Enforce_password_history,

        [Parameter()]
        [ValidateRange(0, 999)]
        [System.UInt32]
        $Maximum_Password_Age,

        [Parameter()]
        [ValidateRange(0, 998)]
        [System.UInt32]
        $Minimum_Password_Age,

        [Parameter()]
        [ValidateRange(0, 14)]
        [System.UInt32]
        $Minimum_Password_Length,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Password_must_meet_complexity_requirements,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Store_passwords_using_reversible_encryption,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.UInt32]
        $Account_lockout_duration,

        [Parameter()]
        [ValidateRange(0, 999)]
        [System.UInt32]
        $Account_lockout_threshold,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.UInt32]
        $Reset_account_lockout_counter_after,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Enforce_user_logon_restrictions,

        [Parameter()]
        [ValidateRange(10, 99999)]
        [System.UInt32]
        $Maximum_lifetime_for_service_ticket,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.UInt32]
        $Maximum_lifetime_for_user_ticket,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.UInt32]
        $Maximum_lifetime_for_user_ticket_renewal,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.UInt32]
        $Maximum_tolerance_for_computer_clock_synchronization
    )

    $kerberosPolicies = @()
    $systemAccessPolicies = @()
    $nonComplaintPolicies = @()
    $accountPolicyList = Get-PolicyOptionList -ModuleName MSFT_AccountPolicy
    $accountPolicyData = Get-PolicyOptionData -FilePath $("$PSScriptRoot\AccountPolicyData.psd1").Normalize()
    $script:seceditOutput = "$env:TEMP\Secedit-OutPut.txt"
    $accountPolicyToAddInf = "$env:TEMP\accountPolicyToAdd.inf"

    $desiredPolicies = $PSBoundParameters.GetEnumerator() | Where-Object -FilterScript { $PSItem.key -in $accountPolicyList }

    foreach ($policy in $desiredPolicies)
    {
        $testParameters = @{
            Name        = 'Test'
            $policy.Key = $policy.Value
            Verbose     = $false
        }

        <# 
            Define what policies are not in a desired state so we only add those policies
            that need to be changed to the INF.
         #>
        $isInDesiredState = Test-TargetResource @testParameters
        if (-not ($isInDesiredState))
        {
            $policyKey = $policy.Key
            $policyData = $accountPolicyData.$policyKey
            $nonComplaintPolicies += $policyKey

            if ($policyData.Option.GetEnumerator().Name -eq 'String')
            {
                if ([String]::IsNullOrWhiteSpace($policyData.Option.String))
                {
                    if ($policy.Key -eq 'Maximum_Password_Age' -and $policy.Value -eq 0)
                    {
                        <#
                            This addresses the scenario when the desired value of Maximum_Password_Age is 0.
                            The INF file consumed by secedit.exe requires the value to be -1.
                        #>
                        $newValue = -1                        
                    }
                    else
                    {
                        $newValue = $policy.value
                    }
                }
                else
                {
                    $newValue = "$($policyData.Option.String)" + "$($policy.Value)"
                }
            }
            else
            {
                $newValue = $($policyData.Option[$policy.value])
            }

            if ($policyData.Section -eq 'System Access')
            {
                $systemAccessPolicies += "$($policyData.Value)=$newValue"
            }
            else
            {
                $kerberosPolicies += "$($policyData.Value)=$newValue"
            }
        }
    }

    $infTemplate = Add-PolicyOption -SystemAccessPolicies $systemAccessPolicies -KerberosPolicies $kerberosPolicies

    Out-File -InputObject $infTemplate -FilePath $accountPolicyToAddInf -Encoding unicode -Force

    Invoke-Secedit -InfPath $accountPolicyToAddInf -SecEditOutput $script:seceditOutput
    Remove-Item -Path $accountPolicyToAddInf

    $successResult = Test-TargetResource @PSBoundParameters

    if ($successResult -eq $false)
    {
        throw "$($script:localizedData.SetFailed -f $($nonComplaintPolicies -join ','))"
    }
    else
    {
        Write-Verbose -Message ($script:localizedData.SetSuccess)
    }
}

<#
    .SYNOPSIS
        Tests the desired account policy configuration against the current configuration
#>
function Test-TargetResource
{
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingPlainTextForPassword", "")]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingUserNameAndPassWordParams", "")]
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter()]
        [ValidateRange(0, 24)]
        [System.UInt32]
        $Enforce_password_history,

        [Parameter()]
        [ValidateRange(0, 999)]
        [System.UInt32]
        $Maximum_Password_Age,

        [Parameter()]
        [ValidateRange(0, 998)]
        [System.UInt32]
        $Minimum_Password_Age,

        [Parameter()]
        [ValidateRange(0, 14)]
        [System.UInt32]
        $Minimum_Password_Length,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Password_must_meet_complexity_requirements,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Store_passwords_using_reversible_encryption,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.UInt32]
        $Account_lockout_duration,

        [Parameter()]
        [ValidateRange(0, 999)]
        [System.UInt32]
        $Account_lockout_threshold,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.UInt32]
        $Reset_account_lockout_counter_after,

        [Parameter()]
        [ValidateSet("Enabled", "Disabled")]
        [System.String]
        $Enforce_user_logon_restrictions,

        [Parameter()]
        [ValidateRange(10, 99999)]
        [System.UInt32]
        $Maximum_lifetime_for_service_ticket,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.UInt32]
        $Maximum_lifetime_for_user_ticket,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.UInt32]
        $Maximum_lifetime_for_user_ticket_renewal,

        [Parameter()]
        [ValidateRange(0, 99999)]
        [System.UInt32]
        $Maximum_tolerance_for_computer_clock_synchronization
    )

    $currentAccountPolicies = Get-TargetResource -Name $Name -Verbose:0

    $desiredAccountPolicies = $PSBoundParameters

    foreach ($policy in $desiredAccountPolicies.Keys)
    {
        if ($currentAccountPolicies.ContainsKey($policy))
        {
            Write-Verbose -Message ($script:localizedData.TestingPolicy -f $policy)
            Write-Verbose -Message ($script:localizedData.PoliciesBeingCompared -f $($currentAccountPolicies[$policy] -join ','), $($desiredAccountPolicies[$policy] -join ','))

            if ($currentAccountPolicies[$policy] -ne $desiredAccountPolicies[$policy])
            {
                return $false
            }
        }
    }

    # if the code made it this far we must be in a desired state
    return $true
}

Export-ModuleMember -Function *-TargetResource
