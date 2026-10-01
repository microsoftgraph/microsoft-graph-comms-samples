using System.Threading.Tasks;

namespace CseSample.Services
{
    public interface ITokenService
    {
        Task<string> FetchAccessTokenByTenantId(string tenantId);
    }
}