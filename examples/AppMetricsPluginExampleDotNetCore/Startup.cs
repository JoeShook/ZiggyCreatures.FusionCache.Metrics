using App.Metrics;
using App.Metrics.Extensions.Collectors.MetricsRegistries;
using App.Metrics.Filtering;
using App.Metrics.Formatters.InfluxDB;
using AppMetricsPluginExampleDotNetCore.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Text.Json;
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
            var mvcBuilder = services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
            });


            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AppMetricsPluginExampleDotNetCore", Version = "v1" });
            });

            services.AddSingleton(new DataManager());

            var emailCache = new MemoryCache(new MemoryCacheOptions());
            var hostNameCache = new MemoryCache(new MemoryCacheOptions());

            ConfigureAppMetrics(services, mvcBuilder);

            var mySemantics = new SemanticConventions("my_cache_events", "my_cache_gauges");

            //
            // Cache called "domain"
            //
            // Register AppMetricsProvider as a IFusionCachePlugin.
            // Note that a MemoryCache object must be created outside of AddFusionCache extension method so that
            // AppMetricsProvider is holding the same object as FusionCache to enabled cache count reporting.
            // See line 180 in FusionCacheEventSource.cs
            //
            services.AddSingleton<IMemoryCache>(hostNameCache);
            services.AddSingleton<IFusionCachePlugin>(serviceProvider => new AppMetricsProvider("domain", serviceProvider.GetService<IMetrics>(), hostNameCache, mySemantics));
            services.AddFusionCache(options =>
            {
                options.DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromSeconds(1),
                    JitterMaxDuration = TimeSpan.FromMilliseconds(200),
                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromHours(1),
                    FailSafeThrottleDuration = TimeSpan.FromSeconds(1),
                    FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
                    FactoryHardTimeout = TimeSpan.FromSeconds(1)
                };
                //
                // Event logs are very noisy when testing this example mostly because the example is designed to 
                // demonstrate all cache events and to track them with the AppMetrics library.  
                //
                // options.FailSafeActivationLogLevel = LogLevel.None;
                // options.SerializationErrorsLogLevel = LogLevel.None;
                // options.DistributedCacheSyntheticTimeoutsLogLevel = LogLevel.None;
                // options.DistributedCacheErrorsLogLevel = LogLevel.None;
                // options.FactorySyntheticTimeoutsLogLevel = LogLevel.None;
                // options.FactoryErrorsLogLevel = LogLevel.None;
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

                var metrics = new AppMetricsProvider("email", serviceProvider.GetService<IMetrics>(), emailCache, mySemantics);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="mvcBuilder">Get from services.AddControllers()</param>
        private void ConfigureAppMetrics(IServiceCollection services, IMvcBuilder mvcBuilder)
        {
            mvcBuilder.AddMetrics();

            var metricsConfig = new MetricsConfig();
            var appMetricsContextLabel = $"{metricsConfig.Prefix}_{metricsConfig.ApplicationName}";
            var filter = new MetricsFilter();
            filter.WhereContext(c =>
                c != "appmetrics.internal"); //remove default AppMetrics metrics.

            var formatterChoice = Configuration.GetValue<string>("UseMetricNameFormatter");

            var appMetrics = new MetricsBuilder()
                .OutputMetrics.AsJson()
                .OutputMetrics.AsPlainText()
                .Configuration.Configure(
                    options =>
                    {
                        options.DefaultContextLabel = appMetricsContextLabel;
                        options.WithGlobalTags(
                            (globalTags, envInfo) =>
                            {
                                globalTags.Add("application", metricsConfig.ApplicationName);
                                globalTags.Add("applicationVersion", metricsConfig.ApplicationVersion);
                            });
                    })
                .Report
                .ToInfluxDb(
                    options =>
                    {
                        options.Filter = filter;
                        options.InfluxDb.BaseUri =
                            new Uri($"http://{Configuration["InfluxDbConfig.Host"]}:{Configuration["InfluxDbConfig.Port"]}");
                        options.InfluxDb.Database = Configuration["InfluxDbConfig.Database"];
                        options.InfluxDb.RetentionPolicy = Configuration["InfluxDbConfig.RetentionPolicy"];
                        options.InfluxDb.UserName = Configuration["InfluxDbConfig.Username"];
                        options.InfluxDb.Password = Configuration["InfluxDbConfig.Password"];
                        options.InfluxDb.CreateDataBaseIfNotExists = false;
                        if (! string.IsNullOrEmpty(formatterChoice))
                        {
                            options.MetricsOutputFormatter = new MetricsInfluxDbLineProtocolOutputFormatter(
                                new MetricsInfluxDbLineProtocolOptions
                                {
                                    MetricNameFormatter = GetMetricNameFormatter(appMetricsContextLabel, formatterChoice)
                                });
                        }
                    })
                // .Report.ToTextFile(
                //     options => {
                //         options.MetricsOutputFormatter = new MetricsJsonOutputFormatter();
                //         options.AppendMetricsToTextFile = true;
                //         // options.Filter = filter;
                //         options.FlushInterval = TimeSpan.FromSeconds(20);
                //         options.OutputPathAndFileName = @"C:\temp\metrics.txt";
                //     })
                .Build();

            services.AddMetrics(appMetrics)
                .AddMetricsTrackingMiddleware()
                .AddMetricsEndpoints();
        }

        private Func<string, string, string> GetMetricNameFormatter(string appMetricsContextLabel, string formatterChoice)
        {
            if (formatterChoice == "MetricNameFormatterByMeasurementName")
            {
                return MetricNameFormatterByMeasurementName(appMetricsContextLabel);
            }

            return MetricNameFormatterByContextName();
        }


        private static Func<string, string, string> MetricNameFormatterByMeasurementName(string appMetricsContextLabel)
        {
            return (metricContext, metricName) =>
                metricContext == SystemUsageMetricsRegistry.ContextName ||
                metricContext == GcMetricsRegistry.ContextName ||
                metricContext == "Application.HttpRequests" ? 
                    $"{appMetricsContextLabel}_{metricContext}_{metricName}"
                        .Replace(' ', '_').Replace('.', '_') :  //  AppMetrics namespace convention
                    $"{appMetricsContextLabel}_{metricContext}"
                        .Replace(' ', '_');  // FusionCache namespace convention
        }

        private static Func<string, string, string> MetricNameFormatterByContextName()
        {
            return (metricContext, metricName) =>

                metricContext == SystemUsageMetricsRegistry.ContextName ||
                metricContext == GcMetricsRegistry.ContextName ||
                metricContext == "Application.HttpRequests" ?
                    $"{metricContext}_{metricName}"
                        .Replace(' ', '_').Replace('.', '_') :  //  AppMetrics namespace convention
                    $"{metricContext}"
                        .Replace(' ', '_');  // FusionCache namespace convention
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AppMetricsPluginExampleDotNetCore v1"));
            }

            app.UseMetricsAllMiddleware();

            // Or to cherry-pick the tracking of interest
            // app.UseMetricsActiveRequestMiddleware();
            // app.UseMetricsErrorTrackingMiddleware();
            // app.UseMetricsPostAndPutSizeTrackingMiddleware();
            // app.UseMetricsRequestTrackingMiddleware();
            // app.UseMetricsOAuth2TrackingMiddleware();
            // app.UseMetricsApdexTrackingMiddleware();

            app.UseRouting();

            // app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            app.UseMetricsAllEndpoints();
        }
    }
}