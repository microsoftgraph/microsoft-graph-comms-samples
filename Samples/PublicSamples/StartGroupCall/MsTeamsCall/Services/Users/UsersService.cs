using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CseSample.Utils;
using Microsoft.Graph;

namespace CseSample.Services
{
    public class UsersService : IUsersService
    {
        private readonly IGraphServiceClient _graphClient;

        public UsersService(IGraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }

        public async Task<string[]> GetUserIdsFromEmailAsync(string[] emails, string accessToken)
        {
            BatchRequestStep[] batchSteps = new BatchRequestStep[emails.Length];

            for (int index = 0; index < emails.Length; index++)
            {
                Uri requestUri = new Uri($"https://graph.microsoft.com/v1.0/users?$filter=mail eq \'{emails[index]}\'&$select=id");
                BatchRequestStep batchStep = new BatchRequestStep(index.ToString(), new HttpRequestMessage(HttpMethod.Get, requestUri));
                batchSteps[index] = batchStep;
            }
            BatchRequestContent batchContent = new BatchRequestContent(batchSteps);

            try
            {
                var requestHeaders = AuthUtil.CreateRequestHeader(accessToken);
                var batchResult = await _graphClient.Batch.Request(requestHeaders).PostAsync(batchContent).ConfigureAwait(false);
                var batchResultContent = await batchResult.GetResponsesAsync();

                var ids = new List<string>();
                foreach (var item in batchResultContent.Values)
                {
                    // TODO: Need to handle if each batch request http response is not 200 
                    Users users = await item.Content.ReadAsAsync<Users>();
                    var targetUser = users.Value.FirstOrDefault();
                    if (targetUser != null) ids.Add(targetUser.Id);
                }

                return ids.ToArray();
            }
            catch(ServiceException)
            {
                // Catch Microsoft Graph SDK exception
                throw;
            }
            catch(Exception)
            {
                throw;
            }
        }
    }
}
