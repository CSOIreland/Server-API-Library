using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using System.IdentityModel.Tokens.Jwt;

namespace API
{
    public interface ITokenAuthentication
    {
       string? GetToken(HttpContext contect);
       bool ValidateToken(string token);
       Dictionary<string, string> GetClaimData(string? jsonToken);

       Task<AuthenticationResult> GetExternalTokenAsync(string scope);

       string? GetExternalApplicationScope(string application_name);
    }
}