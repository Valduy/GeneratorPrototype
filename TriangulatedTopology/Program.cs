using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Utils;
using MeshTopology;
using TextureUtils;
using Vector3 = OpenTK.Mathematics.Vector3;
using TriangulatedTopology.Wfc;
using TriangulatedTopology.Props;
using TriangulatedTopology.Props.Algorithms;
using TriangulatedTopology.LevelGraph;

namespace TriangulatedTopology
{
    public class Program
    {
        public const int LogicalResolution = 4;
        public const int DetailedResolution = 20;       

        public static void Main(string[] args)
        {
            var random = new Random();
            int seed = random.Next();
            Console.WriteLine(seed);
            CollectionsHelper.UseSeed(seed);

            //CollectionsHelper.UseSeed(584469435);

            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var model = Model.Load("Content/Models/TriangulatedTower.obj");
            //var model = Model.Load("Content/Models/Cube12x12.obj");

            int textureSize = 2048;
            int cellSize = 32;

            var topology = new Topology(model.Meshes[0], 3);

            var roomGo = engine.CreateGameObject();
            var roomRenderer = roomGo.Add<MaterialRenderComponent>();
            roomRenderer.Model = model;

            var wallRules = RulesLoader.CreateRules(
                "Content/WallLogical.png",
                "Content/WallDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var floorRules = RulesLoader.CreateRules(
                "Content/FloorLogical.png",
                "Content/FloorDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var ceilRules = RulesLoader.CreateRules(
                "Content/CeilLogical.png",
                "Content/CeilDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var cells = LevelGraphCreator.CreateGraph(topology, LogicalResolution, textureSize, cellSize);
            WfcGenerator.GraphWfc(cells, wallRules, floorRules, ceilRules);

            float extrusion = 0.05f;

            var propsGenerator = new PropsGenerator()
                .PushCellAlgorithm(new PanelsGeneratorAlgorithm(extrusion))
                .PushNetAlgorithm(new WiresGeneratorAlgorithm(extrusion))
                .PushNetAlgorithm(new PipesGeneratorAlgorithm(extrusion))
                .PushNetAlgorithm(new VentilationGeneratorAlgorithm(extrusion));
            propsGenerator.Generate(engine, topology, cells, textureSize);

            var texture = TextureCreator.CreateDetailedTexture(cells, textureSize, cellSize);
            roomRenderer.Texture = Texture.LoadFromMemory(texture, textureSize, textureSize);
            var bmp = TextureHelper.TextureToBitmap(texture, textureSize);
            bmp.Save("Test.bmp");

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);
            engine.Run();
        }
    }
}