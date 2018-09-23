# Release dates and notes:

09/23/2018

1. Updated to https://graph.microsoft.com/beta endpoint for Microsoft Ignite 2018 release.
2. Updated to public nuget version of Graph Comms SDK. Note new version scheme: 1.0.0-prerelease.48

08/21/2018

1. Updated to /TeamsBeta for prod supported endpoint from Graph.
2. Updated to latest version of SDK, which brings in latest protocol changes.
3. Added `MeetingId` parameter for meeting join.  When `MeetingId` is specified, the bot will get the meeting coordinates prior to joining.
4. Added support for joining as anonymous when specifying the `DisplayName` parameter.

07/26/2018

1. Updated to latest version of SDK, which has a couple fixes in regards to how operations are processed.  This removes the `400 Bad Request` after some operations, and fixes issues with Mute/Unmute.
2. Switched to the `Subscribe` and `Unsubscribe` on the video sockets, instead of using the `IParticipant.SubscribeAsync`.

``` csharp
/// <summary>
/// Interface to a VideoSocket.
/// </summary>
public interface IVideoSocket : IDisposable
 
/// <summary>
/// Video Subscription API for the conference scenario, once the MediaReceiveStatus is raised with active status,
/// it is possible to call this api to subscribe to a specific video using the media source id.
/// </summary>
/// <param name="preferredVideoResolution">The requested video resolution,
/// The received video buffers should have the requested resolution if the bandwidth constraints and sender capabilities are satisfied</param>
void Subscribe(VideoResolution preferredVideoResolution, uint MediaSourceId);
 
/// <summary>
/// Subscribe API for the 1:1 case. No need to specify the media source id
/// </summary>
void Subscribe(VideoResolution preferredVideoResolution);
 
void Unsubscribe();
```

Note: The VideoSocket.Subscribe method will throw an InvalidOperationException if it is called too early, before the media session is established/active. The bot can monitor the (also new) VideoSocket.VideoReceiveStatusChanged event to see when the VideoSocket reports MediaReceiveStatus.Active, to know when it can start making video subscription requests.

05/15/2018

1. Updated to latest SDK version.  This includes minor bug fixes and contract changes.
   1. The `OrganizerMeetingInfo.OrganizerId` and `OrganizerMeetingInfo.TenantId` parameters have been replaced with `IdentitySet Organizer`.
   2. The transfer `IdenitySet Target` and `string ReplacesCallId` parameters have been replaced with `InvitationParticipantInfo TransferTarget`.
2. Added logic to handle failed call deletion, or any time a stale call needs to be removed from SDK memory.
``` csharp
// Manually remove the call from SDK state.
// This will trigger the ICallCollection.OnUpdated event with the removed resource.
this.Client.Calls().TryForceRemove(callLegId, out ICall call);
```

05/10/2018

1. Updated AuthenticationProvider to support multiple issuers for inbound request.
2. Consolidated Vbss functionality with BotMediaStream.
3. Changed place call endpoint to talk to graph endpoint and auth token resource to graph

05/03/2018

1. Synced with latest signed binaries.
2. Updated media library
3. Added vbss controller for the bot to switch between vbss sharer and viewer. When bot is the sharer, it streams H264 video file through the vbss socket.

03/26/2018

Synced with latest signed binaries.

03/22/2018

1. CorrelationId is now a Guid.
2. Added auto expiry of certificates in authentication provider.
3. Added support for `IGraphLogger` as part of `IStatefulClient`.

03/10/2018

1. Set `AllowConversationWithoutHost = true;` for joined meetings.  This will ensure that any participants joining the meeting after the bot will not get kicked out of the meeting once bot leaves.
2. Added better tracking of calls by setting the `correlationId` for new calls and media sessions.

03/07/2018

New SDK drop.
1. Added `IStatefulClient.TerminateAsync(bool onlyMedia, TimeSpan timeout);`  SDK supports couple flavors of cleanup:
   1. **Recommended:** `TerminateAsync(false, timeout)` will terminate all existing calls, terminate the media platform, shut down background threads, and dispose internal objects.  Setting `timeout` will still terminate the media platform, shut down background threads, and dispose internal objects, but it will limit the time spent waiting for calls to be terminated.
   2. `IStatefulClient.TermianteAsync(true, timeout)` will only terminate the media platform.  In this instance the `timeout` parameter is ignored.
   3. `IStatefulClient.TerminateAsync(timeout)` is used for media hosted on MSFT cloud, and not relevant in this drop.
2. The termination state now also bubbles up in the call.OnUpdated notification.

If bots wish to shut down cleanly, we recommend the following:
``` csharp
try
{
  // Terminate all existing calls and wait for confirmation.
  // Terminate media platform, terminate background threads, dispose objects.
  await this.Client
    .TerminateAsync(false, new TimeSpan(hours: 0, minutes: 1, seconds: 0))
    .ConfigureAwait(false);
}
catch (Exception)
{
  // Terminate with error.
}

// Perform graceful termination logic.
```

02/26/2018

1. Added setting of tenant in `JoinMeetingParameters`.  The tenant is forwared to the `IRequestAuthenticationProvider` so that tenantized tokens can be created.
2. Fixed extraction and validation of tenant for incoming requests.

02/16/2018

1. Updated to latest version of calling SDK.
   1. Added support for joining the meeting multiple times.
   2. Specifying the `User` property in the `call.Source` `IdentitySet` when making an outbound call will allow impersonation of a guest user.
2. Added IRequestAuthenticationProvider to create outbound tokens and validate incoming requests.

02/07/2018

1. Added support for reading incoming video streams from multiple video sources.  An LRU cache keeps track of the dominant speaker msi to socket id mapping, and with every roster update it chooses the appropriate socket from the cache.
2. Added support for reading incoming VBSS (screen sharing) if available.

01/10/2018

1. Enabled StyleCop Analyzer on Sample code. Fixed build warnings generated by build pipeline

01/09/2018

1. Updated sample to use the latest media nuget and Graph.Calls nuget packages

2. Fixed out-of-sync issue when playing back audio video files

3. Fixed the video startup glitch

4. More comments of using the media player were added
 
19/12/2017

1. Initial release of the audio video player bot sample


