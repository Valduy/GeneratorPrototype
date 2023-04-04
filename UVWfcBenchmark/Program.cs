using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using SciFiAlgorithms;
using System.Diagnostics;
using TextureUtils;
using UVWfc.LevelGraph;
using UVWfc.Props;
using UVWfc.Textures;
using UVWfc.Wfc;

namespace UVWfcBenchmark
{
    public class PerformanceBenchmark
    {
        private Engine _engine;
        private Topology _topology;
        private PropsGenerator _propsGenerator;
        private List<Cell> _cells;
        private List<Rule> _wallRules;
        private List<Rule> _floorRules;
        private List<Rule> _ceilRules;
        private int _textureSize;

        public PerformanceBenchmark(
            Engine engine,
            Topology topology,
            PropsGenerator propsGenerator,
            List<Cell> cells,
            List<Rule> wallRules,
            List<Rule> floorRules,
            List<Rule> ceilRules,
            int textureSize)
        {
            _engine = engine;
            _topology = topology;
            _propsGenerator = propsGenerator;
            _cells = cells;
            _wallRules = wallRules;
            _floorRules = floorRules;
            _ceilRules = ceilRules;
            _textureSize = textureSize;
        }

        public void Run()
        {
            WfcGenerator.GraphWfc(_cells, _wallRules, _floorRules, _ceilRules);
            _propsGenerator.Generate(_engine, _topology, _cells, _textureSize);
        }
    }

    public class Program
    {
        public const int LogicalResolution = 4;
        public const int DetailedResolution = 20;

        public static void Main(string[] args)
        {
            float extrusion = 0.05f;
            int textureSize = 2048;
            int cellSize = 32;
            var model = Model.Load("Content/Models/Cube16x16.obj");

            var wallRules = RulesLoader.CreateRules(
                "Content/Rules/WallLogical.png",
                "Content/Rules/WallDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var floorRules = RulesLoader.CreateRules(
                "Content/Rules/FloorLogical.png",
                "Content/Rules/FloorDetailed.png",
                LogicalResolution,
                DetailedResolution);


            var ceilRules = RulesLoader.CreateRules(
                "Content/Rules/CeilLogical.png",
                "Content/Rules/CeilDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var propsGenerator = new PropsGenerator()
                    .PushCellAlgorithm(new PanelsGeneratorAlgorithm(extrusion))
                    .PushNetAlgorithm(new WiresGeneratorAlgorithm(extrusion))
                    .PushNetAlgorithm(new PipesGeneratorAlgorithm(extrusion))
                    .PushNetAlgorithm(new VentilationGeneratorAlgorithm(extrusion));

            var topology = new Topology(model.Meshes[0], 3);
            var cells = LevelGraphCreator.CreateGraph(topology, LogicalResolution, textureSize, cellSize);

            int countOfMeasurements = 100;
            long total = 0;
            var timer = new Stopwatch();
            
            for (int i = 0; i < countOfMeasurements; i++)
            {
                using var engine = new Engine();
                var benchmark = new PerformanceBenchmark(
                    engine, topology, propsGenerator, cells, wallRules, floorRules, ceilRules, textureSize);

                timer.Reset();
                timer.Start();
                benchmark.Run();
                timer.Stop();

                total += timer.Elapsed.Milliseconds;
            }

            Console.WriteLine($"Total: {total}");
            Console.WriteLine($"Average: {(float)total / countOfMeasurements}");
        }
    }
}