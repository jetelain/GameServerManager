using Microsoft.AspNetCore.Authentication;

namespace GameServerManagerWebApp.Models
{
    public class SignInViewModel
    {
        public string ReturnUrl { get; set; }
        public AuthenticationScheme[] Providers { get; set; }
    }
}
