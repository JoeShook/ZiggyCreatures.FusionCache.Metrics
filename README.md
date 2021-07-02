<div align="center">

![FusionCache logo](./artwork/logo-128x128.png)

</div>

# FusionCache Metrics Plugins

<div align="center">

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Twitter](https://img.shields.io/twitter/url/http/shields.io.svg?style=flat&logo=twitter)](https://twitter.com/intent/tweet?hashtags=fusioncache,caching,cache,dotnet,oss,csharp,eventsource,eventlistener,appmetrics&text=🚀+FusionCache.Metrics:+FusionCche+metric+plugins&url=https%3A%2F%2Fgithub.com%2Fjoeshook%2FZiggyCreatures.FusionCache.Metrics&via=josephshook)

  ![badge](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/JoeShook/141e4a826a0a2f46dad85ccf3213a894/raw/FusionCache.EventCounters.Plugin-code-coverage.json)

  ![badge](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/JoeShook/59e2f3ef3dddc3f86e372c161ea501cc/raw/FusionCache.AppMetrics.Plugin-code-coverage.json)
</div>

Metrics are missing from open-source resiliency projects in the .NET ecosystem where in equivalent Java libraries, metrics tend to be common.  
FusionCache is a feature rich caching library addressing resiliency needs of today’s enterprise implementations.  This project is a collection of plugins that enable the collection and reporting of cache metrics.

This naming convention of these projects, examples and packages are prefixed with `ZiggyCreatures.Caching.Fusion.Metrics` to highlight that they are different from the packages produced by the [ZiggyCreatures.FusionCache repository](https://github.com/jodydonetti/ZiggyCreatures.FusionCache)

# Current plugins and examples

Currently two plugins and three example usages exist in the repository.  Read the docs in those areas to start using metrics in your projects.

- [FusionCache.EventCounters](./src/ZiggyCreatures.FusionCache.Metrics.EventCounters.Plugin)
- [FusionCache.AppMetrics](./src/ZiggyCreatures.FusionCache.Metrics.AppMetrics.Plugin)

examples

- [EventCountersPluginExampleDotNetCore](./examples/EventCountersPluginExampleDotNetCore)
- [AppMetricsPluginExampleFrameworkOnAspNetCore](./examples/AppMetricsPluginExampleFrameworkOnAspNetCore)
- [AppMetricsPluginExampleFramework](./examples/AppMetricsPluginExampleFramework)

## Future plugins

- OpenTelemetry.Instrumentation.FusionCache.  FusionCache OpenTelemetery implementation for metrics
- OpenTelemetry.Exporter.InfluxDB.  An InfluxDB reporter if you are using OpenTelemetry and want to publish metrics to InfluxDB or InfluxCloud.
