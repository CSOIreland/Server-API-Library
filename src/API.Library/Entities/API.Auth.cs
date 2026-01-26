using Microsoft.AspNetCore.Http;
using System.DirectoryServices.AccountManagement;
using System.Dynamic;

namespace API
{
    /// <summary>
    /// Static implementation of the API Constants
    /// </summary>
    public class Auth
    {
        /// <summary>
        /// Network Identity from IIS authentication
        /// </summary>
        internal string NetworkIdentity = null;

        /// <summary>
        /// Network Username from IIS authentication
        /// </summary>
        internal string NetworkUsername = null;

        public Auth()
        {
        }

        /// <summary>
        /// Authenticate the user in the context
        /// </summary>
        /// <param name="context"></param>
        internal bool? Authenticate(ref HttpContext context, ref UserDetail userDetail, CancellationTokenSource apiCancellationToken)
        {
            /// <summary>
            /// Active Directory User Principal
            /// </summary>
            UserPrincipal _UserPrincipal = null;

            string? token = null;
            bool? isAuthenticated = null;
            string authType = ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"];


            Log.Instance.Info("Stateless: " + ApiServicesHelper.ApiConfiguration.Settings["API_STATELESS"]);

            if (!authType.Equals(APIConstants.AUTHENTICATION_TYPE_ENTRAID) || !authType.Equals(APIConstants.AUTHENTICATION_TYPE_ANY_ENTRAID))
            { 
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
            }



           if (authType.Equals(APIConstants.AUTHENTICATION_TYPE_ENTRAID) || authType.Equals(APIConstants.AUTHENTICATION_TYPE_ANY_ENTRAID))
           {
                if (context.Request.Headers.ContainsKey("MSAL"))
                {
                    token = ApiServicesHelper.EntraTokenAuthentication.GetToken(context);
                }
                else
                {
                    if (authType.Equals(APIConstants.AUTHENTICATION_TYPE_ENTRAID)) return false;
                    return null;
                }
                
            }


            // Check if the request is Stateless
            if (Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_STATELESS"]))
            {
                // Check for the cached userPrincipal
                MemCachedD_Value userPricipalCache = new MemCachedD_Value();

                if (!authType.Equals(APIConstants.AUTHENTICATION_TYPE_ENTRAID) || authType.Equals(APIConstants.AUTHENTICATION_TYPE_ANY_ENTRAID))
                {
                    userPricipalCache = ApiServicesHelper.CacheD.Get_BSO<dynamic>("API", "Common", "Authenticate", NetworkIdentity);
                }

                //ENTRA ID IS NOT CACHED
                if (userPricipalCache.hasData)
                {
                    isAuthenticated = true;

                    UserDetail userPricipalCacheDeserialized = Utility.JsonDeserialize_IgnoreLoopingReference<UserDetail>(userPricipalCache.data.ToString());
                    userDetail = userPricipalCacheDeserialized == null ? null : userPricipalCacheDeserialized;
    
                    Log.Instance.Info("Authentication retrieved from Cache");
                }
                else
                {

                        
                    isAuthenticated = AuthenticateByType(token, ref _UserPrincipal);

                    // Store the cache only when authentication works.
                    if (isAuthenticated == true)//!= null)
                    {
                        if (authType.Equals(APIConstants.AUTHENTICATION_TYPE_ENTRAID) || authType.Equals(APIConstants.AUTHENTICATION_TYPE_ANY_ENTRAID))
                        {
                            //get the identity from the token
                            Dictionary<string, string> identityData = ApiServicesHelper.EntraTokenAuthentication.GetClaimData(token);
                            if (identityData != null)
                            {

                                //check if identiy is null
                               // string identity = identityData.ContainsKey(APIConstants.API_IDENTITY) ? identityData[APIConstants.API_IDENTITY] : null;



                                string? identity = identityData.ContainsKey(APIConstants.API_IDENTITY)
                                    ? identityData[APIConstants.API_IDENTITY]
                                    : identityData.ContainsKey(APIConstants.API_IDENTITY_EXTERNAL_SSO_AUTHENTICATION)
                                        ? identityData[APIConstants.API_IDENTITY_EXTERNAL_SSO_AUTHENTICATION]
                                        : null;
                                
                                if (!string.IsNullOrEmpty(identity))
                                {

                                    //set asynclocal value of userIdentity to be persisted for tracing
                                    APIMiddleware.userIdentity.Value = identity;


                                    var additionalProperties = new ExpandoObject() as IDictionary<string, object>;

                                    //// Add the custom properties to the additionalProperties if any
                                    foreach (string property in ApiServicesHelper.ApiConfiguration.Settings["API_ENTRAID_CUSTOM_CLAIMS"].Split(','))
                                    {
                                        if (identityData.ContainsKey(property))
                                        {
                                            additionalProperties.Add(property, identityData[property]);
                                        }
                                    }

                                    userDetail = new UserDetail(
                                        identity,
                                        null, //samaccountname not an entra standard attribute
                                        identityData.ContainsKey("upn") ? identityData["upn"] : null,
                                        null, //DisplayName not an entra standard attribute
                                        identityData.ContainsKey("name") ? identityData["name"] : null,
                                        identityData.ContainsKey("Name") ? identityData["name"] : null,
                                        identityData.ContainsKey("given_name") ? identityData["given_name"] : null,
                                        null, //middlename not an entra standard attribute
                                        identityData.ContainsKey("family_name") ? identityData["family_name"] : null,
                                        identityData.ContainsKey("email") ? identityData["email"] : null,
                                        identityData.ContainsKey("employeeid") ? identityData["employeeid"] : null,
                                        null,//"Description  not an entra standard attribute
                                        null, //enabled
                                        identityData.ContainsKey("phone_number") ? identityData["phone_number"] : null,
                                        identityData.ContainsKey("job_title") ? identityData["job_title"] : null,
                                        identityData.ContainsKey("manager") ? identityData["manager"] : null,
                                        additionalProperties
                                     );

                                }
                                else
                                {
                                    Log.Instance.Error("Users identity is null");
                                }

                            }
                        }
                        else
                        {

                            //set asynclocal value of userIdentity to be persisted for tracing
                            APIMiddleware.userIdentity.Value = _UserPrincipal.SamAccountName;

                            var samaccountname = _UserPrincipal.SamAccountName;
                            //set userprincipal to be a smaller object
                            Dictionary<string, UserDetail> userPrincipal = ApiServicesHelper.ActiveDirectory.CreateAPIUserPrincipalObject(_UserPrincipal);

                            //get the specific user
                            userDetail = userPrincipal[samaccountname];

                            // Set the cache to expire at midnight
                            if (ApiServicesHelper.CacheD.Store_BSO<dynamic>("API", "Common", "Authenticate", NetworkIdentity, Utility.JsonSerialize_IgnoreLoopingReference(userDetail), DateTime.Today.AddDays(1)))
                            {
                                Log.Instance.Info("Authentication stored in Cache");
                            }
                        }

                    }
                    else
                    {
                        //if authentication failed but we also allow anonymous, let the request proceed with null user for further authorisation by the app
                        if (authType.Equals(APIConstants.AUTHENTICATION_TYPE_ANY_ENTRAID) || authType.Equals(APIConstants.AUTHENTICATION_TYPE_ANY_WINDOWS ))
                            isAuthenticated = null;
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

                    var sessionUser = context.Session.GetString(APIConstants.UserPrincipal_Container);
                    if (sessionUser == null)
                    {
                        isAuthenticated = AuthenticateByType(token,ref _UserPrincipal);

                        // Initiate a new Session only when authentication works.
                        if (isAuthenticated.HasValue && !isAuthenticated.Value)
                        {
                            // Save the serialized userPrincipal in the Session

                            //set userprincipal to be a smaller object
                            Dictionary<string, UserDetail> userPrincipal = ApiServicesHelper.ActiveDirectory.CreateAPIUserPrincipalObject(_UserPrincipal);
                            string upString = Utility.JsonSerialize_IgnoreLoopingReference(userPrincipal);
                            context.Session.SetString(APIConstants.UserPrincipal_Container, upString);
                            Log.Instance.Info("Authentication stored in Session");
                        }
                    }
                    else
                    {
                        isAuthenticated = true;

                        // Call the SessionID to initiate the Session
                        Log.Instance.Info("Session ID: " + context.Session.Id);
                        // Deserialise userPrincipal from Session
                        _UserPrincipal = Utility.JsonDeserialize_IgnoreLoopingReference<UserPrincipal>(context.Session.GetString(APIConstants.UserPrincipal_Container), new PrincipalContext(ContextType.Domain));
                        Log.Instance.Info("Authentication retrieved from Session");
                    }

                }
            }

            // Log userPrincipal
            Common common = new Common();
            Log.Instance.Info("User Principal: " + common.MaskParameters(Utility.JsonSerialize_IgnoreLoopingReference(userDetail)));
            return isAuthenticated;
        }


        /// <summary>
        /// Authenticate the user by the relative Authentication Type
        /// </summary>
        private bool? AuthenticateByType(string? token, ref UserPrincipal? userPrincipal)
        {
            //override for security
            userPrincipal = null;

            string[] AuthenticationTypeAllowed = new string[]
            {
               APIConstants.AUTHENTICATION_TYPE_WINDOWS,
               APIConstants.AUTHENTICATION_TYPE_ANONYMOUS,
               APIConstants.AUTHENTICATION_TYPE_ANY_WINDOWS,
               APIConstants.AUTHENTICATION_TYPE_ENTRAID,
               APIConstants.AUTHENTICATION_TYPE_ANY_ENTRAID
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
                case APIConstants.AUTHENTICATION_TYPE_ANY_WINDOWS:
                    // Process the Any Authentication
                    return AnyWindowsAuthentication(ref userPrincipal);
                case APIConstants.AUTHENTICATION_TYPE_WINDOWS:
                    // Process the Windows Authentication
                    return WindowsAuthentication(ref userPrincipal);
                case APIConstants.AUTHENTICATION_TYPE_ENTRAID:
                    // Process the Entra ID Authentication
                    return EntraIDAuthentication(token);
                case APIConstants.AUTHENTICATION_TYPE_ANY_ENTRAID:
                    // Process the Entra ID Authentication
                    return AnyEntraIDAuthentication(token);
                case APIConstants.AUTHENTICATION_TYPE_ANONYMOUS:
                default:
                    // Process the Windows Authentication
                    return AnonymousAuthentication(ref userPrincipal);
            }
        }

        private bool? EntraIDAuthentication(string? token)
        {
            string authType = ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"];

            if (!ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"].Equals(APIConstants.AUTHENTICATION_TYPE_ENTRAID))
            {
                Log.Instance.Fatal("Entra ID not enabled for authentication");
                return null;
            }


            Log.Instance.Info("Entra ID Authentication");
            if (token == null)
            {
                Log.Instance.Info("Token is missing");
                return false;
            }

            var isValid = ApiServicesHelper.EntraTokenAuthentication.ValidateToken(token);

            if (!isValid)
            {
                Log.Instance.Info("Token is invalid");
                return false;
            }

            return isValid;
        }

        /// <summary>
        /// Process Windows Authentication
        /// </summary>
        private bool? WindowsAuthentication(ref UserPrincipal userPrincipal)
        {
            // Override userPrincipal for security
            userPrincipal = null;

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

                userPrincipal = API_UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, NetworkUsername);
                if (userPrincipal == null)
                {
                    Log.Instance.Fatal("Undefined User Principal against AD");
                    return false;
                }
                else
                {
                    //if account is enabled
                    if (userPrincipal.Enabled ?? false)
                    {
                        return true;
                    }
                    else
                    {
                        Log.Instance.Info("User Principal account not enabled for : " + userPrincipal.SamAccountName);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Instance.Fatal("Unable to connect/query AD");
                Log.Instance.Fatal("NetworkUsername :" + NetworkUsername);
                Log.Instance.Fatal(e);
                return false;
            }
        }

        /// <summary>
        /// Process Anonymous Authentication
        /// </summary>
        private bool? AnonymousAuthentication(ref UserPrincipal userPrincipal)
        {
            // Override userPrincipal for security
            userPrincipal = null;
            return null;
        }

        /// <summary>
        /// Process Any Authentication
        /// </summary>
        private bool? AnyWindowsAuthentication(ref UserPrincipal UserPrincipal)
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
                    Log.Instance.Fatal("NetworkUsername :" + NetworkUsername);
                    Log.Instance.Fatal(e);
                    return false;
                }
            }

            return null;
        }

        private bool? AnyEntraIDAuthentication(string? token)
        {
            if (token == null)
            {
                Log.Instance.Info("Token is missing");
                return null;
            }

            string authType = ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"];

            if (!ApiServicesHelper.ApiConfiguration.Settings["API_AUTHENTICATION_TYPE"].Equals(APIConstants.AUTHENTICATION_TYPE_ANY_ENTRAID))
            {
                Log.Instance.Fatal("Entra ID not enabled for authentication");
                return null;
            }

            Log.Instance.Info("Entra ID Authentication");
            var isValid = ApiServicesHelper.EntraTokenAuthentication.ValidateToken(token);

            if (!isValid)
            {
                Log.Instance.Info("Token is invalid");
                return false;
            }

            return isValid;
        }

        /// <summary>
        /// Checks if the user is authenticated
        /// </summary>
        /// <param name="userDetail"></param>
        /// <returns></returns>
        public static bool IsAuthenticated(string identity)
        {
            if (identity == null || String.IsNullOrEmpty(identity))
                return false;
            else
                return true;
        }
    }
}