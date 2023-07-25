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

public class BackgroundFailSafeTests : BaseTest
{
    [Fact]
    public async Task BackgroundFailSafeAsync()
    {
        var exportedItems = new List<Metric>();
        var cacheName = "BackgroundFailSafeCache";
        var duration = TimeSpan.FromSeconds(2);
        var softTimeout = TimeSpan.FromMilliseconds(100);
        var hardTimeout = TimeSpan.FromMilliseconds(500);
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
        cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
        cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;
        cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

        fusionMeter.Start(cache);

        // INITIAL, NON-TRACKED SET
        await cache.SetAsync("foo", 42);

        // LET IT BECOME STALE
        await Task.Delay(throttleDuration);

        // HIT (STALE): +1
        // FAIL-SAFE: +1
        // SET: +1
        _ = await cache.GetOrSetAsync("foo", async _ =>
        {
            await Task.Delay(hardTimeout, _);
            return 42;
        });


        //await cache.SetAsync<int>("foo", 42);
        // LET IT BECOME STALE
        await Task.Delay(throttleDuration);

        // HIT (STALE): +1
        // FAIL-SAFE: +1
        // STALE_REFRESH_ERROR: +1
        _ = await cache.GetOrSetAsync<int>("foo", async _ =>
        {
            await Task.Delay(hardTimeout, _);
            throw new Exception("Sloths are cool");
        });

        await Task.Delay(500);

        // Assert
        meterProvider.ForceFlush(MaxTimeToAllowForFlush);

        var metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheHitTagValue);
        Assert.Equal(0, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheFailSafeActivateTagValue, cacheName);
        Assert.Equal(2, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheSetTagValue, cacheName);
        Assert.Equal(2, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheStaleHitTagValue, cacheName);
        Assert.Equal(2, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheBackgroundRefreshedTagValue, cacheName);
        Assert.Equal(1, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheBackgroundFailedRefreshedTagValue, cacheName);
        Assert.Equal(1, metricPoint.GetSumLong());
    }

    [Fact]
    public async Task BackgroundFailSafeAdaptiveAsync()
    {
        var exportedItems = new List<Metric>();
        var cacheName = $"{Utils.GetCurrentMethodName()}";
        var duration = TimeSpan.FromMinutes(2);
        var softTimeout = TimeSpan.FromMilliseconds(100);
        var hardTimeout = TimeSpan.FromMilliseconds(500);
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
        cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
        cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;
        cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

        fusionMeter.Start(cache);

        // INITIAL
        // MISS: +1
        // SET: +1
        await cache.GetOrSetAsync("foo", async (ctx, _) =>
        {
            await Task.Delay(1, _);
            // Duration set by factory return value
            ctx.Options.SetDuration(TimeSpan.FromSeconds(2));

            return 42;
        });

        // LET IT BECOME STALE
        await Task.Delay(throttleDuration);

        // HIT (STALE): +1
        // FAIL-SAFE: +1
        _ = await cache.GetOrSetAsync("foo", async (ctx, _) =>
        {
            await Task.Delay(hardTimeout, _);
            // Duration set by factory return value
            ctx.Options.SetDuration(TimeSpan.FromSeconds(2));

            return 42;
        });


        //await cache.SetAsync<int>("foo", 42);
        // LET IT BECOME STALE
        await Task.Delay(throttleDuration);

        // HIT (STALE): +1
        // FAIL-SAFE: +1
        // STALE_REFRESH_ERROR: +1
        _ = await cache.GetOrSetAsync<int>("foo", async (ctx, _) =>
        {
            await Task.Delay(hardTimeout, _);
            // Duration set by factory return value
            ctx.Options.SetDuration(TimeSpan.FromSeconds(2));

            throw new Exception("Sloths are cool");
        });


        // Let EventListener poll for data
        await Task.Delay(500);

        // Assert
        meterProvider.ForceFlush(MaxTimeToAllowForFlush);

        var metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheMissTagValue);
        Assert.Equal(1, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheHitTagValue);
        Assert.Equal(0, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheFailSafeActivateTagValue);
        Assert.Equal(2, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheSetTagValue);
        Assert.Equal(2, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheStaleHitTagValue);
        Assert.Equal(2, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheBackgroundRefreshedTagValue);
        Assert.Equal(1, metricPoint.GetSumLong());

        metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheBackgroundFailedRefreshedTagValue);
        Assert.Equal(1, metricPoint.GetSumLong());
    }
}
