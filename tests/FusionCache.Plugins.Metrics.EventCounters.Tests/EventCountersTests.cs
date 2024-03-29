﻿#region (c) 2021 Joseph Shook. All rights reserved.
// /*
//  Authors:
//     Joseph Shook   Joseph.Shook@Surescripts.com
// 
//  See LICENSE in the project root for license information.
// */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using Xunit.Abstractions;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.EventCounters;

namespace ZiggyCreatures.Caching.Fusion.Plugins.EventCounters.Tests
{
    public class EventCountersTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private static ISemanticConventions _semanticConventions = new SemanticConventions();

        public EventCountersTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void SetCacheName()
        {
            using (var cache = new MemoryCache(new MemoryCacheOptions()))
            {
                using (var eventSource = new FusionCacheEventSource("testCacheName", cache))
                {
                    Assert.Equal("testCacheName", eventSource.Name);
                }
            }
        }

        [Fact]
        public void LoadPluginWithItemCountCheck()
        {
            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            using (var eventSource = new FusionCacheEventSource("testCacheName", memoryCache))
            using (var listener = new TestEventListener())
            {
                using (var cache = new FusionCache(
                    new FusionCacheOptions(),
                    memoryCache))
                {
                    const long AllKeywords = -1;
                    listener.EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords) AllKeywords,
                        new Dictionary<string, string>
                        {
                            ["EventCounterIntervalSec"] = "1"
                        });

                    for (int i = 0; i < 100; i++)
                    {
                        cache.GetOrSet($"A-Key-{i}", (cts) => $"A-Value{i}");
                    }

                    // Let EventListener poll for data
                    System.Threading.Thread.Sleep(2500);

                    var messages = listener.Messages.ToList();
                    AssertItemCount(messages);
                    AssertCacheNameInjected(messages);
                }
            }
        }

        private static void AssertItemCount(List<EventWrittenEventArgs> messages)
        {
            long itemCount = 0;
            System.Threading.Thread.Sleep(3000);


            foreach (var eventData in messages)
            {
                bool itemCountFlag = false;
                for (int i = 0; i < eventData.Payload.Count; ++i)
                {
                    if (eventData.Payload[i] is IDictionary<string, object> eventPayload)
                    {
                        if (eventPayload.TryGetValue("Name", out object nameValue))
                        {
                            if (nameValue.ToString() == "ITEM_COUNT")
                            {
                                itemCountFlag = true;
                            }
                        }

                        if (itemCountFlag)
                        {
                            if (eventPayload.TryGetValue("Mean", out object meanValue))
                            {
                                itemCount = Convert.ToInt64(meanValue);
                            }
                        }
                    }
                }
            }

            Assert.Equal(100, itemCount);
        }
        
        private static void AssertCacheNameInjected(List<EventWrittenEventArgs> messages)
        {
            string cacheName = null;


            foreach (var eventData in messages)
            {
                for (int i = 0; i < eventData.Payload.Count; ++i)
                {
                    if (eventData.Payload[i] is IDictionary<string, object> eventPayload)
                    {
                        if (eventPayload.TryGetValue("Metadata", out object metaDataValue))
                        {
                            var metaDataString = Convert.ToString(metaDataValue);
                            var metaData = metaDataString
                                .Split(',')
                                .Select(item => item.Split(':'))
                                .ToDictionary(s => s[0], s => s[1]);

                            cacheName = metaData[_semanticConventions.CacheNameTagName];
                        }
                    }
                }
            }

            Assert.Equal("testCacheName", cacheName);
        }

        private static long GetMetric(List<EventWrittenEventArgs> messages, string metricName)
        {
            long itemCount = 0;

            foreach (var eventData in messages)
            {
                bool itemCountFlag = false;
                for (int i = 0; i < eventData.Payload.Count; ++i)
                {
                    if (eventData.Payload[i] is IDictionary<string, object> eventPayload)
                    {
                        if (eventPayload.TryGetValue("Name", out object nameValue))
                        {
                            if (nameValue.ToString() == metricName)
                            {
                                itemCountFlag = true;
                            }
                        }

                        if (itemCountFlag)
                        {
                            if (eventPayload.TryGetValue("Increment", out object meanValue))
                            {
                                itemCount += Convert.ToInt64(meanValue);
                            }
                        }
                    }
                }
            }

            return itemCount;
        }

        private List<long> GetCountMetrics(List<EventWrittenEventArgs> messages, string metricName)
        {
            var itemCount = new List<long>();

            foreach (var eventData in messages)
            {
                bool itemCountFlag = false;
                for (int i = 0; i < eventData.Payload.Count; ++i)
                {
                    if (eventData.Payload[i] is IDictionary<string, object> eventPayload)
                    {
                        if (eventPayload.TryGetValue("Name", out object nameValue))
                        {
                            if (nameValue.ToString() == metricName)
                            {
                                itemCountFlag = true;
                            }
                        }

                        if (itemCountFlag)
                        {
                            if (eventPayload.TryGetValue("Mean", out object meanValue))
                            {
                                itemCount.Add(Convert.ToInt64(meanValue));
                            }
                        }
                    }
                }
            }
            
            return itemCount;
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EntryEventsWorkAsync(bool adaptive)
        {
            var duration = TimeSpan.FromSeconds(2);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(3);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            using (var eventSource = new FusionCacheEventSource("testCacheName", memoryCache))
            using (var listener = new TestEventListener())
            using (var cache = new FusionCache(
                new FusionCacheOptions() {EnableSyncEventHandlersExecution = true},
                memoryCache))
            {
                const long AllKeywords = -1;
                listener.EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords) AllKeywords,
                    new Dictionary<string, string>
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });

                cache.DefaultEntryOptions.Duration = duration;
                cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

                eventSource.Start(cache);

                // MISS: +1
                await cache.TryGetAsync<int>("foo");

                // MISS: +1
                await cache.TryGetAsync<int>("bar");

                // SET: +1
                await cache.SetAsync<int>("foo", 123);

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

                await Task.Delay(TimeSpan.FromSeconds(5));

                // REMOVE HANDLERS
                eventSource.Stop(cache);

                // Let EventListener poll for data
                await Task.Delay(1500);

                var messages = listener.Messages.ToList();

                Assert.Equal(3, GetMetric(messages, SemanticConventions.Instance().CacheMissTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
                Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheSetTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheRemovedTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheFailSafeActivateTagValue));
            }
        }

        [Fact]
        public async Task EntryEventsWorkAdaptiveCacheAsync()
        {
            var duration = TimeSpan.FromSeconds(2);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(3);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            using (var eventSource = new FusionCacheEventSource("testCacheName", memoryCache))
            using (var listener = new TestEventListener())
            using (var cache = new FusionCache(
                new FusionCacheOptions() { EnableSyncEventHandlersExecution = true },
                memoryCache))
            {
                const long AllKeywords = -1;
                listener.EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)AllKeywords,
                    new Dictionary<string, string>
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });

                cache.DefaultEntryOptions.Duration = duration;
                cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

                eventSource.Start(cache);

                // MISS: +1
                await cache.TryGetAsync<int>("foo");

                // MISS: +1
                await cache.TryGetAsync<int>("bar");

                // MISS: +1
                // SET: +1
                await cache.GetOrSetAsync<int>(
                    "foo",
                    async (ctx, _) =>
                    {
                        await Task.Delay(1, _);
                        return 123;
                    });

                // HIT: +1
                await cache.GetOrSetAsync<int>(
                    "foo",
                    async (ctx, _) =>
                    {
                        await Task.Delay(1, _);
                        throw new Exception("Should not be here");
                    });
                
                // HIT: +1
                await cache.GetOrSetAsync<int>(
                    "foo",
                    async (ctx, _) =>
                    {
                        await Task.Delay(1, _);
                        throw new Exception("Should not be here");
                    });

                await Task.Delay(throttleDuration);
                await Task.Delay(100);

                // HIT (STALE): +1
                // FAIL-SAFE: +1
                await cache.GetOrSetAsync<int>(
                    "foo",
                    async (ctx, _) =>
                    {
                        await Task.Delay(1, _);
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
                    async (ctx, _) =>
                    {
                        await Task.Delay(1, _);
                        throw new Exception("Sloths are cool");
                    });

                // REMOVE: +1
                await cache.RemoveAsync("foo");

                // REMOVE: +1
                await cache.RemoveAsync("bar");

                await Task.Delay(TimeSpan.FromSeconds(5));

                // REMOVE HANDLERS
                eventSource.Stop(cache);

                // Let EventListener poll for data
                await Task.Delay(1500);

                var messages = listener.Messages.ToList();

                Assert.Equal(4, GetMetric(messages, SemanticConventions.Instance().CacheMissTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
                Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheSetTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheRemovedTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheFailSafeActivateTagValue));
            }
        }

        [Fact]
        public void TryGetStaleFailSafe()
        {
            var duration = TimeSpan.FromSeconds(2);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(3);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            using (var eventSource = new FusionCacheEventSource("testCacheName", memoryCache))
            using (var listener = new TestEventListener())
            using (var cache = new FusionCache(
                new FusionCacheOptions() { EnableSyncEventHandlersExecution = true },
                memoryCache))
            {
                const long AllKeywords = -1;
                listener.EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)AllKeywords,
                    new Dictionary<string, string>
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });

                cache.DefaultEntryOptions.Duration = duration;
                cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

                // INITIAL, NON-TRACKED SET
                cache.Set<int>("foo", 42);

                eventSource.Start(cache);

                // HIT: +1
                cache.TryGet<int>("foo");

                // LET IT BECOME STALE
                Thread.Sleep(duration);

                // HIT (STALE): +1
                cache.TryGet<int>("foo");
                // HIT (STALE): +1
                cache.TryGet<int>("foo");

                // REMOVE HANDLERS
                eventSource.Stop(cache);

                // Let EventListener poll for data
                Thread.Sleep(1500);

                var messages = listener.Messages.ToList();

                Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
            }
        }

        [Fact]
        public void TryGetStaleFailSafeAdaptive()
        {
            var duration = TimeSpan.FromMinutes(2);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(1);
            var softTimeout = TimeSpan.FromMilliseconds(100);
            var hardTimeout = TimeSpan.FromMilliseconds(500);
            var adaptiveDuration = TimeSpan.FromSeconds(2);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            using (var eventSource = new FusionCacheEventSource("testCacheName", memoryCache))
            using (var listener = new TestEventListener())
            using (var cache = new FusionCache(
                new FusionCacheOptions() { EnableSyncEventHandlersExecution = true },
                memoryCache))
            {
                const long AllKeywords = -1;
                listener.EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)AllKeywords,
                    new Dictionary<string, string>
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });

                cache.DefaultEntryOptions.Duration = duration; // duration in minutes
                cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;
                cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
                cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;

                eventSource.Start(cache);

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

                // REMOVE HANDLERS
                eventSource.Stop(cache);

                // Let EventListener poll for data
                Thread.Sleep(1500);

                var messages = listener.Messages.ToList();

                Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheSetTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
            }
        }

        [Fact]
        public async Task BackgroundFailSafeAsync()
        {
            var duration = TimeSpan.FromSeconds(2);
            var softTimeout = TimeSpan.FromMilliseconds(100);
            var hardTimeout = TimeSpan.FromMilliseconds(500);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(3);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            using (var eventSource = new FusionCacheEventSource("testCacheName", memoryCache))
            using (var listener = new TestEventListener())
            using (var cache = new FusionCache(
                new FusionCacheOptions() {EnableSyncEventHandlersExecution = true},
                memoryCache))
            {
                const long AllKeywords = -1;
                listener.EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords) AllKeywords,
                    new Dictionary<string, string>
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });

                cache.DefaultEntryOptions.Duration = duration;
                cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
                cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;
                cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

                eventSource.Start(cache);

                // INITIAL, NON-TRACKED SET
                await cache.SetAsync<int>("foo", 42);

                // LET IT BECOME STALE
                await Task.Delay(throttleDuration);

                // HIT (STALE): +1
                // FAIL-SAFE: +1
                // SET: +1
                _ = await cache.GetOrSetAsync<int>("foo", async _ =>
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


                // Let EventListener poll for data
                await Task.Delay(3500);

                var messages = listener.Messages.ToList();

                Assert.Equal(0, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheFailSafeActivateTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheSetTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
                Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheBackgroundRefreshedTagValue));
                Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheBackgroundFailedRefreshedTagValue));
            }
        }

        [Fact]
        public async Task BackgroundFailSafeAdaptiveAsync()
        {
            var duration = TimeSpan.FromMinutes(2);
            var softTimeout = TimeSpan.FromMilliseconds(100);
            var hardTimeout = TimeSpan.FromMilliseconds(500);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(3);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            using (var eventSource = new FusionCacheEventSource("testCacheName", memoryCache))
            using (var listener = new TestEventListener())
            using (var cache = new FusionCache(
                new FusionCacheOptions() { EnableSyncEventHandlersExecution = true },
                memoryCache))
            {
                const long AllKeywords = -1;
                listener.EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)AllKeywords,
                    new Dictionary<string, string>
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });

                cache.DefaultEntryOptions.Duration = duration;
                cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
                cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;
                cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

                eventSource.Start(cache);

                // INITIAL
                // SET: +1
                await cache.GetOrSetAsync<int>("foo", async (ctx, _) =>
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
                _ = await cache.GetOrSetAsync<int>("foo", async (ctx, _) =>
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
                await Task.Delay(3500);

                var messages = listener.Messages.ToList();

                Assert.Equal(0, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheFailSafeActivateTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheSetTagValue));
                Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
                Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheBackgroundRefreshedTagValue));
                Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheBackgroundFailedRefreshedTagValue));
            }
        }

        [Fact]
        public async Task EvictExpiredAsync()
        {
            var duration = TimeSpan.FromSeconds(2);
            var softTimeout = TimeSpan.FromMilliseconds(100);
            var hardTimeout = TimeSpan.FromMilliseconds(500);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(3);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            using (var eventSource = new FusionCacheEventSource("testCacheName", memoryCache))
            using (var listener = new TestEventListener())
            using (var cache = new FusionCache(
                new FusionCacheOptions() { EnableSyncEventHandlersExecution = true },
                memoryCache))
            {
                const long AllKeywords = -1;
                listener.EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)AllKeywords,
                    new Dictionary<string, string>
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });

                cache.DefaultEntryOptions.Duration = duration;
                cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
                cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;
                cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

                eventSource.Start(cache);

                // INITIAL, NON-TRACKED SET
                await cache.SetAsync<int>("foo", 42, options => options.FailSafeMaxDuration = TimeSpan.FromSeconds(2));

                // LET IT BECOME STALE
                await Task.Delay(TimeSpan.FromSeconds(4));
                
                await cache.TryGetAsync<int>("foo"); // wake up the sloth

                // Let EventListener poll for data
                await Task.Delay(3500);

                var messages = listener.Messages.ToList();

                Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheExpiredEvictTagValue));

            }
        }
        
        [Fact]
        public async Task EvictExpiredCapacityAsync()
        {
            var duration = TimeSpan.FromSeconds(2);
            var softTimeout = TimeSpan.FromMilliseconds(100);
            var hardTimeout = TimeSpan.FromMilliseconds(500);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(3);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions {SizeLimit = 100} ))
            using (var eventSource = new FusionCacheEventSource("testCacheName", memoryCache))
            using (var listener = new TestEventListener())
            using (var cache = new FusionCache(
                new FusionCacheOptions(),
                memoryCache))
            {
                const long AllKeywords = -1;
                listener.EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords) AllKeywords,
                    new Dictionary<string, string>
                    {
                        ["EventCounterIntervalSec"] = "1"
                    });

                cache.DefaultEntryOptions.Duration = duration;
                //cache.DefaultEntryOptions.SetSize(100);  // Doesn't respect this when sending in your own cache?
                cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
                cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;
                cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

                eventSource.Start(cache);

                for (int i = 0; i < 2000; i++)
                {
                    await cache.SetAsync<int>($"foo{i}", i, options =>
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
                // Let EventListener poll for data
                await Task.Delay(1500);

                var messages = listener.Messages.ToList();

                var counts = GetCountMetrics(messages, SemanticConventions.Instance().CacheItemCountTagValue);
                counts.ForEach(c => _testOutputHelper.WriteLine(c.ToString()));

                Assert.All(counts, c => Assert.True(c <= 100));

                Assert.True(GetMetric(messages, SemanticConventions.Instance().CacheCapacityEvictTagValue) > 1899);

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
}

