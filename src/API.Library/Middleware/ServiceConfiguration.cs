using Enyim.Caching;
using Enyim.Caching.Memcached;
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

            Log.Instance.Info("service configuration started");

            // Add services to the container.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            builder.Services.AddHttpContextAccessor();

            service.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"))
                .AddOptions<CacheSettings>()
                .ValidateDataAnnotations()
                .Validate(config =>
                {
                    if (config.API_MEMCACHED_ENABLED)
                    {

                        if (!ConfigValidation.cacheSalsaValidation(config))
                        {
                            return false;
                        }


                        return ConfigValidation.cacheLockValidation(config);

                    }
                    return true;
                });




            service.Configure<ADOSettings>(builder.Configuration.GetSection("ADOSettings"));
            service.Configure<APIConfig>(builder.Configuration.GetSection("API_Config"));
            service.Configure<APPConfig>(builder.Configuration.GetSection("APP_Config"));
            service.Configure<APISettings>(builder.Configuration.GetSection("API_SETTINGS"));
            service.Configure<BlockedRequests>(builder.Configuration.GetSection("Blocked_Requests"));
            service.Configure<APIPerformanceSettings>(builder.Configuration.GetSection("APIPerformanceSettings"));

            bool memcachedFailedtoLoad = false;
            try
            {
                service.AddEnyimMemcached();
            }
            catch (Exception ex)
            {
                Log.Instance.Fatal(ex);
                Log.Instance.Fatal("Memcache failed to load");
                memcachedFailedtoLoad = true;
            }



            service.AddSingleton<ICacheConfig, CacheConfig>();
            service.AddSingleton<IApiConfiguration, ApiConfiguration>();
            service.AddSingleton<IAppConfiguration, APPConfiguration>();
            service.AddSingleton<IWebUtility, WebUtility>();
            service.AddSingleton<IMemcachedClient, MemcachedClient>();
            service.AddSingleton<ICacheD, MemCacheD>();
            service.AddSingleton<IActiveDirectory, ActiveDirectory>();
            service.AddSingleton<IAPIPerformanceConfiguration, APIPerformanceConfiguration>();
            service.AddSingleton<IDatabaseTracingConfiguration, DatabaseTracingConfiguration>();

            // service.AddScoped<IADO, ADO>();

            var sp = service.BuildServiceProvider();
            var ADOSettings = sp.GetService<IOptions<ADOSettings>>();
            var APISettings = sp.GetService<IOptions<APISettings>>();
            var BlockedRequests = sp.GetService<IOptions<BlockedRequests>>();
            var APIConfig = sp.GetService<IOptions<APIConfig>>();
            var APPConfig = sp.GetService<IOptions<APPConfig>>();
            //     var CacheSettings = sp.GetService<IOptions<CacheSettings>>();

            ApiServicesHelper.ServiceProvider = sp;
            ApiServicesHelper.Configuration = builder.Configuration;
            ApiServicesHelper.ADOSettings = ADOSettings.Value;
            ApiServicesHelper.APISettings = APISettings.Value;
            ApiServicesHelper.BlockedRequests = BlockedRequests.Value;
            ApiServicesHelper.APIConfig = APIConfig.Value;
            ApiServicesHelper.APPConfig = APPConfig.Value;

            ///       ApiServicesHelper.CacheSettings = CacheSettings.Value;

            ApiServicesHelper.APIPerformanceSettings = sp.GetRequiredService<IAPIPerformanceConfiguration>();
            ApiServicesHelper.DatabaseTracingConfiguration = sp.GetRequiredService<IDatabaseTracingConfiguration>();


            //setup memcache here
            try
            {
                ApiServicesHelper.CacheConfig = sp.GetRequiredService<ICacheConfig>();
                ApiServicesHelper.MemcachedClient = sp.GetRequiredService<IMemcachedClient>();
                ApiServicesHelper.CacheD = sp.GetRequiredService<ICacheD>();
                ServerStats stats = ApiServicesHelper.CacheD.GetStats();
                if (stats == null)
                {
                    throw new Exception("Error reading memcache stats");
                }
            }
            catch (ConfigurationException ex)
            {
                Log.Instance.Fatal(ex);
                throw;
            }
            catch (Exception ex)
            {
                Log.Instance.Fatal(ex);
                Log.Instance.Fatal("Memcache failed to load");
                memcachedFailedtoLoad = true;
            }

            //if memcached failed to load then mark it as disabled
            if (memcachedFailedtoLoad == true)
            {
                ApiServicesHelper.CacheConfig.API_MEMCACHED_ENABLED = false;
            }


            //we need to load API config here as needed for application to work.
            ApiServicesHelper.ApiConfiguration = sp.GetRequiredService<IApiConfiguration>();
            if (ApiServicesHelper.ApiConfiguration.Settings == null)
            {
                throw new Exception("API Settings failed to load");
            }



            ApiServicesHelper.WebUtility = sp.GetRequiredService<IWebUtility>();
            ApiServicesHelper.ActiveDirectory = sp.GetRequiredService<IActiveDirectory>();

            if (ApiServicesHelper.APPConfig.enabled)
            {
                //load APP config here as if can't load application wont work
                ApiServicesHelper.AppConfiguration = sp.GetRequiredService<IAppConfiguration>();

                if (ApiServicesHelper.AppConfiguration.Settings == null)
                {
                    throw new Exception("APP Settings failed to load");
                }
            }

            bool isStateless = Convert.ToBoolean(ApiServicesHelper.ApiConfiguration.Settings["API_STATELESS"]);
            if (!isStateless)
            {
                service.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromMinutes(30);//We set Time here 
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.Name = "apiSessionCookie";
                });
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
