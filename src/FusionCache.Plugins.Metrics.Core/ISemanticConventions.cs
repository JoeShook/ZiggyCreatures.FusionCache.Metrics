﻿#region (c) 2021 Joseph Shook. All rights reserved.
// /*
//  Authors:
//     Joseph Shook   Joseph.Shook@Surescripts.com
// 
//  See LICENSE in the project root for license information.
// */
#endregion

namespace ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core
{
    /// <summary>
    /// Implement this interface to change cache measurement semantics
    /// </summary>
    public interface ISemanticConventions
    {
        /// <summary>
        /// Measurement name property for standard counter style metrics.
        /// </summary>
        string MeasurementName { get; }

        /// <summary>
        /// Measurement name property for gauge style metrics
        /// </summary>
        string GaugeName { get; }

        /// <summary>
        /// Cache metric field value name
        /// </summary>
        string ValueFieldName { get; }

        /// <summary>
        /// Tag metric with application name
        /// </summary>
        string ApplicationTagName { get; }

        /// <summary>
        /// Tag metric with application version
        /// </summary>
        string ApplicationVersionTagName { get; }

        /// <summary>
        /// Cache name tag.  
        /// </summary>
        string CacheNameTagName { get; }

        /// <summary>
        /// Cache event tag name.  Match with *TagValues below
        /// </summary>
        string CacheEventTagName { get; }
        /// <summary>
        /// Cache item hit tag
        /// </summary>
        string CacheHitTagValue { get; }

        /// <summary>
        /// Cache item miss tag
        /// </summary>
        string CacheMissTagValue { get; }

        /// <summary>
        /// Cache item set tag.
        /// </summary>
        string CacheSetTagValue { get; }
        
        /// <summary>
        /// Cache item stale hit tag.  Cache was pulled from Fail-Safe cache
        /// </summary>
        string CacheStaleHitTagValue { get; }

        /// <summary>
        /// Cache item background refreshed tag.  Request was not returned within soft timeout period and the Fail-Safe cache was returned.
        /// In the background the cached was refreshed.
        /// </summary>
        string CacheBackgroundRefreshedTagValue { get; }

        /// <summary>
        /// Cache item background failed refreshed tag.  Request was not returned within soft timeout period and the Fail-Safe cache was returned.
        /// In the background the cached refresh also timed out.
        /// </summary>
        string CacheBackgroundFailedRefreshedTagValue { get; }

        /// <summary>
        /// Cache item expired tag.
        /// </summary>
        string CacheExpiredEvictTagValue { get; }

        /// <summary>
        /// Cache item capacity evicted tag.  
        /// </summary>
        string CacheCapacityEvictTagValue { get; }

        /// <summary>
        /// Cache item removed tag
        /// </summary>
        string CacheRemovedTagValue { get; }

        /// <summary>
        /// Cache item count tag
        /// </summary>
        string CacheItemCountTagValue { get; }

        /// <summary>
        /// Cache factory threw and error.
        /// </summary>
        string CacheCacheFactoryErrorTagValue { get; }

        /// <summary>
        /// Cache factory timed out according to FusionCache soft-timeout settings.
        /// </summary>
        string CacheFactorySyntheticTimeoutTagValue { get; }

        /// <summary>
        /// Cache returned a stale cache value.
        /// </summary>
        string CacheFailSafeActivateTagValue { get; }
    }
}