function Get-TargetResource
{
  [CmdletBinding()]
  [OutputType([System.Collections.Hashtable])]
  param
  (
    [Parameter(Mandatory = $true)][ValidateSet('Domain','Private','Public')]
    [System.String]
    $Zone,

    [Parameter(Mandatory = $true)][ValidateSet('Present','Absent')]
    [System.String]
    $Ensure
  )
  $firewall = Get-NetFirewallProfile $Zone | Select-Object -Property Enabled, LogAllowed, LogBlocked, LogIgnored, LogMaxSizeKilobytes, DefaultInboundAction, DefaultOutboundAction

  if ($firewall.Enabled -eq $false) 
  {
    return @{
      Ensure                = 'Absent'
      Zone                  = $Zone
      LogAllowed            = $firewall.LogAllowed
      LogBlocked            = $firewall.LogBlocked
      LogIgnored            = $firewall.LogIgnored
      LogMaxSizeKilobytes   = $firewall.LogMaxSizeKilobytes
      DefaultInboundAction  = $firewall.DefaultInboundAction
      DefaultOutboundAction = $firewall.DefaultOutboundAction
    }
  }
  else
  {
    return @{
      Ensure                = 'Present'
      Zone                  = $Zone
      LogAllowed            = $firewall.LogAllowed
      LogBlocked            = $firewall.LogBlocked
      LogIgnored            = $firewall.LogIgnored
      LogMaxSizeKilobytes   = $firewall.LogMaxSizeKilobytes
      DefaultInboundAction  = $firewall.DefaultInboundAction
      DefaultOutboundAction = $firewall.DefaultOutboundAction
    }
  }
}


