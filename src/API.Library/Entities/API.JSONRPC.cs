using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.SessionState;

namespace API
{
    /// <summary>
    /// JSON-RPC listener following specifications at http://www.jsonrpc.org/specification
    /// </summary>
    public class JSONRPC : Common, IHttpHandler, IRequiresSessionState
    {
        #region Properties
        /// <summary>
        /// JSON RPC version in use
        /// </summary>
        internal const string JSONRPC_Version = "2.0";

        /// <summary>
        ///  GET request parameter
        /// </summary>
        internal const string JSONRPC_GetParam = "data";

        /// <summary>
        ///  JSON-stat mimetype
        /// </summary>
        internal const string JSONRPC_MimeType = "application/json";

        /// <summary>
        /// Mask parametrs 
        /// </summary>
        private static List<string> API_JSONRPC_MASK_PARAMETERS = (ConfigurationManager.AppSettings["API_JSONRPC_MASK_PARAMETERS"]).Split(',').ToList<string>();
        #endregion

        #region Methods
        /// <summary>
        /// ProcessRequest executed automatically by the iHttpHandler interface
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            Log.Instance.Info("API Interface Opened");

            // Set HTTP Requests
            httpGET = GetHttpGET();
            httpPOST = GetHttpPOST();

            // Set Mime-Type for the Content Type and override the Charset
            context.Response.ContentType = JSONRPC_MimeType;
            context.Response.Charset = null;
            // Set CacheControl to no-cache
            context.Response.CacheControl = "no-cache";

            // Deserialize and parse the JSON request into an Object dynamically
            JSONRPC_Request JSONRPC_Request = ParseRequest(ref context);

