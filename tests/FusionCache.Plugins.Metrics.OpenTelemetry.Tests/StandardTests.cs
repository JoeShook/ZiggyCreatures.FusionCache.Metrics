using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Xunit;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.OpenTelemetry;

namespace FusionCache.Plugins.Metrics.OpenTelemetry.Tests;

public class StandardTests
{
    public class StandardEventTests : BaseTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EntryEventsWorkAsync(bool adaptive)
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

            fusionMeter.Start(cache);

            // MISS: +1
            await cache.TryGetAsync<int>("foo");

            // MISS: +1
            await cache.TryGetAsync<int>("bar");

            // SET: +1
            await cache.SetAsync("foo", 123);

            // HIT: +1
            await cache.TryGetAsync<int>("foo");

            // HIT: +1
            await cache.TryGetAsync<int>("foo");

            await Task.Delay(throttleDuration);
            await Task.Delay(100);

            // HIT (STALE): +1
            // FAIL-SAFE: +1
            if (adaptive)
            {
                _ = await cache.GetOrSetAsync<int>(
                    "foo",
                    async (ctx, _) =>
                    {
                        await Task.Delay(1, _);
                        throw new Exception("Sloths are cool");
                    });
            }
            else
            {
                _ = await cache.GetOrSetAsync<int>("foo", _ => throw new Exception("Sloths are cool"));
            }

            // MISS: +1
            await cache.TryGetAsync<int>("bar");

            // LET THE THROTTLE DURATION PASS
            await Task.Delay(throttleDuration);
            await Task.Delay(100);

            // HIT (STALE): +1
            // FAIL-SAFE: +1
            if (adaptive)
            {
                _ = await cache.GetOrSetAsync<int>(
                    "foo",
                    async (ctx, _) =>
                    {
                        await Task.Delay(1, _);
                        throw new Exception("Sloths are cool");
                    });
            }
            else
            {
                _ = await cache.GetOrSetAsync<int>("foo", _ => throw new Exception("Sloths are cool"));
            }

            // REMOVE: +1
            await cache.RemoveAsync("foo");

            // REMOVE: +1
            await cache.RemoveAsync("bar");

            await Task.Delay(600);

            // Assert
            meterProvider.ForceFlush(MaxTimeToAllowForFlush);

            var metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheMissTagValue);
            Assert.Equal(3, metricPoint.GetSumLong());

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheHitTagValue);
            Assert.Equal(2, metricPoint.GetSumLong());

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheStaleHitTagValue);
            Assert.Equal(2, metricPoint.GetSumLong());

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheSetTagValue);
            Assert.Equal(1, metricPoint.GetSumLong());

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheRemovedTagValue);
            Assert.Equal(2, metricPoint.GetSumLong());

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheFailSafeActivateTagValue);
            Assert.Equal(2, metricPoint.GetSumLong());
        }
    }

    public class StandardAdaptiveTests : BaseTest
    {
        [Fact]
        public async Task EntryEventsWorkAdaptiveCacheAsync()
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

            fusionMeter.Start(cache);

            // MISS: +1
            await cache.TryGetAsync<int>("foo");

            // MISS: +1
            await cache.TryGetAsync<int>("bar");

            // SET: +1
            // cache.GetOrSet<int>(
            //     "foo",
            //     (_) =>
            //     {
            //         return 123;
            //     });

            // SET: +1
            // SHOULD get a MISS: +1 here with an updated to FusionCache. https://github.com/jodydonetti/ZiggyCreatures.FusionCache/issues/49
            await cache.GetOrSetAsync<int>(
                "foo",
                async (_) =>
                {
                    await Task.Delay(1);
                    return 123;
                });

            // HIT: +1
            await cache.GetOrSetAsync<int>(
                "foo",
                async (_) =>
                {
                    await Task.Delay(1);
                    throw new Exception("Should not be here");
                });

            // HIT: +1
            await cache.GetOrSetAsync<int>(
                "foo",
                async (_) =>
                {
                    await Task.Delay(1);
                    throw new Exception("Should not be here");
                });

            await Task.Delay(throttleDuration);
            await Task.Delay(100);

            // HIT (STALE): +1
            // FAIL-SAFE: +1
            await cache.GetOrSetAsync<int>(
                "foo",
                async (_) =>
                {
                    await Task.Delay(1);
                    throw new Exception("Sloths are cool");
                });


            // MISS: +1
            await cache.TryGetAsync<int>("bar");

            // LET THE THROTTLE DURATION PASS
            await Task.Delay(throttleDuration);
            await Task.Delay(100);

            // HIT (STALE): +1
            // FAIL-SAFE: +1
            _ = await cache.GetOrSetAsync<int>(
                "foo",
                async (_) =>
                {
                    await Task.Delay(1);
                    throw new Exception("Sloths are cool");
                });

            // REMOVE: +1
            await cache.RemoveAsync("foo");

            // REMOVE: +1
            await cache.RemoveAsync("bar");

            await Task.Delay(600);

            // Assert
            meterProvider.ForceFlush(MaxTimeToAllowForFlush);

            var metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheMissTagValue);
            Assert.Equal(3, metricPoint.GetSumLong());

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheHitTagValue);
            Assert.Equal(2, metricPoint.GetSumLong());

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheStaleHitTagValue);
            Assert.Equal(2, metricPoint.GetSumLong());

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheSetTagValue);
            Assert.Equal(1, metricPoint.GetSumLong());

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheRemovedTagValue);
            Assert.Equal(2, metricPoint.GetSumLong());

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheFailSafeActivateTagValue);
            Assert.Equal(2, metricPoint.GetSumLong());
        }
    }
}