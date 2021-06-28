# FusionCache.EventCounters

<div align="center">

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Twitter](https://img.shields.io/twitter/url/http/shields.io.svg?style=flat&logo=twitter)](https://twitter.com/intent/tweet?hashtags=fusioncache,caching,cache,dotnet,oss,csharp&text=🚀+FusionCache:+a+new+cache+with+an+optional+2nd+layer+and+some+advanced+features&url=https%3A%2F%2Fgithub.com%2Fjodydonetti%2FZiggyCreatures.FusionCache&via=jodydonetti)

</div>

## FusionCache.EventCounters is a plugin to capture caching metrics using [FusionCache](https://github.com/jodydonetti/ZiggyCreatures.FusionCache).

Metrics are missing from open-source resiliency projects in the .NET ecosystem where in equivalent Java libraries, metrics tend to be common.  FusionCache is a feature rich caching library addressing resiliency needs of today’s enterprise implementations.  [EventCounters](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/event-counters) is a lightweight .NET Core API library that works in .NET Core.  Joining these two excellent libraries together you can easily be caching and writing metrics to your favorite timeseries database or use the dotnet-counters tool to monitor from the console.

## Usage

Setup option 1

Notes:  MemoryCache is created outside of AddFusionCache extenstion method so it can be passed to FusionCacheEvenSource.  This is required if you want the cache count reportable. 

```csharp

    var memoryCache = new MemoryCache(new MemoryCacheOptions());
    services.AddSingleton<IMemoryCache>(memoryCache);
    services.AddSingleton<IFusionCachePlugin>(new FusionCacheEventSource("domain", memoryCache));
    services.AddFusionCache(options =>
        options.DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromSeconds(60)
            }
            .SetFailSafe(true, TimeSpan.FromHours(1), TimeSpan.FromSeconds(5))
            .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))
        );
```

Setup option 2 or in addtion to 1 if you have multiple caches

```csharp

    var memoryCache = new MemoryCache(new MemoryCacheOptions());
    
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

        var metrics = new FusionCacheEventSource("email", memoryCache);
        var fusionCache = new ZiggyCreatures.Caching.Fusion.FusionCache(fusionCacheOptions, memoryCache, logger);
        metrics.Start(fusionCache);

        return new EmailService(serviceProvider.GetRequiredService<DataManager>(), fusionCache);
    });

```

## Events:: Incrementing Polling Counters for Hits and Misses

The following counters are all IncrementingPollingCounters which tracks based on a time interval. EventListeners will get a value based on the difference between the current invocation and the last invocation. Read [EventCounter API overview](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/event-counters#eventcounter-api-overview) to get a better understanding of the various EventCounter implementations.

### CacheHit()

The cache tag is "HIT".  Every cache hit will increment a counter.

### CacheMiss()

The cache tag is "MISS".  Every cache miss will increment a counter.

### CacheStaleHit()

The cache tag is "STALE_HIT".  When [FailSafe](https://github.com/jodydonetti/ZiggyCreatures.FusionCache/blob/main/docs/Timeouts.md) is enabled and a request times out due to a "soft timeout" and a stale cache item exists then increment a counter.  Note this will not trigger the CacheMiss() counter.  

### CacheBackgroundRefresh()

The cache tag is "STALE_REFRESH".  When [FailSafe](https://github.com/jodydonetti/ZiggyCreatures.FusionCache/blob/main/docs/Timeouts.md) is enabled and a request times out due to a "soft timeout" the request will continue for the length of a "hard timeout".  If the request finds data it will call this CacheBackgroundRefresh() and increment a counter.  Note it would be normal for this counter and CacheStaleHit() to track with each other.

### CacheRemoved()

The cache tag is "REMOVE".  When the EvictionReason is Replaced increment a counter.

## Incrementing Polling Counter for Evictions

Eviction counters are wired into the ICacheEntries with the PostEvictionDelegate.  

### CacheExpired

The cache tag is "EXPIRE".  When the EvictionReason is Expired increment a counter.

### CacheCapacityExpired()

The cache tag is "CAPACITY".  When the EvictionReason is Capacity increment a counter.

### Reporting

In addition to implementing a `EventListener` as mentioned previously one can also monitor the events from the command line.

dotnet-counters can listen to the metrics above.
Example command line for a example.exe application

```cmd
dotnet-counters monitor -n example --counters domainCache
```

Example output would look like the following
[domainCache]
    Cache Background Refresh (Count / 1 sec)           0
    Cache Capacity Eviction (Count / 1 sec)            0
    Cache Expired Eviction (Count / 1 sec)             5
    Cache Hits (Count / 1 sec)                       221
    Cache Misses (Count / 1 sec)                       0
    Cache Removed (Count / 1 sec)                      0
    Cache Size                                     1,157
    Cache Stale Hit (Count / 1 sec)                    5

To make reporting seemless implent a [EventListener](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/event-counters#sample-code)  and run it as a HostedService.

## Listenting for events

See this [EventListener](examples/EventCountersPluginExampleDotNetCore/Services/MetricsListenerService.cs) in action in the example project [EventCountersPluginExampleDotNetCore](https://github.com/JoeShook/FusionCacheMetricsPlayground/tree/main/examples/EventCountersPluginExampleDotNetCore)

```csharp

    public class MetricsListenerService : EventListener, IHostedService
    {
        private List<string> RegisteredEventSources = new List<string>();
        private Task _dataSource;
        private InfluxDBClient _influxDBClient;
        private MetricsConfig _metricsConfig;
        private readonly string _measurementName;
        private readonly ISemanticConventions _conventions;
        private readonly IInfluxCloudConfig _influxCloudConfig;

        public MetricsListenerService(
            InfluxDBClient influxDBClient, 
            MetricsConfig metricsConfig, 
            ISemanticConventions conventions = null,
            IInfluxCloudConfig influxCloudConfig = null)
        {
            _influxDBClient = influxDBClient ?? throw new ArgumentNullException(nameof(influxDBClient));
            _metricsConfig = metricsConfig ?? throw  new ArgumentNullException(nameof(metricsConfig));
            _conventions = conventions ?? new SemanticConventions();
            _influxCloudConfig = influxCloudConfig;

            _measurementName = $"{metricsConfig.Prefix}{metricsConfig.ApplicationName}_{metricsConfig.MeasurementName}";
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _dataSource = Task.Run(async () =>
            {
                while (true)
                {
                    GetNewSources();
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void GetNewSources()
        {
            foreach (var eventSource in EventSource.GetSources())
            {
                OnEventSourceCreated(eventSource);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (!RegisteredEventSources.Contains(eventSource.Name))
            {
                RegisteredEventSources.Add(eventSource.Name);
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string>
                {
                    ["EventCounterIntervalSec"] = "5"
                });
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!eventData.EventSource.Name.Equals("email") &&
                !eventData.EventSource.Name.Equals("domain"))
            {
                return;
            }

            List<PointData> pointData = null;
            var time = DateTime.UtcNow;

            for (int i = 0; i < eventData.Payload?.Count; ++i)
            {
                if (eventData.Payload[i] is IDictionary<string, object> eventPayload)
                {
                    pointData ??= new List<PointData>();
                    var (cacheName, counterName, counterValue) = GetMeasurement(eventPayload);

                    var point = PointData
                        .Measurement(_measurementName)
                        .Field(_conventions.ValueFieldName, counterValue)
                        .Tag(_conventions.ApplicationTagName, _metricsConfig.ApplicationName)
                        .Tag(_conventions.ApplicationVersionTagName, _metricsConfig.ApplicationVersion)
                        .Tag(_conventions.CacheNameTagName, cacheName)
                        .Tag(_conventions.CacheEventTagName, counterName)
                        .Timestamp(time, WritePrecision.S);

                    pointData.Add(point);
                }
            }

            if (pointData != null && pointData.Any())
            {
                WriteData(pointData);
            }
        }

        public virtual void WriteData(List<PointData> pointData)
        {
            using (var writeApi = _influxDBClient.GetWriteApi())
            {
                if (_influxCloudConfig != null)
                {
                    writeApi.WritePoints(_influxCloudConfig.Bucket, _influxCloudConfig.Organization, pointData);
                }
                else
                {
                    writeApi.WritePoints(pointData);
                }
            }
        }

        private (string cacheName, string counterName, long counterValue) GetMeasurement(
            IDictionary<string, object> eventPayload)
        {
            var cacheName = "";
            var counterName = "";
            long counterValue = 0;

            if (eventPayload.TryGetValue("Metadata", out object metaDataValue))
            {
                var metaDataString = Convert.ToString(metaDataValue);
                var metaData = metaDataString
                    .Split(',')
                    .Select(item => item.Split(':'))
                    .ToDictionary(s => s[0], s => s[1]);

                cacheName = metaData[_conventions.CacheNameTagName];
            }

            if (eventPayload.TryGetValue("Name", out object displayValue))
            {
                counterName = displayValue.ToString();
            }

            if (eventPayload.TryGetValue("Increment", out object incrementingPollingCounterValue))
            {
                counterValue = Convert.ToInt64(incrementingPollingCounterValue);
            }
            else if (eventPayload.TryGetValue("Mean", out object pollingCounterValue))
            {
                counterValue = Convert.ToInt64(pollingCounterValue);
            }


            return (cacheName, counterName, counterValue);
        }
    }

```

## Reporting on metrics

The listener example writes metrics to the console Influx database or Influx Cloud.  It is very easy to setup Influx Cloud and start experimenting.  Below is an pulled from the example project [EventCountersPluginExampleDotNetCore](https://github.com/JoeShook/FusionCacheMetricsPlayground/tree/main/examples/EventCountersPluginExampleDotNetCore)

```csharp

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
       
```
