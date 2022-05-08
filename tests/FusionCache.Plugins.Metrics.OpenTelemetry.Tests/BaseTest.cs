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
    protected static MetricPoint GetMetricPoint(List<Metric> exportedItems, string eventName, string? meterName = null)
    {
        List<Metric> metrics = null;

        if (meterName != null)
        {
            metrics = exportedItems.Where(i => i.MeterName == meterName).ToList();
        }
        else
        {
            metrics = exportedItems;
        }

        if (!metrics.Any())
        {
            return new MetricPoint();
        }

        var metricPoints = new List<MetricPoint>();

        foreach (var result in metrics)
        {
            foreach (ref readonly var mp in result.GetMetricPoints())
            {
                metricPoints.Add(mp);
            }
        }

       
        var metricPoint = metricPoints.SingleOrDefault(p =>
        {
            foreach (var argTag in p.Tags)
            {
                if (argTag.Value == eventName)
                {
                    return true;
                }
            }
            return false;
        });

        return metricPoint;
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