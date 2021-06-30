using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Filters;
using App.Metrics.Formatters;
using App.Metrics.Formatters.Ascii;
using App.Metrics.Reporting;
using Xunit.Abstractions;

namespace FusionCache.AppMertrics.Plugin.Tests
{
    public class TestReporter : IReportMetrics
    {
        private readonly Queue<Tuple<string, Int64>> events;

        private readonly IMetricsOutputFormatter _defaultMetricsOutputFormatter = new MetricsTextOutputFormatter();
        private readonly ITestOutputHelper _testOutputHelper;

        public TestReporter(ITestOutputHelper testOutputHelper)
        {
            Formatter = _defaultMetricsOutputFormatter;
            _testOutputHelper = testOutputHelper;
            this.events = new Queue<Tuple<string, Int64>>();
            this.FlushInterval = TimeSpan.FromMilliseconds(900);
        }

        public IFilterMetrics Filter { get; set; }

        public TimeSpan FlushInterval { get; set; }

        public IMetricsOutputFormatter Formatter { get; set; }

        public Task<bool> FlushAsync(MetricsDataValueSource metricsData, CancellationToken cancellationToken = default)
        {
            // _testOutputHelper.WriteLine("Metrics Report");
            // _testOutputHelper.WriteLine("-------------------------------------------");
            //
            // using (var stream = new MemoryStream())
            // {
            //     await Formatter.WriteAsync(stream, metricsData, cancellationToken);
            //
            //     var output = Encoding.UTF8.GetString(stream.ToArray());
            //
            //     events.Enqueue(metricsData);
            //
            //     _testOutputHelper.WriteLine(output);
            // }

            foreach (var tuple in GetMetric(metricsData))
            {
                events.Enqueue(tuple);
            }


            return Task.FromResult(true);

            // return true;
        }

        /// <summary>Gets the events that have been written.</summary>
        public IEnumerable<Tuple<string, Int64>> Messages
        {
            get
            {
                while (this.events.Count != 0)
                {
                    yield return this.events.Dequeue();
                }
            }
        }

        private IEnumerable<Tuple<string, Int64>> GetMetric(MetricsDataValueSource metricsDataValueSourceList)
        {
            var data = metricsDataValueSourceList;
            foreach (var metricsContextValueSource in data.Contexts)
            {
                foreach (var counterValueSource in metricsContextValueSource.Counters)
                {
                    if(counterValueSource.Tags.Values.Any()) {
                        yield return new Tuple<string, long>($"{counterValueSource.Tags.Values[0]}{metricsDataValueSourceList.Timestamp.ToLongTimeString()}",
                            Convert.ToInt64(counterValueSource.Value.Count));
                    }
                }
            }
        }

    }
}
