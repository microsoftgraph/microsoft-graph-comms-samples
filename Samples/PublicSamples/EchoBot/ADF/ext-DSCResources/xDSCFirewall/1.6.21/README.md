# Builds
|Master   |  Development |
|:------:|:------:|:-------:|:-------:|
[![Build status](https://ci.appveyor.com/api/projects/status/x6a08ruk447c807x/branch/master?svg=true)](https://ci.appveyor.com/project/theonlyway/xdscfirewall/branch/master)|[![Build status](https://ci.appveyor.com/api/projects/status/x6a08ruk447c807x?svg=true)](https://ci.appveyor.com/project/theonlyway/xdscfirewall)|

# xDSCFirewall #
## Overview ##

This custom resource either enables or disables the Public, Private or Domain windows firewall zones

### Parameters ###

**Note:** Currently only supports one zone per config block

**Ensure**

*Note: This is a required parameter*

- Present - Ensures a firewall zone is always enabled
- Absent - Ensures a firewall zone is always disabled

**Zone**

*Note: This is a required parameter*

- Define the zone you want enabled or disabled. Available firewall zones are Public, Private or Domain.

**DefaultInboundAction**

- Allow - Allows all inbound network traffic, whether or not it matches an inbound rule. 
- Block - Blocks inbound network traffic that does not match an inbound rule. 
- NotConfigured - Valid only when configuring a Group Policy Object (GPO). This parameter removes the setting from the GPO, which results in the policy not changing the value on the computer when the policy is applied. 

The default setting when managing a computer is Block. When managing a GPO, the default setting is NotConfigured.


**DefaultOutboundAction**

- Allow - Allows all outbound network traffic, whether or not it matches an outbound rule. 
- Block - Blocks outbound network traffic that does not match an outbound rule. 
- NotConfigured - Valid only when configuring a Group Policy Object (GPO). This parameter removes the setting from the GPO, which results in the policy not changing the value on the computer when the policy is applied. 

The default setting when managing a computer is Allow. When managing a GPO, the default setting is NotConfigured.

**LogAllowed**

- True - Windows writes an entry to the log whenever an incoming or outgoing connection is prevented by the policy. 
- False - No logging for dropped connections. This parameter removes the setting from the GPO, which results in the policy not changing the value on the computer when the policy is applied. 
- NotConfigured - Valid only when configuring a Group Policy Object (GPO). This parameter removes the setting from the GPO, which results in the policy not changing the value on the computer when the policy is applied. 

The default setting when managing a computer is False. When managing a GPO, the default setting is NotConfigured.

**LogIgnored**

- True - Windows writes an entry to the log whenever an incoming or outgoing connection is prevented by policy. 
- False - No logging for dropped connections. 
- NotConfigured - Valid only when configuring a Group Policy Object (GPO). This parameter removes the setting from the GPO, which results in the policy not changing the value on the computer when the policy is applied. 

The default setting when managing a computer is False. When managing a GPO, the default setting is NotConfigured.

**LogBlocked**

- True - Windows writes an entry to the log whenever an incoming or outgoing connection is prevented by policy. 
- False - No logging for dropped connections. 
- NotConfigured - Valid only when configuring a Group Policy Object (GPO). This parameter removes the setting from the GPO, which results in the policy not changing the value on the computer when the policy is applied. 

The default setting when managing a computer is False. When managing a GPO, the default setting is NotConfigured.

**LogMaxSizeKilobytes**

- 4096 - The default setting when managing a computer is 4096. Specifies the maximum file size of the log, in kilobytes. The acceptable values for this parameter are: 1 through 32767 


### Example ###

    Service WindowsFirewall
    {
    Name = "MPSSvc"
    StartupType = "Automatic"
    State = "Running"
    }
    xDSCFirewall DisablePublic
    {
      Ensure = "Absent"
      Zone = "Public"
      Dependson = "[Service]WindowsFirewall"
    }
    xDSCFirewall EnabledDomain
    {
      Ensure = "Present"
      Zone = "Domain"
      LogAllowed = "False"
      LogIgnored = "False"
      LogBlocked = "False"
      LogMaxSizeKilobytes = "4096"
      DefaultInboundAction = "Block"
      DefaultOutboundAction = "Allowed"
      Dependson = "[Service]WindowsFirewall"
    }
    xDSCFirewall EnabledPrivate
    {
      Ensure = "Present"
      Zone = "Private"
      Dependson = "[Service]WindowsFirewall"
    }

# Versions

## 1.6
Merged in requested changes from heoelri adding the following non-mandatory parameters

- LogAllowed
- LogIgnored
- LogBlocked
- LogMaxSizeKilobytes
- DefaultInboundAction
- DefaultOutboundAction

## 1.0
Basic DSC module to enable Firewall profiles
