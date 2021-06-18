# FusionCache Metrics Plugins

<div align="center">

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Twitter](https://img.shields.io/twitter/url/http/shields.io.svg?style=flat&logo=twitter)](https://twitter.com/intent/tweet?hashtags=fusioncache,caching,cache,dotnet,oss,csharp&text=🚀+FusionCache:+a+new+cache+with+an+optional+2nd+layer+and+some+advanced+features&url=https%3A%2F%2Fgithub.com%2Fjodydonetti%2FZiggyCreatures.FusionCache&via=jodydonetti)

</div>

Metrics are missing from open-source resiliency projects in the .NET ecosystem where in equivalent Java libraries, metrics tend to be common.  
FusionCache is a feature rich caching library addressing resiliency needs of today’s enterprise implementations.  This project is a collection of plugins that enable the collection and reporting of cache metrics.

# Current plugins and examples

Currently two plugins and three example usages exist in the repository.  Read the docs in those areas to start using metrics in your projects.

- [FusionCache.EventCounters](https://github.com/JoeShook/FusionCacheMetricsPlayground/tree/main/src/JoeShook.FusionCache.EventCounters.Plugin#fusioncacheeventcounters)
- [FusionCache.AppMetrics](https://github.com/JoeShook/FusionCacheMetricsPlayground/blob/main/src/JoeShook.FusionCache.AppMetrics.Plugin/docs/README.md#fusioncacheappmetrics)

examples

- [EventCountersPluginExampleDotNetCore](https://github.com/JoeShook/FusionCacheMetricsPlayground/tree/main/examples/EventCountersPluginExampleDotNetCore)
- [AppMetricsPluginExampleFrameworkOnAspNetCore](https://github.com/JoeShook/FusionCacheMetricsPlayground/tree/main/examples/AppMetricsPluginExampleFrameworkOnAspNetCore)
- [AppMetricsPluginExampleFramework](https://github.com/JoeShook/FusionCacheMetricsPlayground/tree/main/examples/AppMetricsPluginExampleFramework)

## Future plugins

- OpenTelemetry.Instrumentation.FusionCache.  FusionCache OpenTelemetery implementation for metrics
- OpenTelemetry.Exporter.InfluxDB.  An InfluxDB reporter if you are using OpenTelemetry and want to publish metrics to InfluxDB or InfluxCloud.
