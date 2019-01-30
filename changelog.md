# Changelog for Microsoft Graph Communications SDK and Samples

This changelog covers what's changed in Microsoft Graph Communications SDK and its associated samples.

## January 2019

- Updated Media library 1.11.1.86-alpha
- Updated Communications libraries 1.0.0-prerelease.1272

| API                            | Update                                     |
|:-------------------------------|:-------------------------------------------|
| [ICall.PlayPromptAsync](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/calls/Microsoft.Graph.Communications.Calls.ICall.html#Microsoft_Graph_Communications_Calls_ICall_PlayPromptAsync_System_Collections_Generic_IEnumerable_Microsoft_Graph_MediaPrompt__System_Threading_CancellationToken_) and [ICall.RecordAsync](https://microsoftgraph.github.io/microsoft-graph-comms-samples/docs/calls/Microsoft.Graph.Communications.Calls.ICall.html#Microsoft_Graph_Communications_Calls_ICall_RecordAsync_System_Nullable_System_Int32__System_Nullable_System_Int32__System_Nullable_System_Int32__System_Nullable_System_Boolean__System_Nullable_System_Boolean__System_Collections_Generic_IEnumerable_Microsoft_Graph_Prompt__System_Collections_Generic_IEnumerable_System_String__System_Action_Microsoft_Graph_Communications_Calls_RecordOperationResult__System_Threading_CancellationToken_) | Fixed OData deserialization on the SDK side to create the expected type when the graph endpoint returns a base type.  This patch will fix the 2 API calls here until graph metadata is adjusted to return the correct types.

### Stateful SDK

#### Moved resource deletions from the global queue to the resource queue

When the stateful SDK receives a notification it validates it, queues it up for dispatching to events, and returns `202 Accepted` immediately as not to hold up the response. The stateful SDK then has a number of background queues for processing inbound notifications, one global queue and one queue per top level resource (I.E. call).  In the past the global queue handled additions and deletions, but with this release the deletions have been moved to the resource queue.  This change will ensure that all notifications are sequential, and removes potential bottlenecks on the main queue.

## December 2018

- Updated Media library 1.11.1.2-alpha
- Updated Communications libraries 1.0.0-prerelease.881

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

## August 2018

### Core SDK
Removed propreties from some graph owned contracts:
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
- Guest `Identity` is no longer represented using `IdentitySet.User` with `IndentityProvider.None`.
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