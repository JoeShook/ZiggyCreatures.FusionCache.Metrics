using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace ZiggyCreatures.Fusion.Caching.Plugins.EventCounters.Benchmarks
{
    class Program
    {
        public static async Task Main(string[] args)  
            => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(
                    args
                     // , new DebugInProcessConfig()
                    );
    }
}
