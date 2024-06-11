# Changelog for Microsoft Graph Communications SDK and Samples

This changelog covers what's changed in Microsoft Graph Communications SDK and its associated samples.
## Oct 2020
- Updated all samples to latest media binaries: Microsoft.Skype.Bots.Media 1.19.0.25-alpha
  - Fixed a known memory leak bug in Media library.
- Add AKS Sample: teams-recording-bot.

## Mar 2020

- Updated to latest /beta and /v1 contracts
  - For a full list of changes refer to the [graph blog](https://developer.microsoft.com/en-us/graph/blogs/microsoft-graph-net-sdk-updates/).
- Changed name of Compliance Recording to Policy Recording
- Migrated the Policy Recording sample to the https://graph.microsoft.com/v1.0 endpoint.
- Updated Communications libraries in v1 bots to use the 1.2.0.850 SDK.
  - Exposed the Policy Recording API's in v1.
- Updated Media library in v1 bots to 1.17.0.39-alpha

## Feb 2020

- Updated Compliance Recording sample docs.

## Jan 2020

- Promoted IncidentBot to v1.0 samples.
- Migrated v1.0 samples to use the 1.2.0.1 SDK and the https://graph.microsoft.com/v1.0 endpoint.
- Migrated beta samples to use the 1.2.0-beta.1 SDK.
- Added `POST ~/call/{id}/keepAlive` API to v1.0 samples.
  - Required to persist the call.
- Migrated samples from the `~/app/` endpoint to the `~/communications/` endpoint.

## Dec 2019

- Updated Documentation to be based on the 1.2.0.1 SDK.
- Updated Communications libraries to 1.2.0-beta.1

## Nov 2019

- Updated Communications libraries to 1.1.0-prerelease.2237
  - Exposed delete participant API

- Updated Communications libraries to 1.1.0-prerelease.2027
  - Exposed recording APIs

| **Change type** | **Version**   | **Description**                          |
| :-------------- | :------------ | :--------------------------------------- |
|Addition|beta|Added new action `updateRecordingStatus` to `call` entity.
|Addition|beta|Added new complex type `incomingContext`.
|Addition|beta|Added new property `incomingContext` to `call` entity.
|Addition|beta|Added new property `endpointType` to `participantInfo` complex type.
|Addition|beta|Added new property `endpointType` to `invitationParticipantInfo` complex type.
|Addition|beta|Added new property `recordingStatus` to `recordingInfo` complex type.
|Deletion|beta|Removed property `status` from `recordingInfo` complex type.
|Deletion|beta|Removed inheritance of `participantInfo` from `invitationParticipantInfo` complex type.

## Oct 2019

- Updated Communications libraries to 1.1.0-prerelease.1855
- Updated to latest /beta contacts.
  - For a full list of changes refer to the [graph blog](https://developer.microsoft.com/en-us/graph/blogs/breaking-changes-calls-and-online-meetings-api-updates-in-microsoft-graph-beta-2/).

## Sept 2019

- Updated Communications libraries to 1.1.0-prerelease.1511
- Updated Media library 1.13.1.324-alpha

### Communications 1.1.0-prerelease.1511 Changes

- Updated to latest /beta contacts.
  - For a full list of changes refer to the [graph blog](https://developer.microsoft.com/en-us/graph/blogs/breaking-changes-calls-and-online-meetings-api-updates-in-microsoft-graph-beta/).
- Stateful client now supports HA/DR.
  - Further detail to come in sample and SDK documentation.
- Added more verbose code comments.
- Removed redundant objects.
- Removed obsolete objects.
- Misc bug fixes and improvements.

## June 2019

- Migrated AuthenticationProvider from ADAL to MSAL AAD libraries.
- Removed OnlineMeeting sample IRequestAuthenticationProvider and leveraged the one defined in Sample.Common.
- Updated all samples to latest media binaries: Microsoft.Skype.Bots.Media 1.13.1.98-alpha
- Changed Samples.Common so that it supports dual targets (net461 and netstandard2.0)

## May 2019

- Updated Media library 1.12.1.6-alpha
- Updated Communications libraries to 1.1.0-prerelease.581

### Communications 1.1.0-prerelease.581 Changes

The Communications SDKs are now decoupled from the `Microsoft.Graph` SDK.  New nugets have been released to as version `1.1.0-prerelease.*` to signal breaking changes due to objects being moved to Microsoft.Graph. 

#### Microsoft.Graph.Communications.Core:

This library no longer contains any Calls contracts... it now references `Microsoft.Graph` 1.14.0 and `Microsoft.Graph.Core` 1.15.0-preview.2 and only contains shared contracts not present in Microsoft.Graph.  It also contains serialization/deserialization helpers and extensions methods to help with the Calling APIs (`IdentitySet.GetGuest`/`IdentitySet.SetGuest`/etc...).  The frameworks have been bumped up to `net461` and `netstandard2.0` so the core SDK can leverage `Microsoft.Graph.Communications.Common`.  Customers using this SDK now have to move to the one containing their specific contracts (below).

#### Microsoft.Graph.Communications.Core.Calls:

Calls wire SDK (contains all the calls and online meetings contracts).  Note that some object names have changed as not to conflict with Microsoft.Graph names.
```
DefaultContainerClient => CallsGraphServiceClient
Notification => CommsNotification
Notifications => CommsNotifications
```

#### Microsoft.Graph.Communications.Calls:

Now references `Microsoft.Graph.Communications.Core.Calls` and `Microsoft.Graph.Communications.Client`.  

Naming conventions of these SDKs were changed as they are namespaced and do not need the Call prefix:
```
ICallParticipantCollection => IParticipantCollection
ICallParticipant => IParticipant
```

### Misc changes
- Contract sync with latest Beta Calling contracts.
- Updated to `Microsoft.Graph.Core` 1.15.0-preview.2 SDK to resolve inconsistencies between `ServiceException` types.
- Updated to `Microsoft.Graph` 1.14.0 SDK.
- Updated to `Microsoft.Skype.Bots.Media` 1.12.1.6-alpha SDK.
- Added `promptsQueued` callback to be notified when a prompt has been queued, and the next one can be added.  This is only valid for scenarios where bot developers queue a single prompt at a time.  If 1P developers pass in multiple prompts, order is guaranteed.
- Added proper cleanup of resources when Stateful SDK resources get garbage collected.  This fixes a memory leak where internal notification queues were not getting removed when resources were GCd without `Dispose()` being called.
- First stages of HA/DR support.  SDK supports passing in an `ICache` interface that notifies the bot developer whenever internal state has changed.  It is also used to recover state when calling `ICommunicationsClient.RehydrateAsync`.  An implementation of re-hydration from PMA is built in by default, but it does not support AudioRoutingGroup entities.
- Deprecated support for Chain-Id/Correlation-Id in Stateful SDK.  It is replaced with Scenario-Id, which can be set by the client as a kind of "telemetry identifier" to correlate any calls together.

## January 2019

- Updated Media library 1.11.1.86-alpha
- Updated Communications libraries to 1.0.0-prerelease.1272

| API                            | Update                                     |
|:-------------------------------|:-------------------------------------------|
| [ICall.PlayPromptAsync](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/calls/Microsoft.Graph.Communications.Calls.ICall.html#Microsoft_Graph_Communications_Calls_ICall_PlayPromptAsync_System_Collections_Generic_IEnumerable_Microsoft_Graph_MediaPrompt__System_Threading_CancellationToken_) and [ICall.RecordAsync](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/calls/Microsoft.Graph.Communications.Calls.ICall.html#Microsoft_Graph_Communications_Calls_ICall_RecordAsync_System_Nullable_System_Int32__System_Nullable_System_Int32__System_Nullable_System_Int32__System_Nullable_System_Boolean__System_Nullable_System_Boolean__System_Collections_Generic_IEnumerable_Microsoft_Graph_Prompt__System_Collections_Generic_IEnumerable_System_String__System_Action_Microsoft_Graph_Communications_Calls_RecordOperationResult__System_Threading_CancellationToken_) | Fixed OData deserialization on the SDK side to create the expected type when the graph endpoint returns a base type.  This patch will fix the 2 API calls here until graph metadata is adjusted to return the correct types.

### Stateful SDK

#### Moved resource deletions from the global queue to the resource queue

When the stateful SDK receives a notification it validates it, queues it up for dispatching to events, and returns `202 Accepted` immediately as not to hold up the response. The stateful SDK then has a number of background queues for processing inbound notifications, one global queue and one queue per top level resource (I.E. call).  In the past the global queue handled additions and deletions, but with this release the deletions have been moved to the resource queue.  This change will ensure that all notifications are sequential, and removes potential bottlenecks on the main queue.

## December 2018

- Updated Media library 1.11.1.2-alpha
- Updated Communications libraries to 1.0.0-prerelease.881

| API                            | Update                                     |
|:-------------------------------|:-------------------------------------------|
| [ICall.RecordAsync](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/calls/Microsoft.Graph.Communications.Calls.ICall.html#Microsoft_Graph_Communications_Calls_ICall_RecordAsync_System_Nullable_System_Int32__System_Nullable_System_Int32__System_Nullable_System_Int32__System_Nullable_System_Boolean__System_Nullable_System_Boolean__System_Collections_Generic_IEnumerable_Microsoft_Graph_Prompt__System_Collections_Generic_IEnumerable_System_String__System_Action_Microsoft_Graph_Communications_Calls_RecordOperationResult__System_Threading_CancellationToken_) | Updated to return recordResourceLocation and recordResourceAccessToken. The access token is required to be sent as a bearer token to download the recording. |
| [VideoSocket.SetSendBandwidthLimit](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/bot_media/Microsoft.Skype.Bots.Media.VideoSocket.html#Microsoft_Skype_Bots_Media_VideoSocket_SetSendBandwidthLimit_System_UInt32_) | Sets the bandwidth limit on the send stream of the VideoSocket. |
| [VideoSocket.SetReceiveBandwidthLimit](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/bot_media/Microsoft.Skype.Bots.Media.VideoSocket.html#Microsoft_Skype_Bots_Media_VideoSocket_SetReceiveBandwidthLimit_System_UInt32_) | Sets the bandwidth limit on the receive stream of the VideoSocket.

## November 2018

SDK package names have been updated to avoid confusion with Microsoft Graph SDK. When upgrading to the latest version (1.0.0-prerelease.494) in the _original_ package names, you will encounter build warning [CS0618](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0618). The warning message shows the actions you need to take to upgrade to the new packages.

| Original nuget package         | New nuget package                          |
|:-------------------------------|:-------------------------------------------|
| Microsoft.Graph.Calls          | Microsoft.Graph.Communications.Calls       |
| Microsoft.Graph.Calls.Media    | Microsoft.Graph.Communications.Calls.Media |
| Microsoft.Graph.Core.Stateful  | Microsoft.Graph.Communications.Common      |
| Microsoft.Graph.CoreSDK        | Microsoft.Graph.Communications.Core        |
| Microsoft.Graph.StatefulClient | Microsoft.Graph.Communications.Client      |

Namespaces have been updated to match the assembly and package names. In addition, the top level interfaces have been renamed to match the new naming scheme.

| Original namespace                     | New namespace                                     |
|:---------------------------------------|:--------------------------------------------------|
| Microsoft.Graph.Calls                  | Microsoft.Graph.Communications.Calls              |
| Microsoft.Graph.Calls.Media            | Microsoft.Graph.Communications.Calls.Media        |
| Microsoft.Graph.Core                   | Microsoft.Graph.Communications.Common             |
| Microsoft.Graph.CoreSDK                | Microsoft.Graph.Communications.Core               |
| Microsoft.Graph.StatefulClient         | Microsoft.Graph.Communications.Client             |
| IStatefulClient                        | ICommunicationsClient                             |
| StatefulClientBuilder                  | CommunicationsClientBuilder                       |

This release cleans up interfaces where some members have been renamed or removed. Older names are retained for backward compatibility, it is expected to be removed over the next releases.

| Deprecated items                       | Replacement                                       |
|:---------------------------------------|:--------------------------------------------------|
| AudioRoutingGroup.Owner                | _no longer used_                                  |
| Call.Error                             | Call.ResultInfo                                   |
| Call.Transfer(TransferTarget,...)      | Call.Transfer(TransferTarget)                     |
| CommsOperation.ErrorInfo               | CommsOperation.ResultInfo                         |
| MeetingParticipantInfo.SipProxyAddress | _no longer used_                                  |
| OnlineMeeting.CanceledTime             | OnlineMeeting.CanceledDateTime                    |
| OnlineMeeting.CreationTime             | OnlineMeeting.CreationDateTime                    |
| OnlineMeeting.EndTime                  | OnlineMeeting.EndDateTime                         |
| OnlineMeeting.ExpirationTime           | OnlineMeeting.ExpirationDateTime                  |
| OnlineMeeting.MeetingInfo              | OnlineMeeting.Participants.Organizer              |
| OnlineMeeting.StartTime                | OnlineMeeting.StartDateTime                       |
| Participant.SubscribeVideoAsync        | Call.GetLocalMediaSession().VideoSocket.Subscribe |

## September 2018

1. Updated to https://graph.microsoft.com/beta endpoint for Microsoft Ignite 2018 release.
2. Updated to public nuget version of Graph Comms SDK. Note new version scheme: 1.0.0-prerelease.48

## August 2018

### Core SDK
Removed properties from some graph owned contracts:
- Removed `Identity.TenantId` property.  This still flows through on the wire, but needs to be fetched from `Identity.AdditionalData`.
- Removed `Identity.IdentityProvider` as it was not required.  `IdentityProvider` is now expected to be inferred using `tenantId`
``` json
{
  "@odata.type": "#microsoft.graph.identitySet",
  "user": {
    "@odata.type": "#microsoft.graph.identity",
    "id": "<guid>",
    "displayName": "User Name",
    "tenantId": "<guid>",
  }
}
```
- Guest `Identity` is no longer represented using `IdentitySet.User` with `IdentityProvider.None`.
``` json
{
  "@odata.type": "#microsoft.graph.identitySet",
  "guest": {
    "@odata.type": "#microsoft.graph.identity",
    "id": "<guid>",
    "displayName": "Guest Name",
    "tenantId": "<guid>",
  }
}
```
- Added `CommsOperation`, which inherits from `Operation`.  In the future most calling APIs will return `CommsOperation`.
- Added `RecordingInfo` contracts, which provide recording information on a given call participant.

### Stateful SDK
- Added extension methods for commonly used additionalData keys supported by the stateful APIs:
  - `Identity.GetTenantId()` and `Identity.SetTenantId(string)`
  - `IdentitySet.GetApplicationInstance()` and `IdentitySet.SetApplicationInstance(Identity)`
  - `IdentitySet.GetGuest()` and `IdentitySet.SetGuest(Identity)`
  - `IdentitySet.GetEncrypted()` and `IdentitySet.SetEncrypted(Identity)`
- Added parsing of incoming contracts to remove deprecated objects and properties.
- General Cleanup:
  - Added a common interface base `IResourceBase` used by both `IResource` and `IResourceCollection`.
  - Removed some methods from `IResource` interface which were meant to be internal.
  - Moved some methods definitions from base interfaces to concrete interfaces.

## July 2018

### Core SDK
No changes

### Stateful SDK
Added proper handling of the operation response.
- If operation is `null`, `Idle` or `Running` the action is treated as asynchronous and SDK will wait for subsequent `Running` `Failed` or `Completed` operation notifications.
- If operation is `Failed` SDK will throw a `ServiceException` with the error details.
- If operation is `Completed` the action is treated as synchronous and SDK will return from the calling method.
- Updated to latest version of SDK, which has a couple fixes in regards to how operations are processed.  This removes the `400 Bad Request` after some operations, and fixes issues with Mute/Unmute.
- Switched to the `Subscribe` and `Unsubscribe` on the video sockets, instead of using the `IParticipant.SubscribeAsync`.

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

# Release dates and notes:

## May 2018

### Core SDK
Updated to latest SDK version.  This includes minor bug fixes and contract changes.
- The `OrganizerMeetingInfo.OrganizerId` and `OrganizerMeetingInfo.TenantId` parameters have been replaced with `IdentitySet Organizer`.
- The transfer `IdenitySet Target` and `string ReplacesCallId` parameters have been replaced with `InvitationParticipantInfo TransferTarget`.

### Stateful SDK
Added logic to handle failed call deletion, or any time a stale call needs to be removed from SDK memory.

``` csharp
// Manually remove the call from SDK state.
// This will trigger the ICallCollection.OnUpdated event with the removed resource.
this.Client.Calls().TryForceRemove(callLegId, out ICall call);
```

## March 2018

### Core SDK
No changes

### Stateful SDK
- CorrelationId is now a Guid.
- Added auto expiry of certificates in authentication provider.
- Added support for `IGraphLogger` as part of `ICommunicationsClient`.
- Set `AllowConversationWithoutHost = true;` for joined meetings.  This will ensure that any participants joining the meeting after the bot will not get kicked out of the meeting once bot leaves.
- Added better tracking of calls by setting the `correlationId` for new calls and media sessions.
- Added `ICommunicationsClient.TerminateAsync(bool onlyMedia, TimeSpan timeout);`  SDK supports couple flavors of cleanup:
  - **Recommended:** `TerminateAsync(false, timeout)` will terminate all existing calls, terminate the media platform, shut down background threads, and dispose internal objects.  Setting `timeout` will still terminate the media platform, shut down background threads, and dispose internal objects, but it will limit the time spent waiting for calls to be terminated.
 - `ICommunicationsClient.TermianteAsync(true, timeout)` will only terminate the media platform.  In this instance the `timeout` parameter is ignored.
 - `ICommunicationsClient.TerminateAsync(timeout)` is used for media hosted on MSFT cloud, and not relevant in this drop.
- The termination state now also bubbles up in the call.OnUpdated notification.

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
