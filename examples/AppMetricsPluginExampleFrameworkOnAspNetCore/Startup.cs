using System;
using System.Threading;
using App.Metrics;
using App.Metrics.Extensions.Hosting;
using App.Metrics.Filtering;
using App.Metrics.Formatters.InfluxDB;
using App.Metrics.Formatters.Json;
using AppMetricsPluginExampleFrameworkOnAspNetCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Plugins;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.AppMetrics;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;


namespace AppMetricsPluginExampleFrameworkOnAspNetCore
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
            services.AddSingleton(new DataManager());

            var emailCache = new MemoryCache(new MemoryCacheOptions());
            var hostNameCache = new MemoryCache(new MemoryCacheOptions());
            
            services.AddMvc()
                .AddJsonOptions(options =>

                    options.SerializerSettings.ContractResolver
                                       = new DefaultContractResolver()
                )
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            
            var metricsConfig = new MetricsConfig();
            var appMetricsContextLabel = $"{metricsConfig.Prefix}_{metricsConfig.ApplicationName}";

            var appMetrics = new MetricsBuilder()
                .Configuration.Configure(
                    options =>
                    {
                        options.DefaultContextLabel = appMetricsContextLabel;
                    })
                .Report
                .ToInfluxDb(
                    options =>
                    {
                        var filter = new MetricsFilter();
                        filter.WhereContext(c => c == appMetricsContextLabel); //remove default AppMetrics metrics.
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
                    })
                .Build();

            //
            // Cache called "domain"
            //
            // Register AppMetricsProvider as a IFusionCachePlugin.
            // Note that a MemoryCache object must be created outside of AddFusionCache extension method so that
            // AppMetricsProvider is holding the same object as FusionCache to enabled cache count reporting.
            // See line 180 in FusionCacheEventSource.cs
            //
            services.AddSingleton<IMemoryCache>(hostNameCache);
            services.AddSingleton<IFusionCachePlugin>(new AppMetricsProvider("domain", appMetrics, hostNameCache));
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

                var metrics = new AppMetricsProvider("email", appMetrics, emailCache);
                var fusionCache = new ZiggyCreatures.Caching.Fusion.FusionCache(fusionCacheOptions, emailCache, logger);
                metrics.Start(fusionCache);

                return new EmailService(serviceProvider.GetRequiredService<DataManager>(), fusionCache);
            });


            var metricsReporterService = new MetricsReporterBackgroundService(appMetrics, appMetrics.Options, appMetrics.Reporters);
            metricsReporterService.StartAsync(CancellationToken.None);

            services.AddSingleton(sp => metricsReporterService );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
