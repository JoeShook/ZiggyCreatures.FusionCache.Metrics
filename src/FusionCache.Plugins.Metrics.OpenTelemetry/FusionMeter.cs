using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;
using ZiggyCreatures.Caching.Fusion.Events;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;

namespace ZiggyCreatures.Caching.Fusion.Plugins.Metrics.OpenTelemetry
{
    public class FusionMeter: IFusionCachePlugin, IDisposable
    {
        private Counter<int>? _cacheHitCounter;
        private Counter<int>? _cacheMissCounter;
        private Counter<int>? _cacheSetCounter;
        private Counter<int>? _cacheStaleHitCounter;
        private Counter<int>? _cacheBackgroundRefreshedCounter;
        private Counter<int>? _cacheBackgroundRefreshedErrorCounter;
        private Counter<int>? _cacheCacheFactoryErrorCounter;
        private Counter<int>? _cacheFactorySyntheticTimeoutCounter;
        private Counter<int>? _cacheFailSafeActivateCounter;
        private Counter<int>? _cacheExpiredEvictCounter;
        private Counter<int>? _cacheCapacityEvictCounter;
        private Counter<int>? _cacheRemovedCounter;
        
        private readonly Meter _meter;
        private readonly MemoryCache? _cache;
        private readonly ISemanticConventions _conventions;
        private readonly string _cacheName;
        private readonly string _measurementName;
        private MetricsConfig _metricsConfig;
        private readonly KeyValuePair<string, object?> _cacheNameTag;
        private readonly KeyValuePair<string, object?> _applicationTagName;
        private readonly KeyValuePair<string, object?> _applicationVersionTagName;

        public FusionMeter(
            string cacheName,
            string? measurementName = null,
            MetricsConfig? metricsConfig = null,
            ISemanticConventions? semanticConventions = null)
            : this(
                cacheName,
                new MemoryCache(new MemoryCacheOptions()),
                measurementName, 
                metricsConfig,
                semanticConventions
                )
        {
        } 

        public FusionMeter(
            string cacheName,
            IMemoryCache? cache,
            string? measurementName = null,
            MetricsConfig? metricsConfig = null,
            ISemanticConventions? semanticConventions = null)
        {
            _cacheName = cacheName;
            _metricsConfig = metricsConfig ?? new MetricsConfig();
            _measurementName = measurementName ?? $"{_metricsConfig.Prefix}{_metricsConfig.ApplicationName}_{_metricsConfig.MeasurementName}";
            _conventions = semanticConventions ?? new SemanticConventions();

            if (cache is MemoryCache memoryCache)
            {
                _cache = memoryCache;
            }

            _meter = new Meter(cacheName);
            CreateCounters();
            _cacheNameTag = new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _cacheName);
            _applicationTagName = new KeyValuePair<string, object?>(_conventions.ApplicationTagName, _metricsConfig.ApplicationName);
            _applicationVersionTagName = new KeyValuePair<string, object?>(_conventions.ApplicationVersionTagName, _metricsConfig.ApplicationVersion);
        }

