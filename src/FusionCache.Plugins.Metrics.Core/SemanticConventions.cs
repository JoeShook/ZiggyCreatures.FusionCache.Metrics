namespace ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core
{
    public class SemanticConventions : ISemanticConventions
    {
        public static SemanticConventions Instance()
        {
            return new SemanticConventions();
        }

        public string ValueFieldName => "value";

        public string ApplicationTagName => "application";

        public string ApplicationVersionTagName => "applicationVersion";

        public string CacheNameTagName => "cacheName";

        public string CacheEventTagName => "cacheEvent";

        public string CacheHitTagValue => "HIT";

        public string CacheMissTagValue => "MISS";

        public string CacheSetTagValue => "SET";

        public string CacheStaleHitTagValue => "STALE_HIT";

        public string CacheBackgroundRefreshedTagValue => "STALE_REFRESH";
        public string CacheBackgroundFailedRefreshedTagValue => "STALE_REFRESH_ERROR";
        public string CacheExpiredEvictTagValue => "EXPIRE";
        public string CacheCapacityEvictTagValue => "CAPACITY";
        public string CacheRemovedTagValue => "REMOVE";
        public string CacheItemCountTagValue => "ITEM_COUNT";
        public string CacheCacheFactoryErrorTagValue => "FACTORY_ERROR";
        public string CacheFactorySyntheticTimeoutTagValue => "TIMEOUT";
        public string CacheFailSafeActivateTagValue => "FAIL_SAFE";
    }
}
