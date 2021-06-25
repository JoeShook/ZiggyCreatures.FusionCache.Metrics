using System;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using ZiggyCreatures.Caching.Fusion.Metrics.Core;

namespace ZiggyCreatures.Caching.Fusion.EventCounters.Plugin
{
    /// <summary>
    /// Generic FusionCacheEventSource.  
    /// </summary>
    public sealed partial class FusionCacheEventSource : EventSource
    {
        private long _cacheHits;
        private long _cacheMisses;
        private long _cacheStaleHit;
        private long _cacheBackgroundRefreshed;
        private long _cacheBackgroundRefreshedError;
        private long _cacheCacheFactoryError;
        private long _cacheFactorySyntheticTimeout;
        private long _cacheFailSafeActivate;
        private long _cacheExpiredEvict;
        private long _cacheCapacityEvict;
        private long _cacheRemoved;
        private IncrementingPollingCounter? _cacheHitPollingCounter;
        private IncrementingPollingCounter? _cacheMissPollingCounter;
        private IncrementingPollingCounter? _cacheStaleHitPollingCounter;
        private IncrementingPollingCounter? _cacheBackgroundRefreshedPollingCounter;
        private IncrementingPollingCounter? _cacheBackgroundRefreshedErrorPollingCounter;
        private IncrementingPollingCounter? _cacheCacheFactoryErrorPollingCounter;
        private IncrementingPollingCounter? _cacheFactorySyntheticTimeoutPollingCounter;
        private IncrementingPollingCounter? _cacheFailSafeActivatePollingCounter;
        private IncrementingPollingCounter? _cacheExpiredEvictPollingCounter;
        private IncrementingPollingCounter? _cacheCapacityEvictPollingCounter;
        private IncrementingPollingCounter? _cacheRemovedPollingCounter;
        private PollingCounter? _cacheSizePollingCounter;

        private readonly TimeSpan _displayRateTimeScale;
        private readonly MemoryCache? _cache;
        private readonly ISemanticConventions _conventions;

        public FusionCacheEventSource(string cacheName, IMemoryCache? cache, ISemanticConventions? semanticConventions = null) : base(eventSourceName: cacheName)
        {
            _conventions = semanticConventions ?? new SemanticConventions();
            _displayRateTimeScale = TimeSpan.FromSeconds(5);

            if (cache is MemoryCache memoryCache)
            {
                _cache = memoryCache;
            }

            CreateCounters();
        }

        private void CreateCounters() {
            _cacheHitPollingCounter = new IncrementingPollingCounter(
                _conventions.CacheHitTagValue,
                this,
                () => Volatile.Read(ref _cacheHits))
            {
                DisplayName = "Cache Hits",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheHitPollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);


            _cacheMissPollingCounter = new IncrementingPollingCounter(
                _conventions.CacheMissTagValue,
                this,
                () => Volatile.Read(ref _cacheMisses))
            {
                DisplayName = "Cache Misses",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheMissPollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);


            _cacheStaleHitPollingCounter = new IncrementingPollingCounter(
                _conventions.CacheStaleHitTagValue,
                this,
                () => Volatile.Read(ref _cacheStaleHit))
            {
                DisplayName = "Cache Stale Hit",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheStaleHitPollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);


            _cacheBackgroundRefreshedPollingCounter = new IncrementingPollingCounter(
                _conventions.CacheBackgroundRefreshedTagValue,
                this,
                () => Volatile.Read(ref _cacheBackgroundRefreshed))
            {
                DisplayName = "Cache Background Refresh",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheBackgroundRefreshedPollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);

            
            _cacheBackgroundRefreshedErrorPollingCounter = new IncrementingPollingCounter(
                _conventions.CacheBackgroundFailedRefreshedTagValue,
                this,
                () => Volatile.Read(ref _cacheBackgroundRefreshedError))
            {
                DisplayName = "Cache Background Refresh Error",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheBackgroundRefreshedErrorPollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);


            _cacheCacheFactoryErrorPollingCounter = new IncrementingPollingCounter(
                _conventions.CacheCacheFactoryErrorTagValue,
                this,
                () => Volatile.Read(ref _cacheCacheFactoryError))
            {
                DisplayName = "Cache Factory Error",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheCacheFactoryErrorPollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);


            _cacheFactorySyntheticTimeoutPollingCounter = new IncrementingPollingCounter(
                _conventions.CacheFactorySyntheticTimeoutTagValue,
                this,
                () => Volatile.Read(ref _cacheFactorySyntheticTimeout))
            {
                DisplayName = "Cache Factory Synthetic Timeout",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheFactorySyntheticTimeoutPollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);


            _cacheFailSafeActivatePollingCounter = new IncrementingPollingCounter(
                _conventions.CacheFailSafeActivateTagValue,
                this,
                () => Volatile.Read(ref _cacheFailSafeActivate))
            {
                DisplayName = "Cache Fail-Safe activation",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheFailSafeActivatePollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);

            
            _cacheExpiredEvictPollingCounter = new IncrementingPollingCounter(
                _conventions.CacheExpiredEvictTagValue,
                this,
                () => Volatile.Read(ref _cacheExpiredEvict))
            {
                DisplayName = "Cache Expired Eviction",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheExpiredEvictPollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);


            _cacheCapacityEvictPollingCounter = new IncrementingPollingCounter(
                _conventions.CacheCapacityEvictTagValue,
                this,
                () => Volatile.Read(ref _cacheCapacityEvict))
            {
                DisplayName = "Cache Capacity Eviction",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheCapacityEvictPollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);


            _cacheRemovedPollingCounter = new IncrementingPollingCounter(
                _conventions.CacheRemovedTagValue,
                this,
                () => Volatile.Read(ref _cacheRemoved))
            {
                DisplayName = "Cache Removed",
                DisplayRateTimeScale = _displayRateTimeScale
            };
            _cacheRemovedPollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);

            

