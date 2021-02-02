using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Web;

namespace API
{

    /// <summary>
    /// Static implementation of the API Constants
    /// </summary>
    public class Common
    {
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
        /// Maintenance flag
        /// </summary>
        internal static bool Maintenance = Convert.ToBoolean(ConfigurationManager.AppSettings["API_MAINTENANCE"]);

        /// <summary>
        /// Success response (case sensitive)
        /// </summary>
        public static string success = ConfigurationManager.AppSettings["API_SUCCESS"];

        /// <summary>
        /// Authentication type
        /// </summary>
        internal static string API_AUTHENTICATION_TYPE = ConfigurationManager.AppSettings["API_AUTHENTICATION_TYPE"];

        /// <summary>
        /// Stateless flag
        /// </summary>
        internal static bool API_STATELESS = Convert.ToBoolean(ConfigurationManager.AppSettings["API_STATELESS"]);

        /// <summary>
        /// Active Directory Domain
        /// </summary>
        internal static string API_AD_DOMAIN = ConfigurationManager.AppSettings["API_AD_DOMAIN"];

        /// <summary>
        /// Active Directory Username for Quering 
        /// </summary>
        internal static string API_AD_USERNAME = ConfigurationManager.AppSettings["API_AD_USERNAME"];

        /// <summary>
        /// Active Directory Password for Querying 
        /// </summary>
        internal static string API_AD_PASSWORD = ConfigurationManager.AppSettings["API_AD_PASSWORD"];


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
        /// HTTP GET Request
        /// </summary>
        internal NameValueCollection httpGET = null;

        /// <summary>
        /// HTTP POST Request
        /// </summary>
        internal string httpPOST = null;
        /// <summary>
        /// Session cookie
        /// </summary>
        public static string SessionCookieName = ConfigurationManager.AppSettings["API_SESSION_COOKIE"];

        /// <summary>
        /// Authenticate the user in the context
        /// </summary>
        /// <param name="context"></param>
        internal bool? Authenticate(ref HttpContext context)
        {
            bool? isAuthenticated = null;
            Log.Instance.Info("Stateless: " + API_STATELESS);

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
            }

            Log.Instance.Info("Network Identity: " + NetworkIdentity);
            Log.Instance.Info("Network Username: " + NetworkUsername);
            Log.Instance.Info("AD Domain: " + API_AD_DOMAIN);
            Log.Instance.Info("AD Username: " + API_AD_USERNAME);
            Log.Instance.Info("AD Password: ********"); // Hide API_AD_PASSWORD from logs

            // Check if the request is Stateless
            if (API_STATELESS)
            {
                // Check for the cached userPrincipal
                MemCachedD_Value userPricipalCache = MemCacheD.Get_BSO<dynamic>("API", "Common", "Authenticate", NetworkIdentity);
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
                        if (MemCacheD.Store_BSO<dynamic>("API", "Common", "Authenticate", NetworkIdentity, UserPrincipal, DateTime.Today.AddDays(1)))
                        {
                            Log.Instance.Info("Authentication stored in Cache");
                        }
                    }
                }
            }
            else
            {
                // Check if a new Session has been instantiated
                if (context.Session.IsNewSession)
                {
                    // Call the SessionID to initiate the Session
                    Log.Instance.Info("Session ID: " + context.Session.SessionID);

                    isAuthenticated = AuthenticateByType();

                    // Initiate a new Session only when authentication works.
                    if (isAuthenticated != null)
                    {
                        // Save the serialized userPrincipal in the Session
                        context.Session[UserPrincipal_Container] = Utility.JsonSerialize_IgnoreLoopingReference(UserPrincipal);
                        Log.Instance.Info("Authentication stored in Session");
                    }
                }
                else
                {
                    isAuthenticated = true;

                    // Call the SessionID to initiate the Session
                    Log.Instance.Info("Session ID: " + context.Session.SessionID);

                    // Deserialise userPrincipal from Session
                    UserPrincipal = Utility.JsonDeserialize_IgnoreLoopingReference((string)(context.Session[UserPrincipal_Container]));
                    Log.Instance.Info("Authentication retrieved from Session");
                }
            }

            // Log userPrincipal
            Log.Instance.Info("User Principal: " + Utility.JsonSerialize_IgnoreLoopingReference(UserPrincipal));
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
            Log.Instance.Info("Authentication Type Selected: " + API_AUTHENTICATION_TYPE);

            // Validate the Authentication type
            if (!AuthenticationTypeAllowed.Contains(API_AUTHENTICATION_TYPE))
            {
                Log.Instance.Fatal("Invalid Authentication Type: " + API_AUTHENTICATION_TYPE);
                return false;
            }

            switch (API_AUTHENTICATION_TYPE)
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

            if (String.IsNullOrEmpty(API_AD_DOMAIN))
            {
                Log.Instance.Fatal("Undefined AD Domain");
                return false;
            }

            // Query AD
            PrincipalContext domainContext = null;

            try
            {
                if (!String.IsNullOrEmpty(API_AD_USERNAME) && !String.IsNullOrEmpty(API_AD_PASSWORD))
                {
                    // Define the Domain against the Principal
                    domainContext = new PrincipalContext(ContextType.Domain, API_AD_DOMAIN, API_AD_USERNAME, API_AD_PASSWORD);
                }
                else
                {
                    // Define the Domain against the Principal
                    domainContext = new PrincipalContext(ContextType.Domain, API_AD_DOMAIN);
                }

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
            if (!string.IsNullOrEmpty(NetworkUsername) && !String.IsNullOrEmpty(API_AD_DOMAIN))
            {
                // Query AD
                PrincipalContext domainContext = null;

                try
                {
                    if (!String.IsNullOrEmpty(API_AD_USERNAME) && !String.IsNullOrEmpty(API_AD_PASSWORD))
                    {
                        // Define the Domain against the Principal
                        domainContext = new PrincipalContext(ContextType.Domain, API_AD_DOMAIN, API_AD_USERNAME, API_AD_PASSWORD);
                    }
                    else
                    {
                        // Define the Domain against the Principal
                        domainContext = new PrincipalContext(ContextType.Domain, API_AD_DOMAIN);
                    }

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
        internal static NameValueCollection GetHttpGET()
        {
            try
            {
                // Read the request from GET 
                return HttpContext.Current.Request.QueryString;
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
        internal static string GetHttpPOST()
        {
            try
            {
                // Read the request from POST
                return new StreamReader(HttpContext.Current.Request.InputStream).ReadToEnd();
            }
            catch (Exception e)
            {
                Log.Instance.Info(e);
                return null;
            }
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
