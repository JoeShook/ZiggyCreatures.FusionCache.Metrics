using System;
using System.Text.Json;
using App.Metrics;
using App.Metrics.Filtering;
using App.Metrics.Formatters.InfluxDB;
using App.Metrics.Formatters.Json;
using AppMetricsPluginExampleDotNetCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Plugins;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.AppMetrics;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;

namespace AppMetricsPluginExampleDotNetCore
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
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "EventCountersPluginExampleDotNetCore", Version = "v1" });
            });

            services.AddSingleton(new DataManager());

            var emailCache = new MemoryCache(new MemoryCacheOptions());
            var hostNameCache = new MemoryCache(new MemoryCacheOptions());

            var metricsConfig = new MetricsConfig();
            var appMetricsContextLabel = $"{metricsConfig.Prefix}_{metricsConfig.ApplicationName}";
            var filter = new MetricsFilter();
            filter.WhereContext(c => 
                c == appMetricsContextLabel); //remove default AppMetrics metrics.

            var appMetrics = new MetricsBuilder()
                .Configuration.Configure(
                    options =>
                    {
                        options.DefaultContextLabel = appMetricsContextLabel;
                        options.WithGlobalTags(
                            (globalTags, envInfo) =>
                            {
                                // globalTags.Add("machine_name", envInfo.MachineName);
                                globalTags.Add("app_name", envInfo.EntryAssemblyName);
                                // globalTags.Add("app_version", envInfo.EntryAssemblyVersion);
                            });
                    })
                .Report
                .ToInfluxDb(
                    options =>
                    {
                        // options.Filter = filter;
                        options.InfluxDb.BaseUri = new Uri($"http://{ Configuration["InfluxDbConfig.Host"] }:{ Configuration["InfluxDbConfig.Port"] }");
                        options.InfluxDb.Database = Configuration["InfluxDbConfig.Database"];
                        options.InfluxDb.RetentionPolicy = Configuration["InfluxDbConfig.RetentionPolicy"];
                        options.InfluxDb.UserName = Configuration["InfluxDbConfig.Username"];
                        options.InfluxDb.Password = Configuration["InfluxDbConfig.Password"];
                        options.InfluxDb.CreateDataBaseIfNotExists = false;
                        options.MetricsOutputFormatter = new MetricsInfluxDbLineProtocolOutputFormatter(
                            new MetricsInfluxDbLineProtocolOptions
                            {
                                MetricNameFormatter = (metricContext, metricName) =>
                                    string.IsNullOrWhiteSpace(metricContext)
                                        ? $"{metricName}".Replace(' ', '_')
                                        : $"{metricContext}_{metricName}".Replace(' ', '_')
                            });
                    })
                .Report.ToTextFile(
                    options => {
                        options.MetricsOutputFormatter = new MetricsJsonOutputFormatter();
                        options.AppendMetricsToTextFile = true;
                        // options.Filter = filter;
                        options.FlushInterval = TimeSpan.FromSeconds(20);
                        options.OutputPathAndFileName = @"C:\temp\metrics.txt";
                    });

            services.AddMetrics(appMetrics);

            //
            // Cache called "domain"
            //
            // Register AppMetricsProvider as a IFusionCachePlugin.
            // Note that a MemoryCache object must be created outside of AddFusionCache extension method so that
            // AppMetricsProvider is holding the same object as FusionCache to enabled cache count reporting.
            // See line 180 in FusionCacheEventSource.cs
            //
            services.AddSingleton<IMemoryCache>(hostNameCache);
            services.AddSingleton<IFusionCachePlugin>(serviceProvider => new AppMetricsProvider("domain", serviceProvider.GetService<IMetrics>(), hostNameCache));
            services.AddFusionCache(options =>
                options.DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromSeconds(1),
                    JitterMaxDuration = TimeSpan.FromMilliseconds(200),
                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromHours(1),
                    FailSafeThrottleDuration = TimeSpan.FromSeconds(1),
                    FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
                    FactoryHardTimeout = TimeSpan.FromSeconds(1)
                });

            //
            // Cache called "email"
            //
            // Can't register IFusionCachePlugin via AddFusionCache extenstion method here because IFusionCachePlugin is already registered above.  
            // This is a second cache and a second instance of AppMetricsProvider to collect metrics for a 
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
                        .SetFailSafe(true, TimeSpan.FromHours(1), TimeSpan.FromSeconds(1))
                        .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))
                };

                var metrics = new AppMetricsProvider("email", serviceProvider.GetService<IMetrics>(), emailCache);
                var fusionCache = new ZiggyCreatures.Caching.Fusion.FusionCache(fusionCacheOptions, emailCache, logger);
                metrics.Start(fusionCache);

                return new EmailService(serviceProvider.GetRequiredService<DataManager>(), fusionCache);
            });

            services.AddAppMetricsCollectors(
                options =>
                {
                    options.CollectIntervalMilliseconds = 10000;
                },
                options =>
                {
                    options.CollectIntervalMilliseconds = 10000;
                });

            services.AddMetricsReportingHostedService();
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
