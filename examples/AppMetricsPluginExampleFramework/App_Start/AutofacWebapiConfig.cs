using System;
using System.Configuration;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using App.Metrics;
using App.Metrics.Extensions.Hosting;
using App.Metrics.Filtering;
using App.Metrics.Formatters.InfluxDB;
using App.Metrics.Formatters.Json;
using AppMetricsPluginExample.Services;
using Autofac;
using Autofac.Integration.WebApi;
using AutofacSerilogIntegration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Serilog;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.AppMetrics;

namespace AppMetricsPluginExample
{
    public class AutofacWebapiConfig
    {
        public static IContainer Container;

        public static void Initialize(HttpConfiguration config)
        {
            Initialize(config, RegisterServices(new ContainerBuilder()));
        }

        public static void Initialize(HttpConfiguration config, IContainer container)
        {
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }

        private static IContainer RegisterServices(ContainerBuilder builder)
        {
            //Register your Web API controllers.  
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            builder.RegisterType<DataManager>()
                .As<DataManager>()
                .SingleInstance();

            builder.RegisterType<EmailService>()
                .As<IEmailService>()
                .SingleInstance();

            var emailCache = new MemoryCache(new MemoryCacheOptions());
            var hostNameCache = new MemoryCache(new MemoryCacheOptions());

            var appMetricsContextLabel = $"appMetrics_AppMetricsPluginExampleFramework";

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
                        options.InfluxDb.BaseUri = new Uri($"http://{ConfigurationManager.AppSettings.Get("InfluxDbConfig.Host")}:{ConfigurationManager.AppSettings.Get("InfluxDbConfig.Port")}");
                        options.InfluxDb.Database = ConfigurationManager.AppSettings.Get("InfluxDbConfig.Database");
                        options.InfluxDb.RetentionPolicy = ConfigurationManager.AppSettings.Get("InfluxDbConfig.RetentionPolicy"); 
                        options.InfluxDb.UserName = ConfigurationManager.AppSettings.Get("InfluxDbConfig.Username");
                        options.InfluxDb.Password = ConfigurationManager.AppSettings.Get("InfluxDbConfig.Password");
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
                        options.OutputPathAndFileName = @"C:\temp\metrics.dotnet.framework.example.txt";
                    })
                .Build();

            var metricsReporterService = new MetricsReporterBackgroundService(appMetrics, appMetrics.Options, appMetrics.Reporters);
            metricsReporterService.StartAsync(CancellationToken.None);
            builder.Register(c => metricsReporterService)
                .SingleInstance();

            builder.RegisterType<LoggerFactory>()
                .As<ILoggerFactory>()
                .SingleInstance();

           Log.Logger = new LoggerConfiguration()
               .Enrich.WithWebApiRouteTemplate()
               .Enrich.WithWebApiActionName()
               .CreateLogger();

            builder.RegisterLogger(Log.Logger);
           

            builder.Register(c =>
                {
                    var loggerFactory = c.Resolve<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger<ZiggyCreatures.Caching.Fusion.FusionCache>();

                    var fusionCacheOptions = new FusionCacheOptions
                    {
                        DefaultEntryOptions = new FusionCacheEntryOptions
                            {
                                Duration = TimeSpan.FromSeconds(1),
                                JitterMaxDuration = TimeSpan.FromMilliseconds(200)
                        }
                            .SetFailSafe(true, TimeSpan.FromHours(1), TimeSpan.FromSeconds(1))
                            .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(400))
                    };

                    // Future Plugin for hooking metrics ???
                    var metrics = new AppMetricsProvider("domain", appMetrics, hostNameCache);
                    var fusionCache = new ZiggyCreatures.Caching.Fusion.FusionCache(fusionCacheOptions, hostNameCache, logger);
                    metrics.Start(fusionCache);

                    return fusionCache;
                })
                .As<IFusionCache>()
                .SingleInstance();

            builder.Register(c =>
            {
                var loggerFactory = c.Resolve<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<ZiggyCreatures.Caching.Fusion.FusionCache>();

                var fusionCacheOptions = new FusionCacheOptions
                {
                    DefaultEntryOptions = new FusionCacheEntryOptions
                        {
                            Duration = TimeSpan.FromSeconds(1),
                            JitterMaxDuration = TimeSpan.FromSeconds(20)
                    }
                        .SetFailSafe(true, TimeSpan.FromHours(1), TimeSpan.FromSeconds(1))
                        .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(400))
                };

                var metrics = new AppMetricsProvider("email", appMetrics, emailCache);
                var fusionCache = new ZiggyCreatures.Caching.Fusion.FusionCache(fusionCacheOptions, emailCache, logger);
                metrics.Start(fusionCache);

                return new EmailService(c.Resolve<DataManager>(), fusionCache);
            })
                .As<IEmailService>()
                .SingleInstance();

            //Set the dependency resolver to be Autofac.  
            Container = builder.Build();

            return Container;
        }
    }
}