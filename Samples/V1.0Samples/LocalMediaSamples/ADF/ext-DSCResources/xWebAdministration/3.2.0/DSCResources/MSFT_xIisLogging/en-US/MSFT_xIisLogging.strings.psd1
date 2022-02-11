ConvertFrom-StringData @'
    VerboseGetTargetResult                     = Get-TargetResource has been run.
    VerboseSetTargetUpdateLogPath              = LogPath is not in the desired state and will be updated.
    VerboseSetTargetUpdateLogFlags             = LogFlags do not match and will be updated.
    VerboseSetTargetUpdateLogPeriod            = LogPeriod is not in the desired state and will be updated.
    VerboseSetTargetUpdateLogTruncateSize      = TruncateSize is not in the desired state and will be updated.
    VerboseSetTargetUpdateLoglocalTimeRollover = LoglocalTimeRollover is not in the desired state and will be updated.
    VerboseSetTargetUpdateLogFormat            = LogFormat is not in the desired state and will be updated
    VerboseSetTargetUpdateLogTargetW3C         = LogTargetW3C is not in the desired state and will be updated
    VerboseSetTargetUpdateLogCustomFields      = LogCustomFields is not in the desired state and will be updated.
    VerboseTestTargetUpdateLogCustomFields     = LogCustomFields is not in the desired state and will be updated.
    VerboseTestTargetFalseLogPath              = LogPath does match desired state.
    VerboseTestTargetFalseLogFlags             = LogFlags does not match desired state.
    VerboseTestTargetFalseLogPeriod            = LogPeriod does not match desired state.
    VerboseTestTargetFalseLogTruncateSize      = LogTruncateSize does not match desired state.
    VerboseTestTargetFalseLoglocalTimeRollover = LoglocalTimeRollover does not match desired state.
    VerboseTestTargetFalseLogFormat            = LogFormat does not match desired state.
    VerboseTestTargetFalseLogTargetW3C         = LogTargetW3C does not match desired state.
    WarningLogPeriod                           = LogTruncateSize has is an input as will overwrite this desired state.
    WarningIncorrectLogFormat                  = LogFormat is not W3C, as a result LogFlags will not be used.
'@
