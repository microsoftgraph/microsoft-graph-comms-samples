// <copyright file="PlatformCallController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using PsiBot.Model.Constants;
using PsiBot.Services.Bot;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Core.Notifications;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PsiBot.Services.Controllers
{
    /// <summary>
    /// Entry point for handling call-related web hook requests from Skype Platform.
    /// </summary>
    [Route(HttpRouteConstants.CallSignalingRoutePrefix)]
    public class PlatformCallController : ControllerBase
    {
        /// <summary>
        /// The bot service
        /// </summary>
        private readonly IBotService _botService;
        /// <summary>
        /// The logger
        /// </summary>
        private readonly IGraphLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformCallController" /> class.

        /// </summary>
        public PlatformCallController(IBotService botService, IGraphLogger logger)
        {
            this._botService = botService;
            this._logger = logger;
        }

        /// <summary>
        /// Handle a callback for an incoming call.
        /// </summary>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnIncomingRequestRoute)]
        public async Task<IActionResult> OnIncomingRequestAsync()
        {
            var log = $"Received HTTP {this.Request.Method}, {this.Request.Path.Value}";
            _logger.Info(log);

            var response = await _botService.Client.ProcessNotificationAsync(ConvertHttpRequestToHttpRequestMessage(this.Request)).ConfigureAwait(false);

            var content = response.Content == null ? null : await response.Content?.ReadAsStringAsync();
            return Ok(content);
        }

        /// <summary>
        /// Handle a callback for an existing call
        /// </summary>
        /// <returns>The <see cref="HttpResponseMessage" />.</returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnNotificationRequestRoute)]
        public async Task<IActionResult> OnNotificationRequestAsync()
        {
            var log = $"Received HTTP {this.Request.Method}, {this.Request.Path}";
            _logger.Info(log);

            //var content = await Request.Content.ReadAsStringAsync();
            //if (content.Contains("0#10550"))
            //{
            //    var callHandler = _botService.CallHandlers.First().Value;
            //    var bms = callHandler.BotMediaStream;
            //    bms.GetAudioQualityOfExperienceData();
            //}

            // Pass the incoming notification to the sdk. The sdk takes care of what to do with it.
            var response = await _botService.Client.ProcessNotificationAsync(ConvertHttpRequestToHttpRequestMessage(this.Request)).ConfigureAwait(false);

            var content = response.Content == null ? null : await response.Content?.ReadAsStringAsync();
            return Ok(content);
        }

        private HttpRequestMessage ConvertHttpRequestToHttpRequestMessage(HttpRequest request)
        {
            var uri = new Uri(request.GetDisplayUrl());
            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers
            foreach (var header in request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }
    }
}
