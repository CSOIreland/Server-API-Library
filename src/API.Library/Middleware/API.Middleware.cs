using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
            // Initiate the activity
            Activity activity = Activity.Current;

            log4net.LogicalThreadContext.Properties["correlationID"] = activity.RootId;

            //set asynclocal value
            correlationID.Value = activity.RootId.ToString();

           
            Trace trace = new Trace();

           if (ApiServicesHelper.ApiConfiguration.API_TRACE_ENABLED)
                {
                    trace.TrcStartTime = DateTime.Now;
                    trace.TrcCorrelationID = activity.RootId;
                    trace.TrcMachineName = System.Environment.MachineName;
                    trace.TrcUseragent = ApiServicesHelper.WebUtility.GetUserAgent();
                    trace.TrcIp = ApiServicesHelper.WebUtility.GetIP();
                    trace.TrcContentLength = context.Request.ContentLength;
                    trace.TrcReferrer = context.Request.Headers["Referer"].ToString();

                  

                }

           // Start the activity Stopwatch
           activity.Start();

               
           Log.Instance.Info("API Interface Opened");

           // Thread a PerfomanceCollector

           /// <summary>
                /// Flag to indicate if Performance is enabled 
                /// </summary>
           bool API_PERFORMANCE_ENABLED = ApiServicesHelper.APIPerformanceSettings.API_PERFORMANCE_ENABLED;

           Log.Instance.Info("Performance Enabled: " + API_PERFORMANCE_ENABLED);

           //makes sure always disposed
           using var performanceCollector = new PerformanceCollector();
           using var cancelPerformance = new CancellationTokenSource();
           using var apiCancellationToken = new CancellationTokenSource();

           try{   
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
                        catch (Exception ex)
                        {
                            performanceCollector.Dispose();
                            cancelPerformance.Cancel(true);
                            Log.Instance.Error("Something went wrong with performance thread");
                            Log.Instance.Error(ex);
                        }
                    });


                    if (ApiServicesHelper.CacheConfig.API_CACHE_TRACE_ENABLED)
                    {
                        if (cacheTraceDataTable.Value == null)
                        {
                            cacheTraceDataTable.Value = CacheTrace.CreateCacheTraceDataTable();
                        }
                    }

                    if (ApiServicesHelper.DatabaseTracingConfiguration.API_DATABASE_TRACE_ENABLED)
                    {
                        if (databaseTraceDataTable.Value == null)
                        {
                            databaseTraceDataTable.Value = DatabaseTrace.CreateDatabaseTraceDataTable();
                        }
                    }

      
                    ApiServicesHelper.ApiConfiguration.Refresh();

                    if (ApiServicesHelper.APPConfig.enabled)
                    {
                      ApiServicesHelper.AppConfiguration.Refresh();
                    }
              

                    //https://devblogs.microsoft.com/dotnet/re-reading-asp-net-core-request-bodies-with-enablebuffering/
                    context.Request.EnableBuffering();
                    string incomingUrl = context.Request.Path.ToString();
                    var requestMethod = context.Request.Method;
                    //set the trace verb
                    trace.TrcRequestVerb = requestMethod;

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
           }catch (OperationCanceledException e){
             //don't need to do anything here as operation has been cancelled
           }catch (Exception ex){
                Log.Instance.Fatal(ex);
                await returnResponseAsync(context, "", apiCancellationToken, HttpStatusCode.InternalServerError);
           }finally{
                try{

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

                        if (ApiServicesHelper.ApiConfiguration.API_TRACE_ENABLED)
                        {

                            trace.TrcStatusCode = context.Response.StatusCode;
                            trace.TrcDuration = ((float)Math.Round(activity.Duration.TotalMilliseconds / 1000, 3));

                            //trace parameters

                            if (ActiveDirectory.IsAuthenticated(UserPrincipal))
                            {
                                trace.TrcUsername = UserPrincipal.SamAccountName.ToString();
                            }


                            if (string.IsNullOrEmpty(trace.TrcMethod))
                            {
                                trace.TrcErrorPath = MaskParameters(context.Request.Path.ToString());
                            }

                            Trace_ADO.Create(trace);

                        }

                        if (ApiServicesHelper.CacheConfig.API_CACHE_TRACE_ENABLED)
                        {
                            //store the cache trace info
                            CacheTrace_ADO.Create(cacheTraceDataTable.Value);
                            cacheTraceDataTable.Value.Dispose();
                            cacheTraceDataTable.Value = null;
                        }

                        if (ApiServicesHelper.DatabaseTracingConfiguration.API_DATABASE_TRACE_ENABLED)
                        {
                            //store the database trace info
                            DatabaseTrace_ADO.Create(databaseTraceDataTable.Value);
                            databaseTraceDataTable.Value.Dispose();
                            databaseTraceDataTable.Value = null;
                        }
                }catch (Exception ex){
                    Log.Instance.Error("Something went wrong after response sent");
                    Log.Instance.Error(ex);
                }

           }
        }
    }
}