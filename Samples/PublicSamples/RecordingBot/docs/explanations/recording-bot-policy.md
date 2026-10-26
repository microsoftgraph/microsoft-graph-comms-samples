# Compliance Recording Policies

> [!TIP]  
> Also read the [How To Recording Policies Guide](../guides/policy.md)

Compliance recording policies are the connection between Microsoft Teams users doing calls and bot
recording application ready to record. If a Microsoft Teams user tries to do a call, the Microsoft
Teams platform evaluates the compliance recording policies of the user. When policies are assigned
to a user or to a group of the user the policy is further evaluated. Policies always have to be set
up for each Microsoft Entra Id tenant induvidualy.

Policies itself are complex to setup and allow for a lot of options. It is possible to configure
the policy to invite multiple recording bots for resilience, or that it is allowed for users to do
calls without a recording bot present.

Recording policies are made of 3 parts:

- [Recording Policy](#recording-policies)
- [Recording Application](#recording-applications)
- [Application Instance](#application-instances)

When the Microsoft Teams platform finds a recording policy assigned to an user, it further
evaluates the recording application and loads the app registration from the corresponding
application instance. The Microsoft Teams platform then gets the notification URL from the bot
service that is linked to the app registration and sends the notification to the recording bot via
the notification URL in the bot service.

## Recording Policies

Recording policies are managable instances that can be assigend to users, groups of users or to a
whole tenant (all users of a tenant). Policies contain a recording application.

## Recording Applications

Recording applications define how the Microsoft Teams platform should behave with the policy. A
recording application can define, that the recording bot has to be present before the user is
allowed to establish a connection to the call, that users disconnect if the bot leaves or rejects a
call, or that an audio message should be played if the recording bot sets the recording status to
recording. A recording application can also define a paired recording application that is also
invited. This can, for example, be used as backup to raise resilience.

## Application Instances

An application instance is the connection between the recording application for the policy and the
app registration of the bot implementation. The application instance creates a Microsoft Entra Id
resource that is linked to the app registration, also in Entra Id. The app registration and the
application instance do not have to be in the same Entra Id tenant. This is especially interesting
for multi tenant recording bot implementations as it allows the hosting tenant to configure things
like permissions and the notification URL and the consumer tenants to define the behaviour of the
recording policy.

An application instance is a Microsoft Entra Id resource similar to a user. Application instances
also require user principal names to be set. When the Microsoft Teams platform evaluates the
recording policies, an application instance delivers the app registration that is linked to a bot
service that contains the notification url.
