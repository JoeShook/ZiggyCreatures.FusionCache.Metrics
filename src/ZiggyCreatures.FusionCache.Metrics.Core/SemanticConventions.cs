namespace ZiggyCreatures.Caching.Fusion.Metrics.Core
{
    public class SemanticConventions : ISemanticConventions
    {
        public string ValueFieldName => "value";

        public string ApplicationTagName => "application";

        public string ApplicationVersionTagName => "applicationVersion";

        public string CacheNameTagName => "cacheName";

        public string CacheEventTagName => "cacheEvent";

        public string CacheHitTagValue => "HIT";

        public string CacheMissTagValue => "MISS";

        public string CacheStaleHitTagValue => "STALE_HIT";

        public string CacheBackgroundRefreshedTagValue => "STALE_REFRESH";
        public string CacheExpiredEvictTagValue => "EXPIRE";
        public string CacheCapacityEvictTagValue => "CAPACITY";
        public string CacheRemovedTagValue => "REMOVE";
        public string CacheItemCountTagValue => "ITEM_COUNT";
    }
}
