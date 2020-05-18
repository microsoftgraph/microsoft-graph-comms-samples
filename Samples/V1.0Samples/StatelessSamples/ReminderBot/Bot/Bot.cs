// <copyright file="Bot.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.ReminderBot.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Client.Authentication;
    using Microsoft.Graph.Communications.Client.Transport;
    using Microsoft.Graph.Communications.Common;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Common.Transport;
    using Microsoft.Graph.Communications.Core.Notifications;
    using Microsoft.Graph.Communications.Core.Serialization;
    using Newtonsoft.Json;
    using Sample.Common;
    using Sample.Common.Authentication;
    using Sample.Common.Transport;
    using Sample.ReminderBot.Data;
    using Sample.ReminderBot.Controller;
    using Sample.ReminderBot.Extensions;
    using Microsoft.Graph.Communications.Client;

    /// <summary>
    /// The core bot class.
    /// </summary>
    public class Bot
    {
        private readonly Uri botBaseUri;
        private string appId;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bot" /> class.
        /// </summary>
        /// <param name="options">The bot options.</param>
        /// <param name="graphLogger">The graph logger.</param>
        public Bot(BotOptions options, IGraphLogger graphLogger)
        {
            this.botBaseUri = options.BotBaseUrl;
            this.appId = options.AppId;

            this.GraphLogger = graphLogger;
            var name = this.GetType().Assembly.GetName().Name;
            this.AuthenticationProvider = new AuthenticationProvider(name, options.AppId, options.AppSecret, graphLogger);
            this.Serializer = new CommsSerializer();

            var authenticationWrapper = new AuthenticationWrapper(this.AuthenticationProvider);
            this.NotificationProcessor = new NotificationProcessor(authenticationWrapper, this.Serializer);
            this.NotificationProcessor.OnNotificationReceived += this.NotificationProcessor_OnNotificationReceived;
            this.RequestBuilder = new GraphServiceClient(options.PlaceCallEndpointUrl.AbsoluteUri, authenticationWrapper);

            // Add the default headers used by the graph client.
            // This will include SdkVersion.
            var defaultProperties = new List<IGraphProperty<IEnumerable<string>>>();
            using (HttpClient tempClient = GraphClientFactory.Create(authenticationWrapper))
            {
                defaultProperties.AddRange(tempClient.DefaultRequestHeaders.Select(header => GraphProperty.RequestProperty(header.Key, header.Value)));
            }

            // graph client
            var productInfo = new ProductInfoHeaderValue(
                typeof(Bot).Assembly.GetName().Name,
                typeof(Bot).Assembly.GetName().Version.ToString());
            this.GraphApiClient = new GraphAuthClient(
                this.GraphLogger,
                this.Serializer.JsonSerializerSettings,
                new HttpClient(),
                this.AuthenticationProvider,
                productInfo,
                defaultProperties);
        }

        /// <summary>
        /// Gets graph logger.
        /// </summary>
        public IGraphLogger GraphLogger { get; }

        /// <summary>
        /// Gets the authentication provider.
        /// </summary>
        public IRequestAuthenticationProvider AuthenticationProvider { get; }

        /// <summary>
        /// Gets the notification processor.
        /// </summary>
        public INotificationProcessor NotificationProcessor { get; }

        /// <summary>
        /// Gets the URI builder.
        /// </summary>
        public GraphServiceClient RequestBuilder { get; }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public CommsSerializer Serializer { get; }

        /// <summary>
        /// Gets the stateless graph client.
        /// </summary>
        public IGraphClient GraphApiClient { get; }

        /// <summary>
        /// Bot calls users.
        /// </summary>
        /// <param name="userRequestData">Input data.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task BotCallsUsersAsync(UserRequestData userRequestData)
        {
            var scenarioId = Guid.NewGuid();

            var eventsExist = await this.GetEventsInNextHour(userRequestData.ObjectId, userRequestData.TenantId, scenarioId).ConfigureAwait(false);
                
            if (eventsExist != null)
            {
                var requestCall = new Call
                {
                   Source = new ParticipantInfo
                   {
                       Identity = new IdentitySet
                       {
                           Application = new Identity
                           {
                               Id = this.appId,
                           },
                       },
                   },
                   Targets = new List<InvitationParticipantInfo>()
                   {
                        new InvitationParticipantInfo
                        {
                            Identity = new IdentitySet
                            {
                                User = new Identity
                                {
                                    Id = userRequestData.ObjectId,
                                },
                            }
                        }
                   },
                   MediaConfig = new ServiceHostedMediaConfig()
                   {
                       PreFetchMedia = new List<MediaInfo>()
                       {
                          new MediaInfo()
                          {
                             Uri = new Uri(this.botBaseUri, "audio/reminder.wav").ToString(),
                             ResourceId = Guid.NewGuid().ToString(),
                          }
                       }
                   },
                   RequestedModalities = new List<Modality> { Modality.Audio },
                   TenantId = userRequestData.TenantId,
                   Direction = CallDirection.Outgoing,
                   CallbackUri = new Uri(this.botBaseUri, ControllerConstants.CallbackPrefix).ToString(),
                };

                var callRequest = this.RequestBuilder.Communications.Calls;
                var request = new GraphRequest<Call>(new Uri(callRequest.RequestUrl), requestCall, RequestType.Create);
                await this.GraphApiClient.SendAsync<Call, Call>(request, requestCall.TenantId, scenarioId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Processes the notification asynchronously.
        /// Here we make sure we log the http request and
        /// catch/log any errors.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task ProcessNotificationAsync(
            HttpRequest request,
            HttpResponse response)
        {
            // TODO: Parse out the scenario id and request id headers.
            var headers = request.Headers.Select(
                pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

            // Don't log content since we can't PII scrub here (we don't know the type).
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            this.GraphLogger.LogHttpMessage(
                TraceLevel.Verbose,
                TransactionDirection.Incoming,
                HttpTraceType.HttpRequest,
                request.GetDisplayUrl(),
                request.Method,
                obfuscatedContent: null,
                headers: headers);

            try
            {
                var httpRequest = request.CreateRequestMessage();
                var results = await this.AuthenticationProvider.ValidateInboundRequestAsync(httpRequest).ConfigureAwait(false);
                if (results.IsValid)
                {
                    var httpResponse = await this.NotificationProcessor.ProcessNotificationAsync(httpRequest).ConfigureAwait(false);
                    await httpResponse.CreateHttpResponseAsync(response).ConfigureAwait(false);
                }
                else
                {
                    var httpResponse = httpRequest.CreateResponse(HttpStatusCode.Forbidden);
                    await httpResponse.CreateHttpResponseAsync(response).ConfigureAwait(false);
                }

                headers = response.Headers.Select(
                    pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

                this.GraphLogger.LogHttpMessage(
                    TraceLevel.Verbose,
                    TransactionDirection.Incoming,
                    HttpTraceType.HttpResponse,
                    request.GetDisplayUrl(),
                    request.Method,
                    obfuscatedContent: null,
                    headers: headers,
                    responseCode: response.StatusCode,
                    responseTime: stopwatch.ElapsedMilliseconds);
            }
            catch (ServiceException e)
            {
                string obfuscatedContent = null;
                if ((int)e.StatusCode >= 300)
                {
                    response.StatusCode = (int)e.StatusCode;
                    await response.WriteAsync(e.ToString()).ConfigureAwait(false);
                    obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(e, Formatting.Indented);
                }
                else if ((int)e.StatusCode >= 200)
                {
                    response.StatusCode = (int)e.StatusCode;
                }
                else
                {
                    response.StatusCode = (int)e.StatusCode;
                    await response.WriteAsync(e.ToString()).ConfigureAwait(false);
                    obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(e, Formatting.Indented);
                }

                headers = response.Headers.Select(
                    pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

                if (e.ResponseHeaders?.Any() == true)
                {
                    foreach (var pair in e.ResponseHeaders)
                    {
                        response.Headers.Add(pair.Key, new StringValues(pair.Value.ToArray()));
                    }

                    headers = headers.Concat(e.ResponseHeaders);
                }

                this.GraphLogger.LogHttpMessage(
                    TraceLevel.Error,
                    TransactionDirection.Incoming,
                    HttpTraceType.HttpResponse,
                    request.GetDisplayUrl(),
                    request.Method,
                    obfuscatedContent,
                    headers,
                    response.StatusCode,
                    responseTime: stopwatch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await response.WriteAsync(e.ToString()).ConfigureAwait(false);

                var obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(e, Formatting.Indented);
                headers = response.Headers.Select(
                    pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

                this.GraphLogger.LogHttpMessage(
                    TraceLevel.Error,
                    TransactionDirection.Incoming,
                    HttpTraceType.HttpResponse,
                    request.GetDisplayUrl(),
                    request.Method,
                    obfuscatedContent,
                    headers,
                    response.StatusCode,
                    responseTime: stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Raised when the <see cref="INotificationProcessor"/> has received a notification.
        /// </summary>
        /// <param name="args">The <see cref="NotificationEventArgs"/> instance containing the event data.</param>
        private void NotificationProcessor_OnNotificationReceived(NotificationEventArgs args)
        {
#pragma warning disable 4014
            // Processing notification in the background.
            // This ensures we're not holding on to the request.
            this.NotificationProcessor_OnNotificationReceivedAsync(args).ForgetAndLogExceptionAsync(
                this.GraphLogger,
                $"Error processing notification {args.Notification.ResourceUrl} with scenario {args.ScenarioId}");
#pragma warning restore 4014
        }

        /// <summary>
        /// Raised when the <see cref="INotificationProcessor"/> has received a notification asynchronously.
        /// </summary>
        /// <param name="args">The <see cref="NotificationEventArgs"/> instance containing the event data.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task NotificationProcessor_OnNotificationReceivedAsync(NotificationEventArgs args)
        {
            this.GraphLogger.CorrelationId = args.ScenarioId;
            var headers = new[]
            {
                new KeyValuePair<string, IEnumerable<string>>(HttpConstants.HeaderNames.ScenarioId, new[] { args.ScenarioId.ToString() }),
                new KeyValuePair<string, IEnumerable<string>>(HttpConstants.HeaderNames.ClientRequestId, new[] { args.RequestId.ToString() }),
                new KeyValuePair<string, IEnumerable<string>>(HttpConstants.HeaderNames.Tenant, new[] { args.TenantId }),
            };

            // Create obfuscation content to match what we
            // would have gotten from the service, then log.
            var notifications = new CommsNotifications { Value = new[] { args.Notification } };
            var obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(notifications, Formatting.Indented);
            this.GraphLogger.LogHttpMessage(
                TraceLevel.Info,
                TransactionDirection.Incoming,
                HttpTraceType.HttpRequest,
                args.CallbackUri.ToString(),
                HttpMethods.Post,
                obfuscatedContent,
                headers,
                correlationId: args.ScenarioId,
                requestId: args.RequestId);

            if (args.ResourceData is Call call)
            {
                if (call.State == CallState.Established && call.MediaState?.Audio == MediaState.Active)
                {
                    await this.BotPlayPromptAsync(call.Id, args.TenantId, args.ScenarioId).ConfigureAwait(false);
                }
                else if (args.ChangeType == ChangeType.Deleted && call.State == CallState.Terminated)
                {
                    this.GraphLogger.Log(TraceLevel.Info, $"Call State:{call.State}");
                }
            }
            else if (args.ResourceData is PlayPromptOperation playPromptOperation)
            {
                //  checking for the call id sent in ClientContext.
                if (string.IsNullOrWhiteSpace(playPromptOperation.ClientContext))
                {
                    throw new ServiceException(new Error()
                    {
                        Message = "No call id provided in PlayPromptOperation.ClientContext.",
                    });
                }
                else if (playPromptOperation.Status == OperationStatus.Completed)
                {
                    // The operation has been completed, hang up the call
                    await this.BotHangupCallAsync(playPromptOperation.ClientContext, args.TenantId, args.ScenarioId).ConfigureAwait(false);
                    this.GraphLogger.Log(TraceLevel.Info, $"Disconnecting the call.");
                }
            }
        }

        /// <summary>
        /// Bot Hangs up the call.
        /// </summary>
        /// <param name="callId">The call identifier to hang up.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <returns>
        /// When the prompt has been played.
        /// </returns>
        private async Task BotHangupCallAsync(string callId, string tenantId, Guid scenarioId)
        {
            var hangupRequest = this.RequestBuilder.Communications.Calls[callId].Request();
            await this.GraphApiClient.SendAsync(hangupRequest, RequestType.Delete, tenantId, scenarioId).ConfigureAwait(false);
        }

        /// <summary>
        /// Get Events in next hour.
        /// </summary>
        /// <param name="userObjectId">The user object identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <returns>The <see cref="Task"/> representing the event of calendar.</returns>
        private async Task<Event> GetEventsInNextHour(string userObjectId, string tenantId, Guid scenarioId)
        {
            var listEvents = this.RequestBuilder.Users[userObjectId].Calendar.Events.Request()
                               .Select(e => new
                               {
                                   e.Subject,
                                   e.Body,
                                   e.BodyPreview,
                                   e.Organizer,
                                   e.Attendees,
                                   e.Start,
                                   e.End,
                                   e.Location
                               });  

            var calendarEventList = await this.GraphApiClient.SendAsync<PagedList<Event>>(listEvents, RequestType.Get, tenantId, scenarioId).ConfigureAwait(false);

            foreach (var calendarEvent in calendarEventList)
            {
                var startTime = Convert.ToDateTime(calendarEvent.Start.DateTime);
                if (startTime > DateTime.UtcNow && DateTime.UtcNow.Subtract(startTime) < TimeSpan.FromHours(1))
                {
                    var attendees = calendarEvent.Attendees;
                    foreach (var attendee in attendees)
                    {
                        var name = attendee.EmailAddress.Name.ToString();
                        this.GraphLogger.Log(TraceLevel.Info, $"Attendee's name {name}");
                    }

                    var eventSubject = calendarEvent.Subject;
                    var time = calendarEvent.Start.DateTime;
                    this.GraphLogger.Log(TraceLevel.Info, $"Subject: {eventSubject}");
                    this.GraphLogger.Log(TraceLevel.Info, $"Time: {time}");
                    return calendarEvent;
                }
            }
            return null;

            // if calendarEventList?.NextLinkUrl isn't null, then you should read the next page.
        }      

         /// <summary>
        /// Bot play the given prompts
        /// </summary>
        /// <param name="prompts">The prompts to play.</param>
        /// <param name="callId">The call identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task BotPlayPromptAsync(string callId, string tenantId, Guid scenarioId)
        {
            var prompts = new Prompt[] {
                        new MediaPrompt
                        {
                            MediaInfo = new MediaInfo()
                            {
                                Uri = new Uri(this.botBaseUri, "audio/reminder.wav").ToString(),
                                ResourceId = Guid.NewGuid().ToString(),
                            },
                        },
                    };
            var playPromptRequest = this.RequestBuilder.Communications.Calls[callId].PlayPrompt(
                prompts: prompts,
                clientContext: callId).Request();
            await this.GraphApiClient.SendAsync<PlayPromptOperation>(playPromptRequest, RequestType.Create, tenantId, scenarioId).ConfigureAwait(false);
        }
    }
}