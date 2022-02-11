# Localized resources for DSC_IPAddress

ConvertFrom-StringData @'
    GettingIPAddressMessage = Getting the IP Address.
    ApplyingIPAddressMessage = Applying the IP Address.
    IPAddressSetStateMessage = IP Interface was set to the desired state.
    CheckingIPAddressMessage = Checking the IP Address.
    IPAddressDoesNotMatchMessage = IP Address does NOT match desired state. Expected {0}, actual {1}.
    IPAddressMatchMessage = IP Address is in desired state.
    IPAddressDoesNotMatchInterfaceAliasMessage = IP Address set on different InterfaceAlias. Expected {0}, actual {1}.
    PrefixLengthDoesNotMatchMessage = Prefix Length does NOT match desired state. Expected {0}, actual {1}.
    PrefixLengthMatchMessage = Prefix Length is in desired state.
    InterfaceNotAvailableError = Interface "{0}" is not available. Please select a valid interface and try again.
    PrefixLengthError = A Prefix Length of {0} is not valid for {1} addresses. Please correct the Prefix Length and try again.
'@
