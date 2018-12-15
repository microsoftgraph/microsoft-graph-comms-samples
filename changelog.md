# Changelog for Microsoft Graph Communications SDK and Samples

This changelog covers what's changed in Microsoft Graph Communications SDK and its associated samples.

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
