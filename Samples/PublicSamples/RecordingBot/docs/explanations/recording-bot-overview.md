# High Level Overview

![Image 1](../images/Overview.svg)

As seen in the image Microsoft Teams clients communicate with the Micorsoft Teams platform. The Microsoft Teams Platform communicates with the bot recording application via different channels. It gets a notification URL of the bot recording application from a bot service.

The flow if a Microsoft Teams user initiates a call (a call with another user or a call into a meeting) in the Microsoft Teams client looks approxamatly like this:

1. The users Microsoft Teams client connects to the Microsoft Teams platform.
2. The Microsoft Teams platform evaluates the compliance recording policies.

    - If the user is under a policy:

3. The Microft Teams platform loads the HTTPS notification URL from the corresponding bot service.
4. The Microsoft Teams platform creates a call(an entity) on the Graph Communications API for the bot recording application, with destination to the users call and metadata of the users call.
5. The call is sent to the notification URL of the bot recording application.

> [!IMPORTANT]  
> The notification URL must be HTTPS and the certificate used must be signed by a valid authority.

When the bot recording application receives the notification with the new call from the Microsoft Teams platform, the bot recording application should answer the call via the Graph Communications API. The answer must contain some configuration e.g. the TCP endpoint of the media processor of the bot or a new notification URL for further notifications regarding the call. With a new notification URL further notifications, of the call that is being answered, are sent to the new URL. The recording bot application receives notifications for users joining, leaving, muting, starting screen shares and more.

After the bot recording application answered and accepted a call, the Microsoft Teams platform opens a connection on the provided TCP endpoint. After an initial handshake for authorization and encryption, metadata and media events are transferred via the TCP connection.

> [!IMPORTANT]  
> The TCP endpoint also requires a certificate signed by a valid authority. The Certificate for the HTTPS endpoint and the TCP endpoint **can** be the same.

## Graph Communications SDK

The overhead of the TCP endpoints is completly managed by the Graph Communications SDK. But the HTTPS endpoints for notifications must be custom implemented by the bot recording application, ASP.NET Core can be used. After a HTTP request is authorized and notifications are parsed from the request, the notifications should be forwarded to the SDK. It can then process them and trigger event handlers based on the notifications. The event handlers can implement business logic. For Example an event handler, that triggers when a new call notification was received, should be used for answering calls. Before answering business logic can decide whether the call should be accepted. Such an event handler should use the `answer` method of the SDK with the desired configuration to accept a call. The configuration specifies how many video sockets should be created, if the notification url should change and more. In the case that a call shouldn't be accepted a `reject` method of the SDK can be used.

As the Microsoft Graph Communications SDK takes care of a lot of overhead and endpoint handling, the SDK also needs to be configured correctly: with a valid certificate as part of the TCP endpoint configuration with DNS name and port(s), the configuration of the notification Endpoints and configuration for the connection to the Graph Communications API. The SDK also needs a implentation of an authorization handler for outgoing API calls to the Graph Communications API, and for validating incoming requests, within the initial TCP handshake of the SDK and authorizing HTTP requests on the notification endpoint SDK. For the latter it's not required to use the handler but a simple possibility.

> [!NOTE]  
> The application requires some application permissions on an app registration that is bound to a bot service to be able to answer calls and access media from calls, see the [Application Permissions Page](./recording-bot-permission.md) for reference.
