using System.Reflection;

namespace ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core
{
    public class MetricsConfig
    {
        public string ApplicationName { get; set; } = Assembly.GetEntryAssembly().GetName().Name;
        public string ApplicationVersion { get; set; } = Assembly.GetEntryAssembly().ImageRuntimeVersion;
        public string Prefix { get; set; } = "appMetrics";
        public string MeasurementName { get; set; } = "cache-events";
    }
}