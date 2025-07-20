You need to register following MS Graph permissions in your Azure AD.

## Permissions
|Permission Name|Permission type|Objective|
|--|--|--|
|Calendars.Read|Application|Read schedule information including attendees and online meeting url|
|Calls.InitiateGroupCall.All|Application|Create group call, Invite user to call|
|Calls.JoinGroupCall.All|Application|Join existing meeting|
|User.Read.All|Application|Get user id from email|


## Reference
- Graph permission all lists and details is described in [here](https://docs.microsoft.com/en-us/graph/permissions-reference)
- If you are not familiar with Azure AD permission configuration, you can refer this [document](https://docs.microsoft.com/en-us/graph/auth-v2-service#2-configure-permissions-for-microsoft-graph).