            _cacheSizePollingCounter = new PollingCounter(
                _conventions.CacheItemCountTagValue,
                this,
                () => _cache?.Count ?? 0)
            {
                DisplayName = "Cache Size",
            };
            _cacheSizePollingCounter.AddMetadata(_conventions.CacheNameTagName, Name);
        }

        #region IFusionMetrics

        /// <summary>Cache item hit counter.</summary>
        [NonEvent]
        public void CacheHit()
        {
            Interlocked.Increment(ref _cacheHits);
        }

        /// <summary>
        /// Cache item miss counter.  When a cache item is written to local cache
        /// </summary>
        [NonEvent]
        public void CacheMiss()
        {
            Interlocked.Increment(ref _cacheMisses);
        }

        /// <summary>
        /// Cache item stale hit counter.  Cache item failed to complete within soft timeout period.
        /// </summary>
        [NonEvent]
        public void CacheStaleHit()
        {
            Interlocked.Increment(ref _cacheStaleHit);
        }

        /// <summary>Cache item refresh in background.</summary>
        [NonEvent]
        public void CacheBackgroundRefreshSuccess()
        {
            Interlocked.Increment(ref _cacheBackgroundRefreshed);
        }

        /// <summary>Cache item refresh in background failed.</summary>
        [NonEvent]
        public void CacheBackgroundRefreshError()
        {
            Interlocked.Increment(ref _cacheBackgroundRefreshedError);
        }

        /// <summary>Generic cache factory error.</summary>
        [NonEvent]
        public void CacheFactoryError()
        {
            Interlocked.Increment(ref _cacheCacheFactoryError);
        }

        /// <summary>Cache factory synthetic timeout</summary>
        [NonEvent]
        public void CacheFactorySyntheticTimeout()
        {
            Interlocked.Increment(ref _cacheFactorySyntheticTimeout);
        }

        /// <summary>The event for a fail-safe activation.</summary>
        [NonEvent]
        public void CacheFailSafeActivate()
        {
            Interlocked.Increment(ref _cacheFailSafeActivate);
        }

        /// <summary>Cache item expired</summary>
        [NonEvent]
        public void CacheExpired()
        {
            Interlocked.Increment(ref _cacheExpiredEvict);
        }

        /// <summary>Cache item removed due to capacity</summary>
        [NonEvent]
        public void CacheCapacityExpired()
        {
            Interlocked.Increment(ref _cacheCapacityEvict);
        }

        /// <summary>Cache item explicitly removed by user code</summary>
        [NonEvent]
        public void CacheRemoved()
        {
            Interlocked.Increment(ref _cacheRemoved);
        }

        
       
        #endregion

        public void Wireup(IFusionCache fusionCache, FusionCacheOptions? fusionCacheOptions = null)
        {
            fusionCache.Events.Hit += (s, e) =>
            {
                if (e.IsStale)
                {
                    CacheStaleHit();
                }
                else
                {
                    CacheHit();
                }
            };

            fusionCache.Events.Miss += (s, e) => CacheMiss();
            fusionCache.Events.Remove += (s, e) => CacheRemoved();
           

            fusionCache.Events.Memory.Eviction += (sender, e) =>
            {
                // If you need it...
                // var cache = (IFusionCache)sender;

                switch (e.Reason)

                {

                    case EvictionReason.Expired:

                        CacheExpired();
                        break;

                    case EvictionReason.Capacity:

                        CacheCapacityExpired();
                        break;
                }
            };

            fusionCache.Events.BackgroundFactorySuccess += (s, e) => CacheBackgroundRefreshSuccess();
            fusionCache.Events.BackgroundFactoryError += (s, e) => CacheBackgroundRefreshError();
            fusionCache.Events.FactoryError += (s, e) => CacheFactoryError();
            fusionCache.Events.FactorySyntheticTimeout += (s, e) => CacheFactorySyntheticTimeout();
            fusionCache.Events.FailSafeActivate += (s, e) => CacheFailSafeActivate();
        }
    }
}
