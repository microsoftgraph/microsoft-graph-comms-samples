# culture="en-US"
ConvertFrom-StringData -StringData @'
        ErrorWebApplicationTestAutoStartProviderFailure        = Desired AutoStartProvider is not valid due to a conflicting Global Property. Ensure that the serviceAutoStartProvider is a unique key.
        VerboseGetTargetResource                               = Get-TargetResource has been run.
        VerboseSetTargetAbsent                                 = Removing existing Web Application "{0}".
        VerboseSetTargetPresent                                = Creating new Web application "{0}".
        VerboseSetTargetPhysicalPath                           = Updating physical path for Web application "{0}".
        VerboseSetTargetWebAppPool                             = Updating application pool for Web application "{0}".
        VerboseSetTargetSslFlags                               = Updating SslFlags for Web application "{0}".
        VerboseSetTargetAuthenticationInfo                     = Updating AuthenticationInfo for Web application "{0}".
        VerboseSetTargetPreload                                = Updating Preload for Web application "{0}".
        VerboseSetTargetAutostart                              = Updating AutoStart for Web application "{0}".
        VerboseSetTargetIISAutoStartProviders                  = Updating AutoStartProviders for IIS.
        VerboseSetTargetWebApplicationAutoStartProviders       = Updating AutoStartProviders for Web application "{0}".
        VerboseSetTargetEnabledProtocols                       = Updating EnabledProtocols for Web application "{0}".
        VerboseTestTargetFalseAbsent                           = Web application "{0}" is absent and should not absent.
        VerboseTestTargetFalsePresent                          = Web application $Name should be absent and is not absent.
        VerboseTestTargetFalsePhysicalPath                     = Physical path for web application "{0}" does not match desired state.
        VerboseTestTargetFalseWebAppPool                       = Web application pool for web application "{0}" does not match desired state.
        VerboseTestTargetFalseSslFlags                         = SslFlags for web application "{0}" are not in the desired state.
        VerboseTestTargetFalseAuthenticationInfo               = AuthenticationInfo for web application "{0}" is not in the desired state.
        VerboseTestTargetFalsePreload                          = Preload for web application "{0}" is not in the desired state.
        VerboseTestTargetFalseAutostart                        = Autostart for web application "{0}" is not in the desired state.
        VerboseTestTargetFalseIISAutoStartProviders            = AutoStartProviders for IIS are not in the desired state.
        VerboseTestTargetFalseWebApplicationAutoStartProviders = AutoStartProviders for web application "{0}" are not in the desired state.
        VerboseTestTargetFalseEnabledProtocols                 = EnabledProtocols for web application "{0}" are not in the desired state.
'@
