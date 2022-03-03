#requires -Version 1.0
Import-Module -Name .\DSCResources\xDSCFirewall\xDSCFirewall.psm1

$Global:DSCModuleName      = 'xDSCFirewall'
$Global:DSCResourceName    = 'xDSCFirewall'

InModuleScope -ModuleName XDSCFirewall -ScriptBlock {
  $Firewall = New-Object -TypeName PSObject -Property @{
    Enabled               = $true
    LogAllowed            = $false
    LogBlocked            = $true
    LogIgnored            = 'NotConfigured'
    LogMaxSizeKilobytes   = '4096'
    DefaultInboundAction  = 'Block'
    DefaultOutboundAction = 'Allow'
  }

  Describe -Name 'Testing if functions return correct objects' -Fixture {
    It -name 'Get-TargetResource returns a hashtable' -test {
      Get-TargetResource -Zone Public -Ensure Present | Should Be 'System.Collections.Hashtable'
    }

    It -name 'Test-TargetResource returns true or false' -test {
      (Test-TargetResource -Zone Public -Ensure 'Present').GetType() -as [string] | Should Be 'bool'
    }
  }

  Describe -Name "$($Global:DSCResourceName)\Get-TargetResource" -Fixture {
    $Firewall.Enabled = $false
    Mock -CommandName Get-NetFirewallProfile -MockWith {
      $Firewall
    }
    It -name 'Firewall disabled Get-TargetResource should return absent in hash table' -test {
      (Get-TargetResource -Zone Public -Ensure Present).Ensure | Should Be 'Absent'
    }

    $Firewall.Enabled = $true
    Mock -CommandName Get-NetFirewallProfile -MockWith {
      $Firewall
    }
    It -name 'Firewall enabled Get-TargetResource should return present in hash table' -test {
      (Get-TargetResource -Zone Public -Ensure Present).Ensure | Should Be 'Present'
    }
  }
  Describe -Name "Disabling Firewall with $($Global:DSCResourceName)\Set-TargetResource" -Fixture {
    It -name 'Disabling firewall and configuring with values' -test {
      Set-TargetResource -Zone Public -Ensure Absent -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow
    }
    Context -Name "Testing ensure/absent logic for $($Global:DSCResourceName)\Test-TargetResource on a disabled firewall zone" -Fixture {
      It -name 'Testing Test-TargetResource present logic should return false' -test {
        Test-TargetResource -Zone Public -Ensure Present -LogBlocked True -LogAllowed False -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name 'Testing Test-TargetResource absent logic should return true' -test {
        Test-TargetResource -Zone Public -Ensure Absent -LogBlocked True -LogAllowed False -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'True'
      }
    }
    Context -Name "Testing $($Global:DSCResourceName)\Test-TargetResource operater logic for absent" -Fixture {
      It -name "LogBlocked shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Absent -LogBlocked False -LogAllowed False -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "LogAllowed shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Absent -LogBlocked True -LogAllowed True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "LogIgnored shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Absent -LogAllowed False -LogBlocked True -LogIgnored False -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "LogMaxSizeKilobytes shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Absent -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 1024 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "DefaultInboundAction shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Absent -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Allow -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "DefaultInboundAction shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Absent -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Block | Should Be 'False'
      }
    }
  }
  Describe -Name "Enabling Firewall with $($Global:DSCResourceName)\Set-TargetResource" -Fixture {
    It -name 'Enabling firewall and configuring with values' -test {
      Set-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow
    }
    Context -Name 'Testing ensure/absent logic for Test-TargetResource on a enabled firewall zone' -Fixture {
      It -name 'Testing Test-TargetResource present logic should return true' -test {
        Test-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'true'
      }
      It -name 'Testing Test-TargetResource absent logic should return false' -test {
        Test-TargetResource -Zone Public -Ensure Absent -LogBlocked True -LogAllowed False -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'false'
      }
    }
    Context -Name "Testing $($Global:DSCResourceName)\Test-TargetResource operater logic for present" -Fixture {
      It -name "LogBlocked shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogBlocked False -LogAllowed False -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "LogAllowed shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogBlocked True -LogAllowed True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "LogIgnored shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked True -LogIgnored False -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "LogMaxSizeKilobytes shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 1024 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "DefaultInboundAction shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Allow -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "DefaultInboundAction shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Block | Should Be 'False'
      }
    }
  }
  Describe -Name "Setting firewall back to defaults with $($Global:DSCResourceName)\Set-TargetResource" -Fixture {
    It -name 'Enabling firewall with default values' -test {
      Set-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked False -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction NotConfigured -DefaultOutboundAction NotConfigured
    }
    Context -Name "Testing ensure/absent logic for $($Global:DSCResourceName)\Test-TargetResource on a enabled firewall zone" -Fixture {
      It -name 'Testing Test-TargetResource present logic should return true' -test {
        Test-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked False -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction NotConfigured -DefaultOutboundAction NotConfigured | Should Be 'true'
      }
      It -name 'Testing Test-TargetResource absent logic should return false' -test {
        Test-TargetResource -Zone Public -Ensure Absent -LogAllowed False -LogBlocked False -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction NotConfigured -DefaultOutboundAction NotConfigured | Should Be 'false'
      }
    }
    Context -Name "Testing $($Global:DSCResourceName)\Test-TargetResource operater logic for present" -Fixture {
      It -name "LogBlocked shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogBlocked False -LogAllowed False -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "LogAllowed shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogBlocked True -LogAllowed True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "LogIgnored shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked True -LogIgnored False -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "LogMaxSizeKilobytes shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 1024 -DefaultInboundAction Block -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "DefaultInboundAction shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Allow -DefaultOutboundAction Allow | Should Be 'False'
      }
      It -name "DefaultInboundAction shouldn't match so should return false" -test {
        Test-TargetResource -Zone Public -Ensure Present -LogAllowed False -LogBlocked True -LogIgnored NotConfigured -LogMaxSizeKilobytes 4096 -DefaultInboundAction Block -DefaultOutboundAction Block | Should Be 'False'
      }
    }
  }
}