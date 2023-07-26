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

public class StaleTests : BaseTest
{
    [Fact]
    public void TryGetStaleFailSafe()
    {
        var exportedItems = new List<Metric>();
        var cacheName = $"{Utils.GetCurrentMethodName()}";
        var duration = TimeSpan.FromSeconds(2);
        var maxDuration = TimeSpan.FromDays(1);
        var throttleDuration = TimeSpan.FromSeconds(3);

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var fusionMeter = new FusionMeter(cacheName, memoryCache);
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(cacheName)
            .AddInMemoryExporter(exportedItems)
            .Build();
        using var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
            new FusionCacheOptions(),
            memoryCache);

        cache.DefaultEntryOptions.Duration = duration;
        cache.DefaultEntryOptions.IsFailSafeEnabled = true;
        cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
        cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

        // INITIAL, NON-TRACKED SET
        cache.Set("foo", 42);

        fusionMeter.Start(cache);

        // HIT: +1
        cache.TryGet<int>("foo");

        // LET IT BECOME STALE
        Thread.Sleep(duration);

        // HIT (STALE): +1
        cache.TryGet<int>("foo");
        // HIT (STALE): +1
        cache.TryGet<int>("foo");

        Thread.Sleep(600);

        // Assert
        meterProvider.ForceFlush(MaxTimeToAllowForFlush);

        var metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheHitTagValue);
        Assert.Equal(1, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheStaleHitTagValue);
        Assert.Equal(2, metricPoint.GetSumLong());
    }

    [Fact]
    public void TryGetStaleFailSafeAdaptive()
    {
        var exportedItems = new List<Metric>();
        var cacheName = $"{Utils.GetCurrentMethodName()}";
        var duration = TimeSpan.FromMinutes(2);
        var maxDuration = TimeSpan.FromDays(1);
        var throttleDuration = TimeSpan.FromSeconds(1);
        var softTimeout = TimeSpan.FromMilliseconds(100);
        var hardTimeout = TimeSpan.FromMilliseconds(500);
        var adaptiveDuration = TimeSpan.FromSeconds(2);

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var fusionMeter = new FusionMeter(cacheName, memoryCache);
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(cacheName)
            .AddInMemoryExporter(exportedItems)
            .Build();
        using var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
            new FusionCacheOptions(),
            memoryCache);


        cache.DefaultEntryOptions.Duration = duration; // duration in minutes
        cache.DefaultEntryOptions.IsFailSafeEnabled = true;
        cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
        cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;
        cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
        cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;

        fusionMeter.Start(cache);

        // SET: +1
        cache.GetOrSet<int>("foo", (ctx, _) =>
        {
            // Duration set by factory return value
            ctx.Options.SetDuration(adaptiveDuration).SetFailSafe(true);

            return 42;
        });


        // HIT: +1
        cache.GetOrSet<int>("foo", (ctx, _) =>
        {
            // Duration overriden in factory from minutes to seconds.
            ctx.Options.SetDuration(adaptiveDuration).SetFailSafe(true);

            return 42;
        });

        // LET IT BECOME STALE
        Thread.Sleep(adaptiveDuration);

        // HIT (STALE): +1
        cache.GetOrSet<int>("foo", (ctx, _) =>
        {
            Thread.Sleep(throttleDuration);
            // Duration overriden in factory from minutes to seconds.
            ctx.Options.SetDuration(adaptiveDuration).SetFailSafe(true);

            return 42;
        });

        // HIT (STALE): +1
        cache.GetOrSet<int>("foo", (ctx, _) =>
        {
            Thread.Sleep(throttleDuration);
            // Duration overriden in factory from minutes to seconds.
            ctx.Options.SetDuration(adaptiveDuration).SetFailSafe(true);

            return 42;
        });

        Thread.Sleep(600);

        // Assert
        meterProvider.ForceFlush(MaxTimeToAllowForFlush);

        var metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheHitTagValue);
        Assert.Equal(1, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheSetTagValue);
        Assert.Equal(1, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheStaleHitTagValue);
        Assert.Equal(2, metricPoint.GetSumLong());
    }
}