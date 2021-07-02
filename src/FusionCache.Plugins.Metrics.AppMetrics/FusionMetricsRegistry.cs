using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;

namespace ZiggyCreatures.Caching.Fusion.Plugins.Metrics.AppMetrics
{
    /// <summary>
    /// Define FusionCache metrics
    /// </summary>
    public class FusionMetricsRegistry
    {
        // In time series database the MetricsOptions.DefaultContextLabel will be prefixed to the MeasurementName
        private static string MeasurementName = "cache-events";
        private static string GaugeName = "cache-gauges";

        /// <summary>
        /// Cache hit counter
        /// </summary>
        public static CounterOptions CacheHitCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheHitTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache miss counter.  When a cache is not found in local cache
        /// </summary>
        public static CounterOptions CacheMissCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheMissTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache set counter.  When a cache is written to local cache
        /// </summary>
        public static CounterOptions CacheSetCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheSetTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache stale hit counter.  Cache failed to complete within soft timeout period. 
        /// </summary>
        public static CounterOptions CacheStaleHitCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheStaleHitTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache refresh in background.
        /// </summary>
        public static CounterOptions CacheBackgroundRefreshed(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheBackgroundRefreshedTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache refresh in background failed.
        /// </summary>
        public static CounterOptions CacheBackgroundRefreshError(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheBackgroundFailedRefreshedTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Generic cache factory error.
        /// </summary>
        public static CounterOptions CacheFactoryError(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheCacheFactoryErrorTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache factory synthetic timeout
        /// </summary>
        public static CounterOptions CacheFactorySyntheticTimeout(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheFactorySyntheticTimeoutTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache Fail-Safe activation
        /// </summary>
        public static CounterOptions CacheFailSafeActivate(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheFailSafeActivateTagValue),
            ResetOnReporting = true
        };
        
        /// <summary>
        /// Cache expired counter
        /// </summary>
        public static CounterOptions CacheExpireCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheExpiredEvictTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache eviction from capacity limit
        /// </summary>
        public static CounterOptions CacheCapacityCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheCapacityEvictTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache item removed counter
        /// </summary>
        public static CounterOptions CacheRemoveCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Name = MeasurementName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheRemovedTagValue),
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache item count.  Tracked by add and remove counters. 
        /// </summary>
        public static GaugeOptions CacheItemGauge(ISemanticConventions conventions) => new GaugeOptions
        {
            Name = GaugeName,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheItemCountTagValue),
            ResetOnReporting = true,
        };
    }
}
