using GameEngine.Core;
using MeshTopology;
using TextureUtils;
using UVWfc.LevelGraph;
using UVWfc.Props;
using UVWfc.Wfc;
using static UVWfc.Wfc.WfcGenerator;

namespace UVWfcBenchmark
{
    public class WfcAndDecorationBenchmark : IBenchmark
    {
        private Engine? _engine;
        private Topology _topology;
        private WfcGenerator _wfcGenerator;
        private PropsGenerator _propsGenerator;        
        private List<Cell> _cells;
        private RuleSetSelectorCallback _ruleSetSelector;
        private int _textureSize;

        public WfcAndDecorationBenchmark(
            Topology topology,
            WfcGenerator wfcGenerator,
            PropsGenerator propsGenerator,
            List<Cell> cells,
            RuleSetSelectorCallback ruleSetSelector,
            int textureSize)
        {            
            _topology = topology;
            _wfcGenerator = wfcGenerator;
            _propsGenerator = propsGenerator;
            _cells = cells;
            _ruleSetSelector = ruleSetSelector;
            _textureSize = textureSize;
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

            _wfcGenerator.GraphWfc(_cells, _ruleSetSelector);
            _propsGenerator.Generate(_engine, _topology, _cells, _textureSize);
        }

        public void Terminate()
        {
            _engine?.Dispose();
        }
    }
}