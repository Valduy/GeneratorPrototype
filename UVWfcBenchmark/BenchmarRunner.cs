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

        public delegate void InitializationCallback();
        public delegate void MasurementCallback(int measurement, long time);
        public delegate void TerminationCallback();

        public event InitializationCallback? Initialized;
        public event MasurementCallback? SingleMeasurementCompleted;
        public event TerminationCallback? Terminated;

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
                Initialized?.Invoke();

                timer.Reset();
                timer.Start();
                benchmark.Run();
                timer.Stop();
                SingleMeasurementCompleted?.Invoke(i + 1, timer.ElapsedMilliseconds);

                benchmark.Terminate();
                Terminated?.Invoke();
                total += timer.ElapsedMilliseconds;                
            }

            long average = total / measurements;
            return new Results(total, average);
        }
    }
}