using System.Collections.Generic;
using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;

namespace GameServerManagerWebApp.Security
{
    public class ApiKey : IApiKey
    {
        public ApiKey(string key)
        {
            Key = key;
            Claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, "APIUSER"),
            };
        }

        public string Key { get; }

        public string OwnerName => "APIUSER";

        public IReadOnlyCollection<Claim> Claims { get; }
    }
}
