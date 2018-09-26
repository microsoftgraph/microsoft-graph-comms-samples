# Version 0.8
- Updated to latest version of SDK, which brings in latest protocol changes.
- Updated the approach to join a meeting.
- Provide a way to query on going calls.
- Provide a way to terminate the on going calls.
- Provide a way to call/raise alert to application. It's for first party app only now.

# Version 0.7
- Updated to latest version of SDK, which brings in latest protocol changes.

# Version 0.6
- Updated to latest version of SDK, which has a couple fixes in regards to how operations are processed.  This removes the `400 Bad Request` after some operations, and fixes issues with Mute/Unmute.

# Version 0.5
- Fixed the incoming call issue in PlatformCallController.

# Version 0.4
- Removed ICall.AppContext dependency which will be deprecated.
- Added error handling for input argument check

# Version 0.3
- Updated the responders' meeting call status in incident data.
- Terminated the notification calls after the users joined the incident meeting.

# Version 0.2
- Updated the audio prompts and prompt logic to support audio prompts for notification calls.
- Added the DTMF decoding logic to do different actions based on responder's inputs.
- Added the logic to add responders into the scheduled meeting.
- Updated the base url from CallControlBaseUrl to BotBaseUrl in appsettings.json for both IncomingRequest ans audio prompt files.
- Fix the multiple web instance routing issue, by updating the CallAffinityHelper.cs to CallAffinityMiddleware.cs.
- Fix the incoming reqeust auth issue by removing the auth module.
- Removed the DisplayName in the incident/raise post body. Bot will join meeting with Bot name in AAD always.

# Initial Version
- First version to enable out-going call to responders and enable bot to join scheduled meeting