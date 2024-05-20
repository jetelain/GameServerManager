using System.Linq;
using System.Threading.Tasks;
using AspNetCore.Authentication.ApiKey;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GameServerManagerWebApp.Security
{
    public class ApiKeyProvider : IApiKeyProvider
    {
        private readonly IConfiguration _configuration;

        public ApiKeyProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<IApiKey> ProvideAsync(string key)
        {
            var allKeys = _configuration.GetSection("AllowedApiKeys").Get<string[]>();
            if (allKeys != null && allKeys.Contains(key))
            {
                return Task.FromResult<IApiKey>(new ApiKey(key));
            }
            return Task.FromResult<IApiKey>(null);
        }
    }
}