function Set-TargetResource
{
  [CmdletBinding()]
  param
  (
    [Parameter(Mandatory = $true)][ValidateSet('Domain','Private','Public')]
    [System.String]
    $Zone,

    [Parameter(Mandatory = $true)][ValidateSet('Present','Absent')]
    [System.String]
    $Ensure,

    [Parameter()][ValidateSet('True','False','NotConfigured')]
    [System.String]$LogBlocked = 'False',

    [Parameter()][ValidateSet('True','False','NotConfigured')]
    [System.String]$LogAllowed = 'False',

    [Parameter()][ValidateSet('True','False','NotConfigured')]
    [System.String]$LogIgnored = 'NotConfigured',

    [Parameter()]
    [System.String]$LogMaxSizeKilobytes = '4096',

    [Parameter()][ValidateSet('Allow','Block','NotConfigured')]
    [System.String]$DefaultInboundAction = 'NotConfigured',

    [Parameter()][ValidateSet('Allow','Block','NotConfigured')]
    [System.String]$DefaultOutboundAction = 'NotConfigured'
  )

  if ($Ensure -eq 'Present')
  {
    $Output = @(
      "Enabled firewall zone $Zone and configured the following
        DefaultInboundAction: $DefaultInboundAction
        DefaultOutboundAction: $DefaultOutboundAction
        LogAllowed: $LogAllowed
        LogBlocked: $LogBlocked
        LogIgnored: $LogIgnored
        LogMaxSizeKilobytes: $LogMaxSizeKilobytes
    ")
    
    Get-NetFirewallProfile $Zone | Set-NetFirewallProfile -Enabled True -LogAllowed $LogAllowed -LogBlocked $LogBlocked -LogIgnored $LogIgnored -LogMaxSizeKilobytes $LogMaxSizeKilobytes -DefaultInboundAction $DefaultInboundAction -DefaultOutboundAction $DefaultOutboundAction
    New-EventLog -LogName 'Microsoft-Windows-DSC/Operational' -Source 'xDSCFirewall' -ErrorAction SilentlyContinue
    Write-EventLog -LogName 'Microsoft-Windows-DSC/Operational' -Source 'xDSCFirewall' -EventId 3001 -EntryType Information -Message ($Output | Out-String)
  }
  elseif ($Ensure -eq 'Absent')
  {
    $Output = @(
      "Disabled firewall zone $Zone and configured the following
        DefaultInboundAction: $DefaultInboundAction
        DefaultOutboundAction: $DefaultOutboundAction
        LogAllowed: $LogAllowed
        LogBlocked: $LogBlocked
        LogIgnored: $LogIgnored
        LogMaxSizeKilobytes: $LogMaxSizeKilobytes
    ")
    
    Get-NetFirewallProfile $Zone | Set-NetFirewallProfile -Enabled False -LogAllowed $LogAllowed -LogBlocked $LogBlocked -LogIgnored $LogIgnored -LogMaxSizeKilobytes $LogMaxSizeKilobytes -DefaultInboundAction $DefaultInboundAction -DefaultOutboundAction $DefaultOutboundAction
    New-EventLog -LogName 'Microsoft-Windows-DSC/Operational' -Source 'xDSCFirewall' -ErrorAction SilentlyContinue
    Write-EventLog -LogName 'Microsoft-Windows-DSC/Operational' -Source 'xDSCFirewall' -EventId 3001 -EntryType Information -Message ($Output | Out-String)
  }
  else
  {
    Write-Verbose -Message 'No match'
  }

  #Include this line if the resource requires a system reboot.
  #$global:DSCMachineStatus = 1
}


function Test-TargetResource
{
  [CmdletBinding()]
  [OutputType([System.Boolean])]
  param
  (
    [Parameter(Mandatory = $true)][ValidateSet('Domain','Private','Public')]
    [System.String]
    $Zone,

    [Parameter(Mandatory = $true)][ValidateSet('Present','Absent')]
    [System.String]
    $Ensure,

    [Parameter()][ValidateSet('True','False','NotConfigured')]
    [System.String]$LogBlocked = 'False',

    [Parameter()][ValidateSet('True','False','NotConfigured')]
    [System.String]$LogAllowed = 'False',

    [Parameter()][ValidateSet('True','False','NotConfigured')]
    [System.String]$LogIgnored = 'NotConfigured',

    [Parameter()]
    [System.String]$LogMaxSizeKilobytes = '4096',

    [Parameter()][ValidateSet('Allow','Block','NotConfigured')]
    [System.String]$DefaultInboundAction = 'NotConfigured',

    [Parameter()][ValidateSet('Allow','Block','NotConfigured')]
    [System.String]$DefaultOutboundAction = 'NotConfigured'
  )

  $firewall = Get-NetFirewallProfile $Zone | Select-Object -Property Enabled, LogAllowed, LogBlocked, LogIgnored, LogMaxSizeKilobytes, DefaultInboundAction, DefaultOutboundAction

  if ($Ensure -eq 'Present')
  {
    if ($firewall.Enabled -eq $true -and $DefaultInboundAction -eq $firewall.DefaultInboundAction -and $DefaultOutboundAction -eq $firewall.DefaultOutboundAction -and $LogAllowed -eq $firewall.LogAllowed -and $LogBlocked -eq $firewall.LogBlocked -and $LogIgnored -eq $firewall.LogIgnored -and $LogMaxSizeKilobytes -eq $firewall.LogMaxSizeKilobytes)
    {
      return $true
    }
    else
    {
      return $false
    }
  }
  elseif ($Ensure -eq 'Absent')
  {
    if ($firewall.Enabled -eq $false -and $DefaultInboundAction -eq $firewall.DefaultInboundAction -and $DefaultOutboundAction -eq $firewall.DefaultOutboundAction -and $LogAllowed -eq $firewall.LogAllowed -and $LogBlocked -eq $firewall.LogBlocked -and $LogIgnored -eq $firewall.LogIgnored -and $LogMaxSizeKilobytes -eq $firewall.LogMaxSizeKilobytes)
    {
      return $true
    }
    else
    {
      return $false
    }
  }
}


Export-ModuleMember -Function *-TargetResource
