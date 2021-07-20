using BenchmarkDotNet.Running;

namespace ZiggyCreatures.Fusion.Caching.Plugins.EventCounters.Benchmarks
{
    class Program
    {
        public static void Main(string[] args)  
            => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(
                    args
                     // , new DebugInProcessConfig()
                    );
    }
}
