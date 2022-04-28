using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTelemetry.Metrics;
using Xunit;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;

namespace FusionCache.Plugins.Metrics.OpenTelemetry.Tests;

public class BaseTest
{
    protected static ISemanticConventions SemanticConventions = new SemanticConventions();
    protected const int MaxTimeToAllowForFlush = 10000;
    protected static MetricPoint GetMetricPoint(List<Metric> exportedItems, string cacheTag)
    {
        var metric = exportedItems.SingleOrDefault(i => i.Name == cacheTag);
        if (metric == null)
        {
            return new MetricPoint();
        }

        var metricPoints = new List<MetricPoint>();
        foreach (ref readonly var mp in metric.GetMetricPoints())
        {
            metricPoints.Add(mp);
        }

        Assert.Single(metricPoints);
        var metricPoint1 = metricPoints[0];
        return metricPoint1;
    }
        
    internal class Utils
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCurrentMethodName()
        {
            var method = new StackFrame(1).GetMethod();
            return $"{method?.DeclaringType?.FullName}.{method?.Name}";
        }
    }
}