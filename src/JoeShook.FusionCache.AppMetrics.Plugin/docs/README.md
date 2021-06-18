﻿# FusionCache.AppMetrics

<div align="center">

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Twitter](https://img.shields.io/twitter/url/http/shields.io.svg?style=flat&logo=twitter)](https://twitter.com/intent/tweet?hashtags=fusioncache,caching,cache,dotnet,oss,csharp&text=🚀+FusionCache:+a+new+cache+with+an+optional+2nd+layer+and+some+advanced+features&url=https%3A%2F%2Fgithub.com%2Fjodydonetti%2FZiggyCreatures.FusionCache&via=jodydonetti)

</div>

## FusionCache.AppMetrics is a plugin to capture caching metrics using [FusionCache](https://github.com/jodydonetti/ZiggyCreatures.FusionCache)

Metrics are missing from open-source resiliency projects in the .NET ecosystem where in equivalent Java libraries, metrics tend to be common.  FusionCache is a feature rich caching library addressing resiliency needs of today’s enterprise implementations.  [Appmetrics](https://github.com/AppMetrics/AppMetrics) is an easy-to-use metrics library that works in .NET Framework and .NET Core.  Joining these two excellent libraries together you can easily be caching and writing metrics to your favorite timeseries database.

## Usage

```csharp
    var hostNameCache = new MemoryCache(new MemoryCacheOptions());

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
                .Build();

    services.AddSingleton<IFusionCache>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<ZiggyCreatures.Caching.Fusion.FusionCache>>();

                var fusionCacheOptions = new FusionCacheOptions
                {
                    DefaultEntryOptions = new FusionCacheEntryOptions
                        {
                            Duration = TimeSpan.FromSeconds(5),
                            JitterMaxDuration = TimeSpan.FromSeconds(20),
                            FailSafeThrottleDuration = TimeSpan.FromMilliseconds(10)
                        }
                        .SetFailSafe(true, TimeSpan.FromSeconds(10))
                        .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(3))
                };

                // Future Plugin for hooking metrics ???
                var metrics = new AppMetricsProvider("domain", appMetrics, hostNameCache);
                var fusionCache = new ZiggyCreatures.Caching.Fusion.FusionCache(fusionCacheOptions, hostNameCache, logger);
                metrics.Wireup(fusionCache, fusionCacheOptions);

                return fusionCache;
            });
```

AppMetrics Plugin is an easy to use solution for .NET Framework apps.  If you already use [AppMetrics](https://github.com/AppMetrics/AppMetrics) then this would be an easy way to go.  

There are two example WebApi style projects included:

- [AppMetricsPluginExampleFrameworkOnAspNetCore](https://github.com/JoeShook/FusionCacheMetricsPlayground/tree/main/examples/AppMetricsPluginExampleFrameworkOnAspNetCore) 
- [AppMetricsPluginExampleFramework](https://github.com/JoeShook/FusionCacheMetricsPlayground/tree/main/examples/AppMetricsPluginExampleFramework)

Metrics plugins are created by subscribing to FusionCache [Events](https://github.com/jodydonetti/ZiggyCreatures.FusionCache/blob/cecba47e773d799a6b978d43858915cc8fb018d8/docs/Events.md).

## MetricsConfig
`MetricsConfig` contains the following properties and can be overriden by adding a `CacheMetrics` section to appsettings.json.  

- `ApplicationName` is defaulted to the executing assembly name.
- `ApplicationVersion` is defaulted to the executing assembly version.
- `Prefix` is defaulted to "appMetrics"
- `MeasurementName` is defaulted to "cache-events"
  
  see Examples for usage

The following Events are subscribed to.  Each event includes a Tag named "cacheEvent" and a tag value.  Both the "cacheEvent" name and Tag values are defined in the [`SemanticConventions`](https://github.com/JoeShook/FusionCacheMetricsPlayground/blob/main/src/ZiggyCreatures.FusionCache.Metrics.Core/SemanticConventions.cs) class.  These conventions can be changed by implementing a new [`ISemanticConventions`](https://github.com/JoeShook/FusionCacheMetricsPlayground/blob/main/src/ZiggyCreatures.FusionCache.Metrics.Core/ISemanticConventions.cs) interface and registering in you dependency injection framework.  Tags are typical in time series databases and are indexed making them friendly to searching and grouping over time.  

## Events:: Incrementing Polling Counters for Hits and Misses

The following counters all set AppMetrics's CounterOptions for ResetOnReporting to true.  The effect is each time a reporter queries the counter it will be reset.  All the following counters accept the  CacheCountIncrement and CacheCountDecrement set ResetOnReporting to true.

### CacheHit()

The cache tag is "HIT".  Every cache hit will increment a counter.

### CacheMiss()

The cache tag is "MISS".  Every cache miss will increment a counter.

### CacheStaleHit()

The cache tag is "STALE_HIT".  When [FailSafe](https://github.com/jodydonetti/ZiggyCreatures.FusionCache/blob/main/docs/Timeouts.md) is enabled and a request times out due to a "soft timeout" and a stale cache item exists then increment a counter.  Note this will not trigger the CacheMiss() counter.  

### CacheBackgroundRefresh()

The cache tag is "STALE_REFRESH".  When [FailSafe](https://github.com/jodydonetti/ZiggyCreatures.FusionCache/blob/main/docs/Timeouts.md) is enabled and a request times out due to a "soft timeout" the request will continue for the length of a "hard timeout".  If the request finds data it will call this CacheBackgroundRefresh() and increment a counter.  Note it would be normal for this counter and CacheStaleHit() to track with eachother.

### CacheRemoved()

The cache tag is "REMOVE".  When the cache is removed by user code.

## Incrementing Polling Counter for Evictions

Eviction counters are wired into the ICacheEntries with the PostEvictionDelegate.  

### CacheExpired

The cache tag is "EXPIRE".  When the EvictionReason is Expired increment a counter.

### CacheCapacityExpired()

The cache tag is "CAPACITY".  When the EvictionReason is Capacity increment a counter.

## Events:: Polling Counters

### CacheCount()

The cach tag is "ITEM_COUNT".  Calls the `MemoryCache.Count`

For this feature `MemoryCache` must be created and passed to `FusionCache` and `AppMetricsProvider` so they share the same intance of `MemoryCache`
