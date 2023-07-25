#region (c) 2022 Joseph Shook. All rights reserved.
// /*
//  Authors:
//     Joseph Shook   Joseph.Shook@Surescripts.com
// 
//  See LICENSE in the project root for license information.
// */
#endregion

using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Xunit;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.OpenTelemetry;

namespace FusionCache.Plugins.Metrics.OpenTelemetry.Tests;

public class CountTests : BaseTest
{
    [Fact]
    public void LoadPluginWithItemCountCheck()
    {
        var exportedItems = new List<Metric>();
        var cacheName = "LoadPluginWithItemCountCheckCache";
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var fusionMeter = new FusionMeter(cacheName, memoryCache);
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(cacheName)
            .AddInMemoryExporter(exportedItems)
            .Build();
        using var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
            new FusionCacheOptions(),
            memoryCache);

        fusionMeter.Start(cache);

        for (int i = 0; i < 100; i++)
        {
            var i1 = i;
            cache.GetOrSet($"A-Key-{i}", () => $"A-Value{i1}");
        }

        Thread.Sleep(600);

        meterProvider.ForceFlush(MaxTimeToAllowForFlush);

        var metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheSetTagValue);
        Assert.Equal(100, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheItemCountTagValue);
        Assert.Equal(100, metricPoint.GetGaugeLastValueLong());
    }
}
