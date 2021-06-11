﻿using App.Metrics;
using Microsoft.Extensions.Caching.Memory;
using ZiggyCreatures.Caching.Fusion;

namespace JoeShook.FusionCache.AppMetrics.Plugins
{
    /// <summary>
    /// AppMetrics implementation of IFusionMetrics provider
    /// </summary>
    public class AppMetricsProvider // : IFusionMetrics
    {
        private IMetrics _metrics;
        private MetricTags _cacheNameMetricTag;
        private string _cacheName;
        
        /// <summary>
        /// Instantiate AppMetricsProvider
        /// </summary>
        /// <param name="metrics">App.Metrics IMetric instance</param>
        /// <param name="cacheName">Used to capture metrics tagged by cacheName</param>
        public AppMetricsProvider(string cacheName, IMetrics metrics)
        {
            _metrics = metrics;
            _cacheName = cacheName;
            _cacheNameMetricTag = new MetricTags("cacheName", cacheName);
        }

        /// <inheritdoc/>
        public string CacheName => _cacheName;

        /// <inheritdoc/>
        public void CacheHit()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheHitCounter, _cacheNameMetricTag);
        }

        /// <inheritdoc/>
        public void CacheMiss()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheMissCounter, _cacheNameMetricTag);
        }

        /// <inheritdoc/>
        public void CacheStaleHit()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheStaleHitCounter, _cacheNameMetricTag);
        }

        /// <inheritdoc/>
        public void CacheBackgroundRefresh()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheBackgroundRefreshed, _cacheNameMetricTag);
        }

        /// <inheritdoc/>
        public void CacheExpired()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheExpireCounter, _cacheNameMetricTag);
        }

        /// <inheritdoc/>
        public void CacheCapacityExpired()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheCapacityCounter, _cacheNameMetricTag);
        }

        /// <inheritdoc/>
        public void CacheRemoved()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheRemoveCounter, _cacheNameMetricTag);
        }

        /// <inheritdoc/>
        public void CacheReplaced()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheReplaceCounter, _cacheNameMetricTag);
        }

        /// <inheritdoc/>
        public void CacheEvicted()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheEvictCounter, _cacheNameMetricTag);
        }

        /// <inheritdoc/>
        public void CacheCountIncrement()
        {
            _metrics.Measure.Counter.Increment(FusionMetricsRegistry.CacheItemCounter, _cacheNameMetricTag);
        }

        /// <inheritdoc/>
        public void CacheCountDecrement()
        {
            _metrics.Measure.Counter.Decrement(FusionMetricsRegistry.CacheItemCounter, _cacheNameMetricTag);
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
