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

        private static List<Rule> SelectRuleSet(
            Cell cell,
            List<Rule> wallRules,
            List<Rule> floorRules,
            List<Rule> ceilRules)
        {
            if (IsFloor(cell.Normal))
            {
                return floorRules;
            }
            if (IsCeil(cell.Normal))
            {
                return ceilRules;
            }

            return wallRules;
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

        private static void WriteSingleMeasurementResult(StreamWriter writer, int measurement, long time)
        {
            writer.WriteLine($"Measurement {measurement} : {time}");
        }

        private static void RunTimeFromCellCountDependenceBenchmark(
            StreamWriter writer, 
            int measurements)
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

            var wfcGenerator = new WfcGenerator();

            int[] sizes = { 4, 8, 12, 16, 20 };

            writer.WriteLine("Time from cells count dependency benchmark");

            foreach (int size in sizes)
            {
                writer.WriteLine($"Cells count: {size * size * 6}");

                var model = Model.Load($"Content/Models/Cube{size}x{size}.obj");
                var topology = new Topology(model.Meshes[0], 3);
                var cells = LevelGraphCreator.CreateGraph(
                    topology, 
                    LogicalResolution, 
                    TextureSize, 
                    CellSize);

                var factory = () => new WfcAndDecorationBenchmark(
                    topology,
                    wfcGenerator,
                    PropsGenerator,
                    cells,
                    cell => SelectRuleSet(cell, wallRules, floorRules, ceilRules),
                    TextureSize);

                var runner = new BenchmarRunner(factory);
                runner.SingleMeasurementCompleted += (int measurement, long time) 
                    => WriteSingleMeasurementResult(writer, measurement, time);               
                var result = runner.Run(measurements);

                writer.WriteLine($"Total: {result.Total}");
                writer.WriteLine($"Average: {result.Average}");
                writer.WriteLine();
            }
        }

        private static void RunWfcTimeFromRuleSetDependenceBenchmark(
            StreamWriter writer,
            int measurements)
        {
            var model = Model.Load($"Content/Models/Cube8x8.obj");
            var topology = new Topology(model.Meshes[0], 3);
            var cells = LevelGraphCreator.CreateGraph(
                topology,
                LogicalResolution,
                TextureSize,
                CellSize);

            writer.WriteLine("Time from rule set dependency benchmark");

            for (int number = 1; number <= 4; number++)
            {
                writer.WriteLine($"Rule set: {number}");

                var rules = RulesLoader.CreateRules(
                    $"Content/WallLogical{number}.png",
                    "Content/WallDetailed.png",
                    LogicalResolution,
                    DetailedResolution);

                var factory = () => new WfcBenchmark(new WfcGenerator(), cells, cell => rules);

                var runner = new BenchmarRunner(factory);
                runner.SingleMeasurementCompleted += (int measurement, long time)
                    => WriteSingleMeasurementResult(writer, measurement, time);
                var result = runner.Run(measurements);

                writer.WriteLine($"Total: {result.Total}");
                writer.WriteLine($"Average: {result.Average}");
                writer.WriteLine();
            }
        }

        private static void RunWfcAndDecorationTimeFromRuleSetDependenceBenchmark(
            StreamWriter writer, 
            int measurements)
        {
            var model = Model.Load($"Content/Models/Cube8x8.obj");
            var topology = new Topology(model.Meshes[0], 3);
            var cells = LevelGraphCreator.CreateGraph(
                topology,
                LogicalResolution,
                TextureSize,
                CellSize);

            writer.WriteLine("Time from rule set dependency benchmark");

            for (int number = 1; number <= 4; number++)
            {
                writer.WriteLine($"Rule set: {number}");

                var rules = RulesLoader.CreateRules(
                    $"Content/WallLogical{number}.png",
                    "Content/WallDetailed.png",
                    LogicalResolution,
                    DetailedResolution);
 
                var factory = () => new WfcAndDecorationBenchmark(
                     topology,
                     new WfcGenerator(),
                     PropsGenerator,
                     cells,
                     cell => rules,
                     TextureSize);

                var runner = new BenchmarRunner(factory);
                runner.SingleMeasurementCompleted += (int measurement, long time) 
                    => WriteSingleMeasurementResult(writer, measurement, time);
                var result = runner.Run(measurements);

                writer.WriteLine($"Total: {result.Total}");
                writer.WriteLine($"Average: {result.Average}");
                writer.WriteLine();
            }
        }        

        struct TimeStamp
        {
            public readonly long Time;
            public readonly float Mark;

            public TimeStamp(long time, float mark)
            {
                Time = time;
                Mark = mark;
            }
        }

        private static void RunMeasurementOfObservationSpeed(StreamWriter writer, int measurements)
        {
            var model = Model.Load($"Content/Models/Cube8x8.obj");
            var topology = new Topology(model.Meshes[0], 3);
            var cells = LevelGraphCreator.CreateGraph(
                topology,
                LogicalResolution,
                TextureSize,
                CellSize);

            var rules1 = RulesLoader.CreateRules(
                $"Content/WallLogical1.png",
                "Content/WallDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var rules3 = RulesLoader.CreateRules(
                $"Content/WallLogical3.png",
                "Content/WallDetailed.png",
                LogicalResolution,
                DetailedResolution);

            writer.WriteLine("Rule set 1");
            MeasureObservationSpeed(writer, topology, cells, rules1, measurements);

            writer.WriteLine("Rule set 2");
            MeasureObservationSpeed(writer, topology, cells, rules3, measurements);
        }

        private static void MeasureObservationSpeed(
            StreamWriter writer,
            Topology topology,
            List<Cell> cells,
            List<Rule> rules,
            int measurements)
        {
            var timeStamps = new List<TimeStamp>();
            var test = new Stopwatch();
            var timer = new Stopwatch();

            var wfcGenerator = new WfcGenerator();
            wfcGenerator.Observated += () =>
            {
                //timer.Stop();
                var marks = cells.Select(c => (float)(c.Rules.Count - 1) / (rules.Count - 1));
                var mark = marks.Sum() / marks.Count();
                var stamp = new TimeStamp(test.ElapsedMilliseconds, mark);
                timeStamps.Add(stamp);
                //timer.Start();
            };

            for (int i = 0; i < measurements; i++)
            {
                writer.WriteLine($"Measurement {i + 1}");
                timeStamps.Add(new TimeStamp(0, 1.0f));

                var benchmark = new WfcBenchmark(wfcGenerator, cells, cell => rules);

                benchmark.Initialize();
                test.Restart();
                timer.Restart();
                benchmark.Run();
                timer.Stop();
                test.Stop();
                benchmark.Terminate();

                Console.WriteLine($"{test.ElapsedMilliseconds} ?= {timeStamps.Last().Time}");

                foreach(var stamp in timeStamps)
                {
                    writer.WriteLine($"{stamp.Time};{stamp.Mark}");
                }

                timeStamps.Clear();                
            }
        }

        public static void Main(string[] args)
        {
            using (var writer = new StreamWriter("benchmarks.txt"))
            {
                int measurements = 100;

                //Console.WriteLine("Time from cells count dependency benchmark");
                //RunTimeFromCellCountDependenceBenchmark(writer, measurements);

                Console.WriteLine("Wfc time from rule set dependency benchmark");
                RunWfcTimeFromRuleSetDependenceBenchmark(writer, measurements);

                //Console.WriteLine("Wfc and decoration time from rule set dependency benchmark");
                //RunWfcTimeFromRuleSetDependenceBenchmark(writer, measurements);
            }

            //using (var writer = new StreamWriter("test.csv"))
            //{
            //    int measurements = 6;

            //    Console.WriteLine("Observation mesurements");
            //    RunMeasurementOfObservationSpeed(writer, measurements);
            //}
        }
    }
}