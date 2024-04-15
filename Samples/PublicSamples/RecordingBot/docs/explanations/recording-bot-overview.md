# High Level Overview

![Image 1](../images/Overview.svg)

As seen in the image Microsoft Teams clients communicate with the Micorsoft Teams platform. If a Microsoft Teams user initiates a call (a call with another user or a call into a meeting) in its Microsoft Teams client, the client connects to the Microsoft Teams platform. On the Microsoft Teams platform compliance recording policies are evaluated, if the user is under a policy the HTTPS notification URL from the corresponding bot service is loaded. The Microsoft Teams platform then creates a graph call entity that represent the bot recording applications call into the users call for recording. The call entity is sent to the notification URL, this is the entrypoint for the recording bot application to record the calls.

> [!IMPORTANT]  
> The notification URL must be HTTPS and the certificate used must be signed by a valid authority.

After the bot recording application receives a call from the Microsoft Teams platform, the bot recording application should send an answer to the Microsoft Teams platform via the Graph Communications API. The answer of the recording application must contains some configuration settings e.g. the TCP endpoint of the media processor of the bot. The answer can also contain a new notification URL for further notifications regarding the call. With a new notification URL further notifications, of the call that is being answered, are sent to the new URL. The recording bot application receives a notification for every time a user joins, leaves, mutes, starts a screen sharing and more actions users can do in a meeting.

> [!IMPORTANT]  
> The TCP endpoint also requires a certificate signed by a valid authority. The Certificate for the HTTPS endpoint and the TCP endpoint **can** be the same.

After the bot recording application answered and accepted a call, the Microsoft Teams platform opens a connection on the provided TCP endpoint. After an initial handshake for authorization and encryption, metadata and media events are transferred via the TCP connection.

## Graph Communications SDK

The overhead of the TCP endpoints is completly managed by the Graph Communications SDK. But the HTTPS endpoints for notifications must be custom implemented by the bot recording application, ASP.NET Core can be used. After a HTTP request is authorized and notifications are parsed from the request, the notifications should be forwarded to the SDK. It can then process them and trigger event handlers based on the notifications. The event handlers can implement business logic. For Example an event handler, that triggers when a new call notification was received, should be used for answering calls. Before answering business logic can decide whether the call should be accepted. Such an event handler should use the `answer` method of the SDK with the desired configuration to accept a call. The configuration specifies how many video sockets should be created, if the notification url should change and more. In the case that a call shouldn't be accepted a `reject` method of the SDK can be used.

As the Microsoft Graph Communications SDK takes care of a lot of overhead and endpoint handling, the SDK also needs to be configured correctly: with a valid certificate as part of the TCP endpoint configuration with DNS name and port(s), the configuration of the notification Endpoints and configuration for the connection to the Graph Communications API. The SDK also needs a implentation of an authorization handler for outgoing API calls to the Graph Communications API, and for validating incoming requests, within the initial TCP handshake of the SDK and authorizing HTTP requests on the notification endpoint SDK. For the latter it's not required to use the handler but a simple possibility.

> [!NOTE]  
> The application requires some application permissions on an app registration that is bound to a bot service to be able to answer calls and access media from calls, see the [Application Permissions Page](./recording-bot-permission.md) for reference.
