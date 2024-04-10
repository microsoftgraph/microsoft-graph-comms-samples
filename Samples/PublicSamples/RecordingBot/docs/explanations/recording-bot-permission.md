# Bot Service - Entra Id and Micosoft Graph API Calling Permission

An Azure bot service can be created with an existing app registration in the Entra Id or can create a new app registration in the Entra Id on creation. The app registration can be a single tenant app registration (own organization only) or a multi tenant app registration. If the recording bot should be used from multiple Entra Id tenants a multi tenant app registration is required. Either way it is required that the Entra Id app registration and the bot service are linked.

Also the app registration must have some application permissions exposed by the Microsoft Graph API that allow the recording application to join calls and access the media streams. The following Microsoft Graph application permissions are relevant for the recording bot:
| permission | description |
|------------|-------------|
| Calls.Initiate.All | Allows the app to place outbound calls to a single user and transfer calls to users in your organization's directory, without a signed-in user. |
| Calls.InitiateGroupCall.All | Allows the app to place outbound calls to multiple users and add participants to meetings in your organization, without a signed-in user. |
| Calls.JoinGroupCall.All | Allows the app to join group calls and scheduled meetings in your organization, without a signed-in user. The app will be joined with the privileges of a directory user to meetings in your tenant. |
| Calls.JoinGroupCallasGuest.All | Allows the app to anonymously join group calls and scheduled meetings in your organization, without a signed-in user. The app will be joined as a guest to meetings in your tenant. |
| Calls.AccessMedia.All | Allows the app to get direct access to participant media streams in a call, without a signed-in user. |

But not all of them are necessary for a compliance recording bots. For a compliance recording bot are only

- Calls.AccessMedia.All
- Calls.JoinGroupCall.All
- Calls.JoinGroupCallAsGuest.All

permissions required.

> [!IMPORTANT]
> After configuring the application permissions it is required that an Entra Id adminstrator grants the permission, this also applys for any time the application permissions are changed, changes made to the application permissions of an app registratoin will not reflect until consent of an Entra Id administarator has been reapplied.

It is possible for administrators to grant the application permissions in the [Azure portal](https://portal.azure.com), but often a better option is to provide a sign-up experience for administrators by using the Entra Id `/adminconsent` endpoint, to do that see also the [instructions on constructing an Admin Consent URL](https://learn.microsoft.com/en-us/entra/identity-platform/v2-admin-consent).

> [!Note]
> Application permissions of a multi tenant app registration must be granted by an adminstrator of each targeted Entra Id tenant.
