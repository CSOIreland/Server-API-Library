using Microsoft.AspNetCore.Http;
using System.Data;
using System.Diagnostics;
using System.Net;

namespace API
{
    public class APIMiddleware : Common
    {

        private readonly RequestDelegate _next;
        public static AsyncLocal<DataTable> cacheTraceDataTable = new AsyncLocal<DataTable>();
        public static AsyncLocal<DataTable> databaseTraceDataTable = new AsyncLocal<DataTable>();
        public static AsyncLocal<string> correlationID = new AsyncLocal<string>();

        public APIMiddleware(RequestDelegate next) : base()
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Trace trace = new Trace();
            trace.TrcStartTime = DateTime.Now;

            // Initiate the activity
            var activity = Activity.Current;

            log4net.LogicalThreadContext.Properties["correlationID"] = activity.RootId;

            //set asynclocal value
            correlationID.Value = activity.RootId.ToString();
            if (Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_CACHE_TRACE_ENABLED"])) { 
                if (cacheTraceDataTable.Value == null) {

                    cacheTraceDataTable.Value = CacheTrace.CreateCacheTraceDataTable();
                }
            }

            if (Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_DATABASE_TRACE_ENABLED"]))
            {
                if (databaseTraceDataTable.Value == null)
                {
                    databaseTraceDataTable.Value = DatabaseTrace.CreateDatabaseTraceDataTable();
                }
            }

            if (!ApiServicesHelper.ApplicationLoaded)
            {
                Log.Instance.Fatal("No API configuration loaded.");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync("");
            }
            else
            {
   
                // Start the activity Stopwatch
                activity.Start();

                Log.Instance.Info("API Interface Opened");

                // Thread a PerfomanceCollector

                /// <summary>
                //    /// Flag to indicate if Performance is enabled 
                //    /// </summary>
                bool API_PERFORMANCE_ENABLED = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_PERFORMANCE_ENABLED"]);

                Log.Instance.Info("Performance Enabled: " + API_PERFORMANCE_ENABLED);


                PerformanceCollector performanceCollector = new PerformanceCollector();

                var cancelPerformance = new CancellationTokenSource();

                Thread performanceThread = new Thread(() =>
                {
                    try
                    {
                        performanceCollector.CollectData(cancelPerformance.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        performanceCollector.Dispose();
                        cancelPerformance.Cancel(true);
                        Log.Instance.Info("performance thread cancelled");
                    }
                });


                CancellationTokenSource apiCancellationToken = new CancellationTokenSource();

                try
                {
                    //https://devblogs.microsoft.com/dotnet/re-reading-asp-net-core-request-bodies-with-enablebuffering/
                    context.Request.EnableBuffering();
                    string incomingUrl = context.Request.Path.ToString();
                    var requestMethod = context.Request.Method;


                    if (ApiServicesHelper.BlockedRequests.urls != null)
                    {
                        for (int i = 0; i < ApiServicesHelper.BlockedRequests.urls.Count; i++)
                        {
                            string s = ApiServicesHelper.BlockedRequests.urls[i];
                            //check to see if it is a blocked request, e.g. favicon.ico makes a browser request but we dont need to process 
                            //as will cause an error so best to return
                            if (incomingUrl.StartsWith(s, StringComparison.InvariantCultureIgnoreCase))
                            {
                                context.Response.StatusCode = StatusCodes.Status404NotFound;
                                return;
                            }
                        }
                    }
                    //set the trace verb
                    trace.TrcRequestVerb = requestMethod;

                    incomingUrl = incomingUrl.ToLower();
                    switch (true)
                    {
                        case bool b when incomingUrl.Contains("/api.jsonrpc", StringComparison.InvariantCultureIgnoreCase):
                            trace.TrcRequestType = "jsonrpc";

                            //check that API type is allowed and that http method is allowed
                            if (ApiServicesHelper.APISettings.jsonrpc == null || !ApiServicesHelper.APISettings.jsonrpc.allowed || !ApiServicesHelper.APISettings.jsonrpc.verb.Contains(requestMethod))
                            {
                                await returnResponseAsync(context, "Unsupported Request Type!", apiCancellationToken, HttpStatusCode.MethodNotAllowed);
                            }
                            else
                            {
                                JSONRPC jsonrpc = new JSONRPC();

                                jsonrpc.middlewareType = "jsonrpc";

                                // Validate that allowed http methods are being used
                                if (!jsonrpc.AllowedHTTPMethods.Contains(requestMethod))
                                {
                                    Log.Instance.Info("Invalid HHTP Request Method : " + requestMethod + " : for jsonrpc api");
                                    await returnResponseAsync(context, "Unsupported Request Type!", apiCancellationToken, HttpStatusCode.MethodNotAllowed);
                                }
                                else
                                {
                                    await jsonrpc.ProcessRequest(context, apiCancellationToken, performanceThread, API_PERFORMANCE_ENABLED, trace);
                                }
                            }
                            break;
                        case bool b when incomingUrl.Contains("/api.restful", StringComparison.InvariantCultureIgnoreCase):
                            trace.TrcRequestType = "restful";
                            
                            //check that API type is allowed and that http method is allowed
                            if (ApiServicesHelper.APISettings.restful == null || !ApiServicesHelper.APISettings.restful.allowed ||
                            !ApiServicesHelper.APISettings.restful.verb.Contains(requestMethod))
                            {
                                await returnResponseAsync(context, "Unsupported Request Type!", apiCancellationToken, HttpStatusCode.MethodNotAllowed);
                            }
                            else
                            {
                                RESTful restful = new RESTful();
                                restful.middlewareType = "restful";

                                // Validate that allowed http methods are being used
                                if (!restful.AllowedHTTPMethods.Contains(requestMethod))
                                {
                                    Log.Instance.Info("Invalid HHTP Request Method : " + requestMethod + " : for restful api");
                                    await returnResponseAsync(context, "Unsupported Request Type!", apiCancellationToken, HttpStatusCode.MethodNotAllowed);
                                }
                                else
                                {
                                    await restful.ProcessRequest(context, apiCancellationToken, performanceThread, API_PERFORMANCE_ENABLED, trace);
                                }
                            }
                            break;
                        case bool b when incomingUrl.Contains("/api.static", StringComparison.InvariantCultureIgnoreCase):
                            trace.TrcRequestType = "static";
                            //check that API type is allowed and that http method is allowed
                            if (ApiServicesHelper.APISettings.Static == null || !ApiServicesHelper.APISettings.Static.allowed ||
                            !ApiServicesHelper.APISettings.Static.verb.Contains(requestMethod))
                            {
                                await returnResponseAsync(context, "Unsupported Request Type!", apiCancellationToken, HttpStatusCode.MethodNotAllowed);
                            }
                            else
                            {
                                Static Static = new Static();
                                Static.middlewareType = "static";

                                // Validate that allowed http methods are being used
                                if (!Static.AllowedHTTPMethods.Contains(requestMethod))
                                {
                                    Log.Instance.Info("Invalid HHTP Request Method : " + requestMethod + " : for static api");
                                    await returnResponseAsync(context, "Unsupported Request Type!", apiCancellationToken, HttpStatusCode.MethodNotAllowed);
                                }
                                else
                                {
                                    // Set Mime-Type for the Content Type and override the Charset
                                    context.Response.ContentType = null;

                                    // Set CacheControl to public 
                                    context.Response.Headers.Append("Cache-Control", "public");

                                    // Check if the client has already a cached record
                                    string rawIfModifiedSince = context.Request.Headers["If-Modified-Since"];
                                    Log.Instance.Info(" rawModifiedSince:" + rawIfModifiedSince);
                                    if (!string.IsNullOrEmpty(rawIfModifiedSince))
                                    {
                                        if(DateTime.TryParse(rawIfModifiedSince, out  DateTime modifiedSince))
                                        {
                                            if (modifiedSince.AddYears(1)>DateTime.Now)
                                            {
                                                // Do not process the request at all, stop here.
                                                context.Response.StatusCode = StatusCodes.Status304NotModified;
                                                Log.Instance.Info("Response Status: " + context.Response.StatusCode);
                                            }
                                            else
                                            {
                                                Log.Instance.Info("Response Status: " + context.Response.StatusCode);
                                                await Static.ProcessRequest(context, apiCancellationToken, performanceThread, API_PERFORMANCE_ENABLED, trace);
                                            }

                                        }
                                        else
                                        {
                                            Log.Instance.Info("Response Status: " + context.Response.StatusCode);
                                            await Static.ProcessRequest(context, apiCancellationToken, performanceThread, API_PERFORMANCE_ENABLED, trace);
                                        }
                                    }
                                    else
                                    {
                                        Log.Instance.Info("Response Status: " + context.Response.StatusCode);
                                        await Static.ProcessRequest(context, apiCancellationToken, performanceThread, API_PERFORMANCE_ENABLED, trace);
                                    }
                                }
                            }
                            break;
                        default:
                            await returnResponseAsync(context, "", apiCancellationToken, HttpStatusCode.BadRequest);
                            break;
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
                catch (Exception ex)
                {
                    Log.Instance.Fatal(ex);
                    await returnResponseAsync(context, "", apiCancellationToken, HttpStatusCode.InternalServerError);
                }
                finally
                {
                    if (context.Response.StatusCode != (int)HttpStatusCode.NotModified)
                    {

                        //IF NO RESPONSE SENT AT ALL SEND 204 STATUS CODE
                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                            await context.Response.WriteAsync("");
                        }
                    }

                    apiCancellationToken.Cancel(true);
                    apiCancellationToken.Dispose();
                    // Terminate Performance collection
                    cancelPerformance.Cancel(true);//safely cancel thread    
                    cancelPerformance.Dispose();
                    // Stop the activity
                    activity.Stop();
                    
                    Log.Instance.Info("API Execution Time (s): " + ((float)Math.Round(activity.Duration.TotalMilliseconds / 1000, 3)).ToString());
                    Log.Instance.Info("API Interface Closed");



                    if (Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_TRACE_ENABLED"]))
                    {
                       
                            trace.TrcStatusCode = context.Response.StatusCode;
                            trace.TrcDuration = ((float)Math.Round(activity.Duration.TotalMilliseconds / 1000, 3));
                            trace.TrcMachineName = System.Environment.MachineName;
                            trace.TrcUseragent = ApiServicesHelper.WebUtility.GetUserAgent();
                            trace.TrcIp = ApiServicesHelper.WebUtility.GetIP();
                            trace.TrcCorrelationID = activity.RootId;
                            //trace parameters

                            if (ActiveDirectory.IsAuthenticated(UserPrincipal))
                            {
                                trace.TrcUsername = UserPrincipal.SamAccountName.ToString();
                            }

                            if (string.IsNullOrEmpty(trace.TrcMethod))
                                trace.TrcErrorPath = MaskParameters(context.Request.Path.ToString());

                            Trace_ADO.Create(trace);

                    }

                    if (Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_CACHE_TRACE_ENABLED"]))
                    {
                        //store the cache trace info
                        CacheTrace_ADO.Create(cacheTraceDataTable.Value);
                        cacheTraceDataTable.Value.Dispose();
                        cacheTraceDataTable.Value = null;
                    }

                    if (Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_DATABASE_TRACE_ENABLED"]))
                    {
                        //store the database trace info
                        DatabaseTrace_ADO.Create(databaseTraceDataTable.Value);
                        databaseTraceDataTable.Value.Dispose();
                        databaseTraceDataTable.Value = null;
                    }
                }
            }
        }
    }
}