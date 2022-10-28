using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.SessionState;

namespace API
{
    public class HEAD : Common, IHttpHandler, IRequiresSessionState
    {
        #region Properties
        /// <summary>
        ///  List of URL Request Parameters
        /// </summary>
        private List<string> RequestParams = new List<string>();
        #endregion

        #region Methods
        /// <summary>
        /// ProcessRequest executed automatically by the iHttpHandler interface
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {


            // Set Mime-Type for the Content Type and override the Charset
            context.Response.Charset = null;

            // Set CacheControl to public 
            context.Response.CacheControl = "public";

            // Check if the client has already a cached record
            string rawIfModifiedSince = context.Request.Headers.Get("If-Modified-Since");
            if (!string.IsNullOrEmpty(rawIfModifiedSince))
            {
                context.Response.StatusCode = 304;
                // Do not process the request at all, stop here.
                return;
            }

            // Initiate Stopwatch
            Stopwatch sw = new Stopwatch();
            // Start Stopwatch
            sw.Start();

            // Thread a PerfomanceCollector
            PerfomanceCollector performanceCollector = new PerfomanceCollector();
            Thread performanceThread = new Thread(new ThreadStart(performanceCollector.CollectData));

            try
            {
                Log.Instance.Info("API Interface Opened");

                // Set HTTP Requests
                httpGET = GetHttpGET();
                httpPOST = GetHttpPOST();

                // Extract the request parameters from the URL
                ParseRequest(ref context);

                // Check for the maintenance flag
                if (Maintenance)
                {
                    ParseError(ref context, HttpStatusCode.ServiceUnavailable, "System maintenance");
                }

                Head_Output result = null;

                // Call the public method
                performanceThread.Start();
                result = GetResult(ref context);
                if (result == null)
                {
                    ParseError(ref context, HttpStatusCode.InternalServerError, "Internal Error");
                }
                else if (result.statusCode == HttpStatusCode.OK)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.Cache.SetLastModified(DateTime.Now);
                    context.Response.Cache.SetExpires(DateTime.Now.AddYears(1));

                    context.Response.Write(result.response);

                }
                else
                {
                    ParseError(ref context, result.statusCode, result.response);
                }

                if (!String.IsNullOrEmpty(result.mimeType))
                {
                    context.Response.AppendHeader("Content-Type", result.mimeType);

                }

            }
            catch (ThreadAbortException e)
            {
                // Thread aborted, do nothing
                // The finally block will take care of everything safely
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                throw;
            }
            finally
            {
                // Terminate Perfomance collection
                performanceThread.Abort();

                // Stop Stopwatch
                sw.Stop();

                Log.Instance.Info("API Execution Time (s): " + ((float)Math.Round(sw.Elapsed.TotalMilliseconds / 1000, 3)).ToString());
                Log.Instance.Info("API Interface Closed");
            }
        }

        /// <summary>
        /// Invoke and return the results from the mapped method
        /// </summary>
        /// <returns></returns>
        private dynamic GetResult(ref HttpContext context, HttpCookie sessionCookie = null)
        {
            // Set the API object
            Head_API apiRequest = new Head_API();
            apiRequest.method = RequestParams[0];
            apiRequest.parameters = RequestParams;
            apiRequest.ipAddress = Utility.GetIP();
            apiRequest.userAgent = Utility.GetUserAgent();
            apiRequest.httpGET = httpGET;
            apiRequest.httpPOST = httpPOST;

            // Hide password from logs
            Log.Instance.Info("API Request: " + Utility.JsonSerialize_IgnoreLoopingReference(apiRequest));

            // Verify the method exists
            MethodInfo methodInfo = MapMethod(RequestParams);

            //Invoke the API Method
            return methodInfo.Invoke(null, new object[] { apiRequest });
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
            foreach (Assembly currentassembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type HeadClass = currentassembly.GetType(methodPath, false, true);
                if (HeadClass != null)
                {
                    if (HeadClass.CustomAttributes.Where(x => x.AttributeType.Name == "AllowAPICall").ToList().Count == 0) { break; }
                    MethodInfo methodInfo = HeadClass.GetMethod(methodName, new Type[] { typeof(Head_API) });
                    if (methodInfo == null)
                        return null;
                    else
                        return methodInfo;
                }
            }

            return null;
        }

        /// <summary>
        /// Parse the API error returning a HTTP status
        /// </summary>
        /// <param name="context"></param>
        /// <param name="statusCode"></param>
        /// <param name="statusDescription"></param>
        private void ParseError(ref HttpContext context, HttpStatusCode statusCode, string statusDescription = "")
        {
            Log.Instance.Info("IP: " + Utility.GetIP() + ", Status Code: " + statusCode.ToString() + ", Status Description: " + statusDescription);

            context.Response.StatusCode = (int)statusCode;
            if (!string.IsNullOrEmpty(statusDescription))
                context.Response.StatusDescription = statusDescription;

            try
            {
                context.Response.End();
            }
            catch (ThreadAbortException e)
            {
                // Thread intentially aborted, do nothing
            }
        }


        /// <summary>
        /// Parse and validate the request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private void ParseRequest(ref HttpContext context)
        {
            try
            {


                // Read the URL parameters and split the URL Absolute Path
                Log.Instance.Info("URL Absolute Path: " + context.Request.Url.AbsolutePath);
                RequestParams = Regex.Split(context.Request.Url.AbsolutePath, "api.restful/", RegexOptions.IgnoreCase).ToList();

                // Validate the Application path
                if (RequestParams.Count() != 2)
                {
                    ParseError(ref context, HttpStatusCode.BadRequest, "Invalid Head Request handler");
                }
                // Get the Static parameters
                RequestParams = RequestParams[1].Split('/').ToList();
                Log.Instance.Info("Request params: " + Utility.JsonSerialize_IgnoreLoopingReference(RequestParams));

                // Validate the request
                if (RequestParams.Count() == 0)
                {
                    ParseError(ref context, HttpStatusCode.BadRequest, "Invalid Head Request parameters");
                }

                // Verify the method exists
                if (!ValidateMethod(RequestParams))
                {
                    ParseError(ref context, HttpStatusCode.BadRequest, "Head Request method not found");
                }
            }
            catch (Exception e)
            {
                Log.Instance.Fatal(e);
                ParseError(ref context, HttpStatusCode.BadRequest, e.Message);
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
        /// Handle reusable IHttpHandler instances 
        /// </summary>
        public bool IsReusable
        {
            // Set to false to ensure thread safe operations
            get { return true; }
        }
        #endregion 
    }

    public class Head_API : IRequest
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

        public HttpCookie sessionCookie { get { return null; } set { } }

        NameValueCollection IRequest.httpGET { get { return null; } set { } }
        #endregion
    }

    /// <summary>
    /// Define the Output structure required by the exposed API
    /// </summary>
    public class Head_Output : IResponseOutput
    {
        #region Properties
        /// <summary>
        /// RESTful response
        /// </summary>
        public dynamic response { get; set; }

        /// <summary>
        /// RESTful mime type
        /// </summary>
        public string mimeType { get; set; }

        /// <summary>
        /// RESTful status code
        /// </summary>
        public HttpStatusCode statusCode { get; set; }

        /// <summary>
        /// RESTful filename (optional)
        /// </summary>
        public string fileName { get; set; }

        /// <summary>
        /// Session Cookie
        /// </summary>
        public HttpCookie sessionCookie { get; set; }
        public dynamic data { get; set; }
        public dynamic error { get; set; }
        #endregion
    }
}
