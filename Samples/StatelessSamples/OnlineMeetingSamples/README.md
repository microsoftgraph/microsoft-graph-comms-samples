# Introduction

## About
The online meeting stateless sample demonstrates how one can consume Microsoft.Skype.Graph.CoreSDK in thier bot application to
1. Get an online meeting based on meetingid (current support is only for [vtcid](https://docs.microsoft.com/en-us/microsoftteams/cloud-video-interop)).
1. Create a adhoc online meeting on behalf of an organizer in your tenant.

## Getting Started
### Prerequisites
1. [Permissions](https://developer.microsoft.com/en-us/graph/docs/concepts/permissions_reference#online-meetings-permissions) - The following persmissions are needed by the bot application to successfully authenticate against the online meeting service.
   * OnlineMeetings.Read.All OR OnlineMeetings.ReadWrite.All for getting meeting details
   * OnlineMeetings.ReadWrite.All for creating a meeting.
 
1. Tools.
    * [Visual Studio 2017](https://visualstudio.microsoft.com/downloads/)


## Build and Test

1. Open OnlineMeetingsSample.sln in Visual Studio 2017 and update the values of the following in `program.cs`
  * `appId, appSecret` : AppId, Appsecret of your bot application
  * `tenantId` : Tenant against which to fetch/create the online meeting.
  * `meetingId (Only needed for GET)` : The VTC conference id.
  * `organizerId (Only needed for Create)` : oid of the user on behalf of whom the adhoc meeting is to be created. 
  * Note - The organizerId should belong to the same tenant as specified by teanantid

2. Build, Run the application.
 