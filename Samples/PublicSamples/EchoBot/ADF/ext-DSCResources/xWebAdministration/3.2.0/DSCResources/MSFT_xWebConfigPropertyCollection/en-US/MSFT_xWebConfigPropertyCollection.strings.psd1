# culture="en-US"
ConvertFrom-StringData -StringData @'
    VerboseTargetCheckingTarget            = Checking for the existence of property "{0}" in collection item "{1}/{2}" with key "{3}={4}" using filter "{5}" located at "{6}".
    VerboseTargetItemNotFound              = Collection item "{0}/{1}" with key "{2}={3}" has not been found.
    VerboseTargetPropertyNotFound          = Property "{0}" has not been found.
    VerboseTargetPropertyFound             = Property "{0}" has been found.
    VerboseSetTargetAddItem                = Collection item "{0}/{1}" with key "{2}={3}" does not exist, adding with property "{4}".
    VerboseSetTargetEditItem               = Collection item "{0}/{1}" with key "{2}={3}" exists, editing property "{4}".
    VerboseSetTargetRemoveItem             = Property "{0}" exists, removing property.
    VerboseTestTargetPropertyValueNotFound = Property "{0}" has not been found with expected value.
'@
