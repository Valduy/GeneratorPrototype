using GameEngine.Graphics;
using GameEngine.Mathematics;
using MeshTopology;
using OpenTK.Mathematics;
using SciFiAlgorithms;
using TextureUtils;
using UVWfc.LevelGraph;
using UVWfc.Props;

namespace UVWfcBenchmark
{
    public class Program
    {
        public const int LogicalResolution = 4;
        public const int DetailedResolution = 20;

        private const float CeilTrashold = 45.0f;
        private const float FloorTrashold = 45.0f;

        private const float Extrusion = 0.05f;
        private const int TextureSize = 2048;
        private const int CellSize = 32;

        private static readonly PropsGenerator PropsGenerator = new PropsGenerator()
                .PushCellAlgorithm(new PanelsGeneratorAlgorithm(Extrusion, SelectPanelMaterial))
                .PushNetAlgorithm(new WiresGeneratorAlgorithm(Extrusion))
                .PushNetAlgorithm(new PipesGeneratorAlgorithm(Extrusion))
                .PushNetAlgorithm(new VentilationGeneratorAlgorithm(Extrusion));


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

        private static void WriteSingleMeasurementResult(int measurement, long time)
        {
            var message = $"Measurement {measurement} : {time}";
            Console.WriteLine(message);
        }

        private static void RunTimeFromCellCountDependenceBenchmark(int measurements)
        {
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

            int[] sizes = { 4, 8, 12, 16, 20 };

            Console.WriteLine("Time from cells count dependency benchmark");

            foreach (int size in sizes)
            {
                Console.WriteLine($"Cells count: {size * size * 6}");

                var model = Model.Load($"Content/Models/Cube{size}x{size}.obj");
                var topology = new Topology(model.Meshes[0], 3);
                var cells = LevelGraphCreator.CreateGraph(
                    topology, 
                    LogicalResolution, 
                    TextureSize, 
                    CellSize);

                var factory = () => new PerformanceBenchmark(
                    topology,
                    PropsGenerator,
                    cells,
                    wallRules,
                    floorRules,
                    ceilRules,
                    TextureSize);

                var runner = new BenchmarRunner(factory);
                runner.SingleMeasurementCompleted += WriteSingleMeasurementResult;               
                var result = runner.Run(measurements);

                Console.WriteLine($"Total: {result.Total}");
                Console.WriteLine($"Average: {result.Average}");
                Console.WriteLine();
            }
        }

        private static void RunTimeFromRuleSetDependenceBenchmark(int measurements)
        {
            var model = Model.Load($"Content/Models/Cube8x8.obj");
            var topology = new Topology(model.Meshes[0], 3);
            var cells = LevelGraphCreator.CreateGraph(
                topology,
                LogicalResolution,
                TextureSize,
                CellSize);

            Console.WriteLine("Time from rule set dependency benchmark");

            for (int number = 1; number <= 4; number++)
            {
                Console.WriteLine($"Rule set: {number}");

                var wallRules = RulesLoader.CreateRules(
                    $"Content/WallLogical{number}.png",
                    "Content/WallDetailed.png",
                    LogicalResolution,
                    DetailedResolution);

                var floorRules = RulesLoader.CreateRules(
                    $"Content/WallLogical{number}.png",
                    "Content/WallDetailed.png",
                    LogicalResolution,
                    DetailedResolution);

                var ceilRules = RulesLoader.CreateRules(
                    $"Content/WallLogical{number}.png",
                    "Content/WallDetailed.png",
                    LogicalResolution,
                    DetailedResolution);

                var factory = () => new PerformanceBenchmark(
                     topology,
                     PropsGenerator,
                     cells,
                     wallRules,
                     floorRules,
                     ceilRules,
                     TextureSize);

                var runner = new BenchmarRunner(factory);
                runner.SingleMeasurementCompleted += WriteSingleMeasurementResult;
                var result = runner.Run(measurements);

                Console.WriteLine($"Total: {result.Total}");
                Console.WriteLine($"Average: {result.Average}");
                Console.WriteLine();
            }
        }

        public static void Main(string[] args)
        {
            //RunTimeFromCellCountDependenceBenchmark(5);
            RunTimeFromRuleSetDependenceBenchmark(10);
        }
    }
}