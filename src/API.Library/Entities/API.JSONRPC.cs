using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Reflection;
using System.Collections.Specialized;


namespace API
{
    /// <summary>
    /// JSON-RPC listener following specifications at http://www.jsonrpc.org/specification
    /// </summary>
    public class JSONRPC : Common
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
        ///  allowed http methods
        /// </summary>
        public string[] AllowedHTTPMethods = new string[] { "GET", "POST" };

        public JSONRPC() : base()
        {
        }


        #endregion
        #region Methods
        /// <summary>
        /// ProcessRequest executed automatically by the iHttpHandler interface
        /// </summary>
        /// <param name="context"></param>
        //public async Task Invoke(HttpContext httpContext)
        public async Task ProcessRequest(HttpContext httpContext, CancellationTokenSource apiCancellationToken, Thread performanceThread, bool API_PERFORMANCE_ENABLED, Trace trace)
        {

            // Were we already canceled?
            apiCancellationToken.Token.ThrowIfCancellationRequested();

            try
            {
                Log.Instance.Info("Starting JSONRPC processing");

               
                // Set HTTP Requests
                httpGET = GetHttpGET(httpContext);
                httpPOST = await GetHttpPOST(httpContext);

                // Set Mime-Type for the Content Type and override the Charset
                httpContext.Response.ContentType = JSONRPC_MimeType;
                // httpContext.Response.Headers.ContentType.CharSet = "";
                // Set CacheControl to no-cache
                httpContext.Response.Headers.Append("Cache-Control", "no-cache");
                //set charset to null
                httpContext.Response.Headers.Append("Charset", "");

                // Deserialize and parse the JSON request into an Object dynamically
                JSONRPC_Request JSONRPC_Request = await ParseRequest(httpContext, apiCancellationToken,trace);

                // Check for the maintenance flag
                if (Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.MAINTENANCE))
                {
                    JSONRPC_Error error = new JSONRPC_Error { code = -32001, data = "The system is currently under maintenance." };
                    await ParseError(httpContext, JSONRPC_Request.id, error, apiCancellationToken,trace);
                }
                
                string SessionCookieName = ApiServicesHelper.ApiConfiguration.Settings["API_SESSION_COOKIE"];
               
                // Get Session Cookie
                Cookie sessionCookie = CheckCookie(SessionCookieName,httpContext);

                JSONRPC_Output result = null;
  
                bool? isAuthenticated =  Authenticate(ref httpContext);

                try
                {
                    switch (isAuthenticated)
                    {
                        case null: //Anonymous authentication
                            if (API_PERFORMANCE_ENABLED)
                            {
                                performanceThread.Start();
                            }
                            result = GetResult(httpContext, JSONRPC_Request,trace, sessionCookie);
                            break;
                        case true: //Windows Authentication
                            if (API_PERFORMANCE_ENABLED)
                            {
                                performanceThread.Start();
                            }

                            result = GetResult(httpContext, JSONRPC_Request, trace, null);
                          
                            break;
                        case false: //Error
                            JSONRPC_Error error = new JSONRPC_Error { code = -32002 };
                            await ParseError(httpContext, JSONRPC_Request.id, error, apiCancellationToken,trace);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.Instance.Error(e);
                    Log.Instance.Error(Utility.JsonSerialize_IgnoreLoopingReference(trace));
                    JSONRPC_Error error = new JSONRPC_Error { code = -32603 };
                    await ParseError(httpContext, JSONRPC_Request.id, error, apiCancellationToken, trace);
                }
                

                if (result == null)
                {
                    JSONRPC_Error error = new JSONRPC_Error { code = -32603 };
                    await ParseError(httpContext, JSONRPC_Request.id, error, apiCancellationToken,trace);
                }
                else if (result.error != null)
                {
                    JSONRPC_Error error = new JSONRPC_Error { code = -32099, data = result.error };
                    await ParseError(httpContext, JSONRPC_Request.id, error, apiCancellationToken, trace);
                }
                else
                {
                    // Set the Session Cookie if requested
                    if (!string.IsNullOrEmpty(SessionCookieName) && result.sessionCookie != null && result.sessionCookie.Name.Equals(SessionCookieName))
                    {
                        var cookieOptions = new CookieOptions
                        {
                            Secure = true,
                            HttpOnly = true,
                            Domain = null,
                            SameSite = SameSiteMode.Strict,
                        };

                        // Add the cookie to the response cookie collection
                        httpContext.Response.Cookies.Append(SessionCookieName, result.sessionCookie.Value, cookieOptions);
                    }

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
                        await returnResponseAsync(httpContext, Utility.JsonSerialize_IgnoreLoopingReference(output), apiCancellationToken, HttpStatusCode.OK);
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
                        await returnResponseAsync(httpContext, Utility.JsonSerialize_IgnoreLoopingReference(output), apiCancellationToken, HttpStatusCode.OK);
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                //don't need to do anything here as operation has been cancelled
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(Utility.JsonSerialize_IgnoreLoopingReference(trace));
                Log.Instance.Fatal(e);
                Log.Instance.Fatal(e.StackTrace);
                await returnResponseAsync(httpContext, "", apiCancellationToken, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary> 
        /// Parse the API error
        /// From -32000 to -32099 reserved for implementation-defined errors.
        /// </summary>
        ///
        /// <param name="response"></param>
        /// <param name="id"></param>
        /// <param name="error"></param>
        private async Task ParseError(HttpContext context, string id, JSONRPC_Error error, CancellationTokenSource sourceToken, Trace trace)
        {
            if (error.message == null)
                trace.TrcJsonRpcErrorCode = error.code;
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
                        trace.TrcJsonRpcErrorCode = -32603;
                        break;
                }


            Log.Instance.Info("IP: " + ApiServicesHelper.WebUtility.GetIP() + ", Error Code: " + error.code.ToString() + ", Error Message: " + error.message.ToString() + ", Error Data: " + (error.data == null ? "" : error.data.ToString()));
            object output = new JSONRPC_ResponseError { jsonrpc = JSONRPC_Version, error = error, id = id };


            await returnResponseAsync(context, Utility.JsonSerialize_IgnoreLoopingReference(output), sourceToken, HttpStatusCode.OK);
        }

        /// <summary>
        /// Parse and validate the request
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        private async Task<JSONRPC_Request> ParseRequest(HttpContext httpContext,CancellationTokenSource sourceToken, Trace trace)
        {
            // Initialise requests
            string request = null;
          
            // Check the query input exists
            if(httpGET != null)
            {
                if (string.IsNullOrWhiteSpace(httpGET[JSONRPC_GetParam]) && string.IsNullOrWhiteSpace(httpPOST))
                {
                    JSONRPC_Error error = new JSONRPC_Error { code = -32700 };
                    await ParseError(httpContext, null, error, sourceToken,trace);
                }
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
            } catch (Exception e){
                Log.Instance.Fatal(request);
                Log.Instance.Fatal(e);

                var error = new JSONRPC_Error { code = -32700 };
                await ParseError(httpContext, null, error, sourceToken,trace);
            }

            // N.B. JSONRPC_Request.id is recommended but optional anyway 

            // Validate the request
            if (JSONRPC_Request.jsonrpc == null
            || JSONRPC_Request.method == null)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32600 };
                await ParseError(httpContext, JSONRPC_Request.id, error, sourceToken,trace);
            }

            // Verify the version is right 
            if (JSONRPC_Request.jsonrpc != JSONRPC_Version)
            {
                JSONRPC_Error error = new JSONRPC_Error { code = -32000 };
                await ParseError(httpContext, JSONRPC_Request.id, error, sourceToken,trace);
            }

            // Verify the method exists
            if (!ValidateMethod(JSONRPC_Request))
            {
                var error = new JSONRPC_Error { code = -32601 };
                await ParseError(httpContext, JSONRPC_Request.id, error, sourceToken,trace);
            }

            // Verify the params exist
            if (JSONRPC_Request.@params == null)
            {
                var error = new JSONRPC_Error { code = -32602 };
                await ParseError(httpContext, JSONRPC_Request.id, error, sourceToken,trace);
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
            return CheckAPICallsAllowed(methodName, methodPath, typeof(JSONRPC_API));

        }

        /// <summary>
        /// Invoke and return the results from the mapped method
        /// </summary>
        /// <param name="JSONRPC_Request"></param>
        /// <returns></returns>
        private dynamic GetResult(HttpContext context, JSONRPC_Request JSONRPC_Request, Trace trace, Cookie sessionCookie = null)
        {
            // Set the API object
            JSONRPC_API apiRequest = new JSONRPC_API
            {
                method = JSONRPC_Request.method,
                parameters = JSONRPC_Request.@params,
                userPrincipal = UserPrincipal,
                ipAddress = ApiServicesHelper.WebUtility.GetIP(),
                userAgent = ApiServicesHelper.WebUtility.GetUserAgent(),
                httpGET = httpGET,
                httpPOST = httpPOST,
                sessionCookie = sessionCookie,
                requestType = context.Request.Method,
                requestHeaders = context.Request.Headers,
                scheme = context.Request.Scheme,
                correlationID = APIMiddleware.correlationID.Value
            };

            //gather trace information
            GatherTraceInformation(apiRequest, trace);

            // Hide password from logs
            Log.Instance.Info("API Request: " + MaskParameters(Utility.JsonSerialize_IgnoreLoopingReference(UserPrincipal)));

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
    public class JSONRPC_Output : IResponseOutput
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

        /// <summary>
        /// Session Cookie
        /// </summary>
        public Cookie sessionCookie { get; set; }
        public dynamic response { get; set; }
        public string mimeType { get; set; }
        public HttpStatusCode statusCode { get; set; }
        public string fileName { get; set; }
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
    public class JSONRPC_API : IRequest
    {
        #region Properties
        /// <summary>
        /// API method
        /// </summary>
        public string method { get; set; }

        /// <summary>
        /// API parameters
        /// </summary>
        public dynamic parameters { get; set; }

        /// <summary>
        /// Active Directory userPrincipal
        /// </summary>
        public dynamic userPrincipal { get; set; }

        /// <summary>
        /// Client IP address
        /// </summary>
        public string ipAddress { get; set; }

        /// <summary>
        /// Client user agent
        /// </summary>
        public string userAgent { get; set; }

        /// <summary>
        /// GET request
        /// </summary>
        public NameValueCollection httpGET { get; set; }

        /// <summary>
        /// POST request
        /// </summary>
        public string httpPOST { get; set; }

        /// <summary>
        /// Session Cookie
        /// </summary>
        public Cookie sessionCookie { get; set; }

        /// <summary>
        /// Request Type
        /// </summary>
        public string requestType { get; set; }

        /// <summary>
        /// Request Headers
        /// </summary>
        public IHeaderDictionary requestHeaders { get; set; }


        /// <summary>
        /// Request Scheme
        /// </summary>
        public string scheme { get; set; }


        /// <summary>
        /// Request correlatationID
        /// </summary>
        public string correlationID { get; set; }

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
