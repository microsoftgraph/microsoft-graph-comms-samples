# Multiple Applications within single Bot

Some developers may wish to support multiple applications from within the same application code.  With very little effort, this can be done using multiple instances of `ICommunicationsClient`.  There are some steps that should be taken to ensure that we make outbound requests with the right application, and we forward incoming notifications to the right application.

This writeup will demonstrate how to alter the existing samples to add multiple application support.  We have not created a sample of this scenario explicitly given that it is not a standard way to use the Graph SDK.

## Create multiple communications clients

First, each application instance requires it's own `ICommunicationsClient` instance, given that it supports a single `IRequestAuthenticationProvider`

Let's change the Bot `ICommunicationsClient Client` to an `IDictionary<string, ICommunicationsClient> Clients` and create our clients.

```csharp
/// <summary>
/// Prevents a default instance of the <see cref="Bot"/> class from being created.
/// </summary>
private Bot()
{
    this.AddClient(
        Service.Instance.Configuration.MicrosoftAppId,
        Service.Instance.Configuration.MicrosoftAppPassword);
    this.AddClient(
        Service.Instance.Configuration.MicrosoftApp2Id,
        Service.Instance.Configuration.MicrosoftApp2Password);
}

private void AddClient(string appId, string appSecret)
{
    // Create a unique notification uri for first app instance
    // This appends the app id to the callback uri so we get
    // https://base.uri/callbacks/{appId}
    var notificationUri = new Uri(
        Service.Instance.Configuration.CallControlBaseUrl,
        appId);

    var builder = new CommunicationsClientBuilder("AudioVideoPlaybackBot", appId);
    
    builder
        .SetAuthenticationProvider(
            new AuthenticationProvider(
                appId,
                appSecret,
                Service.Instance.Configuration.TokenAudienceResourceLink))
        .SetNotificationUrl(notificationUri)
        .SetMediaPlatformSettings(Service.Instance.Configuration.MediaPlatformSettings)
        .SetServiceBaseUrl(Service.Instance.Configuration.PlaceCallEndpointUrl);

    var client = builder.Build();
    this.Clients.Add(appId, client);
    client.Calls().OnIncoming += this.CallsOnIncoming;
    client.Calls().OnUpdated += this.CallsOnUpdated;
}

/// <summary>
/// Gets the contained app clients
/// </summary>
public IDictionary<string, ICommunicationsClient> Clients { get; }
```

Let's also add a reference to the ICallCollection to the call handler for ease of access.  This will allow us to reference the correct collection/client from any given call id.

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="CallHandler"/> class.
/// </summary>
/// <param name="callCollection">The call collection.</param>
/// <param name="call">The call.</param>
public CallHandler(ICallCollection callCollection, ICall call);

/// <summary>
/// Gets the call collection
/// </summary>
public ICallCollection CallCollection { get; }

/// <summary>
/// Gets the call
/// </summary>
public ICall Call { get; }
```

## Handle notifications

Next we need to adjust the incoming call controller to forward the notifications to the right client.  If there is no need to handle incoming call, then the above configuration will automatically route the the correct apps endpoint.  If we need to handle incoming call, we can either have a default app process all those scenarios, or the callback URI of each app can contain the app id.

For example, if our service URI is `https://base.uri/callback`, we can set all our bots to use this URI directly, but then we lose knowledge of which app is receiving the incoming call.  If we change each apps callback uri to `https://base.uri/callback/{appId}` (example: https://base.uri/callback/9ecd52e5-6592-42b7-b562-093f37f13bde, where the appId is 9ecd52e5-6592-42b7-b562-093f37f13bde) then we have the app context when an incoming call occurs.  Of course there are other ways at getting the app id, like from the auth token or the payload, but this is one simple option.

To handle the app id in the URI the controllers for callbacks need to be changed to the following:

```csharp
/// <summary>
/// Gets a reference to singleton sample bot/client instance
/// </summary>
private IDictionary<string, ICommunicationsClient> Clients =>
    Bot.Instance.Clients;

/// <summary>
/// Handle a callback for an incoming call.
/// Here we don't know what application is receiving the callback.
/// </summary>
/// <returns>
/// The <see cref="HttpResponseMessage"/>.
/// </returns>
[HttpPost]
[Route("")]
public Task<HttpResponseMessage> OnIncomingRequestAsync()
{
    // Pick some app id to handle this call.
    var appId = this.Clients.Keys.First();
    return this.OnIncomingRequestAsync(appId);
}

/// <summary>
/// Handle a callback for an incoming call or notification.
/// Here we've added the application id to the callback URI.
/// </summary>
/// <returns>
/// The <see cref="HttpResponseMessage"/>.
/// </returns>
[HttpPost]
[Route("{appId}")]
public async Task<HttpResponseMessage> OnIncomingRequestAsync(string appId)
{
    Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Received HTTP {this.Request.Method}, {this.Request.RequestUri}");

    // Pass the incoming message to the sdk. The sdk takes care of what to do with it.
    var client = this.Clients[appId];
    var response = await client.ProcessNotificationAsync(this.Request).ConfigureAwait(false);

    // Enforce the connection close to ensure that requests are evenly load balanced so
    // calls do no stick to one instance of the worker role.
    response.Headers.ConnectionClose = true;
    return response;
}
```

## Additional notes

There are a few items not addressed here.  For instance, when a VTC joins a meeting, which app should it use?  This has been purposefuly excluded from this guide given that it is unique business logic.  Some may want a different bot configuration for each client, others may associate unique permissions to each bot and use depending on action being performed.
