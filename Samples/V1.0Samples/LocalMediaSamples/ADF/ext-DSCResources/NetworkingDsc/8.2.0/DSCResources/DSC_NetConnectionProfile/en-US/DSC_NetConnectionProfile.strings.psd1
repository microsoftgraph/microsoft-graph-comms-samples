# Localized resources for DSC_NetConnectionProfile

ConvertFrom-StringData @'
    GettingNetConnectionProfile = Getting NetConnectionProfile from interface '{0}'.
    TestIPv4Connectivity        = IPv4Connectivity '{0}' does not match set IPv4Connectivity '{1}'
    TestIPv6Connectivity        = IPv6Connectivity '{0}' does not match set IPv6Connectivity '{1}'
    TestNetworkCategory         = NetworkCategory '{0}' does not match set NetworkCategory '{1}'
    SetNetConnectionProfile     = Setting NetConnectionProfile on interface '{0}'
    InterfaceNotAvailableError  = Interface "{0}" is not available. Please select a valid interface and try again.
    ParameterCombinationError   = At least one of the parameters IPv4Connectivity, IPv6Connectivity or NetworkCategory must be set.
'@
