namespace ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core
{
    public class SemanticConventions : ISemanticConventions
    {
        public SemanticConventions(string measurementName, string GaugeName)
        {
            _measurementName = measurementName;
            _gaugeName = GaugeName;
        }

        public SemanticConventions()
        {
        }

        public static SemanticConventions Instance()
        {
            return new SemanticConventions();
        }

        private readonly string _measurementName = "Cache.Events";
        private readonly string _gaugeName = "Cache.Gauges";

        public string MeasurementName => _measurementName;

        public string GaugeName => _gaugeName;

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
