using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace API
{
    public class EntraTokenAuthentication : ITokenAuthentication
    {
        private Jwks _jwks = null;
        public EntraTokenAuthentication()
        {
            _jwks = getJWKS();
        }
        private Jwks getJWKS()
        {
            if (!ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"].Equals("ENTRAID") && !ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"].Equals(APIConstants.AUTHENTICATION_TYPE_ANY_ENTRAID))
            {
                return null;
            }

            try
            {
                using HttpClient _httpClient = new HttpClient();
                var timeout = Convert.ToInt32(ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_EXTERNAL_CALL_TIMEOUT"]);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Convert.ToInt32(ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_EXTERNAL_CALL_TIMEOUT"])));

                //external url
                var jwksUri = $"https://login.microsoftonline.com/{ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_TENANTID"]}/discovery/v2.0/keys";

                var request = new HttpRequestMessage(HttpMethod.Get, jwksUri);

                HttpResponseMessage response = _httpClient.Send(request, cts.Token);
                response.EnsureSuccessStatusCode();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Log.Instance.Fatal($"Unable to get 200 response from {jwksUri}");

                    //if have exception check if we have a cached copy
                    return getJWKSFromCache();
                }
                else
                {
                    Log.Instance.Info("JWKS successfully retrieved. Length : " + response.Content.ReadAsStringAsync().Result.Length);

                    //create key for cache
                    dynamic inputDTO = new ExpandoObject();
                    inputDTO.clientID = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CLIENTID"];
                    inputDTO.tenantID = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_TENANTID"];

                    var data = JsonConvert.DeserializeObject<Jwks>(response.Content.ReadAsStringAsync().Result);
                    ApiServicesHelper.CacheD.Store_BSO<dynamic>("API", "ENTRAID_JWKS", "jwks", Utility.GetSHA256(Utility.JsonSerialize_IgnoreLoopingReference(inputDTO)), Utility.JsonSerialize_IgnoreLoopingReference(data), DateTime.Today.AddDays(30), "ENTRAID");

                    return data;

                }

            }
            catch (TaskCanceledException)
            {
                Log.Instance.Info("JWKS Request timed out or was canceled.");

                //if have exception check if we have a cached copy
                return getJWKSFromCache();
            }
            catch (HttpRequestException e)
            {
                Log.Instance.Info($"JWKS Request error: {e.Message}");

                //if have exception check if we have a cached copy
                return getJWKSFromCache();
            }
            catch (Exception e)
            {
                Log.Instance.Info($"JWKS exception : {e.Message}");

                //if have exception check if we have a cached copy
                return getJWKSFromCache();
            }
        }

        private Jwks getJWKSFromCache()
        {
            try
            {
                //create key for cache
                dynamic inputDTO = new ExpandoObject();
                inputDTO.clientID = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CLIENTID"];
                inputDTO.tenantID = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_TENANTID"];

                //if have exception check if we have a cached copy
                MemCachedD_Value jwksCache = ApiServicesHelper.CacheD.Get_BSO<dynamic>("API", "ENTRAID_JWKS", "jwks", Utility.GetSHA256( Utility.JsonSerialize_IgnoreLoopingReference(inputDTO)));
                if (jwksCache.hasData)
                {
                    Log.Instance.Info("JWKS READ FROM CACHE");
                    var data = JsonConvert.DeserializeObject<Jwks>(jwksCache.data.Value);

                    return data;
                }
                else
                {
                    Log.Instance.Fatal("No Cached JWKS");
                    return null;
                }
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                return null;
            }

        }

        public string? GetToken(HttpContext context)
        {
            var token = context.Request.Headers["MSAL"].First()?.Split(" ").Last();
            Log.Instance.Info("Token got from headers");
            return token;
        }

        public bool ValidateToken(string token)
        {
            if (!ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"].Equals("ENTRAID") && !ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"].Equals(APIConstants.AUTHENTICATION_TYPE_ANY_ENTRAID))
            {
                Log.Instance.Error("Entra ID is not enabled");
                return false;
            }

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                if (_jwks == null)
                {
                    _jwks = getJWKS();
                    //if still null then a problem
                    if (_jwks == null)
                    {
                        //cant validate token without this
                        Log.Instance.Fatal($"Json web key set data is not available. Authentication not possible");
                        return false;
                    }
                }

                //get the tenantID
                var _tenantId = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_TENANTID"];


                // Decode JWT token
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token) as JwtSecurityToken;
                var kid = jsonToken.Header.Kid;


                // Get JWKS keys
                var key = _jwks.Keys.FirstOrDefault(k => k.Kid == kid);
                if (key == null)
                {
                    Log.Instance.Error("No key id found in token : ");
                    return false; // No matching key found
                }

                //check token expiry time
                var exp = jsonToken.ValidTo;

                if (exp <= DateTime.UtcNow)
                {
                    Log.Instance.Info("expired token : " + exp);
                    return false;
                }
                var _issuerId = jsonToken.Issuer;

                // Validate the signature using the public key
                var certificateBytes = Convert.FromBase64String(key.X5c[0]);
                var certificate = new X509Certificate2(certificateBytes);
                var rsa = certificate.GetRSAPublicKey();
                var publicKey = new RsaSecurityKey(rsa);

                var validationParameters = new TokenValidationParameters
                {
                    ValidIssuers = new[]
                   {
                       $"https://login.microsoftonline.com/{_tenantId}/v2.0",
                       $"https://sts.windows.net/{_tenantId}/",
                       _issuerId
                   },
                    ValidAudiences = new[]{
                        ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_TOKEN_AUDIENCE"], // Set the audience of your app
                        "api://"+ ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CLIENTID"], // audience will be othersystem when api to api
                    },
                    IssuerSigningKey = publicKey,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateAudience = true
                };

                // Validate the token
                handler.ValidateToken(token, validationParameters, out _);

                Log.Instance.Info("Valid token");


                return true; // Token is valid
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Invalid token : {ex}");
                return false; // Validation failed
            }
            finally
            {
                sw.Stop();
                Log.Instance.Info("Token Validation completed " + Utility.StopWatchToSeconds(sw));
            }
        }

        public Dictionary<string, string> GetClaimData(string token)
        {
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                // Decode JWT token
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token) as JwtSecurityToken;

                var claimsDictionary = new Dictionary<string, string>();
                //List<string> requiredClaims = new List<string> { "email", "name", "given_name", "family_name", Constants.API_IDENTITY };
                List<string> requiredClaims = new List<string> {"appid", "name", "given_name", "family_name", APIConstants.API_IDENTITY };

                List<string> customClaims = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CUSTOM_CLAIMS"].Split(",").ToList();
                customClaims = customClaims.Where(s => !string.IsNullOrEmpty(s)).ToList();

                if (customClaims.Count > 0)
                {
                    requiredClaims.AddRange(customClaims);
                }

                foreach (var claim in jsonToken.Claims)
                {
                    if (requiredClaims.Contains(claim.Type))
                    {
                        ((IDictionary<string, string>)claimsDictionary).Add(claim.Type, claim.Value);
                    }
                }


                //validate that all mandatory keys exist

                // Check if the dictionary contains all required claims
                bool allClaimsPresent = true;
                foreach (string claim in requiredClaims)
                {
                    if (!claimsDictionary.ContainsKey(claim))
                    {
                        allClaimsPresent = false;
                        Log.Instance.Error($"Missing claim: {claim} from token");

                    }
                }

                //not all mandatory claims are present, issue with the token
                if (!allClaimsPresent)
                {
                    Log.Instance.Fatal("not all mandatory claims are present, issue with the token");
                }


                return claimsDictionary;

            }
            catch (Exception ex)
            {
                Log.Instance.Error("Error getting claim information : " + ex);
                return null;
            }
            finally
            {
                sw.Stop();
                Log.Instance.Info("Token Claim parsing completed " + Utility.StopWatchToSeconds(sw));
            }
        }


        public async Task<AuthenticationResult> GetExternalTokenAsync(string scope)
        {
            //my client ID
            string clientId = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CLIENTID"];
            //my secret
            string clientSecret = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CLIENTSECRET"]; 
            string tenantId =  ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_TENANTID"];


            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .Build();

            var result = await app.AcquireTokenForClient(new[] { scope }).ExecuteAsync();

            // This is the JWT token
            return result;
        }

        public string? GetExternalApplicationScope(string application_name)
        {
            try
            {
                string? scope = null;
                var obj = ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_OTHER_SYSTEM_ACCESS"];
                var result = Utility.JsonDeserialize_IgnoreLoopingReference<List<Dictionary<string, string>>>(obj);

                foreach (var item in result)
                {
                    if (item.ContainsKey("app_name") && item["app_name"]?.ToString() == application_name)
                    {
                        scope = item.ContainsKey("scope") ? item["scope"]?.ToString() : null;
                    }
                }

                return scope;

            }
            catch (Exception ex)
            {
                Log.Instance.Fatal(ex);
                return null;
            }

        }
    }

    public class Jwks
    {
        public Jwk[] Keys { get; set; }
    }

    public class Jwk
    {
        public string Kid { get; set; }
        public string[] X5c { get; set; }
    }
}
