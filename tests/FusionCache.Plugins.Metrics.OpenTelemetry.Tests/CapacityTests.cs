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

namespace FusionCache.Plugins.Metrics.OpenTelemetry.Tests
{
    public class CapacityTests : BaseTest
    {
        [Fact]
        public async Task EvictExpiredAsync()
        {
            var exportedItems = new List<Metric>();
            var cacheName = $"{Utils.GetCurrentMethodName()}";
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
            await cache.SetAsync("foo", 42, options => options.FailSafeMaxDuration = TimeSpan.FromSeconds(2));

            // LET IT BECOME STALE
            await Task.Delay(TimeSpan.FromSeconds(4));

            await cache.TryGetAsync<int>("foo"); // wake up the sloth

            await Task.Delay(500);

            // Assert
            meterProvider.ForceFlush(MaxTimeToAllowForFlush);

            var metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheExpiredEvictTagValue);
            Assert.Equal(1, metricPoint.GetSumLong());
        }

        [Fact]
        public async Task EvictExpiredCapacityAsync()
        {
            var exportedItems = new List<Metric>();
            var cacheName = $"{Utils.GetCurrentMethodName()}";
            var duration = TimeSpan.FromSeconds(2);
            var softTimeout = TimeSpan.FromMilliseconds(100);
            var hardTimeout = TimeSpan.FromMilliseconds(500);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(3);

            using var memoryCache = new MemoryCache(new MemoryCacheOptions() { SizeLimit = 100 });
            using var fusionMeter = new FusionMeter(cacheName, memoryCache);
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter(cacheName)
                .AddInMemoryExporter(exportedItems)
                .Build();
            using var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
                new FusionCacheOptions(),
                memoryCache);

            cache.DefaultEntryOptions.Duration = duration;
            //cache.DefaultEntryOptions.SetSize(100);  // Doesn't respect this when sending in your own cache?
            cache.DefaultEntryOptions.IsFailSafeEnabled = true;
            cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
            cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
            cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;
            cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

            fusionMeter.Start(cache);

            for (int i = 0; i < 2000; i++)
            {
                await cache.SetAsync($"foo{i}", i, options =>
                {
                    options.SetSize(1);
                    options.Priority = CacheItemPriority.High;
                });

                if (i % 5 == 0) // slow it down but still go fast. :)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1));
                }
            }

            await cache.TryGetAsync<int>("foo10"); // wake up the sloth

            // Assert
            meterProvider.ForceFlush(MaxTimeToAllowForFlush);

            var metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheItemCountTagValue);
            Assert.True(metricPoint.GetGaugeLastValueLong() < 101);

            metricPoint = GetMetricPoint(exportedItems, SemanticConventions.CacheCapacityEvictTagValue);
            Assert.True(metricPoint.GetSumLong() > 1899);

            //
            // Ensure the newest items are in the cache.
            // Can't assert perfection here.  The capacity behavior of MemoryCache is not perfect.
            // One would expect the last 100 items to be in cache but typically I find about 95 items from the last 200 entered.
            // Comment out the WriteLine to see for yourself.
            //
            for (int i = 1999; i > -1; i--)
            {
                var item = await cache.TryGetAsync<int>($"foo{i}", token: CancellationToken.None);
                var value = item.GetValueOrDefault(3000);
                if (value != 3000)
                {
                    //_testOutputHelper.WriteLine(value.ToString());
                }

                if (i > 1800)
                {
                    //Assert.Equal(i, item.GetValueOrDefault(3000));
                }
            }
        }
    }
}