            // Check for the maintenance flag
            if (Maintenance)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32001, data = "The system is currently under maintenance." };
                ParseError(ref context, JSONRPC_Request.id, error);
            }

            // Authenticate and append credentials
            if (Authenticate(ref context) == false)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32002 };
                ParseError(ref context, JSONRPC_Request.id, error);
            }

            // Get results from the relevant method with the params
            JSONRPC_Output result = GetResult(ref context, JSONRPC_Request);
            if (result == null)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32603 };
                ParseError(ref context, JSONRPC_Request.id, error);
            }
            else if (result.error != null)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32099, data = result.error };
                ParseError(ref context, JSONRPC_Request.id, error);
            }
            else
            {
                // Check if the result.data is already a JSON type casted as: new JRaw(data); 
                var jsonRaw = result.data as JRaw;
                if (jsonRaw != null)
                {
                    JSONRPC_API_ResponseDataJRaw output = new JSONRPC_API_ResponseDataJRaw
                    {
                        jsonrpc = JSONRPC_Request.jsonrpc,
                        result = jsonRaw,
                        id = JSONRPC_Request.id
                    };
                    // Output the JSON-RPC repsonse with JRaw data
                    context.Response.Write(Utility.JsonSerialize_IgnoreLoopingReference(output));
                }
                else
                {
                    JSONRPC_API_ResponseData output = new JSONRPC_API_ResponseData
                    {
                        jsonrpc = JSONRPC_Request.jsonrpc,
                        result = result.data,
                        id = JSONRPC_Request.id
                    };
                    // Output the JSON-RPC repsonse
                    context.Response.Write(Utility.JsonSerialize_IgnoreLoopingReference(output));
                }
            }

            Log.Instance.Info("API Interface Closed");
        }

        /// <summary> 
        /// Parse the API error
        /// From -32000 to -32099 reserved for implementation-defined errors.
        /// </summary>
        ///
        /// <param name="response"></param>
        /// <param name="id"></param>
        /// <param name="error"></param>
        private void ParseError(ref HttpContext context, string id, JSONRPC_Error error)
        {
            if (error.message == null)
                switch (error.code)
                {
                    // Custom codes and messages
                    case -32000:
                        error.message = "Invalid version";
                        break;
                    case -32001:
                        error.message = "System maintenance";
                        break;
                    case -32002:
                        error.message = "Invalid authentication";
                        break;
                    case -32099:
                        error.message = "Application error";
                        break;

                    // Standard codes and messages
                    case -32700:
                        error.message = "Parse error";
                        break;
                    case -32600:
                        error.message = "Invalid request";
                        break;
                    case -32601:
                        error.message = "Method not found";
                        break;
                    case -32602:
                        error.message = "Invalid params";
                        break;
                    case -32603:
                    default:
                        error.code = -32603;
                        error.message = "Internal error";
                        break;
                }

            Log.Instance.Info("IP: " + Utility.GetIP() + ", Error Code: " + error.code.ToString() + ", Error Message: " + error.message.ToString() + ", Error Data: " + (error.data == null ? "" : error.data.ToString()));
            object output = new JSONRPC_ResponseError { jsonrpc = JSONRPC_Version, error = error, id = id };
            context.Response.Write(Utility.JsonSerialize_IgnoreLoopingReference(output));
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

            // Check the query input exists
            if (string.IsNullOrWhiteSpace(httpGET[JSONRPC_GetParam])
            && string.IsNullOrWhiteSpace(httpPOST))
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32700 };
                ParseError(ref context, null, error);
            }

            // POST request overrides GET one
            if (!string.IsNullOrWhiteSpace(httpPOST))
            {
                request = httpPOST;
                Log.Instance.Info("Request type: POST");
            }
            else
            {
                request = httpGET[JSONRPC_GetParam];
                Log.Instance.Info("Request type: GET");
            }

            JSONRPC_Request JSONRPC_Request = new JSONRPC_Request();

            try
            {
                // Deserialize JSON to an Object dynamically
                JSONRPC_Request = Utility.JsonDeserialize_IgnoreLoopingReference<JSONRPC_Request>(request);
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);

                JSONRPC_Error error = new JSONRPC_Error { code = -32700 };
                ParseError(ref context, null, error);
            }

            // N.B. JSONRPC_Request.id is recommended but optional anyway 

            // Validate the request
            if (JSONRPC_Request.jsonrpc == null
            || JSONRPC_Request.method == null)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32600 };
                ParseError(ref context, JSONRPC_Request.id, error);
            }

            // Verify the version is right 
            if (JSONRPC_Request.jsonrpc != JSONRPC_Version)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32000 };
                ParseError(ref context, JSONRPC_Request.id, error);
            }

            // Verify the method exists
            if (!ValidateMethod(JSONRPC_Request))
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32601 };
                ParseError(ref context, JSONRPC_Request.id, error);
            }

            // Verify the params exist
            if (JSONRPC_Request.@params == null)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32602 };
                ParseError(ref context, JSONRPC_Request.id, error);
            }

            return JSONRPC_Request;
        }

        /// <summary>
        /// Validate the requested method
        /// </summary>
        /// <param name="JSONRPC_Request"></param>
        /// <returns></returns>
        private static bool ValidateMethod(JSONRPC_Request JSONRPC_Request)
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
            string[] mapping = JSONRPC_Request.method.Split('.');

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
                    MethodInfo methodInfo = StaticClass.GetMethod(methodName, new Type[] { typeof(JSONRPC_API) });
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
            apiRequest.method = JSONRPC_Request.method;
            apiRequest.parameters = JSONRPC_Request.@params;
            apiRequest.userPrincipal = UserPrincipal;
            apiRequest.ipAddress = Utility.GetIP();
            apiRequest.userAgent = Utility.GetUserAgent();
            apiRequest.httpGET = httpGET;
            apiRequest.httpPOST = httpPOST;

            // Hide password from logs
            Log.Instance.Info("API Request: " + MaskParameters(Utility.JsonSerialize_IgnoreLoopingReference(apiRequest)));

            // Verify the method exists
            MethodInfo methodInfo = MapMethod(JSONRPC_Request);

            //Invoke the API Method
            return methodInfo.Invoke(null, new object[] { apiRequest });
        }

        /// <summary>
        /// Mask an input password
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string MaskParameters(string input)
        {
            if (String.IsNullOrEmpty(input))
            {
                return "";
            }
            // Init the output
            string output = input;

            // Loop trough the parameters to mask
            foreach (var param in API_JSONRPC_MASK_PARAMETERS)
            {
                // https://stackoverflow.com/questions/171480/regex-grabbing-values-between-quotation-marks
                Log.Instance.Info("Masked parameter: " + param);
                output = Regex.Replace(output, "\"" + param + "\"\\s*:\\s*\"(.*?[^\\\\])\"", "\"" + param + "\": \"********\"", RegexOptions.IgnoreCase);
            }

            return output;
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
        public string jsonrpc = null;

        /// <summary>
        /// JSON-RPC method
        /// </summary>
        public string method = null;

        /// <summary>
        /// JSON-RPC parameters
        /// </summary>
        public dynamic @params = null;

        /// <summary>
        /// JSON-RPC id
        /// </summary>
        public string id = null;
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
    /// Define the Error structure for the API
    /// </summary>
    internal class JSONRPC_Error
    {
        #region Properties
        /// <summary>
        /// JSON-RPC error code
        /// </summary>
        public int code;

        /// <summary>
        /// JSON-RPC error message
        /// </summary>
        public string message;

        /// <summary>
        /// JSON-RPC error data
        /// </summary>
        public dynamic data;

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
        public string method { get; internal set; }

        /// <summary>
        /// API parameters
        /// </summary>
        public dynamic parameters { get; set; }

        /// <summary>
        /// Active Directory userPrincipal
        /// </summary>
        public dynamic userPrincipal { get; internal set; }

        /// <summary>
        /// Client IP address
        /// </summary>
        public string ipAddress { get; internal set; }

        /// <summary>
        /// Client user agent
        /// </summary>
        public string userAgent { get; internal set; }

        /// <summary>
        /// GET request
        /// </summary>
        public NameValueCollection httpGET { get; internal set; }

        /// <summary>
        /// POST request
        /// </summary>
        public string httpPOST { get; internal set; }
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
        public string jsonrpc = null;

        /// <summary>
        /// JSON-RPC error
        /// </summary>
        public JSONRPC_Error error = new JSONRPC_Error();

        /// <summary>
        /// JSON-RPC id
        /// </summary>
        public string id = null;

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
        public string jsonrpc { get; set; }

        /// <summary>
        /// JSON-RPC result as object
        /// </summary>
        public dynamic result { get; set; }

        /// <summary>
        /// JSON-RPC id
        /// </summary>
        public string id { get; set; }

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
        public string jsonrpc { get; set; }

        /// <summary>
        /// JSON-RPC result as raw json
        /// </summary>
        public JRaw result { get; set; }

        /// <summary>
        /// JSON-RPC id
        /// </summary>
        public string id { get; set; }

        #endregion
    }
}
