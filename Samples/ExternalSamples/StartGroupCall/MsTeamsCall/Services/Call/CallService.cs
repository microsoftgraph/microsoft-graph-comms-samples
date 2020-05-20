using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CseSample.Utils;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace CseSample.Services
{
    public class CallService : ICallService
    {
        private readonly IGraphServiceClient _graphClient;

        public CallService(IGraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }

        public async Task<bool> StartGroupCallWithSpecificMembers(string[] userIds, string tenantId, string accessToken)
        {
            try
            {
                var requestHeaders = AuthUtil.CreateRequestHeader(accessToken);
                Call callRequest = CreateGroupCallRequest(userIds, tenantId);
                await _graphClient.Communications.Calls.Request(requestHeaders).AddAsync(callRequest);

                return true;
            }
            catch (ServiceException ex)
            {
                throw;
            }
        }

        private Call CreateGroupCallRequest(string[] userIds, string tenantId)
        {
            Call callRequest = new Call();
            callRequest.TenantId = tenantId;
            callRequest.CallbackUri = "https://bing.com"; // TODO: Need to update correct call back url
            callRequest.RequestedModalities = new Modality[] { Modality.Audio };
            callRequest.MediaConfig = new ServiceHostedMediaConfig();

            callRequest.Source = new ParticipantInfo()
            {
                Identity = new IdentitySet()
                {
                    Application = new Identity() { Id = Environment.GetEnvironmentVariable("ClientId") }
                }
            };

            var targets = new List<InvitationParticipantInfo>();
            foreach (string userId in userIds)
            {
                var participant = new InvitationParticipantInfo()
                {
                    Identity = new IdentitySet()
                    {
                        User = new Identity() { Id = userId }
                    }
                };
                targets.Add(participant);
            }
            callRequest.Targets = targets;
            Console.WriteLine(JsonConvert.SerializeObject(callRequest));

            return callRequest;
        }

        public async Task<bool> JoinExistingOnlineMeeting(string userId, Meeting meetingInfo, string accessToken)
        {
            try
            {
                var requestHeaders = AuthUtil.CreateRequestHeader(accessToken);
                Call joinMeetingRequest = this.CreateJoinCallRequest(userId, meetingInfo);
                await _graphClient.Communications.Calls.Request(requestHeaders).AddAsync(joinMeetingRequest);

                return true;
            }
            catch (ServiceException ex)
            {
                throw;
            }
        }

        private Call CreateJoinCallRequest(string userId, Meeting meetingInfo)
        {
            Call callRequest = new Call();
            callRequest.TenantId = meetingInfo.TenantId;
            callRequest.CallbackUri = $"{Settings.CallBackEndpoint}/api/callback?userId={userId}&tenantId={meetingInfo.TenantId}";
            callRequest.RequestedModalities = new Modality[] { Modality.Audio };
            callRequest.MediaConfig = new ServiceHostedMediaConfig();
            callRequest.ChatInfo = new ChatInfo()
            {
                ThreadId = meetingInfo.ThreadId,
                MessageId = meetingInfo.MessageId
            };
            callRequest.MeetingInfo = new OrganizerMeetingInfo()
            {
                Organizer = new IdentitySet()
                {
                    User = new Identity()
                    {
                        Id = userId,
                        AdditionalData = new Dictionary<string, object>() { { "tenantId", meetingInfo.TenantId } }
                    }
                },
                AdditionalData = new Dictionary<string, object>() { { "allowConversationWithoutHost", true } }
            };

            var test = JsonConvert.SerializeObject(callRequest);
            return callRequest;
        }

        public async Task<bool> InviteUserToOnlineMeeting(string userId, string tenantId, string callId, string accessToken)
        {
            try
            {
                var participants = CreateParticipant();
                var requestHeaders = AuthUtil.CreateRequestHeader(accessToken);
                await _graphClient.Communications.Calls[callId].Participants.Invite(participants).Request(requestHeaders).PostAsync();

                return true;
            }
            catch (ServiceException)
            {
                throw;
            }

            InvitationParticipantInfo[] CreateParticipant()
            {
                var participantInfo = new InvitationParticipantInfo();
                participantInfo.Identity = new IdentitySet()
                {
                    User = new Identity()
                    {
                        Id = userId,
                        AdditionalData = new Dictionary<string, object>() { { "tenantId", tenantId } }
                    }
                };

                return new InvitationParticipantInfo[] { participantInfo };
            }
        }
    }
}