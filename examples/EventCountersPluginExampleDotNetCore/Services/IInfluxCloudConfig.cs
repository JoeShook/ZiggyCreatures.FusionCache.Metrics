namespace EventCountersPluginExampleDotNetCore.Services
{
    public interface IInfluxCloudConfig
    {
        string Bucket { get; set; }

        string Organization { get; set; }
    }

    public class InfluxCloudConfig : IInfluxCloudConfig
    {
        public string Bucket { get; set; }

        public string Organization { get; set; }
    }
}
