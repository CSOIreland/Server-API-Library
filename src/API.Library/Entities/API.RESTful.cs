﻿using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;
using System.Dynamic;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.RegularExpressions;

namespace API
{
    /// <summary>
    /// RESTful implementation
    /// </summary>
    public class RESTful : Common
    {

        #region Properties
        /// <summary>
        ///  List of URL Request Parameters
        /// </summary>
        private List<string> RequestParams = new List<string>();
        #endregion

        /// <summary>
        ///  allowed http methods
        /// </summary>
        public string[] AllowedHTTPMethods = new string[] { "GET", "POST","HEAD" };


        public RESTful() : base()
           {
        }



        #region Methods
        /// <summary>
        /// ProcessRequest executed automatically by the iHttpHandler interface
        /// </summary>
        /// <param name="httpContext"></param>
        public async Task ProcessRequest(HttpContext httpContext, CancellationTokenSource apiCancellationToken, Thread performanceThread, bool API_PERFORMANCE_ENABLED)
        {
            // Were we already canceled?
            apiCancellationToken.Token.ThrowIfCancellationRequested();

            try
            {
                Log.Instance.Info("Starting Restful processing");

                httpGET = GetHttpGET(httpContext);
                httpPOST = await GetHttpPOST(httpContext);
                
                // Set Mime-Type for the Content Type and override the Charset
                httpContext.Response.ContentType = $"charset=utf-8";

                // Set CacheControl to no-cache
                httpContext.Response.Headers.Append("Cache-Control", "no-cache");

                // Extract the request parameters from the URL
                await ParseRequest(httpContext, apiCancellationToken);

                // Check for the maintenance flag
                if (Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.MAINTENANCE))
                {
                    await ParseError(httpContext, HttpStatusCode.ServiceUnavailable, apiCancellationToken, "System maintenance");
                }

                string SessionCookieName = ApiServicesHelper.ApiConfiguration.Settings["API_SESSION_COOKIE"];
                // Get Session Cookie
                Cookie sessionCookie = new Cookie();
                sessionCookie = null;

                if (!string.IsNullOrEmpty(SessionCookieName))
                {
                    //need to create a cookie using the value and  the SessionCookieName
                    var testSessionCookieValue = httpContext.Request.Cookies[SessionCookieName];
                    if (!string.IsNullOrEmpty(testSessionCookieValue))
                    {
                        sessionCookie.Name = SessionCookieName;
                        sessionCookie.Value = testSessionCookieValue;
                    }
                }

                IResponseOutput result = null;

                bool? isAuthenticated = Authenticate(ref httpContext);
                try
                {
                    switch (isAuthenticated)
                    {
                        case null: //Anonymous authentication
                            if (API_PERFORMANCE_ENABLED)
                            {
                                performanceThread.Start();
                            }
                            result = GetResult(httpContext, sessionCookie);
                            break;
                        case true: //Windows Authentication
                            if (API_PERFORMANCE_ENABLED)
                            {
                                performanceThread.Start();
                            }
                            result = GetResult(httpContext);
                            break;
                        case false: //Error
                            await ParseError(httpContext, HttpStatusCode.InternalServerError, apiCancellationToken, "Internal Error");
                            break;
                    }
                }
                catch (Exception e)
                {
                    await ParseError(httpContext, HttpStatusCode.InternalServerError, apiCancellationToken, "Internal Error");
                }

                if (result == null)
                {
                    await ParseError(httpContext, HttpStatusCode.InternalServerError, apiCancellationToken, "Internal Error");
                }
                else if (result.statusCode == HttpStatusCode.OK)
                {
                    if(httpContext.Request.Method == "HEAD")
                    {
                        // httpContext.Response.StatusCode = (int)HttpStatusCode.OK;


                        if (!String.IsNullOrEmpty(result.mimeType))
                        {
                            httpContext.Response.Headers.Add("Content-Type", result.mimeType);

                        }

                        await returnResponseAsync(httpContext, result.response, apiCancellationToken, HttpStatusCode.OK);

                    }
                    else
                    {
                       // httpContext.Response.StatusCode = (int)result.statusCode;
                        httpContext.Response.ContentType = result.mimeType;                    

                        // Set the Session Cookie if requested
                        if (!string.IsNullOrEmpty(SessionCookieName) && result.sessionCookie != null && result.sessionCookie.Name.Equals(SessionCookieName))
                        {
                            // No expiry time allowed in the future
                            if (result.sessionCookie.Expires > DateTime.Now)
                            {
                                result.sessionCookie.Expires = default;
                            }
                            var cookieOptions = new CookieOptions
                            {
                                Secure = true,
                                HttpOnly = true,
                                Domain = null,
                                SameSite = SameSiteMode.Strict,
                                Expires = result.sessionCookie.Expires
                            };

                            // Add the cookie to the response cookie collection
                            httpContext.Response.Cookies.Append(SessionCookieName, result.sessionCookie.Value, cookieOptions);
                        }


                        if (!string.IsNullOrEmpty(result.fileName))
                        {
                            httpContext.Response.Headers.Add("Content-Disposition", new ContentDisposition { DispositionType = DispositionTypeNames.Attachment, Inline = true, FileName = result.fileName }.ToString());
                        }

                        if (result.response?.GetType() == typeof(byte[]))
                        {
                            Stream stream = new MemoryStream(result.response);
                            var fullFileName = Path.GetTempFileName();
                            File.WriteAllBytes(fullFileName, result.response);
                            await returnResponseAsync(httpContext, fullFileName, apiCancellationToken, HttpStatusCode.OK,true);
                       }
                        else
                        {
                            await returnResponseAsync(httpContext, result.response, apiCancellationToken, result.statusCode);
                        }
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
                Log.Instance.Fatal(e);
                await returnResponseAsync(httpContext, "", apiCancellationToken, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Parse the API error returning a HTTP status
        /// </summary>
        /// <param name="context"></param>
        /// <param name="statusCode"></param>
        /// <param name="statusDescription"></param>
        /// 
        private async Task ParseError(HttpContext context, HttpStatusCode statusCode, CancellationTokenSource sourceToken, string statusDescription = "")
        {
            Log.Instance.Info("IP: " + ApiServicesHelper.WebUtility.GetIP() + ", Status Code: " + statusCode.ToString() + ", Status Description: " + statusDescription);

            if (!string.IsNullOrEmpty(statusDescription))
                await returnResponseAsync(context, Utility.JsonSerialize_IgnoreLoopingReference(statusDescription), sourceToken, statusCode);
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

                string requestType = context.Request.Method.ToLower();

                if(requestType != "head")
                {
                    requestType = "RESTful";
                }
                // Read the URL parameters and split the URL Absolute Path
                Log.Instance.Info("URL Absolute Path: " + context.Request.Path);

                RequestParams = Regex.Split(context.Request.Path, "api.restful/", RegexOptions.IgnoreCase).ToList();

                // Validate the Application path
                if (RequestParams.Count() != 2)
                {
                   await ParseError(context, HttpStatusCode.BadRequest, sourceToken, "Invalid " + requestType+" handler");
                }

                // Get the RESTful parameters
                RequestParams = RequestParams[1].Split('/').ToList();

                Log.Instance.Info("Request params: " + Utility.JsonSerialize_IgnoreLoopingReference(RequestParams));

                // Validate the request
                if (RequestParams.Count() == 0)
                {
                    await ParseError(context, HttpStatusCode.BadRequest, sourceToken, "Invalid " + requestType + "  parameters");
                }

                // Verify the method exists
                if (!ValidateMethod(RequestParams))
                {
                    await ParseError(context, HttpStatusCode.BadRequest, sourceToken, requestType + " method not found");
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

                        MethodInfo methodInfo = calledClass.GetMethod(methodName, new Type[] { typeof(RESTful_API) });
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
        private dynamic GetResult(HttpContext context, Cookie sessionCookie = null)
        {
            // Set the API object
            RESTful_API apiRequest = new RESTful_API();
            apiRequest.method = RequestParams[0];
            apiRequest.parameters = RequestParams;
            apiRequest.userPrincipal = UserPrincipal;
            apiRequest.ipAddress = ApiServicesHelper.WebUtility.GetIP();
            apiRequest.userAgent = ApiServicesHelper.WebUtility.GetUserAgent();
            apiRequest.requestType = context.Request.Method;
            apiRequest.httpGET = httpGET;
            apiRequest.httpPOST = httpPOST;
            apiRequest.sessionCookie = sessionCookie;
            apiRequest.requestHeaders = context.Request.Headers;
            apiRequest.scheme = context.Request.Scheme;

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


        public static IResponseOutput FormatRestfulError(IResponseOutput response, string mimeType = null, HttpStatusCode statusCode4NoContent = HttpStatusCode.NoContent, HttpStatusCode statusCode4Error = HttpStatusCode.InternalServerError)
        {
            if (response == null)
            {
                return null;
            }
            else if (response.error != null)
            {
                return new RESTful_Output
                {
                    mimeType = null,
                    statusCode = statusCode4Error,
                    response = response.error
                };
            }
            else if (response.data == null)
            {
                return new RESTful_Output
                {
                    mimeType = null,
                    statusCode = statusCode4NoContent,
                    response = response.data
                };
            }

            return response;
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
    public class RESTful_Output : IResponseOutput
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
        public Cookie sessionCookie { get; set; }
        public dynamic data { get; set; }
        public dynamic error { get; set; }
        #endregion
    }

    /// <summary>
    /// Define the API Class to pass to the exposed API 
    /// </summary>
    public class RESTful_API : IRequest
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
        #endregion
        /// <summary>
        /// Request Type
        /// </summary>
        public string requestType { get; set; }


        /// <summary>
        /// Request Headers
        /// </summary>
        public IHeaderDictionary requestHeaders { get; set; }
       
        /// <summary>
        /// Request scheme
        /// </summary>
        public string scheme { get; set; }
    }
}
