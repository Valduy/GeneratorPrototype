using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using SciFiAlgorithms;
using TextureUtils;
using UVWfc.LevelGraph;
using UVWfc.Props;
using UVWfc.Textures;
using UVWfc.Wfc;

namespace ReactorDemo
{
    public class Program
    {
        public const int LogicalResolution = 4;
        public const int DetailedResolution = 20;

        public static void Main(string[] args)
        {
            int textureSize = 2048;
            int cellSize = 32;
            float extrusion = 0.05f;

            //var random = new Random();
            //int seed = random.Next();
            //Console.WriteLine(seed);
            //CollectionsHelper.UseSeed(seed);

            CollectionsHelper.UseSeed(1370866345);

            var model = Model.Load("Content/Models/Reactor.obj");
            var topology = new Topology(model.Meshes[0], 3);

            var wallRules = RulesLoader.CreateRules(
                "Content/WallLogical.png",
                "Content/Rules/WallDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var floorRules = RulesLoader.CreateRules(
                "Content/WallLogical.png",
                "Content/Rules/WallDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var ceilRules = RulesLoader.CreateRules(
                "Content/Rules/CeilLogical.png",
                "Content/Rules/CeilDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var cells = LevelGraphCreator.CreateGraph(topology, LogicalResolution, textureSize, cellSize);
            WfcGenerator.GraphWfc(cells, wallRules, floorRules, ceilRules);

            var texture = TextureCreator.CreateDetailedTexture(cells, textureSize, cellSize);
            var bmp = TextureHelper.TextureToBitmap(texture, textureSize);
            bmp.Save("Test.bmp");

            using var engine = new Engine();

            var propsGenerator = new PropsGenerator()
                .PushCellAlgorithm(new PanelsGeneratorAlgorithm(extrusion))
                .PushNetAlgorithm(new WiresGeneratorAlgorithm(extrusion))
                .PushNetAlgorithm(new PipesGeneratorAlgorithm(extrusion))
                .PushNetAlgorithm(new VentilationGeneratorAlgorithm(extrusion));
            propsGenerator.Generate(engine, topology, cells, textureSize);

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var roomGo = engine.CreateGameObject();
            var roomRenderer = roomGo.Add<MaterialRenderComponent>();
            roomRenderer.Model = model;
            roomRenderer.Texture = Texture.LoadFromMemory(texture, textureSize, textureSize);

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);
            engine.Run();
        }
    }
}