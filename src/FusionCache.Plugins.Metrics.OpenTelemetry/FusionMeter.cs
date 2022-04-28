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
        private readonly string _name;

        public FusionMeter(string cacheName, IMemoryCache? cache, ISemanticConventions? semanticConventions = null)
        {
            _name = cacheName;
            _conventions = semanticConventions ?? new SemanticConventions();

            if (cache is MemoryCache memoryCache)
            {
                _cache = memoryCache;
            }

            _meter = new Meter(cacheName);
            CreateCounters();
        }

        private void CreateCounters()
        {
            _cacheHitCounter = _meter.CreateCounter<int>(_conventions.CacheHitTagValue, description: "Cache Hits");
            _cacheMissCounter = _meter.CreateCounter<int>(_conventions.CacheMissTagValue, description: "Cache Misses");
            _cacheSetCounter = _meter.CreateCounter<int>(_conventions.CacheSetTagValue, description: "Cache Sets");
            _cacheStaleHitCounter = _meter.CreateCounter<int>(_conventions.CacheStaleHitTagValue, description: "Cache Stale Hit");
            _cacheBackgroundRefreshedCounter = _meter.CreateCounter<int>(_conventions.CacheBackgroundRefreshedTagValue, description: "Cache Background Refresh");
            _cacheBackgroundRefreshedErrorCounter = _meter.CreateCounter<int>(_conventions.CacheBackgroundFailedRefreshedTagValue, description: "Cache Background Refresh Error");
            _cacheCacheFactoryErrorCounter = _meter.CreateCounter<int>(_conventions.CacheCacheFactoryErrorTagValue, description: "Cache Factory Error");
            _cacheFactorySyntheticTimeoutCounter = _meter.CreateCounter<int>(_conventions.CacheFactorySyntheticTimeoutTagValue, description: "Cache Factory Synthetic Timeout");
            _cacheFailSafeActivateCounter = _meter.CreateCounter<int>(_conventions.CacheFailSafeActivateTagValue, description: "Cache Fail-Safe activation");
            _cacheExpiredEvictCounter = _meter.CreateCounter<int>(_conventions.CacheExpiredEvictTagValue, description: "Cache Expired Eviction");
            _cacheCapacityEvictCounter = _meter.CreateCounter<int>(_conventions.CacheCapacityEvictTagValue, description: "Cache Capacity Eviction");
            _cacheRemovedCounter = _meter.CreateCounter<int>(_conventions.CacheRemovedTagValue, description: "Cache Removed");
            
            _meter.CreateObservableGauge<long>(
                _conventions.CacheItemCountTagValue, 
                () => _cache?.Count ?? 0,
                description: "Cache Size");
        }

        #region IFusionMetrics

        /// <summary>Cache item hit counter.</summary>
        public void CacheHit()
        {
            _cacheHitCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>
        /// Cache item miss counter.  When a cache item is not found in the local cache
        /// </summary>
        public void CacheMiss()
        {
            _cacheMissCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>
        /// Cache item set counter.  When a cache item is written to local cache
        /// </summary>
        public void CacheSet()
        {
            _cacheSetCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>
        /// Cache item stale hit counter.  Cache item failed to complete within soft timeout period.
        /// </summary>
        public void CacheStaleHit()
        {
            _cacheStaleHitCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>Cache item refresh in background.</summary>
        public void CacheBackgroundRefreshSuccess()
        {
            _cacheBackgroundRefreshedCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>Cache item refresh in background failed.</summary>
        public void CacheBackgroundRefreshError()
        {
            _cacheBackgroundRefreshedErrorCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>Generic cache factory error.</summary>
        public void CacheFactoryError()
        {
            _cacheCacheFactoryErrorCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>Cache factory synthetic timeout</summary>
        public void CacheFactorySyntheticTimeout()
        {
            _cacheFactorySyntheticTimeoutCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>The event for a fail-safe activation.</summary>
        public void CacheFailSafeActivate()
        {
            _cacheFailSafeActivateCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>Cache item expired</summary>
        public void CacheExpired()
        {
            _cacheExpiredEvictCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>Cache item removed due to capacity</summary>
        public void CacheCapacityExpired()
        {
            _cacheCapacityEvictCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
        }

        /// <summary>Cache item explicitly removed by user code</summary>
        public void CacheRemoved()
        {
            _cacheRemovedCounter?.Add(1, new KeyValuePair<string, object?>(_conventions.CacheNameTagName, _name));
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
