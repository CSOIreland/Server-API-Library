using Microsoft.Extensions.Configuration;
using API;
using Microsoft.Extensions.Hosting;
using CSO.Email;

namespace TestConsoleApp
{
    internal class Program
    {

        static void Main(string[] args)
        {

            var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("APPConfig.json")
                .AddJsonFile("APIConfig.json").Build();

            IHost builder = ConsoleConfiguration.AddApiLibrary(configuration);

            //ApiServicesHelper.CacheD.Store_BSO<dynamic>("test", "Configuration", "Version", "test", "apples", DateTime.Today.AddDays(30));
            // MemCachedD_Value test = ApiServicesHelper.CacheD.Get_BSO<dynamic>("test", "Configuration", "Version", "test");
            eMail email = new eMail();
            email.To.Add("stephen.lane@cso.ie");
            email.Subject = "console app test";
            email.Body = "this is the body";

            email.Send(ApiServicesHelper.ApiConfiguration.Settings, Log.Instance);
          

            Log.Instance.Info("TEST");
        }
    }
} 