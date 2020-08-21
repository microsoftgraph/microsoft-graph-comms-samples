using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CseSample.Services;
using CseSample.Models;
using System.IO;
using Newtonsoft.Json;

namespace CseSample
{
    public class CallFunction
    {
        private readonly ITokenService _tokenService;
        private readonly IUsersService _usersService;
        private readonly ICallService _callService;
        private readonly IMeetingService _meetingService;
        public CallFunction(ITokenService tokenService, IUsersService usersService, ICallService callService, IMeetingService meetingService)
        {
            // Utilize dependency injection
            // https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
            _tokenService = tokenService;
            _usersService = usersService;
            _callService = callService;
            _meetingService = meetingService;
        }

        [FunctionName(nameof(Calls))]
        public async Task<IActionResult> Calls(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "calls")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var callRequest = JsonConvert.DeserializeObject<GroupCall>(requestBody);
            if (!callRequest.IsValid()) return new BadRequestResult();

            try
            {
                string accessToken = await _tokenService.FetchAccessTokenByTenantId(callRequest.TenantId);
                string[] userIds = await _usersService.GetUserIdsFromEmailAsync(callRequest.ParticipantEmails, accessToken);
                await _callService.StartGroupCallWithSpecificMembers(userIds, callRequest.TenantId, accessToken).ConfigureAwait(false);
                return new OkResult(); // TODO: Should change to 201
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                throw;
            }
        }

        [FunctionName(nameof(CallWithMeetingAttendees))]
        public async Task<IActionResult> CallWithMeetingAttendees(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "calls/{meetingId}")] HttpRequest req, string meetingId,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var meetingCallRequest = JsonConvert.DeserializeObject<MeetingCall>(requestBody);
            if (!meetingCallRequest.IsValid() || meetingId != meetingCallRequest.MeetingId) return new BadRequestObjectResult("Please request with valid tenantId, eventId and userId");

            try
            {
                string accessToken = await _tokenService.FetchAccessTokenByTenantId(meetingCallRequest.TenantId);
                string userId = (await _usersService.GetUserIdsFromEmailAsync(new string[] { meetingCallRequest.UserEmail }, accessToken))[0];
                Meeting meetingInfo = await _meetingService.GetOnlineMeetingInfo(meetingId, userId, accessToken);

                if (meetingInfo.IsOnlineMeetingSet)
                {
                    await _callService.JoinExistingOnlineMeeting(userId, meetingInfo, accessToken);
                }
                else
                {
                    string[] attendeesIds = await _usersService.GetUserIdsFromEmailAsync(meetingInfo.AttendeeEmails, accessToken);
                    await _callService.StartGroupCallWithSpecificMembers(attendeesIds, meetingCallRequest.TenantId, accessToken).ConfigureAwait(false);
                }

                return new OkResult(); // TODO: Should change to 201
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                throw;
            }
        }

        [FunctionName(nameof(HandleCallBack))]
        public async Task<IActionResult> HandleCallBack(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "callback")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string userId = req.Query["userId"];
            string tenantId = req.Query["tenantId"];
            // In general we need to return 400 error in this situation, but MS Graph doens't change action, so we just finish process by sending OKResult
            if (String.IsNullOrEmpty(userId) || String.IsNullOrEmpty(tenantId)) return new OkResult();

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                // Notification change data model depending on type of notification.
                // CallCallback is our expected one. If the model is invalid, we don't need to take any action: Go to catch
                var callBackNotifications = JsonConvert.DeserializeObject<CallCallback>(requestBody);
                foreach (var notification in callBackNotifications.Value)
                {
                    if (!notification.IsValidEstablishedNotification(tenantId)) break;

                    string accessToken = await _tokenService.FetchAccessTokenByTenantId(tenantId);
                    await _callService.InviteUserToOnlineMeeting(userId, tenantId, notification.CallId, accessToken);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                // Don't throw exception becasue MS Graph can't change action based on our Internal Server Exception
            }

            return new OkResult(); // TODO: Should change to 201
        }
    }
}
