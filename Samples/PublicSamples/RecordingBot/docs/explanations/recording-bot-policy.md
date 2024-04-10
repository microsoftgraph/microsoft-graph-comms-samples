# Compliance Recording Policies

> [!TIP]  
> Also read the [How To Recording Policies Guide](../guides/policy.md)

Compliance Recording Policies are the connection between Teams Users doing calls and Recording Bots ready to record. If a Teams user tries to do a call the Teams platform evaluates the Compliance Recording Policies of the user, if policies are assigned to a user or to a group of the user the policy is further evaluated. Policies always have to be set up for each Entra Id tenant induvidualy.

Policies itself are complex to setup and allow for a lot of options, it is possilbe to configure the policy to invite multiple recording bots for resilence, or that it is allowed for users to do calls without a recording bot present.

Recording Policies are made of 3 parts:

- [Recording Policy](#recording-policies)
- [Recording Application](#recording-applications)
- [Application Instance](#application-instances)

When the Teams platform found a recording policy on a user it loads the app registration from the application Instance of the underlying recording application and then gets the notification url from the Bot Service that is linked to the app registration and sends the notification to the recording bot via the notification url in the Bot Service.

## Recording Policies

Recording Policies are the managable instances that can be assigend to users, groups of users or to a whole tenant(all users of a tenant) policies contain a recording application.

## Recording Applications

Recording Applications define how the Teams platform should behave with the policy, a recording application can define that the recording bot has to be present before the user is allowed to establish conncetion to the call, that users disconnect if the bot leaves or rejects a call or that an audio message should be played if the recording bot sets the recording status to recording. A recording Application can also define a paired recording Application that is also invited, for example for resilence. Each Recording Application uses a Application Instance.

## Application Instances

An Application Instance is the connection between the recording application for the policy and the app registration of the bot implentation, the application instance creates a Entra Id resource that is linked to the app registration also in Entra Id, but the app registration and the application instance don't have to be in the same Entra Id tenant. This is especially intersting for multi tenant recording bot implementations as it allows the hosting tenant to configure things like permission and notification url and allows customer tenants to define the behaviour of the recording policy.

An applicaiton instance is an Entra Id Resource similar to a user, and application instances also require user principal names to be set. In the process of evaluating the recording policies by the Teams platform an application instance delivers the app registration which is linked to a bot service with the notification url, the entry point for the recording bot application.
