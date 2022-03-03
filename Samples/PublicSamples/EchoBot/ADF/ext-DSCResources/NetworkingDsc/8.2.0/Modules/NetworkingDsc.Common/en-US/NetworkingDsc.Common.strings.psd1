ConvertFrom-StringData @'
    FindingNetAdapterMessage             = Finding network adapters matching the parameters.
    AllNetAdaptersFoundMessage           = Found all network adapters because no filter parameters provided.
    NetAdapterFoundMessage               = {0} network adapters were found matching the parameters.
    NetAdapterNotFoundError              = A network adapter matching the parameters was not found. Please correct the properties and try again.
    InterfaceAliasNotFoundError          = A network adapter with the alias '{0}' could not be found.
    MultipleMatchingNetAdapterFound      = Please adjust the parameters or specify IgnoreMultipleMatchingAdapters to only use the first and try again.
    InvalidNetAdapterNumberError         = network adapter interface number {0} was specified but only {1} was found. Please correct the interface number and try again.
    GettingDNSServerStaticAddressMessage = Getting staticly assigned DNS server {0} address for interface alias '{1}'.
    GettingWinsServerStaticAddressMessage = Getting staticly assigned WINS server address for interface alias '{0}'.
    SettingWinsServerStaticAddressMessage = Setting staticly assigned WINS server address for interface alias '{0}' to '{1}'.
    DNSServerStaticAddressNotSetMessage  = Statically assigned DNS server {0} address for interface alias '{1}' is not set.
    WinsServerStaticAddressNotSetMessage  = Statically assigned WINS server address for interface alias '{0}' is not set.
    DNSServerStaticAddressFoundMessage   = Statically assigned DNS server {0} address for interface alias '{1}' is '{2}'.
    WinsServerStaticAddressFoundMessage   = Statically assigned WINS server address for interface alias '{0}' is '{1}'.
'@
