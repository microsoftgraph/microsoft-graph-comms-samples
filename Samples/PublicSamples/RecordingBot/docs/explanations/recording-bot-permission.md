# Bot Service - Microsoft Entra Id and Micosoft Graph API Calling Permission

An Azure bot service can be created with an existing app registration in the Microsoft Entra Id or
can create a new app registration in the Microsoft Entra Id on creation. The app registration can
be a single tenant app registration or a multi tenant app registration, also see
["Tenancy in Microsoft Entra ID" documentaion](https://learn.microsoft.com/entra/identity-platform/single-and-multi-tenant-apps). Either way it is required that the Microsoft Entra
 Id app registration and the bot service are linked.

Also the app registration must have some application permissions exposed by the Microsoft Graph API
that allows the recording application to join calls and access the media streams. The following
Microsoft Graph API application permissions are relevant for the recording bot:

|  permission  |  description  |
| ------------ | ------------- |
| [Calls.Initiate.All](https://learn.microsoft.com/graph/permissions-reference#callsinitiateall) | Allows the app to place outbound calls to a single user and transfer calls to users in your organization's directory, without a signed-in user. |
| [Calls.InitiateGroupCall.All](https://learn.microsoft.com/graph/permissions-reference#callsinitiategroupcallall) | Allows the app to place outbound calls to multiple users and add participants to meetings in your organization, without a signed-in user. |
| [Calls.JoinGroupCall.All](https://learn.microsoft.com/graph/permissions-reference#callsjoingroupcallall) | Allows the app to join group calls and scheduled meetings in your organization, without a signed-in user. The app will be joined with the privileges of a directory user to meetings in your tenant. |
| [Calls.JoinGroupCallasGuest.All](https://learn.microsoft.com/graph/permissions-reference#callsjoingroupcallasguestall) | Allows the app to anonymously join group calls and scheduled meetings in your organization, without a signed-in user. The app will be joined as a guest to meetings in your tenant. |
| [Calls.AccessMedia.All](https://learn.microsoft.com/graph/permissions-reference#callsaccessmediaall) | Allows the app to get direct access to participant media streams in a call, without a signed-in user. |

Not all of them are necessary for a compliance recording bots. A compliance recording only requires:

- Calls.AccessMedia.All
- Calls.JoinGroupCall.All
- Calls.JoinGroupCallAsGuest.All

> [!IMPORTANT]  
> After configuring the application permissions it is required that a Microsoft Entra Id
> administrator grants the permissions. This also applies for any time the application permissions
> are changed. Changes made to the application permissions of an app registration will not take
> effect until a Microsoft Entra Id administarator has consented to them.

It is possible for administrators to grant the application permissions in the [Azure portal](https://portal.azure.com),
but often a better option is to provide a sign-up experience for administrators by using the
Microsoft Entra Id `/adminconsent` endpoint, see [instructions on constructing an Admin Consent URL](https://learn.microsoft.com/entra/identity-platform/v2-admin-consent).

> [!Note]  
> Application permissions of a multi tenant app registration must be granted by an administrator of
> each targeted Microsoft Entra Id tenant.
