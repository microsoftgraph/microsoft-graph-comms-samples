#region HEADER

# Unit Test Template Version: 1.2.1
$script:moduleRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ( (-not (Test-Path -Path (Join-Path -Path $script:moduleRoot -ChildPath 'DSCResource.Tests'))) -or `
     (-not (Test-Path -Path (Join-Path -Path $script:moduleRoot -ChildPath 'DSCResource.Tests\TestHelper.psm1'))) )
{
    & git @('clone','https://github.com/PowerShell/DscResource.Tests.git',(Join-Path -Path $script:moduleRoot -ChildPath 'DSCResource.Tests'))
}

Import-Module -Name (Join-Path -Path $script:moduleRoot -ChildPath (Join-Path -Path 'DSCResource.Tests' -ChildPath 'TestHelper.psm1')) -Force

$TestEnvironment = Initialize-TestEnvironment `
    -DSCModuleName 'SecurityPolicyDsc' `
    -DSCResourceName 'MSFT_SecurityOption' `
    -TestType Unit

#endregion HEADER

function Invoke-TestCleanup {
    Restore-TestEnvironment -TestEnvironment $TestEnvironment
}

# Begin Testing
try
{
    InModuleScope 'MSFT_SecurityOption' {

        $dscResourceInfo = Get-DscResource -Name SecurityOption
        $testParameters = @{
            Name = 'Test'
            User_Account_Control_Behavior_of_the_elevation_prompt_for_standard_users = 'Automatically deny elevation request'
            Accounts_Administrator_account_status = 'Enabled'
        }

        $kerberosValueMap = @{
            DES_CBC_CRC      = '4,1'
            DES_CBC_MD5      = '4,2'
            RC4_HMAC_MD5     = '4,4'
            AES128_HMAC_SHA1 = '4,8'
            AES256_HMAC_SHA1 = '4,16'
            FUTURE           = '4,2147483616'
        }

        Describe 'SecurityOptionHelperTests' {
            Context 'Get-PolicyOptionData' {
                $dataFilePath = Join-Path -Path $dscResourceInfo.ParentPath -ChildPath SecurityOptionData.psd1
                $securityOptionData = Get-PolicyOptionData -FilePath $dataFilePath.Normalize()
                $securityOptionPropertyList = $dscResourceInfo.Properties | Where-Object -FilterScript { $PSItem.Name -match '_' }

                It 'Should have the same count as property count' {
                    $securityOptionDataPropertyCount = $securityOptionData.Count
                    $securityOptionDataPropertyCount | Should Be $securityOptionPropertyList.Name.Count
                }

                foreach ( $name in $securityOptionData.Keys ) {
                    It "Should contain property name: $name" {
                        $securityOptionPropertyList.Name -contains $name | Should Be $true
                    }
                }

                $optionData = Get-PolicyOptionData -FilePath $dataFilePath.Normalize()

                foreach ($option in $optionData.GetEnumerator()) {
                    Context "$($option.Name)" {
                        $options = $option.Value.Option

                        foreach ($entry in $options.GetEnumerator())
                        {
                            It "$($entry.Name) Should have string as Option type" {
                                $entry.value.GetType().Name -is [string] | Should Be $true
                            }
                        }
                    }
                }
            }

            Context 'Add-PolicyOption' {
                It 'Should have [Registry Values]' {
                    [string[]]$testString = "Registry\path"
                    [string]$addOptionResult = Add-PolicyOption -RegistryPolicies $testPath

                    $addOptionResult | Should Match '[Registry Values]'
                }
                It 'Should have [System Access]' {
                    [string[]]$testString = "EnableAdminAccount=1"
                    [string]$addOptionResult = Add-PolicyOption -SystemAccessPolicies $testPath

                    $addOptionResult | Should Match '[System Access]'
                }
            }

            Context 'Format-LogonMessage' {
                $singleLineMessage = 'Line 1 - Message for line 1.,Line 2 - Message for line 2"," words"," seperated"," with"," commas.,Line 3 - Message for line 3.'
                $multiLineMessage = @'
                Line 1 - Message for line 1.
                Line 2 - Message for line 2, words, seperated, with, commas.
                Line 3 - Message for line 3.
'@
                It 'Should return a string' {
                    $result = Format-LogonMessage -Message $multiLineMessage
                    $result -is [string] | Should be $true
                }
                It 'Should match SingleLineMessage' {
                    $result = Format-LogonMessage -Message $multiLineMessage
                    $result -eq $singleLineMessage | Should be $true
                }
            }

            Context 'Test-RestrictedRemoteSam' {
                $desiredSettingInput = ConvertTo-CimRestrictedRemoteSam -InputObject "(A;;RC;;;S-1-5-32-544)"

                It 'Should be true' {
                    $currentSettingInput = ConvertTo-CimRestrictedRemoteSam -InputObject "(A;;RC;;;S-1-5-32-544)"
                    $result = Test-RestrictedRemoteSam -DesiredSetting $desiredSettingInput -CurrentSetting $currentSettingInput

                    $result | Should Be $true
                }

                It 'Should be false' {
                    $currentSettingInput = ConvertTo-CimRestrictedRemoteSam -InputObject "(A;;RC;;;S-1-5-20)"
                    $result = Test-RestrictedRemoteSam -DesiredSetting $desiredSettingInput -CurrentSetting $currentSettingInput

                    $result | Should Be $false
                }
            }

            Context 'ConvertTo-CimRestrictedRemoteSam' {
                It 'Should return BuiltIn\Administrators' {
                    $result = ConvertTo-CimRestrictedRemoteSam -InputObject "(D;;RC;;;BA)"

                    $result.Permission | Should Be 'Deny'
                    $result.Identity | Should Be 'BuiltIn\Administrators'
                }

                It 'Should return NT AUTHORITY\NETWORK SERVICE' {
                    $result = ConvertTo-CimRestrictedRemoteSam -InputObject "(A;;RC;;;S-1-5-20)"

                    $result.Permission | Should Be 'Allow'
                    $result.Identity | Should Be 'NT AUTHORITY\NETWORK SERVICE'
                }
            }

            Context 'Format-RestrictedRemoteSam' {
                $formatDescriptorDenyParameters = @{
                    Identity   = 'BUILTIN\Administrators'
                    Permission = 'Deny'
                }

                $formatDescriptorAllowParameters = @{
                    Identity   = 'NT AUTHORITY\NETWORK SERVICE'
                    Permission = 'Allow'
                }

                It 'Should Deny BUILTIN\Administrators' {
                    $result = Format-RestrictedRemoteSam -SecurityDescriptor $formatDescriptorDenyParameters

                    $result | Should Be '"O:BAG:BAD:(D;;RC;;;BA)"'
                }

                It 'Should Allow NT AUTHORITY\NETWORK SERVICE' {
                    $result = Format-RestrictedRemoteSam -SecurityDescriptor $formatDescriptorAllowParameters

                    $result | Should Be '"O:BAG:BAD:(A;;RC;;;S-1-5-20)"'
                }
            }

            Context 'ConvertTo-KerberosEncryptionValue' {
                $validateSet = (Get-Command -Name ConvertTo-KerberosEncryptionValue).Parameters.EncryptionType.Attributes.ValidValues
                $testCases = $validateSet | ForEach-Object -Process { @{EncryptionType = $PSItem} }

                It 'ValidateSet should match valueMap' {
                    $validateSet.Count | Should Be $kerberosValueMap.Count
                }

                It 'Should match valueMap <EncryptionType>' -TestCases $testCases {
                    param ($EncryptionType)

                    $result = ConvertTo-KerberosEncryptionValue -EncryptionType $EncryptionType
                    $result | Should Be $kerberosValueMap.$EncryptionType
                }
            }

            Context 'ConvertTo-KerberosEncryptionOption' {
                $testCases = $kerberosValueMap.Values | ForEach-Object -Process { @{OptionValue = $PSItem} }

                It 'Should match Kerberos Option <OptionValue>' -TestCases $testCases {
                    param ($OptionValue)

                    $result = ConvertTo-KerberosEncryptionOption -EncryptionValue $OptionValue
                    $shouldResult = $kerberosValueMap.GetEnumerator() | Where-Object -FilterScript { $PSItem.value -eq $OptionValue }
                    $result | Should Be $shouldResult.Name
                }
            }
        }
        Describe 'Get-TargetResource' {
            Context 'General operation tests' {
                It 'Should not throw' {
                    { Get-TargetResource -Name Test } | Should Not throw
                }

                It 'Should return one hashTable' {
                    $getTargetResult = Get-TargetResource -Name Test

                    $getTargetResult.GetType().BaseType.Name | Should Not Be 'Array'
                    $getTargetResult.GetType().Name | Should Be 'Hashtable'
                }
            }
        }
        Describe 'Test-TargetResource' {
            $falseMockResult = @{
                User_Account_Control_Behavior_of_the_elevation_prompt_for_standard_users = 'Prompt for credentials'
            }

            Context 'General operation tests' {
                It 'Should return a bool' {
                    $testResult = Test-TargetResource @testParameters
                    $testResult -is [bool] | Should Be $true
                }
            }

            Context 'Not in a desired state' {
                It 'Should return false when NOT in desired state' {
                    Mock -CommandName Get-TargetResource -MockWith { $falseMockResult }
                    $testResult = Test-TargetResource @testParameters
                    $testResult | Should Be $false
                }
            }

            Context 'In a desired State' {
                $trueMockResult = $testParameters.Clone()
                $trueMockResult.Remove('Name')
                It 'Should return true when in desired state' {
                    Mock -CommandName Get-TargetResource -MockWith { $trueMockResult }
                    $testResult = Test-TargetResource @testParameters
                    $testResult | Should Be $true
                }
            }

            Context 'Null handler' {
                $avoidNullTestParameters = @{
                    Name = 'Test'
                    Network_security_Configure_encryption_types_allowed_for_Kerberos = 'AES128_HMAC_SHA1'
                }

                Mock -CommandName Get-TargetResource -MockWith {
                    @{
                        Name = 'Test'
                        Network_security_Configure_encryption_types_allowed_for_Kerberos = $null
                    }
                }

                It 'Should not throw when the existing value is null' {
                    { Test-TargetResource @avoidNullTestParameters } | Should Not Throw
                }
            }

            Context 'Restricted Remote Sam' {
                $restrictedSamvalue = ConvertTo-CimRestrictedRemoteSam -InputObject "(A;;RC;;;S-1-5-20)"
                $parameters = @{
                    Name = 'Test'
                    Network_access_Restrict_clients_allowed_to_make_remote_calls_to_SAM = $restrictedSamvalue
                }

                Mock -CommandName Get-TargetResource -MockWith {
                    @{
                        Name = 'Test'
                        Network_access_Restrict_clients_allowed_to_make_remote_calls_to_SAM = $restrictedSamvalue
                    }
                }

                $result = Test-TargetResource @parameters
                $result | Should Be $true
            }
        }
        Describe 'Set-TargetResource' {
            Mock -CommandName Invoke-Secedit -MockWith {}

            Context 'Successfully applied security policy' {
                Mock -CommandName Test-TargetResource -MockWith { $true }
                It 'Should not throw when successfully updated security option' {
                    { Set-TargetResource @testParameters } | Should Not throw
                }

                It 'Should call Test-TargetResource 2 times' {
                    Assert-MockCalled -CommandName Test-TargetResource -Times 2
                }
            }

            Context 'Failed to apply security policy' {
                Mock -CommandName Test-TargetResource -MockWith { $false }
                It 'Should throw when failed to apply security policy' {
                    { Set-TargetResource @testParameters } | Should throw
                }

                It 'Should call Test-TargetResource 2 times' {
                    Assert-MockCalled -CommandName Test-TargetResource -Times 2
                }
            }

            Context 'Call correct helper functions' {
                Mock -CommandName Test-TargetResource
                Mock -CommandName Format-LogonMessage
                Mock -CommandName ConvertTo-KerberosEncryptionValue
                Mock -CommandName Format-RestrictedRemoteSam

                $settingsThatRequireHelpers = @(
                    @{
                        Name = 'Test'
                        Interactive_logon_Message_text_for_users_attempting_to_log_on = 'You must accept the EULA before preceeding.'
                        HelperFunction = 'Format-LogonMessage'
                    }
                    @{
                        Name = 'Test'
                        Network_security_Configure_encryption_types_allowed_for_Kerberos = 'AES256_HMAC_SHA1'
                        HelperFunction = 'ConvertTo-KerberosEncryptionValue'
                    }
                    @{
                        Name = 'Test'
                        Network_access_Restrict_clients_allowed_to_make_remote_calls_to_SAM = (ConvertTo-CimRestrictedRemoteSam "O:BAG:BAD:(D;;RC;;;BA)")
                        HelperFunction = 'Format-RestrictedRemoteSam'
                    }
                )

                foreach ($setting in $settingsThatRequireHelpers)
                {
                    $parameters = $setting.Clone()
                    $parameters.Remove('HelperFunction')

                    Set-TargetResource @parameters

                    It 'Should call correct helper function' {
                        Assert-MockCalled -CommandName $setting.HelperFunction -Times 1
                    }
                }
            }

            It "Should call Invoke-Secedit 2 times" {
                Assert-MockCalled -CommandName Invoke-Secedit -Times 2
            }
        }
    }
}
finally
{
    Invoke-TestCleanup
}
