using System;
using System.Text.Json;
using EventCountersPluginExampleDotNetCore.Services;
using InfluxDB.Client;
using JoeShook.FusionCache.EventCounters.Plugin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.EventCounters.Plugin;
using ZiggyCreatures.Caching.Fusion.Metrics.Core;

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

            var emailCache = new MemoryCache(new MemoryCacheOptions());
            var hostNameCache = new MemoryCache(new MemoryCacheOptions());

            //
            // Once this is a Fusion Cache Plugin maybe we can just call services.AddFusionCache(...)
            //
            services.AddSingleton<IFusionCache>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<ZiggyCreatures.Caching.Fusion.FusionCache>>();

                var fusionCacheOptions = new FusionCacheOptions
                {
                    DefaultEntryOptions = new FusionCacheEntryOptions
                    {
                        Duration = TimeSpan.FromSeconds(1),
                        JitterMaxDuration = TimeSpan.FromMilliseconds(200)
                    }
                        .SetFailSafe(true, TimeSpan.FromHours(1), TimeSpan.FromSeconds(1))
                        .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))
                };

                // Future Plugin for hooking metrics ???
                var metrics = new FusionCacheEventSource("domain", hostNameCache);
                var fusionCache = new ZiggyCreatures.Caching.Fusion.FusionCache(fusionCacheOptions, hostNameCache, logger);
                metrics.Start(fusionCache);

                return fusionCache;
            });

            services.AddSingleton(new DataManager());

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
                        .SetFailSafe(true, TimeSpan.FromHours(1), TimeSpan.FromSeconds(1))
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