        private void CreateCounters()
        {
            _cacheHitCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Hits");
            _cacheMissCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Misses");
            _cacheSetCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Sets");
            _cacheStaleHitCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Stale Hit");
            _cacheBackgroundRefreshedCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Background Refresh");
            _cacheBackgroundRefreshedErrorCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Background Refresh Error");
            _cacheCacheFactoryErrorCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Factory Error");
            _cacheFactorySyntheticTimeoutCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Factory Synthetic Timeout");
            _cacheFailSafeActivateCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Fail-Safe activation");
            _cacheExpiredEvictCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Expired Eviction");
            _cacheCapacityEvictCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Capacity Eviction");
            _cacheRemovedCounter = _meter.CreateCounter<int>(_measurementName, description: "Cache Removed");
            
            _meter.CreateObservableGauge<long>(
                _measurementName,
                () => new Measurement<long>(_cache?.Count ?? 0,
                    new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheItemCountTagValue),
                    _cacheNameTag, 
                    _applicationTagName, 
                    _applicationVersionTagName),
                description: "Cache Size");
        }

        public MemoryCache MemoryCache => _cache;

        #region IFusionMetrics

        /// <summary>Cache item hit counter.</summary>
        public void CacheHit()
        {
            _cacheHitCounter?.Add(1, 
                _cacheNameTag, 
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheHitTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>
        /// Cache item miss counter.  When a cache item is not found in the local cache
        /// </summary>
        public void CacheMiss()
        {
            _cacheMissCounter?.Add(1, 
                _cacheNameTag,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheMissTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>
        /// Cache item set counter.  When a cache item is written to local cache
        /// </summary>
        public void CacheSet()
        {
            _cacheSetCounter?.Add(1, 
                _cacheNameTag,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheSetTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>
        /// Cache item stale hit counter.  Cache item failed to complete within soft timeout period.
        /// </summary>
        public void CacheStaleHit()
        {
            _cacheStaleHitCounter?.Add(1,
                _cacheNameTag,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheStaleHitTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>Cache item refresh in background.</summary>
        public void CacheBackgroundRefreshSuccess()
        {
            _cacheBackgroundRefreshedCounter?.Add(1,
                _cacheNameTag,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheBackgroundRefreshedTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>Cache item refresh in background failed.</summary>
        public void CacheBackgroundRefreshError()
        {
            _cacheBackgroundRefreshedErrorCounter?.Add(1,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheBackgroundFailedRefreshedTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>Generic cache factory error.</summary>
        public void CacheFactoryError()
        {
            _cacheCacheFactoryErrorCounter?.Add(1,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheCacheFactoryErrorTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>Cache factory synthetic timeout</summary>
        public void CacheFactorySyntheticTimeout()
        {
            _cacheFactorySyntheticTimeoutCounter?.Add(1,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheFactorySyntheticTimeoutTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>The event for a fail-safe activation.</summary>
        public void CacheFailSafeActivate()
        {
            _cacheFailSafeActivateCounter?.Add(1,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheFailSafeActivateTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>Cache item expired</summary>
        public void CacheExpired()
        {
            _cacheExpiredEvictCounter?.Add(1,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheExpiredEvictTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>Cache item removed due to capacity</summary>
        public void CacheCapacityExpired()
        {
            _cacheCapacityEvictCounter?.Add(1,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheCapacityEvictTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        /// <summary>Cache item explicitly removed by user code</summary>
        public void CacheRemoved()
        {
            _cacheRemovedCounter?.Add(1,
                new KeyValuePair<string, object?>(_conventions.CacheEventTagName, _conventions.CacheRemovedTagValue),
                _applicationTagName,
                _applicationVersionTagName);
        }

        #endregion
        /// <inheritdoc />
        public void Stop(IFusionCache fusionCache)
        {
            fusionCache.Events.Hit -= HandleCacheHit;
            fusionCache.Events.Miss -= HandleCacheMiss;
            fusionCache.Events.Set -= HandleCacheSet;
            fusionCache.Events.Remove -= HandleCacheRemoved;
            fusionCache.Events.Memory.Eviction -= HandleCacheEviction;
            fusionCache.Events.BackgroundFactorySuccess -= HandleBackgroundFactorySuccess;
            fusionCache.Events.BackgroundFactoryError -= HanldeBackgroundFactoryError;
            fusionCache.Events.FactoryError -= HanldeFactoryError;
            fusionCache.Events.FactorySyntheticTimeout -= HandleFactorySyntheticTimeout;
            fusionCache.Events.FailSafeActivate -= HandleFailSafeActivate;
        }

        /// <inheritdoc />
        public void Start(IFusionCache fusionCache)
        {
            fusionCache.Events.Hit += HandleCacheHit;
            fusionCache.Events.Miss += HandleCacheMiss;
            fusionCache.Events.Set += HandleCacheSet;
            fusionCache.Events.Remove += HandleCacheRemoved;
            fusionCache.Events.Memory.Eviction += HandleCacheEviction;
            fusionCache.Events.BackgroundFactorySuccess += HandleBackgroundFactorySuccess;
            fusionCache.Events.BackgroundFactoryError += HanldeBackgroundFactoryError;
            fusionCache.Events.FactoryError += HanldeFactoryError;
            fusionCache.Events.FactorySyntheticTimeout += HandleFactorySyntheticTimeout;
            fusionCache.Events.FailSafeActivate += HandleFailSafeActivate;
        }

        private void HandleCacheHit(object sender, FusionCacheEntryHitEventArgs e)
        {
            if (e.IsStale)
            {
                CacheStaleHit();
            }
            else
            {
                CacheHit();
            }
        }
        private void HandleCacheMiss(object sender, FusionCacheEntryEventArgs e)
        {
            CacheMiss();
        }

        private void HandleCacheSet(object sender, FusionCacheEntryEventArgs e)
        {
            CacheSet();
        }

        private void HandleCacheRemoved(object sender, FusionCacheEntryEventArgs e)
        {
            CacheRemoved();
        }

        private void HandleCacheEviction(object sender, FusionCacheEntryEvictionEventArgs e)
        {
            switch (e.Reason)

            {

                case EvictionReason.Expired:

                    CacheExpired();
                    break;

                case EvictionReason.Capacity:

                    CacheCapacityExpired();
                    break;
            }
        }

        private void HandleBackgroundFactorySuccess(object sender, FusionCacheEntryEventArgs e)
        {
            CacheBackgroundRefreshSuccess();
        }

        private void HanldeBackgroundFactoryError(object sender, FusionCacheEntryEventArgs e)
        {
            CacheBackgroundRefreshError();
        }

        private void HanldeFactoryError(object sender, FusionCacheEntryEventArgs e)
        {
            CacheFactoryError();
        }

        private void HandleFactorySyntheticTimeout(object sender, FusionCacheEntryEventArgs e)
        {
            CacheFactorySyntheticTimeout();
        }

        private void HandleFailSafeActivate(object sender, FusionCacheEntryEventArgs e)
        {
            CacheFailSafeActivate();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _meter.Dispose();
        }
    }
}
