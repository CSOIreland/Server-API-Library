using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.DirectoryServices.AccountManagement;
using System.Dynamic;
using System.Net;
using System.Reflection;
using System.Text;
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
        /// the type of middleware that the request is.
        /// </summary>
        public string middlewareType = null;

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
        internal async Task returnResponseAsync(HttpContext context, string message, CancellationTokenSource sourceToken, HttpStatusCode statusCode, MemoryStream memoryStream = null)
        {
            Log.Instance.Info("Returning response");
            //check if already cancelled
            if (sourceToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            if (context.Response.HasStarted)
            {
                sourceToken.Cancel(true);

                if (sourceToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
            }
            else
            {
                context.Response.StatusCode = context.Response.StatusCode == 0 ? (int)statusCode : context.Response.StatusCode;
                if (memoryStream != null)
                {
                    await memoryStream.CopyToAsync(context.Response.Body, sourceToken.Token);
                }
                else
                {


                    await context.Response.WriteAsync(message);

                }

                await context.Response.CompleteAsync();


                sourceToken.Cancel(true);

                if (sourceToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
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
        
        internal void GatherTraceInformation(IRequest apiRequest, Trace trace)
        {
            if (ApiServicesHelper.ApiConfiguration.API_TRACE_ENABLED)
            {
                Type type = apiRequest.GetType();
                if (type == typeof(JSONRPC_API))
                {
                    trace.TrcParams = MaskParameters(apiRequest.parameters.ToString());
                }
                else if (type == typeof(RESTful_API) || type == typeof(Static_API))
                {
                    //in non jsonrpc its a list of strings
                    trace.TrcParams = MaskParameters(apiRequest.parameters[0]);
                }

                //gather trace information
                trace.TrcIp = apiRequest.ipAddress;
                trace.TrcUseragent = apiRequest.userAgent;
                trace.TrcMethod = apiRequest.method;

                if (apiRequest.userDetail != null)
                {
                    trace.TrcUsername = apiRequest.userDetail.Identity;
                }
            }
        }

        /// <summary>
        /// method to check if the an api call is allowed and return the methodinfo if it is
        /// </summary>
        internal static MethodInfo? CheckAPICallsAllowed(string methodName, string methodPath, dynamic typeOfClassType)
        {
           
            //create key for the dictionary
            dynamic jsonObj = new ExpandoObject();
            jsonObj.methodName = methodName;
            jsonObj.methodPath = methodPath;
            jsonObj.methodType = typeOfClassType.Name; //Fixes bug where previous RESTful call breaks subsequent JSON-rpc calls and vice versa

            string serializedAPIInfo = Utility.JsonSerialize_IgnoreLoopingReference(jsonObj);

            //if already in dictionary no need to find again
            if (AttributeDictionary.AllowedAPIDictionary.ContainsKey(serializedAPIInfo))
            {
                //return the methodInfo based on the methodInfo handle
                MethodInfo m2 = MethodBase.GetMethodFromHandle(AttributeDictionary.AllowedAPIDictionary[serializedAPIInfo]) as MethodInfo;
                return m2;
            }else if(!EndpointCache.findApi(methodPath + "." + methodName))
            {
                    //if not in the white list bounce
                 return null;
            }

            // Search in the Assemplies 
            var allAssemblies = EndpointCache.GetListAssemblies();// AppDomain.CurrentDomain.GetAssemblies();

            var calledClass = allAssemblies.Select(y => y.GetType(methodPath, false, true)).Where(p => p != null).FirstOrDefault();


            if (calledClass != null)
            {
                if (calledClass.FullName.Trim().Equals(methodPath.Trim()))
                {

                    if (calledClass.CustomAttributes.Where(xx => xx.AttributeType.Name == "AllowAPICall").ToList().Count > 0)
                    {
                        MethodInfo methodInfo = null;
                        methodInfo = calledClass.GetMethod(methodName, new Type[] { typeOfClassType });

                        if (methodInfo == null)
                        {
                            return null;
                        }
                        else
                        {
                            //get the methods handle
                            RuntimeMethodHandle handle = methodInfo.MethodHandle;

                            //add handle to dictionary for future lookup
                            try
                            {
                                if (!AttributeDictionary.AllowedAPIDictionary.TryAdd(serializedAPIInfo, handle))
                                {
                                    Log.Instance.Debug("Adding : " + serializedAPIInfo + " to dictionary 'CheckAPICallsAllowed' failed");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error(ex);
                            }

                            return methodInfo;
                        }
                    }
                }
            }

            return null;
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
