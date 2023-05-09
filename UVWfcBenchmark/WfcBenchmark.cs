using GameEngine.Core;
using UVWfc.Wfc;
using UVWfc.LevelGraph;
using static UVWfc.Wfc.WfcGenerator;

namespace UVWfcBenchmark
{
    public class WfcBenchmark : IBenchmark
    {
        private Engine? _engine;
        private WfcGenerator _wfcGenerator;
        private List<Cell> _cells;
        private RuleSetSelector _ruleSetSelector;

        public WfcBenchmark(
            WfcGenerator wfcGenerator,
            List<Cell> cells,
            RuleSetSelector ruleSetSelector)
        {
            _wfcGenerator = wfcGenerator;
            _cells = cells;
            _ruleSetSelector = ruleSetSelector;
        }

        public void Initialize()
        {
            _engine = new Engine();
        }

        public void Run()
        {
            if (_engine == null)
            {
                throw new Exception("Benchmark is not initialized.");
            }

            _wfcGenerator.Wfc(_cells, _ruleSetSelector);
        }

        public void Terminate()
        {
            _engine?.Dispose();
        }
    }
}
