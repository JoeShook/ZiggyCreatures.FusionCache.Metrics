using OpenTelemetry;

namespace ZiggyCreatures.Fusion.Caching.Plugins.OpenTelemetry.Benchmarks
{
    internal class TestExporter<T> : BaseExporter<T> where T : class
    {
        private readonly Action<Batch<T>> processBatchAction;

        public TestExporter(Action<Batch<T>> processBatchAction)
        {
            this.processBatchAction = processBatchAction ?? throw new ArgumentNullException(nameof(processBatchAction));
        }

        public override ExportResult Export(in Batch<T> batch)
        {
            this.processBatchAction(batch);

            return ExportResult.Success;
        }
    }
}
