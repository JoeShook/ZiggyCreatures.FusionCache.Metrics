using System;
using App.Metrics;
using Microsoft.Extensions.Caching.Memory;
using ZiggyCreatures.Caching.Fusion.Metrics.Core;

namespace ZiggyCreatures.Caching.Fusion.AppMetrics.Plugins
{
    /// <summary>
    /// Implementation of AppMetrics for caching metrics. 
    /// See https://github.com/AppMetrics/AppMetrics
    /// </summary>
    public class AppMetricsProvider
    {
        private IMetrics _metrics;
        private MetricTags _cacheNameMetricTag;
        private readonly MemoryCache? _cache;
        private readonly ISemanticConventions _semanticConventions;

        /// <summary>
        /// Instantiate AppMetricsProvider
        /// </summary>
        /// <param name="metrics">App.Metrics IMetric instance</param>
        /// <param name="cacheName">Used to capture metrics tagged by cacheName</param>
        /// <param name="cache">Pass the <see cref="MemoryCache"/> instance to enable cache item count measurements</param>
        /// /// <param name="semanticConventions">Semantic naming conventions</param>
        public AppMetricsProvider(string cacheName, IMetrics metrics, IMemoryCache? cache = null, ISemanticConventions? semanticConventions = null)
        {
            if (string.IsNullOrWhiteSpace(cacheName))
            {
                throw new ArgumentNullException(nameof(cacheName));
            }

            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _semanticConventions = semanticConventions ?? new SemanticConventions();
            _cacheNameMetricTag = new MetricTags(_semanticConventions.CacheNameTagName, cacheName);

            if (cache is MemoryCache memoryCache)
            {
                _cache = memoryCache;
            }
        }


        /// <summary>
        /// Cache item hit counter.
        /// </summary>
        public void CacheHit()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheHitCounter(_semanticConventions), _cacheNameMetricTag);

            if (_cache != null)
            {
                _metrics.Measure.Gauge.SetValue(FusionMetricsRegistry.CacheItemCounter(_semanticConventions), () => _cache.Count);
            }
        }

        /// <summary>
        /// Cache item miss counter.  When a cache item is written to local cache
        /// </summary>
        public void CacheMiss()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheMissCounter(_semanticConventions), _cacheNameMetricTag);

            if (_cache != null)
            {
                _metrics.Measure.Gauge.SetValue(FusionMetricsRegistry.CacheItemCounter(_semanticConventions), () => _cache.Count);
            }
        }

        /// <summary>
        /// Cache item stale hit counter.  Cache item failed to complete within soft timeout period. 
        /// </summary>
        public void CacheStaleHit()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheStaleHitCounter(_semanticConventions), _cacheNameMetricTag);
        }

        /// <summary>
        /// Cache item refresh in background.
        /// </summary>
        public void CacheBackgroundRefresh()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheBackgroundRefreshed(_semanticConventions), _cacheNameMetricTag);
        }

        /// <summary>
        /// Cache item expired
        /// </summary>
        public void CacheExpired()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheExpireCounter(_semanticConventions), _cacheNameMetricTag);
        }

        /// <summary>
        /// Cache item removed due to capacity
        /// </summary>
        public void CacheCapacityExpired()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheCapacityCounter(_semanticConventions), _cacheNameMetricTag);
        }

        /// <summary>
        /// Cache item explicitly removed by user code
        /// </summary>
        public void CacheRemoved()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheRemoveCounter(_semanticConventions), _cacheNameMetricTag);
        }
    


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

            // fusionCache.Events.BackgroundFactoryError 
            // fusionCache.Events.FactoryError 
            // fusionCache.Events.FactorySyntheticTimeout 
            // fusionCache.Events.FailSafeActivate  

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

            //
            // Background refresh vs sets?  Not sure I care.  Maybe 
            //
            fusionCache.Events.BackgroundFactorySuccess += (s, e) => CacheBackgroundRefresh();
        }
    }
}
