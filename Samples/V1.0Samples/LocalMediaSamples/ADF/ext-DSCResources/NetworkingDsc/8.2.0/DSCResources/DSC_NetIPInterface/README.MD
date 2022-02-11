# Description

This resource is used to configure the IP interface settings for a network interface.

## Known Issues

- If you define a value for `InterfaceMetric`, the `AutomaticMetric`
setting is ignored. PowerShell ignores `AutomaticMetric` when you
use both arguments with the `Set-NetIPInterface` cmdlet.
