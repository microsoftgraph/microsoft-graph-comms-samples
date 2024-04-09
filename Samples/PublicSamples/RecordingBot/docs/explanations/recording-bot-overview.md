# High Level Overview

![Image 1,](../images/Overview.png)

As seen in the image Teams clients communicate with the Teams Server / Teams Communication Platform. When a user under compliance recording policy initiates a call (a call with another user or a call into a meeting) in its teams client, the Teams platform gets the HTTPS notification URL from a bot service that is linked to the Compliance Recording Policy. The Teams Platform uses the notification URL to do a authorized http call to the Recording Application, informing about the user initiating a call, authorization is done with a JWT token in the headers.

> [!IMPORTANT]
> The notification URL must be HTTPS and the certificate used must be signed by a valid authority.

When the Recording Application receives the HTTPS Request, that a user is about to join a call, from the Teams Platform, the Recording Application should send an answer to the Teams Platform via the Graph Communications API within this answer the Recording Application has to give a TCP endpoint, can specify configuration and can change the notification url for further notifications regarding this call (the call the user initiated, to other users or into a meeting) that should be recorded. Further Notifications are send for everytime a user joins, mutes, starts a screen sharing and a lot of more actions users can do in a meeting.

> [!IMPORTANT]  
> The TCP endpoint also requires a certificate signed by a valid authority.

On the TCP endpoint a connection is opened by the Teams Platform and a handshake for authorization and encryption is completed.

## Supportive SDK

The overhead of the TCP endpoints is completly managed by the Microsft SDK for bots, building an endpoint for receiving the HTTPS notifications (new calls and any updates) by the Teams Platform must implemented by the Recording Application. But parsed responses should be forwarded to the SDK which then fires event handlers based on the notifications, these event handlers then can implement business logic. For Example an event handler, that triggers for new calls(initiated by users) received on the notification endpoint is required for answering, and should use the `answer` method of the SDK with some parameters (how should the TCP Endpoint be configured, how many video sockets should be created, should the notification url change etc.). But before that business logic can decide if the call should be answered or if the user is not allowed to do the call(with a less strict compliance recording policy it is also possible to just not record then), in that case a `reject` method of the SDK can also be used.

So a lot of overhead and endpoint handling is taken care of by the Microsoft SDK for bots. But the SDK also has to be configured: with a valid certificate, the TCP endpoint configuration(DNS name and port) and an implentation of an authorization handler for outgoing API calls with AppId + Secrect of the bot service, that holds the notification URL for new calls, and for validating incoming requests within the initial handshake of the sdk via tcp port. The authorization handler can and should be used for authorizing incoming request on the signaling endpoints.

> [!NOTE]
> The application requires some application permissions on an app registration that is bound to a bot service to be able to answer calls and access media from calls, see the [Application Permissions Page](./application-permission) for reference.
