using System;
using System.Web;
using System.Web.SessionState;
using System.Reflection;
using System.IO;
using System.DirectoryServices.AccountManagement;
using System.Configuration;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace API
{
    /// <summary>
    /// JSON-RPC listener following specifications at http://www.jsonrpc.org/specification
    /// </summary>
    public class JSONRPC : IHttpHandler, IRequiresSessionState
    {
        #region Properties
        /// <summary>
        /// JSON RPC version in use
        /// </summary>
        private static string JSONRPC_Version = "2.0";

        /// <summary>
        ///  GET request parameter
        /// </summary>
        private static string JSONRPC_Container = "data";

        /// <summary>
        /// Active Directory userPrincipal parameter
        /// </summary>
        private static string JSONRPC_userPrincipal = "userPrincipal";

        /// <summary>
        /// Active Directory userPrincipal
        /// </summary>
        private dynamic userPrincipal = null;

        /// <summary>
        /// Successfull response (case sensitive)
        /// </summary>
        public static string success = ConfigurationManager.AppSettings["API_JSONRPC_SUCCESS"];

        /// <summary>
        /// Active Directory Domain
        /// </summary>
        private static string API_AD_DOMAIN = ConfigurationManager.AppSettings["API_AD_DOMAIN"];

        /// <summary>
        /// Active Directory Username for Quering 
        /// </summary>
        private static string API_AD_USERNAME = ConfigurationManager.AppSettings["API_AD_USERNAME"];

        /// <summary>
        /// Active Directory Password for Querying 
        /// </summary>
        private static string API_AD_PASSWORD = ConfigurationManager.AppSettings["API_AD_PASSWORD"];

        /// <summary>
        /// Authentication type
        /// </summary>
        private static string API_JSONRPC_AUTHENTICATION_TYPE = ConfigurationManager.AppSettings["API_JSONRPC_AUTHENTICATION_TYPE"];

        /// <summary>
        /// Windows Authentication constant
        /// </summary>
        private static string AUTHENTICATION_TYPE_WINDOWS = "WINDOWS";

        /// <summary>
        /// Anonymous Authentication constant
        /// </summary>
        private static string AUTHENTICATION_TYPE_ANONYMOUS = "ANONYMOUS";

        /// <summary>
        /// Any Authentication constant
        /// </summary>
        private static string AUTHENTICATION_TYPE_ANY = "ANY";

        /// <summary>
        /// Authenticaiton Types allowed
        /// </summary>
        private string[] AUTHENTICATION_TYPE_ALLOWED = new string[]
        {
            AUTHENTICATION_TYPE_WINDOWS,
            AUTHENTICATION_TYPE_ANONYMOUS,
            AUTHENTICATION_TYPE_ANY
        };

        /// <summary>
        /// Network Identity from IIS authentication
        /// </summary>
        private string networkIdentity = null;

        /// <summary>
        /// Network Username from IIS authentication
        /// </summary>
        private string networkUsername = null;
        #endregion

        #region Methods
        /// <summary>
        /// ProcessRequest executed automatically by the iHttpHandler interface
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            Log.Instance.Info("API Interface Opened");

            // Deserialize and parse the JSON request into an Object dynamically
            JSONRPC_Request JSONRPC_Request = this.ParseRequest(ref context);

            // Authenticate and append credentials to the JSON request
            this.Authenticate(ref context, ref JSONRPC_Request);

            // Get results from the relevant method with the params
            dynamic result = GetResult(ref context, JSONRPC_Request);
            if (result == null)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32603 };
                this.ParseError(ref context, JSONRPC_Request.id.ToString(), error);
            }
            else if (result.error != null)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32099, message = result.error };
                this.ParseError(ref context, JSONRPC_Request.id.ToString(), error);
            }
            else
            {
                // Check if the request.data is already a JSON type casted as: new JRaw(data); 
                var jsonRaw = result.data as JRaw;
                if (jsonRaw != null)
                {
                    JSONRPC_API_ResponseDataJRaw output = new JSONRPC_API_ResponseDataJRaw
                    {
                        jsonrpc = JSONRPC_Request.jsonrpc,
                        data = jsonRaw,
                        id = JSONRPC_Request.id
                    };
                    // Output the JSON-RPC repsonse with JRaw data
                    context.Response.Write(JsonConvert.SerializeObject(output));
                }
                else
                {
                    JSONRPC_API_ResponseData output = new JSONRPC_API_ResponseData
                    {
                        jsonrpc = JSONRPC_Request.jsonrpc,
                        data = result.data,
                        id = JSONRPC_Request.id
                    };
                    // Output the JSON-RPC repsonse
                    context.Response.Write(JsonConvert.SerializeObject(output));
                }
            }

            Log.Instance.Info("API Interface Closed");
        }

        /// <summary> 
        /// Parse the API error
        /// From -32000 to -32098 reserved for implementation-defined errors.
        /// -32099 reserved for application errors
        /// </summary>
        ///
        /// <param name="response"></param>
        /// <param name="id"></param>
        /// <param name="error"></param>
        private void ParseError(ref HttpContext context, string id, JSONRPC_Error error)
        {
            if (error.message == null)
                switch (error.code.ToString())
                {
                    // Custom codes and messages
                    case "-32000":
                        error.message = "Invalid version";
                        break;
                    case "-32002":
                        error.message = "Invalid authentication";
                        break;

                    // Standard codes and messages
                    case "-32700":
                        error.message = "Parse error";
                        break;
                    case "-32600":
                        error.message = "Invalid request";
                        break;
                    case "-32601":
                        error.message = "Method not found";
                        break;
                    case "-32602":
                        error.message = "Invalid params";
                        break;
                    case "-32603":
                    default:
                        error.code = "-32603";
                        error.message = "Internal error";
                        break;
                }

            Log.Instance.Info("IP: " + Utility.IpAddress + ", Error Code: " + error.code + ", Error Message: " + error.message);
            object output = new JSONRPC_ResponseError { jsonrpc = JSONRPC_Version, error = error, id = id };
            context.Response.Write(JsonConvert.SerializeObject(output));
            context.Response.End();
        }

        /// <summary>
        /// Parse and validate the request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private JSONRPC_Request ParseRequest(ref HttpContext context)
        {
            // Initialise requests
            string request = null;
            string requestGET = null;
            string requestPOST = null;

            try
            {
                // Read the request from GET 
                requestGET = context.Request.QueryString[JSONRPC_Container];
                Log.Instance.Info("GET request: " + requestGET);

                // Read the request from POST
                StreamReader HttpReader = new StreamReader(context.Request.InputStream);
                requestPOST = HttpReader.ReadToEnd();
                Log.Instance.Info("POST request: " + requestPOST);
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);

                JSONRPC_Error error = new JSONRPC_Error { code = -32700 };
                this.ParseError(ref context, null, error);
            }

            // Check the query input exists
            if (string.IsNullOrWhiteSpace(requestGET)
            && string.IsNullOrWhiteSpace(requestPOST))
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32700 };
                this.ParseError(ref context, null, error);
            }

            // POST request overrides GET one
            if (!string.IsNullOrWhiteSpace(requestPOST))
            {
                request = requestPOST;
            }
            else
            {
                request = requestGET;
            }

            JSONRPC_Request JSONRPC_Request = new JSONRPC_Request();

            try
            {
                // Deserialize JSON to an Object dynamically
                JSONRPC_Request = JsonConvert.DeserializeObject<JSONRPC_Request>(request);
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);

                JSONRPC_Error error = new JSONRPC_Error { code = -32700 };
                this.ParseError(ref context, null, error);
            }

            // Validate the request
            if (JSONRPC_Request.id == null)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32600 };
                this.ParseError(ref context, null, error);
            }

            // Validate the request
            if (JSONRPC_Request.jsonrpc == null
            || JSONRPC_Request.method == null)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32600 };
                this.ParseError(ref context, JSONRPC_Request.id.ToString(), error);
            }

            // Verify the version is right 
            if (JSONRPC_Request.jsonrpc.ToString() != JSONRPC_Version)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32000 };
                this.ParseError(ref context, JSONRPC_Request.id.ToString(), error);
            }

            // Verify the method exists
            if (!ValidateMethod(JSONRPC_Request))
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32601 };
                this.ParseError(ref context, JSONRPC_Request.id.ToString(), error);
            }

            // Verify the params exist
            if (JSONRPC_Request.@params == null)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32602 };
                this.ParseError(ref context, JSONRPC_Request.id.ToString(), error);
            }

            return JSONRPC_Request;
        }

        /// <summary>
        /// Authenticate the user in the context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="JSONRPC_Request"></param>
        private void Authenticate(ref HttpContext context, ref JSONRPC_Request JSONRPC_Request)
        {
            Log.Instance.Info("Authentication Types Allowed: " + AUTHENTICATION_TYPE_WINDOWS + ", " + AUTHENTICATION_TYPE_ANONYMOUS + ", " + AUTHENTICATION_TYPE_ANY);
            Log.Instance.Info("Authentication Type Selected: " + API_JSONRPC_AUTHENTICATION_TYPE);

            // Validate the Authentication type
            if (!AUTHENTICATION_TYPE_ALLOWED.Contains(API_JSONRPC_AUTHENTICATION_TYPE))
            {
                Log.Instance.Fatal("Invalid Authentication Type: " + API_JSONRPC_AUTHENTICATION_TYPE);

                JSONRPC_Error error = new JSONRPC_Error { code = -32002 };
                this.ParseError(ref context, JSONRPC_Request.id.ToString(), error);
            }

            // Get the username if any
            if (context.User.Identity.IsAuthenticated)
            {
                // IIS Windows Authentication enabled
                networkIdentity = context.User.Identity.Name;
                networkUsername = context.User.Identity.Name.Split('\\')[1];
            }
            else
            {
                // IIS Anonymous Authentication enabled
                networkIdentity = null;
                networkUsername = null;
            }

            Log.Instance.Info("Network Identity: " + networkIdentity);
            Log.Instance.Info("Network Username: " + networkUsername);
            Log.Instance.Info("AD Domain: " + API_AD_DOMAIN);
            Log.Instance.Info("AD Username: " + API_AD_USERNAME);
            Log.Instance.Info("AD Password: ********"); // Hide API_AD_PASSWORD from logs

            // Get the userPrincipal from Session if any
            string sessionUserPrincipal = (string)(context.Session[JSONRPC_userPrincipal]);
            if (String.IsNullOrEmpty(sessionUserPrincipal))
            {
                // Process the Windows Authentication
                if (API_JSONRPC_AUTHENTICATION_TYPE == AUTHENTICATION_TYPE_WINDOWS)
                {
                    // Process the Windows Authentication
                    if (!WindowsAuthentication(ref context, ref JSONRPC_Request))
                    {
                        JSONRPC_Error error = new JSONRPC_Error { code = -32002 };
                        this.ParseError(ref context, JSONRPC_Request.id.ToString(), error);
                    }
                }
                // Process the Anonymous Authentication
                else if (API_JSONRPC_AUTHENTICATION_TYPE == AUTHENTICATION_TYPE_ANONYMOUS)
                {
                    // Process the Windows Authentication
                    AnonymousAuthentication(ref context, ref JSONRPC_Request);
                }
                // Process Any Authentication
                else if (API_JSONRPC_AUTHENTICATION_TYPE == AUTHENTICATION_TYPE_ANY)
                {
                    // Process the Any Authentication
                    AnyAuthentication(ref context, ref JSONRPC_Request);
                }
            }
            else
            {
                //Query Session for Authentication
                userPrincipal = Utility.JsonDeserialize_IgnoreLoopingReference(sessionUserPrincipal);

                // Process the Windows Authentication
                if (API_JSONRPC_AUTHENTICATION_TYPE == AUTHENTICATION_TYPE_WINDOWS)
                {
                    if (userPrincipal == null)
                    {
                        Log.Instance.Fatal("Undefined User Principal in the Session");

                        JSONRPC_Error error = new JSONRPC_Error { code = -32002 };
                        this.ParseError(ref context, JSONRPC_Request.id.ToString(), error);
                    }
                }
                // Process the Anonymous Authentication
                else if (API_JSONRPC_AUTHENTICATION_TYPE == AUTHENTICATION_TYPE_ANONYMOUS)
                {
                    // Override userPrincipal for security
                    userPrincipal = null;
                }
                // Process Any Authentication
                else if (API_JSONRPC_AUTHENTICATION_TYPE == AUTHENTICATION_TYPE_ANY)
                {
                    // Do nothing
                }

                Log.Instance.Info("Authentication retrieved from the Session");
            }
        }

        /// <summary>
        /// Process Windows Authentication
        /// </summary>
        /// <param name="context"></param>
        /// <param name="JSONRPC_Request"></param>
        private Boolean WindowsAuthentication(ref HttpContext context, ref JSONRPC_Request JSONRPC_Request)
        {
            // Check the username exists
            if (string.IsNullOrEmpty(networkUsername))
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

                userPrincipal = JSONRPC_UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, networkUsername);
                string userPrincipal_serialized = Utility.JsonSerialize_IgnoreLoopingReference(userPrincipal);
                Log.Instance.Info("User Principal: " + userPrincipal_serialized);

                if (userPrincipal == null)
                {
                    Log.Instance.Fatal("Undefined User Principal");
                    return false;
                }

                // Save the serialized userPrincipal in the Session
                context.Session[JSONRPC_userPrincipal] = userPrincipal_serialized;

                Log.Instance.Info("Authentication saved in the Session");
                return true;
            }
            catch (Exception e)
            {
                Log.Instance.Fatal("Unable to connect or query AD");
                Log.Instance.Fatal(e);
                return false;
            }
        }

        /// <summary>
        /// Process Anonymous Authentication
        /// </summary>
        /// <param name="context"></param>
        /// <param name="JSONRPC_Request"></param>
        private void AnonymousAuthentication(ref HttpContext context, ref JSONRPC_Request JSONRPC_Request)
        {
            // Override userPrincipal for security
            userPrincipal = null;
            string userPrincipal_serialized = null;

            // Override the serialized userPrincipal in the Session
            context.Session[JSONRPC_userPrincipal] = userPrincipal_serialized;
        }

        /// <summary>
        /// Process Any Authentication
        /// </summary>
        /// <param name="context"></param>
        /// <param name="JSONRPC_Request"></param>
        private void AnyAuthentication(ref HttpContext context, ref JSONRPC_Request JSONRPC_Request)
        {
            // Override userPrincipal for security
            userPrincipal = null;
            string userPrincipal_serialized = null;

            // Check the username exists agains tthe domain
            if (!string.IsNullOrEmpty(networkUsername) && !String.IsNullOrEmpty(API_AD_DOMAIN))
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
                    userPrincipal = JSONRPC_UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, networkUsername);
                    userPrincipal_serialized = Utility.JsonSerialize_IgnoreLoopingReference(userPrincipal);

                    Log.Instance.Info("User Principal: " + userPrincipal_serialized);
                }
                catch (Exception e)
                {
                    Log.Instance.Fatal("Unable to connect or query AD");
                    Log.Instance.Fatal(e);
                }
            }

            // Save the serialized userPrincipal in the Session
            context.Session[JSONRPC_userPrincipal] = userPrincipal_serialized;

            Log.Instance.Info("Authentication saved in the Session");
        }

        /// <summary>
        /// Validate the requested method
        /// </summary>
        /// <param name="JSONRPC_Request"></param>
        /// <returns></returns>
        private static Boolean ValidateMethod(JSONRPC_Request JSONRPC_Request)
        {
            MethodInfo methodInfo = MapMethod(JSONRPC_Request);
            if (methodInfo == null)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Map the request against the method
        /// </summary>
        /// <param name="JSONRPC_Request"></param>
        /// <returns></returns>
        private static MethodInfo MapMethod(JSONRPC_Request JSONRPC_Request)
        {
            // Get Namespace(s).Class.Method
            string[] mapping = JSONRPC_Request.method.ToString().Split('.');

            // At least 1 Namespace, 1 Class and 1 Method (3 in total) must be present
            if (mapping.Length < 3)
                return null;

            // Get method name
            string methodName = mapping[mapping.Length - 1];

            // Get the method path
            Array.Resize(ref mapping, mapping.Length - 1);
            string methodPath = string.Join(".", mapping);

            // Never allow to call Public Methods in the API Namespace
            if (mapping[0].ToUpperInvariant() == "API")
                return null;

            // Search in the entire Assemplies till finding the right one
            foreach (Assembly currentassembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type StaticClass = currentassembly.GetType(methodPath, false, true);
                if (StaticClass != null)
                {
                    MethodInfo methodInfo = StaticClass.GetMethod(methodName);
                    if (methodInfo == null)
                        return null;
                    else
                        return methodInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// Invoke and return the results from the mapped method
        /// </summary>
        /// <param name="JSONRPC_Request"></param>
        /// <returns></returns>
        private dynamic GetResult(ref HttpContext context, JSONRPC_Request JSONRPC_Request)
        {
            // Set the API object
            JSONRPC_API apiRequest = new JSONRPC_API();
            apiRequest.method = JSONRPC_Request.method.ToString();
            apiRequest.parameters = JSONRPC_Request.@params;
            apiRequest.userPrincipal = userPrincipal;
            apiRequest.ipAddress = Utility.IpAddress;
            apiRequest.userAgent = Utility.UserAgent;

            Log.Instance.Info("API Request: " + Utility.JsonSerialize_IgnoreLoopingReference(apiRequest));

            // Verify the method exists
            MethodInfo methodInfo = MapMethod(JSONRPC_Request);

            //Invoke the API Method
            return methodInfo.Invoke(null, new object[] { apiRequest });
        }

        /// <summary>
        /// Handle reusable IHttpHandler instances 
        /// </summary>
        public bool IsReusable
        {
            // Set to false to ensure thread safe operations
            get { return true; }
        }

        #endregion
    }

    /// <summary>
    /// Define the Request structure for the API
    /// </summary>
    internal class JSONRPC_Request
    {
        #region Properties
        /// <summary>
        /// JSON-RPC version
        /// </summary>
        public object jsonrpc = null;

        /// <summary>
        /// JSON-RPC method
        /// </summary>
        public object method = null;

        /// <summary>
        /// JSON-RPC parameters
        /// </summary>
        public object @params = null;

        /// <summary>
        /// JSON-RPC id
        /// </summary>
        public object id = null;
        #endregion
    }

    /// <summary>
    /// Define the Output structure required by the exposed API
    /// </summary>
    public class JSONRPC_Output
    {
        #region Properties
        /// <summary>
        /// JSON-RPC data
        /// </summary>
        public dynamic data { get; set; }

        /// <summary>
        /// JSON-RPC error
        /// </summary>
        public dynamic error { get; set; }

        #endregion
    }

    /// <summary>
    /// Clone the UserPrincipal object structure for serialisation & deserialisation
    /// This is required because of recursive loop
    /// </summary>
    public class JSONRPC_UserPrincipal : UserPrincipal
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public JSONRPC_UserPrincipal(PrincipalContext context) : base(context) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="samAccountName"></param>
        /// <param name="password"></param>
        /// <param name="enabled"></param>
        [JsonConstructor]
        public JSONRPC_UserPrincipal(PrincipalContext context, string samAccountName, string password, bool enabled) : base(context, samAccountName, password, enabled) { }
    }

    /// <summary>
    /// Define the Error structure for the API
    /// </summary>
    internal class JSONRPC_Error
    {
        #region Properties
        /// <summary>
        /// JSON-RPC error code
        /// </summary>
        public object code;

        /// <summary>
        /// JSON-RPC error message
        /// </summary>
        public object message;

        #endregion
    }

    /// <summary>
    /// Define the API Class to pass to the exposed API 
    /// </summary>
    public class JSONRPC_API
    {
        #region Properties
        /// <summary>
        /// API method
        /// </summary>
        public dynamic method { get; internal set; }

        /// <summary>
        /// API parameters
        /// </summary>
        public dynamic parameters { get; internal set; }

        /// <summary>
        /// Active Directory userPrincipal
        /// </summary>
        public dynamic userPrincipal { get; internal set; }

        /// <summary>
        /// Client IP address
        /// </summary>
        public dynamic ipAddress { get; internal set; }

        /// <summary>
        /// Client user agent
        /// </summary>
        public dynamic userAgent { get; internal set; }

        #endregion
    }


    /// <summary>
    /// Define the Response Error structure for the API
    /// </summary>
    internal class JSONRPC_ResponseError
    {
        #region Properties
        /// <summary>
        /// JSON-RPC version
        /// </summary>
        public object jsonrpc = null;

        /// <summary>
        /// JSON-RPC error
        /// </summary>
        public object error = new JSONRPC_Error();

        /// <summary>
        /// JSON-RPC id
        /// </summary>
        public object id = null;

        #endregion
    }

    /// <summary>
    /// Define the Response Data structure for the API
    /// </summary>
    internal class JSONRPC_API_ResponseData
    {
        #region Properties
        /// <summary>
        /// JSON-RPC version
        /// </summary>
        public object jsonrpc { get; set; }

        /// <summary>
        /// JSON-RPC data as object
        /// </summary>
        public object data { get; set; }

        /// <summary>
        /// JSON-RPC id
        /// </summary>
        public object id { get; set; }

        #endregion
    }

    /// <summary>
    /// Define the Response Data JRaw structure for the API
    /// </summary>
    internal class JSONRPC_API_ResponseDataJRaw
    {
        #region Properties
        /// <summary>
        /// JSON-RPC version
        /// </summary>
        public object jsonrpc { get; set; }

        /// <summary>
        /// JSON-RPC data as raw json
        /// </summary>
        public JRaw data { get; set; }

        /// <summary>
        /// JSON-RPC id
        /// </summary>
        public object id { get; set; }

        #endregion
    }
}