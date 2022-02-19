namespace ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core
{
    /// <summary>
    /// Default implementation of <see cref="ISemanticConventions"/> 
    /// </summary>
    public class SemanticConventions : ISemanticConventions
    {
        /// <summary>
        /// Create a new SemanticConventions instance
        /// </summary>
        public SemanticConventions(string measurementName, string gaugeName)
        {
            _measurementName = measurementName;
            _gaugeName = gaugeName;
        }

        /// <summary>
        /// Create a new SemanticConventions instance
        /// </summary>
        public SemanticConventions()
        {
        }

        /// <summary>
        /// Static accessor to creating a new instance of <see cref="SemanticConventions"/>
        /// </summary>
        /// <returns></returns>
        public static SemanticConventions Instance()
        {
            return new SemanticConventions();
        }

        private readonly string _measurementName = "Cache.Events";
        private readonly string _gaugeName = "Cache.Gauges";

        /// <inheritdoc />
        public string MeasurementName => _measurementName;

        /// <inheritdoc />
        public string GaugeName => _gaugeName;

        /// <inheritdoc />
        public string ValueFieldName => "value";

        /// <inheritdoc />
        public string ApplicationTagName => "application";

        /// <inheritdoc />
        public string ApplicationVersionTagName => "applicationVersion";

        /// <inheritdoc />
        public string CacheNameTagName => "cacheName";

        /// <inheritdoc />
        public string CacheEventTagName => "cacheEvent";

        /// <inheritdoc />
        public string CacheHitTagValue => "HIT";

        /// <inheritdoc />
        public string CacheMissTagValue => "MISS";

        /// <inheritdoc />
        public string CacheSetTagValue => "SET";

        /// <inheritdoc />
        public string CacheStaleHitTagValue => "STALE_HIT";

        /// <inheritdoc />
        public string CacheBackgroundRefreshedTagValue => "STALE_REFRESH";

        /// <inheritdoc />
        public string CacheBackgroundFailedRefreshedTagValue => "STALE_REFRESH_ERROR";

        /// <inheritdoc />
        public string CacheExpiredEvictTagValue => "EXPIRE";

        /// <inheritdoc />
        public string CacheCapacityEvictTagValue => "CAPACITY";

        /// <inheritdoc />
        public string CacheRemovedTagValue => "REMOVE";

        /// <inheritdoc />
        public string CacheItemCountTagValue => "ITEM_COUNT";

        /// <inheritdoc />
        public string CacheCacheFactoryErrorTagValue => "FACTORY_ERROR";

        /// <inheritdoc />
        public string CacheFactorySyntheticTimeoutTagValue => "TIMEOUT";

        /// <inheritdoc />
        public string CacheFailSafeActivateTagValue => "FAIL_SAFE";
    }
}
