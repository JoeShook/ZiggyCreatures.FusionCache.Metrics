#region (c) 2022 Joseph Shook. All rights reserved.
// /*
//  Authors:
//     Joseph Shook   Joseph.Shook@Surescripts.com
// 
//  See LICENSE in the project root for license information.
// */
#endregion

using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTelemetry.Metrics;
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

        foreach (var result in metrics.Where(i => i.Name == eventName))
        {
            foreach (ref readonly var mp in result.GetMetricPoints())
            {
                metricPoints.Add(mp);
            }
        }
        
        return metricPoints.SingleOrDefault();
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
