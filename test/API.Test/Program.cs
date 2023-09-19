using API;

namespace Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
            builder.Configuration.AddJsonFile("APPConfig.json");
            builder.Configuration.AddJsonFile("APIConfig.json");
            builder.Services.AddApiLibrary(builder);
            builder.Services.AddStaticConfiguration(builder.Configuration);
            

            //optional
            //builder.Services.AddCors(options =>
            //{
            //    options.AddPolicy(name: "corsPolicy",
            //                      builder =>
            //                      {
            //                          builder
            //                             .WithOrigins("https://tdwebserver.cso.ie")
            //                           //.WithOrigins("https://dev-incubator.cso.ie") // specifying the allowed origin
            //                           .AllowCredentials();
            //                      });
            //});

            var app = builder.Build();
           
            // app.UseCors("corsPolicy");

            app.UseSimpleResponseMiddleware();
            app.Run();
        }
    }
}