# Localized resources for DSC_Route

ConvertFrom-StringData @'
    GettingRouteMessage = Getting '{0}' route on '{1}' destination '{2}' nexthop '{3}'.
    RouteExistsMessage = '{0}' route on '{1}' destination '{2}' nexthop '{3}' exists.
    RouteDoesNotExistMessage = '{0}' route on '{1}' destination '{2}' nexthop '{3}' does not exist.
    SettingRouteMessage = Setting '{0}' route on '{1}' destination '{2}' nexthop '{3}'.
    EnsureRouteExistsMessage = Ensuring '{0}' route on '{1}' destination '{2}' nexthop '{3}' exists.
    EnsureRouteDoesNotExistMessage = Ensuring '{0}' route on '{1}' destination '{2}' nexthop '{3}' does not exist.
    RouteCreatedMessage = '{0}' route on '{1}' destination '{2}' nexthop '{3}' has been created.
    RouteUpdatedMessage = '{0}' route on '{1}' destination '{2}' nexthop '{3}' has been updated.
    RouteRemovedMessage = '{0}' route on '{1}' destination '{2}' nexthop '{3}' has been removed.
    TestingRouteMessage = Testing '{0}' route on '{1}' destination '{2}' nexthop '{3}'.
    RoutePropertyNeedsUpdateMessage = '{4}' property on '{0}' route on '{1}' destination '{2}' nexthop '{3}' is different. Change required.
    RouteDoesNotExistButShouldMessage = '{0}' route on '{1}' destination '{2}' nexthop '{3}' does not exist but should. Change required.
    RouteExistsButShouldNotMessage = '{0}' route on '{1}' destination '{2}' nexthop '{3}' exists but should not. Change required.
    RouteDoesNotExistAndShouldNotMessage = '{0}' route on '{1}' destination '{2}' nexthop '{3}' does not exist and should not. Change not required.
    InterfaceNotAvailableError = Interface '{0}' is not available. Please select a valid interface and try again.
'@
