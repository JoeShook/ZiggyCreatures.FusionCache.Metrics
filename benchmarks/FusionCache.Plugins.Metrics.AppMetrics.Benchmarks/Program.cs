using BenchmarkDotNet.Running;

namespace ZiggyCreatures.Fusion.Caching.Plugins.AppMetrics.Benchmarks
{
    class Program
    {
        public static void Main(string[] args) =>
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        
    }
}
