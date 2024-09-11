using Enyim.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace API
{
    public class ApiServicesHelper
    {
        public static IConfiguration Configuration { get; set; }
        public static ServiceProvider ServiceProvider { get; set; }

        public static ADOSettings ADOSettings;

        public static APISettings APISettings;

        public static BlockedRequests BlockedRequests;

        public static APIConfig APIConfig;

        public static APPConfig APPConfig;

        public static CacheSettings CacheSettings;

        public static ICacheConfig CacheConfig;

        public static IApiConfiguration ApiConfiguration;

        public static IAppConfiguration AppConfiguration;

        public static ICacheD CacheD;

        internal static IMemcachedClient MemcachedClient;

        public static IWebUtility WebUtility;

        public static IActiveDirectory ActiveDirectory;

        public static IAPIPerformanceConfiguration APIPerformanceSettings;

        public static IDatabaseTracingConfiguration DatabaseTracingConfiguration;

    }
}