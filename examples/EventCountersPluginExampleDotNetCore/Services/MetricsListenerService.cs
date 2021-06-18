using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventCountersPluginExampleDotNetCore.Services;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Hosting;
using ZiggyCreatures.Caching.Fusion.Metrics.Core;

namespace JoeShook.FusionCache.EventCounters.Plugin
{
    public class ConsoleMetricsListenter : MetricsListenerService
    {
        public ConsoleMetricsListenter(
            InfluxDBClient influxDBClient, 
            MetricsConfig metricsConfig, 
            ISemanticConventions conventions = null) : 
            base(influxDBClient, metricsConfig, conventions)
        {

        }

        public override void WriteData(List<PointData> pointData)
        {
            foreach (var data in pointData)
            {
                Console.WriteLine(data.ToLineProtocol());
            }
        }
    }

    public class MetricsListenerService : EventListener, IHostedService
    {
        private List<string> RegisteredEventSources = new List<string>();
        private Task _dataSource;
        private InfluxDBClient _influxDBClient;
        private MetricsConfig _metricsConfig;
        private readonly string _measurementName;
        private readonly ISemanticConventions _conventions;
        private readonly IInfluxCloudConfig _influxCloudConfig;

        public MetricsListenerService(
            InfluxDBClient influxDBClient, 
            MetricsConfig metricsConfig, 
            ISemanticConventions conventions = null,
            IInfluxCloudConfig influxCloudConfig = null)
        {
            _influxDBClient = influxDBClient ?? throw new ArgumentNullException(nameof(influxDBClient));
            _metricsConfig = metricsConfig ?? throw  new ArgumentNullException(nameof(metricsConfig));
            _conventions = conventions ?? new SemanticConventions();
            _influxCloudConfig = influxCloudConfig;

            _measurementName = $"{metricsConfig.Prefix}{metricsConfig.ApplicationName}_{metricsConfig.MeasurementName}";
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _dataSource = Task.Run(async () =>
            {
                while (true)
                {
                    GetNewSources();
                    await Task.Delay(5000, cancellationToken);
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void GetNewSources()
        {
            foreach (var eventSource in EventSource.GetSources())
            {
                OnEventSourceCreated(eventSource);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (!RegisteredEventSources.Contains(eventSource.Name))
            {
                RegisteredEventSources.Add(eventSource.Name);
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string>
                {
                    ["EventCounterIntervalSec"] = "5"
                });
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!eventData.EventSource.Name.Equals("email") &&
                !eventData.EventSource.Name.Equals("domain"))
            {
                return;
            }

            List<PointData> pointData = null;
            var time = DateTime.UtcNow;

            for (int i = 0; i < eventData.Payload?.Count; ++i)
            {
                if (eventData.Payload[i] is IDictionary<string, object> eventPayload)
                {
                    pointData ??= new List<PointData>();
                    var (cacheName, counterName, counterValue) = GetMeasurement(eventPayload);

                    var point = PointData
                        .Measurement(_measurementName)
                        .Field(_conventions.ValueFieldName, counterValue)
                        .Tag(_conventions.ApplicationTagName, _metricsConfig.ApplicationName)
                        .Tag(_conventions.ApplicationVersionTagName, _metricsConfig.ApplicationVersion)
                        .Tag(_conventions.CacheNameTagName, cacheName)
                        .Tag(_conventions.CacheEventTagName, counterName)
                        .Timestamp(time, WritePrecision.S);

                    pointData.Add(point);
                }
            }

            if (pointData != null && pointData.Any())
            {
                WriteData(pointData);
            }
        }

        public virtual void WriteData(List<PointData> pointData)
        {
            using (var writeApi = _influxDBClient.GetWriteApi())
            {
                if (_influxCloudConfig != null)
                {
                    writeApi.WritePoints(_influxCloudConfig.Bucket, _influxCloudConfig.Organization, pointData);
                }
                else
                {
                    writeApi.WritePoints(pointData);
                }
            }
        }

        private (string cacheName, string counterName, long counterValue) GetMeasurement(
            IDictionary<string, object> eventPayload)
        {
            var cacheName = "";
            var counterName = "";
            long counterValue = 0;

            if (eventPayload.TryGetValue("Metadata", out object metaDataValue))
            {
                var metaDataString = Convert.ToString(metaDataValue);
                var metaData = metaDataString
                    .Split(',')
                    .Select(item => item.Split(':'))
                    .ToDictionary(s => s[0], s => s[1]);

                cacheName = metaData[_conventions.CacheNameTagName];
            }

            if (eventPayload.TryGetValue("Name", out object displayValue))
            {
                counterName = displayValue.ToString();
            }

            if (eventPayload.TryGetValue("Increment", out object incrementingPollingCounterValue))
            {
                counterValue = Convert.ToInt64(incrementingPollingCounterValue);
            }
            else if (eventPayload.TryGetValue("Mean", out object pollingCounterValue))
            {
                counterValue = Convert.ToInt64(pollingCounterValue);
            }


            return (cacheName, counterName, counterValue);
        }
    }
}
