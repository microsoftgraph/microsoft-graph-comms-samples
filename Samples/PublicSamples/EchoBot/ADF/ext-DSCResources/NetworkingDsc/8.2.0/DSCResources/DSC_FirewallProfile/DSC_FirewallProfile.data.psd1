@{
    ParameterList = @(
        @{
            Name = 'AllowInboundRules'
            Type = 'String'
        },
        @{
            Name = 'AllowLocalFirewallRules'
            Type = 'String'
        },
        @{
            Name = 'AllowLocalIPsecRules'
            Type = 'String'
        },
        @{
            Name = 'AllowUnicastResponseToMulticast'
            Type = 'String'
        },
        @{
            Name = 'AllowUserApps'
            Type = 'String'
        },
        @{
            Name = 'AllowUserPorts'
            Type = 'String'
        },
        @{
            Name = 'DefaultInboundAction'
            Type = 'String'
        },
        @{
            Name = 'DefaultOutboundAction'
            Type = 'String'
        },
        @{
            Name = 'DisabledInterfaceAliases'
            Type = 'Array'
        },
        @{
            Name = 'Enabled'
            Type = 'String'
        },
        @{
            Name = 'EnableStealthModeForIPsec'
            Type = 'String'
        },
        @{
            Name = 'LogAllowed'
            Type = 'String'
        },
        @{
            Name = 'LogBlocked'
            Type = 'String'
        },
        @{
            Name = 'LogFileName'
            Type = 'String'
        },
        @{
            Name = 'LogIgnored'
            Type = 'String'
        },
        @{
            Name = 'LogMaxSizeKilobytes'
            Type = 'Uint64'
        }
        @{
            Name = 'NotifyOnListen'
            Type = 'String'
        }
    )
}
