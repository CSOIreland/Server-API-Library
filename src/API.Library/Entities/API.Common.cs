using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.DirectoryServices.AccountManagement;
using System.Dynamic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace API
{

    /// <summary>
    /// Static implementation of the API Constants
    /// </summary>
    public class Common
    {
        /// <summary>
        /// the type of middlware that the request is.
        /// </summary>
        public string middlewareType = null;

        /// <summary>
        /// Windows Authentication constant
        /// </summary>
        internal const string AUTHENTICATION_TYPE_WINDOWS = "WINDOWS";

        /// <summary>
        /// Anonymous Authentication constant
        /// </summary>
        internal const string AUTHENTICATION_TYPE_ANONYMOUS = "ANONYMOUS";

        /// <summary>
        /// Any Authentication constant
        /// </summary>
        internal const string AUTHENTICATION_TYPE_ANY = "ANY";

        /// <summary>
        /// Active Directory User Principal
        /// </summary>
        internal dynamic UserPrincipal = null;

        /// <summary>
        /// UserPrincipal container in Session
        /// </summary>
        const string UserPrincipal_Container = "UserPrincipal";

        /// <summary>
        /// Network Identity from IIS authentication
        /// </summary>
        internal string NetworkIdentity = null;

        /// <summary>
        /// Network Username from IIS authentication
        /// </summary>
        internal string NetworkUsername = null;


        /// <summary>
        /// HTTP POST Request
        /// </summary>
        internal string httpPOST = null;


        /// <summary>
        /// HTTP GET Request
        /// </summary>
        internal NameValueCollection httpGET = null;

        /// <summary>
        /// last time a request was sent for possible performance monitoring
        /// </summary>
        internal static DateTime lastRequestTime;


        /// <summary>
        /// number of requests
        /// </summary>
        internal static int requestCount = 0;

        public Common()
        {
        }

        /// <summary>
        /// Authenticate the user in the context
        /// </summary>
        /// <param name="context"></param>
        internal bool? Authenticate(ref HttpContext context)
        {

            bool? isAuthenticated = null;

            Log.Instance.Info("Stateless: " + ApiServicesHelper.ApiConfiguration.Settings["API_STATELESS"]);

            // Get the username if any
            if (context.User.Identity.IsAuthenticated)
            {
                // IIS Windows Authentication enabled
                NetworkIdentity = context.User.Identity.Name;
                NetworkUsername = context.User.Identity.Name.Split('\\')[1];
            }
            else
            {
                // IIS Anonymous Authentication enabled
                NetworkIdentity = null;
                NetworkUsername = null;
                Log.Instance.Info("User.Identity not authenticated");
            }

            //Log.Instance.Info("Network Identity: " + NetworkIdentity);
            //Log.Instance.Info("Network Username: " + NetworkUsername);
            //Log.Instance.Info("AD Domain: " + ApiServicesHelper.ApiConfiguration.Settings["API_AD_DOMAIN"]);
            //Log.Instance.Info("AD Username: " + ApiServicesHelper.ApiConfiguration.Settings["API_AD_USERNAME"]);
            //Log.Instance.Info("AD Password: ********"); // Hide API_AD_PASSWORD from logs

            // Check if the request is Stateless
            if (Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_STATELESS"]))
            {
                // Check for the cached userPrincipal
                MemCachedD_Value userPricipalCache = ApiServicesHelper.CacheD.Get_BSO<dynamic>("API", "Common", "Authenticate", NetworkIdentity);
                if (userPricipalCache.hasData)
                {
                    isAuthenticated = true;

                    UserPrincipal = userPricipalCache.data == null ? null : userPricipalCache.data;
                    Log.Instance.Info("Authentication retrieved from Cache");
                }
                else
                {
                    isAuthenticated = AuthenticateByType();

                    // Store the cache only when authentication works.
                    if (isAuthenticated != null)
                    {
                        // Set the cache to expire at midnight
                        if (ApiServicesHelper.CacheD.Store_BSO<dynamic>("API", "Common", "Authenticate", NetworkIdentity, UserPrincipal, DateTime.Today.AddDays(1)))
                        {
                            Log.Instance.Info("Authentication stored in Cache");
                        }
                    }
                }
            }
            else
            {
                // Check if a new Session has been instantiated
                if (context.Session.IsAvailable)
                {
                    // Call the SessionID to initiate the Session
                    Log.Instance.Info("Session ID: " + context.Session.Id);

                    isAuthenticated = AuthenticateByType();

                    // Initiate a new Session only when authentication works.
                    if (isAuthenticated != null)
                    {
                        // Save the serialized userPrincipal in the Session
                        //context.Session[UserPrincipal_Container] = Utility.JsonSerialize_IgnoreLoopingReference(UserPrincipal);
                        Log.Instance.Info("Authentication stored in Session");
                    }
                }
                else
                {
                    isAuthenticated = true;

                    // Call the SessionID to initiate the Session
                    Log.Instance.Info("Session ID: " + context.Session.Id);

                    // Deserialise userPrincipal from Session
                    //UserPrincipal = Utility.JsonDeserialize_IgnoreLoopingReference((string)(context.Session[UserPrincipal_Container]));
                    Log.Instance.Info("Authentication retrieved from Session");
                }
            }

            // Log userPrincipal
            Log.Instance.Info("User Principal: " + MaskParameters(Utility.JsonSerialize_IgnoreLoopingReference(UserPrincipal)));
            return isAuthenticated;
        }


        /// <summary>
        /// Authenticate the user by the relative Authentication Type
        /// </summary>
        private bool? AuthenticateByType()
        {
            string[] AuthenticationTypeAllowed = new string[]
            {
                AUTHENTICATION_TYPE_WINDOWS,
                AUTHENTICATION_TYPE_ANONYMOUS,
                AUTHENTICATION_TYPE_ANY
            };

            Log.Instance.Info("Authentication Types Allowed: " + Utility.JsonSerialize_IgnoreLoopingReference(AuthenticationTypeAllowed));
            Log.Instance.Info("Authentication Type Selected: " + ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"]);

            // Validate the Authentication type
            if (!AuthenticationTypeAllowed.Contains(ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"]))
            {
                Log.Instance.Fatal("Invalid Authentication Type: " + ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"]);
                return false;
            }

            switch (ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"])
            {
                case AUTHENTICATION_TYPE_ANY:
                    // Process the Any Authentication
                    return AnyAuthentication();
                case AUTHENTICATION_TYPE_WINDOWS:
                    // Process the Windows Authentication
                    return WindowsAuthentication();
                case AUTHENTICATION_TYPE_ANONYMOUS:
                default:
                    // Process the Windows Authentication
                    return AnonymousAuthentication();
            }
        }
        /// <summary>
        /// Process Windows Authentication
        /// </summary>
        private bool? WindowsAuthentication()
        {
            // Override userPrincipal for security
            UserPrincipal = null;

            // Check the username exists
            if (string.IsNullOrEmpty(NetworkUsername))
            {
                Log.Instance.Fatal("Undefined Network Username");
                return false;
            }

            if (string.IsNullOrEmpty(ApiServicesHelper.ApiConfiguration.Settings["API_AD_DOMAIN"]))
            {
                Log.Instance.Fatal("Undefined AD Domain");
                return false;
            }

            try
            {
                // Query AD
                PrincipalContext domainContext = ApiServicesHelper.ActiveDirectory.adContext;

                UserPrincipal = API_UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, NetworkUsername);
                if (UserPrincipal == null)
                {
                    Log.Instance.Fatal("Undefined User Principal against AD");
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Instance.Fatal("Unable to connect/query AD");
                Log.Instance.Fatal(e);
                return false;
            }
        }

        /// <summary>
        /// Process Anonymous Authentication
        /// </summary>
        private bool? AnonymousAuthentication()
        {
            // Override userPrincipal for security
            UserPrincipal = null;
            return null;
        }

        /// <summary>
        /// Process Any Authentication
        /// </summary>
        private bool? AnyAuthentication()
        {
            // Override userPrincipal for security
            UserPrincipal = null;

            // Check the username exists agains the domain
            if (!string.IsNullOrEmpty(NetworkUsername) && !string.IsNullOrEmpty(ApiServicesHelper.ApiConfiguration.Settings["API_AD_DOMAIN"]))
            {


                try
                {
                    // Query AD
                    PrincipalContext domainContext = ApiServicesHelper.ActiveDirectory.adContext;

                    // Query AD and get the logged username
                    UserPrincipal = API_UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, NetworkUsername);
                    if (UserPrincipal == null)
                    {
                        Log.Instance.Fatal("Undefined User Principal against AD");
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    Log.Instance.Fatal("Unable to connect/query AD");
                    Log.Instance.Fatal(e);
                    return false;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the HTTP request for the GET method
        /// </summary>
        /// <returns></returns>
        internal static NameValueCollection GetHttpGET(HttpContext httpContext)
        {
            try
            {
                // Read the request from GET 
                return HttpUtility.ParseQueryString(httpContext.Request.QueryString.Value);
            }
            catch (Exception e)
            {
                Log.Instance.Info(e);
                return null;
            }
        }


        /// <summary>
        /// Get the HTTP request for the POST method
        /// </summary>
        /// <returns></returns>
        internal async Task<string> GetHttpPOST(HttpContext httpContext)
        {
            try
            {
                // Read the request from POST

                //https://stackoverflow.com/questions/43403941/how-to-read-asp-net-core-response-body
                string body;
                using (var streamReader = new System.IO.StreamReader(
                    httpContext.Request.Body, System.Text.Encoding.UTF8, leaveOpen: true))
                    body = await streamReader.ReadToEndAsync();

                httpContext.Request.Body.Position = 0;

                return body;// httpContext.Request.Form.Keys.FirstOrDefault();
                //new StreamReader(httpContext.Request.Body).ReadToEnd();
            }
            catch (Exception e)
            {
                Log.Instance.Info(e);
                return null;
            }
        }

        public ExpandoObject UserPrincipalForLogging(dynamic up)
        {
            dynamic UserPrincipalLog = new ExpandoObject();
            UserPrincipalLog.SamAccountName = up.SamAccountName;
            return UserPrincipalLog;
        }

        /// <summary>
        /// Mask an input password
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string MaskParameters(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "";
            }
            // Init the output
            string output = input;

            // Loop trough the parameters to mask API_MASK_PARAMETERS
            foreach (var param in ApiServicesHelper.ApiConfiguration.Settings["API_MASK_PARAMETERS"].Split(','))
            {
                // https://stackoverflow.com/questions/171480/regex-grabbing-values-between-quotation-marks
                Log.Instance.Info("Masked parameter: " + param);
                output = Regex.Replace(output, "\"" + param + "\"\\s*:\\s*\"(.*?[^\\\\])\"", "\"" + param + "\": \"********\"", RegexOptions.IgnoreCase);
            }

            return output;
        }

        /// <summary>
        /// manage responses to client
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <param name="sourceToken"></param>     
        /// <param name="statusCode"></param>
        /// <param name="isFile"></param>
        internal async Task returnResponseAsync(HttpContext context, string message, CancellationTokenSource sourceToken, HttpStatusCode statusCode, bool isFile = false)
        {
            Log.Instance.Info("Returning response");
            //check if already cancelled
            if (sourceToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            if (context.Response.HasStarted) {
                sourceToken.Cancel(true);

                if (sourceToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }
            else
            {
                context.Response.StatusCode = (int)statusCode;
                if (isFile)
                {
                    await context.Response.SendFileAsync(message);
                }
                else
                {
                    await context.Response.WriteAsync(message);
                }
                await context.Response.CompleteAsync();
                sourceToken.Cancel(true);

                if (sourceToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }
        }

        /// <summary>
        /// Determines if cookie is not empty and adds name and value
        /// </summary>
        /// <param name="httpContext"></param>
        internal Cookie CheckCookie(string SessionCookieName, HttpContext httpContext)
        {
            //add a cookie for testing
            //httpContext.Request.Headers.Add("Cookie", "session=\"84c2f0b319460ee991924908198d46795049c83f1ebdfcaf90bd899c8d9d0bd2\";");        

            Cookie sessionCookie = new Cookie();
                
            if (!string.IsNullOrEmpty(SessionCookieName))
                {
                    //need to create a cookie using the value and  the SessionCookieName
                    string testSessionCookieValue = httpContext.Request.Cookies[SessionCookieName];

                    if (!string.IsNullOrEmpty(testSessionCookieValue))
                    {
                        sessionCookie.Name = SessionCookieName;
                        sessionCookie.Value = testSessionCookieValue;
                    }
            }
            return sessionCookie;
        }
    }

    /// <summary>
    /// Clone the UserPrincipal object structure for serialisation & deserialisation
    /// This is required because of recursive loop
    /// </summary>
    public class API_UserPrincipal : UserPrincipal
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public API_UserPrincipal(PrincipalContext context) : base(context) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="samAccountName"></param>
        /// <param name="password"></param>
        /// <param name="enabled"></param>
        [JsonConstructor]
        public API_UserPrincipal(PrincipalContext context, string samAccountName, string password, bool enabled) : base(context, samAccountName, password, enabled) { }
    }
}
