using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;
using System.Dynamic;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.RegularExpressions;

namespace API
{
    /// <summary>
    /// Static implementation
    /// </summary>
    public class Static : Common
    {

        #region Properties
        /// <summary>
        ///  List of URL Request Parameters
        /// </summary>
        private List<string> RequestParams = new List<string>();

        /// <summary>
        ///  allowed http methods
        /// </summary>
        public string[] AllowedHTTPMethods = new string[] { "GET", "POST" };
        #endregion

        public Static() : base()
        {
        }

        #region Methods
        /// <summary>
        /// ProcessRequest executed automatically by the iHttpHandler interface
        /// </summary>
        /// <param name="httpContext"></param>
        public async Task ProcessRequest(HttpContext httpContext, CancellationTokenSource apiCancellationToken, Thread performanceThread, bool API_PERFORMANCE_ENABLED, Trace trace)
        {
            // Were we already canceled?
            apiCancellationToken.Token.ThrowIfCancellationRequested();

            try
            {
                Log.Instance.Info("Starting Static Processing");


                httpGET = GetHttpGET(httpContext);
                httpPOST = await GetHttpPOST(httpContext);

                // Extract the request parameters from the URL
                await ParseRequest(httpContext, apiCancellationToken);

                // Check for the maintenance flag
                if (Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.MAINTENANCE))
                {
                    await ParseError(httpContext, HttpStatusCode.ServiceUnavailable, apiCancellationToken,"System maintenance");
                }

                Static_Output result = null;


                // Call the public method
                if (API_PERFORMANCE_ENABLED)
                {
                    performanceThread.Start();
                }
                result = GetResult(ref httpContext, trace);

                if (result == null)
                {
                    await ParseError(httpContext, HttpStatusCode.InternalServerError, apiCancellationToken, "Internal Error");
                }
                else if (result.statusCode == HttpStatusCode.OK)
                {
                   
                    httpContext.Response.ContentType = result.mimeType;
                    httpContext.Response.Headers["expires"]= DateTime.Now.AddYears(1).ToString();
                    httpContext.Response.Headers.Add("Last-Modified", DateTime.Now.ToString());
                    httpContext.Response.Headers["Cache-Control"] = "31536000"; //one year

                    if (!string.IsNullOrEmpty(result.fileName))
                    {
                        httpContext.Response.Headers.Add("Content-Disposition", new ContentDisposition { Inline = true, FileName = result.fileName }.ToString());
                    }

                    if (result.response?.GetType() == typeof(byte[]))
                    {
                        httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                        Stream stream = new MemoryStream(result.response);
                        var fullFileName = Path.GetTempFileName();
                        File.WriteAllBytes(fullFileName, result.response);
                        await returnResponseAsync(httpContext, fullFileName, apiCancellationToken, HttpStatusCode.OK,true);
                    }
                    else
                    {                       
                        await returnResponseAsync(httpContext, result.response, apiCancellationToken, HttpStatusCode.OK);
                    }
                }
                else
                {
                    await ParseError(httpContext, result.statusCode, result.response);
                }

            }
            catch (OperationCanceledException e)
            {
                //don't need to do anything here as operation has been cancelled
            }
            catch (ThreadAbortException e)
            {
                // Thread aborted, do nothing
                // The finally block will take care of everything safely
            }
            catch (Exception e)
            {
                await returnResponseAsync(httpContext, "", apiCancellationToken, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Parse the API error returning a HTTP status
        /// </summary>
        /// <param name="context"></param>
        /// <param name="statusCode"></param>
        /// <param name="statusDescription"></param>
        private async Task ParseError(HttpContext context, HttpStatusCode statusCode,CancellationTokenSource sourceToken, string statusDescription = "")
        {
            Log.Instance.Info("IP: " + ApiServicesHelper.WebUtility.GetIP() + ", Status Code: " + statusCode.ToString() + ", Status Description: " + statusDescription);

            if (!string.IsNullOrEmpty(statusDescription))
                await returnResponseAsync(context, statusDescription, sourceToken, statusCode);

       
        }

        /// <summary>
        /// Parse and validate the request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ParseRequest(HttpContext context, CancellationTokenSource sourceToken)
        {
            try
            {
                /*
                URL : http://localhost:8080/mysite/page.aspx?p1=1&p2=2

                Value of HttpContext.Current.Request.Url.Host
                localhost

                Value of HttpContext.Current.Request.Url.Authority
                localhost:8080

                Value of HttpContext.Current.Request.Url.AbsolutePath
                /mysite/page.aspx

                Value of HttpContext.Current.Request.ApplicationPath
                /

                Value of HttpContext.Current.Request.Url.AbsoluteUri
                http://localhost:8080/mysite/page.aspx?p1=1&p2=2

                Value of HttpContext.Current.Request.RawUrl
                /mysite/page.aspx?p1=1&p2=2

                Value of HttpContext.Current.Request.Url.PathAndQuery
                /mysite/page.aspx?p1=1&p2=2
                */

                // Read the URL parameters and split the URL Absolute Path
                Log.Instance.Info("URL Absolute Path: " + context.Request.Path);
                RequestParams = Regex.Split(context.Request.Path, "api.static/", RegexOptions.IgnoreCase).ToList();

                // Validate the Application path
                if (RequestParams.Count() != 2)
                {
                    await ParseError(context, HttpStatusCode.BadRequest, sourceToken, "Invalid Static handler");
                }
                // Get the Static parameters
                RequestParams = RequestParams[1].Split('/').ToList();
                Log.Instance.Info("Request params: " + Utility.JsonSerialize_IgnoreLoopingReference(RequestParams));

                // Validate the request
                if (RequestParams.Count() == 0)
                {
                    await ParseError(context, HttpStatusCode.BadRequest, sourceToken, "Invalid Static parameters");
                }

                // Verify the method exists
                if (!ValidateMethod(RequestParams))
                {
                    await ParseError(context, HttpStatusCode.BadRequest, sourceToken, "Static method not found");
                }
            }
            catch (Exception e)
            {
                Log.Instance.Fatal("Request params: " + Utility.JsonSerialize_IgnoreLoopingReference(RequestParams));
                Log.Instance.Fatal(e);
                await ParseError(context, HttpStatusCode.BadRequest, sourceToken, e.Message);
            }
        }

        /// <summary>
        /// Validate the requested method
        /// </summary>
        /// <param name="requestParams"></param>
        /// <returns></returns>
        private static bool ValidateMethod(List<string> requestParams)
        {
            MethodInfo methodInfo = MapMethod(requestParams);
            if (methodInfo == null)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Map the request against the method
        /// </summary>
        /// <param name="requestParams"></param>
        /// <returns></returns>
        private static MethodInfo MapMethod(List<string> requestParams)
        {
            // Get Namespace(s).Class.Method
            string[] mapping = requestParams[0].Split('.');

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

            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            var calledClass = allAssemblies.Select(y => y.GetType(methodPath, false, true)).Where(p => p != null).FirstOrDefault();

            if (calledClass != null)
            {
                if (calledClass.FullName.Trim().Equals(methodPath.Trim()))
                {

                    if (calledClass.CustomAttributes.Where(xx => xx.AttributeType.Name == "AllowAPICall").ToList().Count > 0)
                    {

                        MethodInfo methodInfo = calledClass.GetMethod(methodName, new Type[] { typeof(Static_API) });
                        if (methodInfo == null)
                        {
                            return null;
                        }
                        else
                        {
                            return methodInfo;
                        }
                    }
                }
            }


            return null;
        }

        /// <summary>
        /// Invoke and return the results from the mapped method
        /// </summary>
        /// <returns></returns>
        private dynamic GetResult(ref HttpContext context,Trace trace, Cookie sessionCookie = null)
        {
            // Set the API object
            Static_API apiRequest = new Static_API();
            apiRequest.method = RequestParams[0];
            apiRequest.parameters = RequestParams;
            apiRequest.ipAddress = ApiServicesHelper.WebUtility.GetIP();
            apiRequest.userAgent = ApiServicesHelper.WebUtility.GetUserAgent();
            //namevaluecollection not the same in .net6
            apiRequest.httpGET = httpGET;
            apiRequest.httpPOST = httpPOST;
            apiRequest.requestType = context.Request.Method;
            apiRequest.requestHeaders = context.Request.Headers;

            //gather trace information
            GatherTraceInformation(apiRequest, trace);

            dynamic logMessage = new ExpandoObject();
            logMessage = apiRequest;
            if(UserPrincipal!=null)
                logMessage.userPrincipal = UserPrincipalForLogging(UserPrincipal);

            Log.Instance.Info("API Request: " + MaskParameters(Utility.JsonSerialize_IgnoreLoopingReference(logMessage)));

            // Verify the method exists
            MethodInfo methodInfo = MapMethod(RequestParams);

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
    /// Define the Output structure required by the exposed API
    /// </summary>
    public class Static_Output
    {
        #region Properties
        /// <summary>
        /// Static response
        /// </summary>
        public dynamic response { get; set; }

        /// <summary>
        /// Static mime type
        /// </summary>
        public string mimeType { get; set; }

        /// <summary>
        /// Static status code
        /// </summary>
        public HttpStatusCode statusCode { get; set; }

        /// <summary>
        /// Static filename (optional)
        /// </summary>
        public string fileName { get; set; }
        #endregion
    }

    /// <summary>
    /// Define the API Class to pass to the exposed API 
    /// </summary>
    public class Static_API : IRequest
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

        public dynamic userPrincipal { get { return null; } set { } }

        public Cookie sessionCookie { get { return null; } set { } }
      
        /// <summary>
        /// Request Type
        /// </summary>
        public string requestType { get; set; }
        #endregion

        /// <summary>
        /// Request Headers
        /// </summary>
        public IHeaderDictionary requestHeaders { get; set; }

        public string scheme { get; set; }
    }


}
