using System;
using System.Text.Json;
using EventCountersPluginExampleDotNetCore.Services;
using InfluxDB.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;
using ZiggyCreatures.Caching.Fusion.Plugins;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.EventCounters;

namespace EventCountersPluginExampleDotNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
            });

            services.AddSingleton(new DataManager());

            var emailCache = new MemoryCache(new MemoryCacheOptions());
            var hostNameCache = new MemoryCache(new MemoryCacheOptions());


            //
            // Cache called "domain"
            //
            // Register FusionCacheEventSource as a IFusionCachePlugin.
            // Note that a MemoryCache object must be created outside of AddFusionCache extension method so that
            // FusionCacheEventSource is holding the same object as FusionCache to enabled cache count reporting.
            // See line 193 in FusionCacheEventSource.cs
            //
            services.AddSingleton<IMemoryCache>(hostNameCache);
            services.AddSingleton<IFusionCachePlugin>(new FusionCacheEventSource("domain", hostNameCache));
            services.AddFusionCache()
                .WithDefaultEntryOptions(options =>
                {
                    options.Duration = TimeSpan.FromSeconds(1);
                    options.JitterMaxDuration = TimeSpan.FromMilliseconds(200);
                    options.SetFailSafe(true, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1));
                    options.SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
                });

            //
            // Cache called "email"
            //
            // Can't register IFusionCachePlugin here because it is already registered above.  
            // This is a second cache and a second instance of FusionCacheEventSource to collect metrics for a 
            // different cache called "email"
            //
            services.AddSingleton<IEmailService>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<ZiggyCreatures.Caching.Fusion.FusionCache>>();

                var fusionCacheOptions = new FusionCacheOptions
                {
                    DefaultEntryOptions = new FusionCacheEntryOptions
                    {
                        Duration = TimeSpan.FromSeconds(1),
                        JitterMaxDuration = TimeSpan.FromMilliseconds(200)
                    }
                        .SetFailSafe(true, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1))
                        .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))
                };

                var metrics = new FusionCacheEventSource("email", emailCache);
                var fusionCache = new ZiggyCreatures.Caching.Fusion.FusionCache(fusionCacheOptions, emailCache, logger);
                metrics.Start(fusionCache);

                return new EmailService(serviceProvider.GetRequiredService<DataManager>(), fusionCache);
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "EventCountersPluginExampleDotNetCore", Version = "v1" });
            });

            // EventListener too write metrics to InfluxDb
            services.AddSingleton(this.Configuration.GetSection("CacheMetrics").Get<MetricsConfig>());

            switch (this.Configuration.GetValue<string>("UseInflux").ToLowerInvariant())
            {
                case "cloud":
                    services.AddSingleton(sp =>
                        InfluxDBClientFactory.Create(
                            Configuration["InfluxCloudConfig.Url"],
                            Configuration["InfluxCloudConfig.Token"].ToCharArray()));
                    services.AddHostedService<MetricsListenerService>();

                    services.AddSingleton<IInfluxCloudConfig>(new InfluxCloudConfig
                    {
                        Bucket = Configuration["InfluxCloudConfig.Bucket"],
                        Organization = Configuration["InfluxCloudConfig.Organization"]
                    });

                    break;

                case "db":
                    services.AddSingleton(sp =>
                        InfluxDBClientFactory.CreateV1(
                            $"http://{Configuration["InfluxDbConfig.Host"]}:{Configuration["InfluxDbConfig.Port"]}",
                            Configuration["InfluxDbConfig.Username"],
                            Configuration["InfluxDbConfig.Password"].ToCharArray(),
                            Configuration["InfluxDbConfig.Database"],
                            Configuration["InfluxDbConfig.RetentionPolicy"]));
                    services.AddHostedService<MetricsListenerService>();

                    break;

                default:
                    services.AddSingleton(sp =>
                        InfluxDBClientFactory.Create(
                            $"http://localhost",
                            "nullToken".ToCharArray()));
                    services.AddHostedService<ConsoleMetricsListenter>();

                    break;
            }



        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EtwPluginExampleDotNetCore v1"));
            }

            app.UseRouting();

            // app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
