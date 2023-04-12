using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using MeshTopology;
using OpenTK.Mathematics;
using SciFiAlgorithms;
using System.Diagnostics;
using TextureUtils;
using UVWfc.LevelGraph;
using UVWfc.Props;
using UVWfc.Wfc;

namespace UVWfcBenchmark
{
    public interface IBenchmark
    {
        public void Run();
    }

    public class PerformanceBenchmark : IBenchmark
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

        private const float CeilTrashold = 45.0f;
        private const float FloorTrashold = 45.0f;

        private static Material WallMaterial = new Material
        {
            Color = new Vector3(0.714f, 0.4284f, 0.18144f),
            Ambient = 0.15f,
            Specular = 0.25f,
            Shininess = 25.6f,
        };

        private static Material FloorMaterial = new Material
        {
            Color = new Vector3(0.4f, 0.4f, 0.4f),
            Ambient = 0.25f,
            Specular = 0.774597f,
            Shininess = 76.8f,
        };

        private static bool IsCeil(Vector3 normal)
        {
            var cosa = Vector3.Dot(-Vector3.UnitY, normal);
            var acos = MathF.Acos(cosa);
            var angle = MathHelper.RadiansToDegrees(acos);
            return angle < CeilTrashold;
        }

        private static bool IsFloor(Vector3 normal)
        {
            var cosa = Vector3.Dot(Vector3.UnitY, normal);
            var acos = MathF.Acos(cosa);
            var angle = MathHelper.RadiansToDegrees(acos);
            return angle < FloorTrashold;
        }

        private static Material SelectPanelMaterial(LogicalNode node)
        {
            var normal = Mathematics.GetNormal(node.Corners);

            if (IsCeil(normal) || IsFloor(normal))
            {
                return FloorMaterial;
            }

            return WallMaterial;
        }

        public static void Main(string[] args)
        {
            float extrusion = 0.05f;
            int textureSize = 2048;
            int cellSize = 32;
            var model = Model.Load("Content/Models/Cube20x20.obj");

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
                    .PushCellAlgorithm(new PanelsGeneratorAlgorithm(extrusion, SelectPanelMaterial))
                    .PushNetAlgorithm(new WiresGeneratorAlgorithm(extrusion))
                    .PushNetAlgorithm(new PipesGeneratorAlgorithm(extrusion))
                    .PushNetAlgorithm(new VentilationGeneratorAlgorithm(extrusion));

            var topology = new Topology(model.Meshes[0], 3);
            var cells = LevelGraphCreator.CreateGraph(topology, LogicalResolution, textureSize, cellSize);

            int countOfMeasurements = 1000;
            long total = 0;
            var timer = new Stopwatch();

            for (int i = 0; i < countOfMeasurements; i++)
            {
                using var engine = new Engine();
                var benchmark = new PerformanceBenchmark(
                    engine, 
                    topology, 
                    propsGenerator, 
                    cells, 
                    wallRules, 
                    floorRules, 
                    ceilRules, 
                    textureSize);

                timer.Reset();
                timer.Start();
                benchmark.Run();
                timer.Stop();

                total += timer.ElapsedMilliseconds;
                Console.WriteLine($"Measurement {i} : {timer.ElapsedMilliseconds}");
            }

            Console.WriteLine($"Total: {total}");
            Console.WriteLine($"Average: {total / countOfMeasurements}");
        }
    }
}