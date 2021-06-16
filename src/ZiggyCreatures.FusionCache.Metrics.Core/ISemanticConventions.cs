namespace ZiggyCreatures.Caching.Fusion.Metrics.Core
{
    /// <summary>
    /// Implement this interface to change cache measurement semantics
    /// </summary>
    public interface ISemanticConventions
    {
        /// <summary>
        /// Cache metric field value name
        /// </summary>
        string ValueFieldName { get; }

        string ApplicationTagName { get; }

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
        /// Cache item stale hit tag.  Cache was pulled from Fail-Safe cache
        /// </summary>
        string CacheStaleHitTagValue { get; }

        /// <summary>
        /// Cache item background refreshed tag.  Request was not returned within soft timeout period and the Fail-Safe cache was returned.
        /// In the background the cached was refreshed.
        /// </summary>
        string CacheBackgroundRefreshedTagValue { get; }

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
    }
}