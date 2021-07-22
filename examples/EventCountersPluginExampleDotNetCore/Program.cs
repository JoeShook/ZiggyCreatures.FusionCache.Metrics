using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace EventCountersPluginExampleDotNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });


        // This is probably better than startup.cs because the MetricsListener will start
        // https://andrewlock.net/controlling-ihostedservice-execution-order-in-aspnetcore-3/
        // .ConfigureServices(
        //     services => services.AddHostedService<MetricsListenerService>());
    }
}
