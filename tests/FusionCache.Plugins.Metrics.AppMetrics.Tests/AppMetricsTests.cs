using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Extensions.Hosting;
using App.Metrics.Formatters.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Moq;
using Xunit;
using Xunit.Abstractions;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;

namespace ZiggyCreatures.Caching.Fusion.Plugins.Metrics.AppMetrics.Tests
{
    public class AppMetricsTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private static ISemanticConventions _semanticConventions = new SemanticConventions();

        public AppMetricsTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void SetCacheName()
        {
            var appMetrics = new MetricsBuilder()
                .Configuration.Configure(
                    options =>
                    {
                        options.DefaultContextLabel = "appMetricsContextLabel";
                    })
                .Build();

            Assert.Throws<ArgumentNullException>(() => new AppMetricsProvider(null, appMetrics));

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
            {
                var reporter = new TestReporter(_testOutputHelper);
                reporter.Formatter = new MetricsJsonOutputFormatter();

                var appMetrics = new MetricsBuilder()
                    .Configuration.Configure(
                        options =>
                        {
                            options.DefaultContextLabel = "appMetricsContextLabel";
                        })
                    .Report.Using(reporter)
                    .Build();


                using (var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
                    new FusionCacheOptions(),
                    memoryCache))
                using (var metricsReporterService =
                    new MetricsReporterBackgroundService(appMetrics, appMetrics.Options, appMetrics.Reporters, new Mock<IHostApplicationLifetime>().Object))
                {
                    var appMetricsProvider = new AppMetricsProvider("testCacheName", appMetrics, memoryCache);
                    appMetricsProvider.Start(cache);
                    await metricsReporterService.StartAsync(CancellationToken.None);


                    cache.DefaultEntryOptions.Duration = duration;
                    cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                    cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                    cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;





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
                    appMetricsProvider.Stop(cache);

                    // Let EventListener poll for data
                    await Task.Delay(1000);

                    var messages = reporter.Messages.ToList();

                    // foreach (var message in messages)
                    // {
                    //     _testOutputHelper.WriteLine(message.ToString());
                    // }

                    Assert.Equal(3, GetMetric(messages, SemanticConventions.Instance().CacheMissTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
                    Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheSetTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheRemovedTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheFailSafeActivateTagValue));
                }
            }
        }

        [Fact]
        public async Task EntryEventsWorkAdaptiveCacheAsync()
        {
            var duration = TimeSpan.FromSeconds(2);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(3);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            {
                var reporter = new TestReporter(_testOutputHelper);
                reporter.Formatter = new MetricsJsonOutputFormatter();

                var appMetrics = new MetricsBuilder()
                    .Configuration.Configure(
                        options => { options.DefaultContextLabel = "appMetricsContextLabel"; })
                    .Report.Using(reporter)
                    .Build();


                using (var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
                           new FusionCacheOptions(),
                           memoryCache))
                using (var metricsReporterService =
                       new MetricsReporterBackgroundService(appMetrics, appMetrics.Options, appMetrics.Reporters,
                           new Mock<IHostApplicationLifetime>().Object))
                {
                    var appMetricsProvider = new AppMetricsProvider("testCacheName", appMetrics, memoryCache);
                    appMetricsProvider.Start(cache);
                    await metricsReporterService.StartAsync(CancellationToken.None);


                    cache.DefaultEntryOptions.Duration = duration;
                    cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                    cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                    cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

                    // MISS: +1
                    await cache.TryGetAsync<int>("foo");

                    // MISS: +1
                    await cache.TryGetAsync<int>("bar");

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
                    appMetricsProvider.Stop(cache);

                    // Let EventListener poll for data
                    await Task.Delay(1000);

                    var messages = reporter.Messages.ToList();

                    Assert.Equal(3, GetMetric(messages, SemanticConventions.Instance().CacheMissTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
                    Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheSetTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheRemovedTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheFailSafeActivateTagValue));
                }
            }
        }

        [Fact]
        public async Task TryGetStaleFailSafe()
        {
            var duration = TimeSpan.FromSeconds(2);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(3);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            {
                var reporter = new TestReporter(_testOutputHelper);
                reporter.Formatter = new MetricsJsonOutputFormatter();

                var appMetrics = new MetricsBuilder()
                    .Configuration.Configure(
                        options =>
                        {
                            options.DefaultContextLabel = "appMetricsContextLabel";
                        })
                    .Report.Using(reporter)
                    .Build();


                using (var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
                    new FusionCacheOptions(),
                    memoryCache))
                using (var metricsReporterService =
                    new MetricsReporterBackgroundService(appMetrics, appMetrics.Options, appMetrics.Reporters, new Mock<IHostApplicationLifetime>().Object))
                {
                    cache.DefaultEntryOptions.Duration = duration;
                    cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                    cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                    cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;

                    var appMetricsProvider = new AppMetricsProvider("testCacheName", appMetrics);
                    appMetricsProvider.Start(cache);
                    await metricsReporterService.StartAsync(CancellationToken.None);


                    // INITIAL, NON-TRACKED SET
                    cache.Set<int>("foo", 42);

                    // HIT: +1
                    cache.TryGet<int>("foo");

                    // LET IT BECOME STALE
                    Thread.Sleep(duration);

                    // HIT (STALE): +1
                    cache.TryGet<int>("foo");

                    await Task.Delay(TimeSpan.FromSeconds(5));

                    // REMOVE HANDLERS
                    appMetricsProvider.Stop(cache);

                    // Let EventListener poll for data
                    await Task.Delay(1000);

                    var messages = reporter.Messages.ToList();

                    // foreach (var message in messages)
                    // {
                    //     _testOutputHelper.WriteLine(message.ToString());
                    // }

                    Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
                    Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                }
            }
        }

        [Fact]
        public async Task TryGetStaleFailSafeAdaptive()
        {
            var duration = TimeSpan.FromMinutes(2);
            var maxDuration = TimeSpan.FromDays(1);
            var throttleDuration = TimeSpan.FromSeconds(1);
            var softTimeout = TimeSpan.FromMilliseconds(100);
            var hardTimeout = TimeSpan.FromMilliseconds(500);
            var adaptiveDuration = TimeSpan.FromSeconds(2);

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions()))
            {
                var reporter = new TestReporter(_testOutputHelper);
                reporter.Formatter = new MetricsJsonOutputFormatter();

                var appMetrics = new MetricsBuilder()
                    .Configuration.Configure(
                        options =>
                        {
                            options.DefaultContextLabel = "appMetricsContextLabel";
                        })
                    .Report.Using(reporter)
                    .Build();


                using (var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
                    new FusionCacheOptions(),
                    memoryCache))
                using (var metricsReporterService =
                    new MetricsReporterBackgroundService(appMetrics, appMetrics.Options, appMetrics.Reporters, new Mock<IHostApplicationLifetime>().Object))
                {
                    cache.DefaultEntryOptions.Duration = duration;
                    cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                    cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                    cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;
                    cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
                    cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;

                    var appMetricsProvider = new AppMetricsProvider("testCacheName", appMetrics);
                    appMetricsProvider.Start(cache);
                    await metricsReporterService.StartAsync(CancellationToken.None);


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
                    
                    // REMOVE HANDLERS
                    appMetricsProvider.Stop(cache);

                    // Let EventListener poll for data
                    await Task.Delay(1000);

                    var messages = reporter.Messages.ToList();

                    // foreach (var message in messages)
                    // {
                    //     _testOutputHelper.WriteLine(message.ToString());
                    // }

                    Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                    Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheSetTagValue));
                    Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
                }
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
            {
                var reporter = new TestReporter(_testOutputHelper);
                var appMetrics = new MetricsBuilder()
                    .Configuration.Configure(
                        options =>
                        {
                            options.DefaultContextLabel = "appMetricsContextLabel";
                        })
                    .Report.Using(reporter)
                    .Build();


                using (var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
                    new FusionCacheOptions(),
                    memoryCache))
                using (var metricsReporterService =
                    new MetricsReporterBackgroundService(appMetrics, appMetrics.Options, appMetrics.Reporters, new Mock<IHostApplicationLifetime>().Object))
                {
                    var appMetricsProvider = new AppMetricsProvider("testCacheName", appMetrics, memoryCache);
                    appMetricsProvider.Start(cache);
                    await metricsReporterService.StartAsync(CancellationToken.None);

                    cache.DefaultEntryOptions.Duration = duration;
                    cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                    cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                    cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
                    cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;
                    cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;


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
                    await Task.Delay(1000);
                    
                    var messages = reporter.Messages.ToList();
                    // messages.ForEach(c => _testOutputHelper.WriteLine(c.ToString()));

                    Assert.Equal(0, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheFailSafeActivateTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheSetTagValue));
                    Assert.Equal(2, GetMetric(messages, SemanticConventions.Instance().CacheStaleHitTagValue));
                    Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheBackgroundRefreshedTagValue));
                    Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheBackgroundFailedRefreshedTagValue));
                }
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
            {
                var reporter = new TestReporter(_testOutputHelper);
                var appMetrics = new MetricsBuilder()
                    .Configuration.Configure(
                        options => { options.DefaultContextLabel = "appMetricsContextLabel"; })
                    .Report.Using(reporter)
                    .Build();


                using (var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
                    new FusionCacheOptions(),
                    memoryCache))
                using (var metricsReporterService =
                    new MetricsReporterBackgroundService(appMetrics, appMetrics.Options, appMetrics.Reporters, new Mock<IHostApplicationLifetime>().Object))
                {
                    var appMetricsProvider = new AppMetricsProvider("testCacheName", appMetrics, memoryCache);
                    appMetricsProvider.Start(cache);
                    await metricsReporterService.StartAsync(CancellationToken.None);

                    cache.DefaultEntryOptions.Duration = duration;
                    cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                    cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                    cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
                    cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;
                    cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;


                    // INITIAL, NON-TRACKED SET
                    await cache.SetAsync<int>("foo", 42,
                        options => options.FailSafeMaxDuration = TimeSpan.FromSeconds(5));

                    // LET IT BECOME STALE
                    await Task.Delay(TimeSpan.FromSeconds(10));

                    await cache.TryGetAsync<int>("foo"); // wake up the sloth

                    // Let EventListener poll for data
                    await Task.Delay(1100);

                    var messages = reporter.Messages.ToList();
                    // messages.ForEach(c => _testOutputHelper.WriteLine(c.ToString()));

                    Assert.Equal(0, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                    Assert.Equal(1, GetMetric(messages, SemanticConventions.Instance().CacheExpiredEvictTagValue));
                }
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

            using (var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 } ))
            {
                var reporter = new TestReporter(_testOutputHelper);
                var appMetrics = new MetricsBuilder()
                    .Configuration.Configure(
                        options =>
                        {
                            options.DefaultContextLabel = "appMetricsContextLabel";
                        })
                    .Report.Using(reporter)
                    .Build();


                using (var cache = new ZiggyCreatures.Caching.Fusion.FusionCache(
                    new FusionCacheOptions(),
                    memoryCache))
                using (var metricsReporterService =
                    new MetricsReporterBackgroundService(appMetrics, appMetrics.Options, appMetrics.Reporters, new Mock<IHostApplicationLifetime>().Object))
                {
                    var appMetricsProvider = new AppMetricsProvider("testCacheName", appMetrics, memoryCache);
                    appMetricsProvider.Start(cache);
                    await metricsReporterService.StartAsync(CancellationToken.None);

                    cache.DefaultEntryOptions.Duration = duration;
                    cache.DefaultEntryOptions.IsFailSafeEnabled = true;
                    cache.DefaultEntryOptions.FailSafeMaxDuration = maxDuration;
                    cache.DefaultEntryOptions.FactorySoftTimeout = softTimeout;
                    cache.DefaultEntryOptions.FactoryHardTimeout = hardTimeout;
                    cache.DefaultEntryOptions.FailSafeThrottleDuration = throttleDuration;


                    for (int i = 0; i < 2000; i++)
                    {
                        await cache.SetAsync<int>($"foo{i}", i, options => options.SetSize(1));

                        if (i % 5 == 0) // slow it down but still go fast. :)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(1));
                        }
                    }

                    // Let EventListener poll for data
                    await Task.Delay(1000);

                    var messages = reporter.Messages.ToList();
                    // messages.ForEach(c => _testOutputHelper.WriteLine(c.ToString()));

                    Assert.Equal(0, GetMetric(messages, SemanticConventions.Instance().CacheHitTagValue));
                    Assert.True(GetMetric(messages, SemanticConventions.Instance().CacheCapacityEvictTagValue) > 1899);
                }
            }


        }

        private Int64 GetMetric(List<Tuple<string, long>> messages, string cacheMissTagValue)
        {
            var eventCount = messages
                .LastOrDefault(m => m.Item1.StartsWith(cacheMissTagValue))?
                .Item2;

            return eventCount.GetValueOrDefault();
        }
    }
}
