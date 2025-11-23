using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Brudixy.Tests.Benchmarks;

namespace Brudixy.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner
                .Run<DataBm>(
                    ManualConfig
                        .Create(DefaultConfig.Instance)
                        .WithOptions(ConfigOptions.DisableOptimizationsValidator));
        }
    }
}