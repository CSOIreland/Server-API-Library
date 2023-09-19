using Microsoft.Extensions.Options;

namespace Sample
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddStaticConfiguration(this IServiceCollection service, ConfigurationManager configuration)
        {
            configuration.AddJsonFile("Static.json");
            service.Configure<StaticConfig>(configuration.GetSection("appStatic"));

            var sp = service.BuildServiceProvider();
            var StaticConfig = sp.GetService<IOptions<StaticConfig>>();

            AppServicesHelper.ServiceProvider = sp;
            AppServicesHelper.StaticConfig = StaticConfig.Value;

            return service;
        }

    }
}
