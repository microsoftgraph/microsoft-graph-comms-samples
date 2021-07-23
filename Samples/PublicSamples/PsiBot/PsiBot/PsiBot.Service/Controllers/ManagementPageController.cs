// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ManagementPageController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   JoinCallController is a third-party controller (non-Bot Framework) that can be called in CVI scenario to trigger the bot to join a call
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace PsiBot.Services.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using PsiBot.Model.Constants;
    using System.Net.Http;
    using System.Text;

    /// <summary>
    /// Management page allowing joining, listing, and leaving calls.
    /// </summary>
    public class ManagementPageController : ControllerBase
    {
        /// <summary>
        /// The join call async.
        /// </summary>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [HttpGet]
        [Route(HttpRouteConstants.Management + "/")]
        public ContentResult ManagementPage()
        {
            var html =
                @"<!DOCTYPE html>
                  <html lang='en'>
                      <head>
                          <meta charset='UTF-8'>
                          <title>Teams Bot</title>
                          <style>
                              body {
                                  color: white;
                                  background: black;
                                  text-align: center;
                                  font-family: sans-serif;
                              }
                              table { margin: auto; }
                              td { padding: 0.5em; }
                          </style>
                          <script language='javascript'>
                              function api(method, path, payload, callback) {
                                  var xhr = new XMLHttpRequest();
                                  xhr.onreadystatechange = function () {
                                      if (this.readyState == 4) {
                                          callback(this.responseText);
                                      }
                                  };
                                  xhr.open(method, document.location.protocol + '//' + document.location.host + '/' + path, true);
                                  if (payload) {
                                      xhr.setRequestHeader('Content-type', 'application/json');
                                      xhr.send(JSON.stringify(payload));
                                  }
                                  else {
                                      xhr.send();
                                  }
                              }
                              function join(meetingUrl) {
                                  api('POST', 'joinCall', { JoinURL: meetingUrl }, _ => { updateCalls(); });
                              }
                              function leave(legId) {
                                  api('DELETE', 'calls/' + legId, null, _ => { window.setTimeout(updateCalls, 2000); });
                              }
                              function updateCalls() {
                                  api('GET', 'calls', null, rsp => {
                                      var htm = '<table>';
                                      if (rsp) {
                                          var calls = JSON.parse(rsp);
                                          htm += '<tr><th></th><th>LegID</th><th>ScenarioID</th><th></th></tr>';
                                          for (var i = 0; i < calls.length; i++) {
                                              var call = calls[i];
                                              htm += '<tr>';
                                              htm += '<td><button onclick=""leave(\'' + call.legId + '\')"">Leave</button>';
                                              htm += '<td>' + call.legId + '</td>';
                                              htm += '<td>' + call.scenarioId + '</td>';
                                              htm += '<td><a target=""_blank"" href=""' + call.logs + '"">Logs</a></td>';
                                              htm += '</tr>';
                                          }
                                      }
                                      htm += '</table>';
                                      document.getElementById('calls').innerHTML = htm;
                                  });
                              }
                          </script>
                      </head>
                      <body onload='updateCalls()'>
                          <h1>Teams Bot</h1>
                          <input name='JoinURL' type='text' id='joinUrl' />
                          <button onclick='join(document.getElementById(""joinUrl"").value)'>Join Meeting</button>
                          <hr />
                          <h1>List Calls</h1>
                          <button onclick='updateCalls()'>Update</button>
                          <div id='calls' />
                      </body>
                  </html>";
            return Content(html, "text/html", Encoding.UTF8); ;
        }
    }
}