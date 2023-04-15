using System.Diagnostics;

namespace UVWfcBenchmark
{
    public class BenchmarRunner
    {
        public struct Results
        {
            public readonly long Total;
            public readonly long Average;

            public Results(long total, long average)
            {
                Total = total;
                Average = average;
            }
        }

        private readonly Func<IBenchmark> Factory;

        public delegate void MasurementCallback(int measurement, long time);

        public event MasurementCallback? SingleMeasurementCompleted;

        public BenchmarRunner(Func<IBenchmark> factory)
        {
            Factory = factory;
        }

        public Results Run(int measurements)
        {
            long total = 0;
            var timer = new Stopwatch();

            for (int i = 0; i < measurements; i++)
            {
                var benchmark = Factory();
                benchmark.Initialize();

                timer.Reset();
                timer.Start();
                benchmark.Run();
                timer.Stop();

                benchmark.Terminate();
                total += timer.ElapsedMilliseconds;
                SingleMeasurementCompleted?.Invoke(i + 1, timer.ElapsedMilliseconds);
            }

            long average = total / measurements;
            return new Results(total, average);
        }
    }
}