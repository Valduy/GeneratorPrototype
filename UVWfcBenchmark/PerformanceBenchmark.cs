using GameEngine.Core;
using MeshTopology;
using TextureUtils;
using UVWfc.LevelGraph;
using UVWfc.Props;
using UVWfc.Wfc;

namespace UVWfcBenchmark
{
    public class PerformanceBenchmark : IBenchmark
    {
        private Engine? _engine;
        private Topology _topology;
        private PropsGenerator _propsGenerator;
        private List<Cell> _cells;
        private List<Rule> _wallRules;
        private List<Rule> _floorRules;
        private List<Rule> _ceilRules;
        private int _textureSize;

        public PerformanceBenchmark(
            Topology topology,
            PropsGenerator propsGenerator,
            List<Cell> cells,
            List<Rule> wallRules,
            List<Rule> floorRules,
            List<Rule> ceilRules,
            int textureSize)
        {            
            _topology = topology;
            _propsGenerator = propsGenerator;
            _cells = cells;
            _wallRules = wallRules;
            _floorRules = floorRules;
            _ceilRules = ceilRules;
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

            WfcGenerator.GraphWfc(_cells, _wallRules, _floorRules, _ceilRules);
            _propsGenerator.Generate(_engine, _topology, _cells, _textureSize);
        }

        public void Terminate()
        {
            _engine?.Dispose();
        }
    }
}