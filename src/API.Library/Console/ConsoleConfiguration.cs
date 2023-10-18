﻿using Enyim.Caching;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace API
{
    public static class ConsoleConfiguration
    {
        public static IHost AddApiLibrary(IConfiguration configuration)
        {
            var loggingOptions = configuration.GetSection("Log4NetCore").Get<Log4NetProviderOptions>();

            //log name of the machine for identification purposes
            log4net.GlobalContext.Properties["MachineName"] = System.Environment.MachineName;


            var builder = Host.CreateDefaultBuilder()
               .ConfigureAppConfiguration(builder =>
               {
                   builder.Sources.Clear();
                   builder.AddConfiguration(configuration);
               }).ConfigureServices(ser =>
               {
                   ser.AddEnyimMemcached();
                   ser.AddSingleton<IApiConfiguration, ApiConfiguration>();
                   ser.AddSingleton<IApiConfiguration, ApiConfiguration>();
                   ser.AddSingleton<IAppConfiguration, APPConfiguration>();
                   ser.AddSingleton<IWebUtility, WebUtility>();
                   ser.AddSingleton<IMemcachedClient, MemcachedClient>();
                   ser.AddSingleton<ICacheD, MemCacheD>();
                   ser.AddSingleton<IActiveDirectory, ActiveDirectory>();
                   ser.AddSingleton<IFirebase, Firebase>();

                   ser.AddScoped<IADO, ADO>();

                   ser.AddSingleton<ICleanser, Cleanser>();
                   ser.AddSingleton<ISanitizer, Sanitizer>();
                   ser.AddLogging(builder =>
                   {
                       builder.AddLog4Net(loggingOptions);
                   });
                   ser.Configure<ForwardedHeadersOptions>(options =>
                   {
                       options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                   });
                   ser.AddHttpContextAccessor();
                   ser.Configure<ADOSettings>(configuration.GetSection("ADOSettings"));
                   ser.Configure<APIConfig>(configuration.GetSection("API_Config"));
                   ser.Configure<APPConfig>(configuration.GetSection("APP_Config"));
                   ser.Configure<APISettings>(configuration.GetSection("API_SETTINGS"));
                   ser.Configure<BlockedRequests>(configuration.GetSection("Blocked_Requests"));
               }
            ).Build();


            //watches for any changes to log4net config file
            XmlConfigurator.ConfigureAndWatch(new FileInfo(loggingOptions.Log4NetConfigFileName));

            Log.Instance.Info("service configuration started");
        
            var ADOSettings = builder.Services.GetService<IOptions<ADOSettings>>();
            var APISettings = builder.Services.GetService<IOptions<APISettings>>();
            var BlockedRequests = builder.Services.GetService<IOptions<BlockedRequests>>();
            var APIConfig = builder.Services.GetService<IOptions<APIConfig>>();
            var APPConfig = builder.Services.GetService<IOptions<APPConfig>>();

            ApiServicesHelper.ServiceProvider = (ServiceProvider)builder.Services;
            ApiServicesHelper.Configuration = configuration;
            ApiServicesHelper.ADOSettings = ADOSettings.Value;
            ApiServicesHelper.APISettings = APISettings.Value;
            ApiServicesHelper.BlockedRequests = BlockedRequests.Value;
            ApiServicesHelper.APIConfig = APIConfig.Value;
            ApiServicesHelper.APPConfig = APPConfig.Value;

            //we need to load API config here as needed for application to work.
            ApiServicesHelper.ApiConfiguration = builder.Services.GetService<IApiConfiguration>();
            if (ApiServicesHelper.ApiConfiguration.Settings == null)
            {
                ApiServicesHelper.ApplicationLoaded = false;
            }

            ApiServicesHelper.WebUtility = builder.Services.GetService<IWebUtility > ();
            ApiServicesHelper.ActiveDirectory = builder.Services.GetService<IActiveDirectory>();

            ApiServicesHelper.MemcachedClient = builder.Services.GetService<IMemcachedClient>();
            ApiServicesHelper.CacheD = builder.Services.GetService<ICacheD>();
            ApiServicesHelper.Firebase = builder.Services.GetService<IFirebase>();
            ApiServicesHelper.Sanitizer = builder.Services.GetService<ISanitizer>();
            ApiServicesHelper.Cleanser = builder.Services.GetService<ICleanser>();

            if (ApiServicesHelper.APPConfig.enabled && ApiServicesHelper.ApplicationLoaded)
            {
                //load APP config here as if can't load application wont work
                ApiServicesHelper.AppConfiguration = builder.Services.GetService<IAppConfiguration>();
            }

            Log.Instance.Info("All API setup completed");
            return builder;       
        }
    }
}
