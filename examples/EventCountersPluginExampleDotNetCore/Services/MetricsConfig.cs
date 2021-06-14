namespace JoeShook.FusionCache.EventCounters.Plugin
{
    public class MetricsConfig
    {
        public MetricsConfig()
        {

        }

        public string ApplicationName { get; set; }
        public string ApplicationVersion { get; set; }
        public string Prefix { get; set; }
        public string MeasurementName { get; set; }
    }
}