using System.Reflection;

namespace ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core
{
    /// <summary>
    /// Helper model for accessing ApplicationName, ApplicationVersion, metric Prefix and metrics default MeasurementName.
    /// </summary>
    public class MetricsConfig
    {
        /// <summary>
        /// Set ApplicationName or let it discover it from the entry assembly.
        /// </summary>
        public string ApplicationName { get; set; } = Assembly.GetEntryAssembly()?.GetName().Name;

        /// <summary>
        /// Set ApplicationVersion or let it discover it from the entry assembly's ImageRuntimeVersion.
        /// </summary>
        public string ApplicationVersion { get; set; } = Assembly.GetEntryAssembly()?.ImageRuntimeVersion;
        
        /// <summary>
        /// Prefix for all metrics written
        /// </summary>
        public string Prefix { get; set; } = "appMetrics";

        /// <summary>
        /// Consistent MeasurementName for all cache metrics written.
        /// </summary>
        public string MeasurementName { get; set; } = "Cache-Events";
    }
}