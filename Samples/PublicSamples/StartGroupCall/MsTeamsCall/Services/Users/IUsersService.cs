using System.Threading.Tasks;

namespace CseSample.Services
{
    public interface IUsersService
    {
        Task<string[]> GetUserIdsFromEmailAsync(string[] emails, string accessToken);
    }
}