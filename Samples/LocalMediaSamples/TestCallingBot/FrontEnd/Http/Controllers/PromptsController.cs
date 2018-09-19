// <copyright file="PromptsController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Sample.TestCallingBot.FrontEnd.Http
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Web.Http;
    using Microsoft.Graph.CoreSDK.Serialization;
    using Sample.TestCallingBot.FrontEnd.Bot;

    /// <summary>
    ///   MuteParticipantsController is a third-party controller (non-Bot Framework) that can be called to mute participants in a meeting.
    /// </summary>
    public class PromptsController : ApiController
    {
        /// <summary>
        /// Gets all prompt.
        /// </summary>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.OnPromptsRoute)]
        public HttpResponseMessage GetAllPrompt()
        {
            try
            {
                var promptsBaseUri = $"https://{Service.Instance.Configuration.ServiceDnsName}/prompts";
                var files = Directory.GetFiles(".\\", "*.wav");
                var prompts = files
                    .Select(s => s.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => $"{promptsBaseUri}/{s}").ToArray();

                if (prompts.Length < 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }

                var serializer = new Serializer(pretty: true);
                var json = serializer.SerializeObject(prompts);

                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(json, Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }

        /// <summary>
        /// Gets the prompt.
        /// </summary>
        /// <param name="promptId">The prompt identifier.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage" />.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.PromptRoute)]
        public HttpResponseMessage GetPrompt(string promptId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(promptId)
                    || !promptId.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                    || !File.Exists(promptId))
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                var response = this.Request.CreateResponse(HttpStatusCode.OK);
                var stream = new FileStream(promptId, FileMode.Open);
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                return response;
            }
            catch (Exception e)
            {
                return Bot.InspectExceptionAndReturnResponse(e);
            }
        }
    }
}