using Enyim.Caching;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddApiLibrary(this IServiceCollection service, WebApplicationBuilder builder)
        {
            var loggingOptions = builder.Configuration.GetSection("Log4NetCore").Get<Log4NetProviderOptions>();
            
            //log name of the machine for identification purposes
            log4net.GlobalContext.Properties["MachineName"] = System.Environment.MachineName;
            builder.Logging.AddLog4Net(loggingOptions);
          
            //watches for any changes to log4net config file
            XmlConfigurator.ConfigureAndWatch(new FileInfo(loggingOptions.Log4NetConfigFileName));

            Log.Instance.Info("service configration started");
               
            // Add services to the container.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto; 
            });
               
            builder.Services.AddHttpContextAccessor();
               
            service.Configure<ADOSettings>(builder.Configuration.GetSection("ADOSettings"));               
            service.Configure<APIConfig>(builder.Configuration.GetSection("API_Config"));               
            service.Configure<APPConfig>(builder.Configuration.GetSection("APP_Config"));               
            service.Configure<APISettings>(builder.Configuration.GetSection("API_SETTINGS"));               
            service.Configure<BlockedRequests>(builder.Configuration.GetSection("Blocked_Requests"));               
              
            service.AddEnyimMemcached();               
            service.AddSingleton<IApiConfiguration, ApiConfiguration>();               
            service.AddSingleton<IAppConfiguration, APPConfiguration>();               
            service.AddSingleton<IWebUtility, WebUtility>();                          
            service.AddSingleton<IMemcachedClient, MemcachedClient>();              
            service.AddSingleton<ICacheD, MemCacheD>();
            service.AddSingleton<IActiveDirectory, ActiveDirectory>();
            service.AddSingleton<IFirebase, Firebase>();

            service.AddScoped<IADO, ADO>();

            service.AddSingleton<ICleanser, Cleanser>();
            service.AddSingleton<ISanitizer, Sanitizer>();

            var sp = service.BuildServiceProvider();
            var ADOSettings = sp.GetService<IOptions<ADOSettings>>();
            var APISettings = sp.GetService<IOptions<APISettings>>();
            var BlockedRequests = sp.GetService<IOptions<BlockedRequests>>();
            var APIConfig = sp.GetService<IOptions<APIConfig>>();
            var APPConfig = sp.GetService<IOptions<APPConfig>>();

            ApiServicesHelper.ServiceProvider = sp;
            ApiServicesHelper.Configuration = builder.Configuration;
            ApiServicesHelper.ADOSettings = ADOSettings.Value;
            ApiServicesHelper.APISettings = APISettings.Value;
            ApiServicesHelper.BlockedRequests = BlockedRequests.Value;
            ApiServicesHelper.APIConfig = APIConfig.Value;
            ApiServicesHelper.APPConfig = APPConfig.Value;

            //we need to load API config here as needed for application to work.
            ApiServicesHelper.ApiConfiguration = sp.GetRequiredService<IApiConfiguration>();
            if(ApiServicesHelper.ApiConfiguration.Settings == null)
            {
                ApiServicesHelper.ApplicationLoaded = false;
            }
            ApiServicesHelper.WebUtility = sp.GetRequiredService<IWebUtility>();
            ApiServicesHelper.ActiveDirectory = sp.GetRequiredService<IActiveDirectory>();

            ApiServicesHelper.MemcachedClient = sp.GetRequiredService<IMemcachedClient>();
            ApiServicesHelper.CacheD = sp.GetRequiredService<ICacheD>();
            ApiServicesHelper.Firebase = sp.GetRequiredService<IFirebase>();
            ApiServicesHelper.Sanitizer = sp.GetRequiredService<ISanitizer>();
            ApiServicesHelper.Cleanser = sp.GetRequiredService<ICleanser>();

            if (ApiServicesHelper.APPConfig.enabled && ApiServicesHelper.ApplicationLoaded) 
            {
                //load APP config here as if can't load application wont work
                ApiServicesHelper.AppConfiguration = sp.GetRequiredService<IAppConfiguration>(); 
            }

            Log.Instance.Info("All API setup completed");
            return service;
        }

        public static IApplicationBuilder UseSimpleResponseMiddleware(this IApplicationBuilder builder)
        {
            builder.UseEnyimMemcached();
            builder.UseMiddleware<APIMiddleware>();

            return builder;
        }
    }
}
