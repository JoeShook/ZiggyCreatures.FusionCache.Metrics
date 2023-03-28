#region (c) 2021 Joseph Shook. All rights reserved.
// /*
//  Authors:
//     Joseph Shook   Joseph.Shook@Surescripts.com
// 
//  See LICENSE in the project root for license information.
// */
#endregion

using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;

namespace ZiggyCreatures.Caching.Fusion.Plugins.Metrics.AppMetrics
{
    /// <summary>
    /// Define FusionCache metrics
    /// In time series database the <see cref="MetricValueOptionsBase.Context"/> will be prefixed to the
    /// <see cref="ISemanticConventions.MeasurementName"/> or see cref="ISemanticConventions.GaugeName"/>
    /// </summary>
    public class FusionMetricsRegistry
    {
        /// <summary>
        /// Cache hit counter
        /// </summary>
        public static CounterOptions CacheHitCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheHitTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheHitTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache miss counter.  When a cache is not found in local cache
        /// </summary>
        public static CounterOptions CacheMissCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheMissTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheMissTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache set counter.  When a cache is written to local cache
        /// </summary>
        public static CounterOptions CacheSetCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheSetTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheSetTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache stale hit counter.  Cache failed to complete within soft timeout period. 
        /// </summary>
        public static CounterOptions CacheStaleHitCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheStaleHitTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheStaleHitTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache refresh in background.
        /// </summary>
        public static CounterOptions CacheBackgroundRefreshed(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheBackgroundRefreshedTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheBackgroundRefreshedTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache refresh in background failed.
        /// </summary>
        public static CounterOptions CacheBackgroundRefreshError(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheBackgroundFailedRefreshedTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheBackgroundFailedRefreshedTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Generic cache factory error.
        /// </summary>
        public static CounterOptions CacheFactoryError(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheCacheFactoryErrorTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheCacheFactoryErrorTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache factory synthetic timeout
        /// </summary>
        public static CounterOptions CacheFactorySyntheticTimeout(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheFactorySyntheticTimeoutTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheFactorySyntheticTimeoutTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache Fail-Safe activation
        /// </summary>
        public static CounterOptions CacheFailSafeActivate(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheFailSafeActivateTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheFailSafeActivateTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache expired counter
        /// </summary>
        public static CounterOptions CacheExpireCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheExpiredEvictTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheExpiredEvictTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache eviction from capacity limit
        /// </summary>
        public static CounterOptions CacheCapacityCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheCapacityEvictTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheCapacityEvictTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache item removed counter
        /// </summary>
        public static CounterOptions CacheRemoveCounter(ISemanticConventions conventions) => new CounterOptions
        {
            Context = conventions.MeasurementName,
            Name = conventions.CacheRemovedTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheRemovedTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = true
        };

        /// <summary>
        /// Cache item count.  Tracked by add and remove counters. 
        /// </summary>
        public static GaugeOptions CacheItemGauge(ISemanticConventions conventions) => new GaugeOptions
        {
            Context = conventions.GaugeName,
            Name = conventions.CacheItemCountTagValue,
            Tags = new MetricTags(conventions.CacheEventTagName, conventions.CacheItemCountTagValue),
            MeasurementUnit = Unit.Events,
            ResetOnReporting = false,
        };
    }
}