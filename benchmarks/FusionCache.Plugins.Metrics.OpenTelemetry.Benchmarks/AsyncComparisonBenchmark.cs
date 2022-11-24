using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.OpenTelemetry;

namespace ZiggyCreatures.Fusion.Caching.Plugins.OpenTelemetry.Benchmarks
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class AsyncComparisonBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddColumn(
                    StatisticColumn.P95
                );
            }
        }

        [Params(20)]
        public int FactoryDurationMs;

        [Params(10, 100)]
        public int Accessors;

        [Params(100)]
        public int KeysCount;

        [Params(1, 50)]
        public int Rounds;

        private List<string> Keys;
        private TimeSpan CacheDuration = TimeSpan.FromDays(10);
        private MemoryCache MemoryCache;
        private FusionMeter FusionMeter;
        private FusionCache Cache;

        [GlobalSetup]
        public void Setup()
        {
            // SETUP KEYS
            Keys = new List<string>();
            for (int i = 0; i < KeysCount; i++)
            {
                var key = Guid.NewGuid().ToString("N") + "-" + i.ToString();
                Keys.Add(key);
            }

            // create memory cache external so it can be shared with FusionCacheEventSource metrics provider so it can reporting on cache item count.
            MemoryCache = new MemoryCache(new MemoryCacheOptions());
            FusionMeter = new FusionMeter("fusionCache", MemoryCache);
            Cache = new FusionCache(
                new FusionCacheOptions { DefaultEntryOptions = new FusionCacheEntryOptions(CacheDuration) },
                MemoryCache);
            FusionMeter.Start(Cache);

        }

        [GlobalCleanup]
        public void CleanUp()
        {
            MemoryCache.Dispose();
            FusionMeter.Stop(Cache);
            FusionMeter.Dispose();
        }

        [Benchmark(Baseline = true)]
        public async Task FusionCache()
        {
            // Creates a MemoryCache in FusionCache
            using var cache = new FusionCache(new FusionCacheOptions { DefaultEntryOptions = new FusionCacheEntryOptions(CacheDuration) });
            for (int i = 0; i < Rounds; i++)
            {
                var tasks = new ConcurrentBag<Task>();
        
                Parallel.ForEach(Keys, key =>
                {
                    Parallel.For(0, Accessors, _ =>
                    {
                        var t = cache.GetOrSetAsync<SamplePayload>(
                            key,
                            async ct =>
                            {
                                await Task.Delay(FactoryDurationMs).ConfigureAwait(false);
                                return new SamplePayload();
                            }
                        );
                        tasks.Add(t.AsTask());
                    });
                });
        
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        
            // NO NEED TO CLEANUP, AUTOMATICALLY DONE WHEN DISPOSING
        }
        
        [Benchmark]
        public async Task FusionCacheWithFusionMeter()
        {
            for (int i = 0; i < Rounds; i++)
            {
                var tasks = new ConcurrentBag<Task>();
        
                Parallel.ForEach(Keys, key =>
                {
                    Parallel.For(0, Accessors, _ =>
                    {
                        var t = Cache.GetOrSetAsync<SamplePayload>(
                            key,
                            async ct =>
                            {
                                await Task.Delay(FactoryDurationMs).ConfigureAwait(false);
                                return new SamplePayload();
                            }
                        );
                        tasks.Add(t.AsTask());
                    });
                });
        
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task FusionCacheWithFusionMeterAndMeterProvider()
        {
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("fusionCache")
                .Build();

            for (int i = 0; i < Rounds; i++)
            {
                var tasks = new ConcurrentBag<Task>();

                Parallel.ForEach(Keys, key =>
                {
                    Parallel.For(0, Accessors, _ =>
                    {
                        var t = Cache.GetOrSetAsync<SamplePayload>(
                            key,
                            async ct =>
                            {
                                await Task.Delay(FactoryDurationMs).ConfigureAwait(false);
                                return new SamplePayload();
                            }
                        );
                        tasks.Add(t.AsTask());
                    });
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
    }
}
