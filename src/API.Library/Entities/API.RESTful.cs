using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.SessionState;

namespace API
{
    /// <summary>
    /// RESTful implementation
    /// </summary>
    public class RESTful : Common, IHttpHandler, IRequiresSessionState
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

                // Set Mime-Type for the Content Type and override the Charset
                context.Response.Charset = null;
                // Set CacheControl to no-cache
                context.Response.CacheControl = "no-cache";

                // Extract the request parameters from the URL
                ParseRequest(ref context);

                // Check for the maintenance flag
                if (Maintenance)
                {
                    ParseError(ref context, HttpStatusCode.ServiceUnavailable, "System maintenance");
                }

                // Get Session Cookie
                HttpCookie sessionCookie = null;
                if (!String.IsNullOrEmpty(SessionCookieName))
                {
                    sessionCookie = context.Request.Cookies[SessionCookieName];
                }

                RESTful_Output result = null;

                bool? isAuthenticated = Authenticate(ref context);
                switch (isAuthenticated)
                {
                    case null: //Anonymous authentication
                        performanceThread.Start();
                        result = GetResult(ref context, sessionCookie);
                        break;
                    case true: //Windows Authentication
                        performanceThread.Start();
                        result = GetResult(ref context);
                        break;
                    case false: //Error
                        ParseError(ref context, HttpStatusCode.InternalServerError, "Internal Error");
                        break;
                }

                if (result == null)
                {
                    ParseError(ref context, HttpStatusCode.InternalServerError, "Internal Error");
                }
                else if (result.statusCode == HttpStatusCode.OK)
                {
                    context.Response.StatusCode = (int)result.statusCode;
                    context.Response.ContentType = result.mimeType;

                    // Set the Session Cookie if requested
                    if (!String.IsNullOrEmpty(SessionCookieName) && result.sessionCookie != null && result.sessionCookie.Name.Equals(SessionCookieName))
                    {
                        // No expiry time allowed in the future
                        if (result.sessionCookie.Expires > DateTime.Now)
                        {
                            result.sessionCookie.Expires = default;
                        }

                        result.sessionCookie.Secure = true;
                        result.sessionCookie.Domain = null;
                        result.sessionCookie.HttpOnly = true;
                        result.sessionCookie.SameSite = SameSiteMode.Strict;
                        context.Response.Cookies.Add(result.sessionCookie);
                    }


                    if (!String.IsNullOrEmpty(result.fileName))
                    {
                        context.Response.AppendHeader("Content-Disposition", new ContentDisposition { Inline = true, FileName = result.fileName }.ToString());
                    }

                    if (result.response?.GetType() == typeof(byte[]))
                    {
                        context.Response.BinaryWrite(result.response);
                    }
                    else
                    {
                        context.Response.Write(result.response);
                    }
                }
                else
                {
                    ParseError(ref context, result.statusCode, result.response);
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
                Log.Instance.Info("URL Absolute Path: " + context.Request.Url.AbsolutePath);
                RequestParams = Regex.Split(context.Request.Url.AbsolutePath, "api.restful/", RegexOptions.IgnoreCase).ToList();

                // Validate the Application path
                if (RequestParams.Count() != 2)
                {
                    ParseError(ref context, HttpStatusCode.BadRequest, "Invalid RESTful handler");
                }
                // Get the RESTful parameters
                RequestParams = RequestParams[1].Split('/').ToList();
                Log.Instance.Info("Request params: " + Utility.JsonSerialize_IgnoreLoopingReference(RequestParams));

                // Validate the request
                if (RequestParams.Count() == 0)
                {
                    ParseError(ref context, HttpStatusCode.BadRequest, "Invalid RESTful parameters");
                }

                // Verify the method exists
                if (!ValidateMethod(RequestParams))
                {
                    ParseError(ref context, HttpStatusCode.BadRequest, "RESTful method not found");
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
                Type StaticClass = currentassembly.GetType(methodPath, false, true);
                if (StaticClass != null)
                {
                    if (StaticClass.CustomAttributes.Where(x => x.AttributeType.Name == "AllowAPICall").ToList().Count == 0) { break; }
                    MethodInfo methodInfo = StaticClass.GetMethod(methodName, new Type[] { typeof(RESTful_API) });
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
        /// <returns></returns>
        private dynamic GetResult(ref HttpContext context, HttpCookie sessionCookie = null)
        {
            // Set the API object
            RESTful_API apiRequest = new RESTful_API();
            apiRequest.method = RequestParams[0];
            apiRequest.parameters = RequestParams;
            apiRequest.userPrincipal = UserPrincipal;
            apiRequest.ipAddress = Utility.GetIP();
            apiRequest.userAgent = Utility.GetUserAgent();
            apiRequest.httpGET = httpGET;
            apiRequest.httpPOST = httpPOST;
            apiRequest.sessionCookie = sessionCookie;

            // Hide password from logs
            Log.Instance.Info("API Request: " + Utility.JsonSerialize_IgnoreLoopingReference(apiRequest));

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
    public class RESTful_Output
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
        #endregion
    }

    /// <summary>
    /// Define the API Class to pass to the exposed API 
    /// </summary>
    public class RESTful_API
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

        /// <summary>
        /// Session Cookie
        /// </summary>
        public HttpCookie sessionCookie { get; internal set; }
        #endregion
    }
}
