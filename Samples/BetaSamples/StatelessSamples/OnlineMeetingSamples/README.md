# THE SAMPLE PROVIDED IS PURELY FOR DEMONSTRATION PURPOSES ONLY.THIS CODE AND INFORMATION IS PROVIDED "AS IS" 
# WITHOUT WARRANTY OF ANY KIND.

# Introduction

## About
The online meeting stateless sample demonstrates how one can consume Microsoft.Graph.Communications.Client in bot application to
1. Get an online meeting based on on [vtcid](https://docs.microsoft.com/en-us/microsoftteams/cloud-video-interop)).
2. Create a online meeting on behalf a user (delegated auth) in your tenant.

## Getting Started
### Prerequisites
1. [Permissions](https://developer.microsoft.com/en-us/graph/docs/concepts/permissions_reference#online-meetings-permissions) - The following persmissions are needed by the bot application to successfully authenticate against the online meeting service.
   * OnlineMeetings.Read.All OR OnlineMeetings.ReadWrite.All for getting meeting details
   * OnlineMeetings.ReadWrite for creating a meeting.
 
1. Tools.
    * [Visual Studio 2017 or above](https://visualstudio.microsoft.com/downloads/)


## Build and Test

1. Open OnlineMeetingsSample.sln in Visual Studio and update the values of the following in `program.cs`
  * `appId, appSecret` : AppId, Appsecret of your bot application
  * `tenantId` : Tenant against which to fetch/create the online meeting.
  * `vtcid (Only needed for GET)` : The VTC conference id.
  * `userName, password (Only needed for Create)` : Username, Password of the user. 
  * Note - The user should belong to the same tenant as specified by teanantId  

2. Build, Run the application.
 

# References
Please refer following links on various ways to get access tokens. Please use the appropriate mechanism which meets with the requirements of your organization. 
 * https://docs.microsoft.com/en-us/graph/auth-v2-user
 * https://github.com/microsoftgraph/msgraph-sdk-dotnet-auth