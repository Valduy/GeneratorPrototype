using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using SciFiAlgorithms;
using TextureUtils;
using UVWfc.LevelGraph;
using UVWfc.Props;
using UVWfc.Textures;
using UVWfc.Wfc;

namespace CubeDemo
{
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

        private static Material ModelMaterial = new Material
        {
            Color = new Vector3(0.01f, 0.01f, 0.01f),
            Ambient = 0.02f,
            Specular = 0.4f,
            Shininess = 32.0f,
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

        public static void Main(string[] args)
        {
            int textureSize = 2048;
            int cellSize = 32;
            float extrusion = 0.05f;

            var random = new Random();
            int seed = random.Next();
            Console.WriteLine(seed);
            CollectionsHelper.UseSeed(seed);

            //CollectionsHelper.UseSeed(1533573477);

            var model = Model.Load("Content/Models/Cube12x12.obj");
            var topology = new Topology(model.Meshes[0], 3);

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

            var cells = LevelGraphCreator.CreateGraph(topology, LogicalResolution, textureSize, cellSize);

            var wfcGenerator = new WfcGenerator();
            wfcGenerator.GraphWfc(cells, cell => SelectRuleSet(cell, wallRules, floorRules, ceilRules));

            var texture = TextureCreator.CreateDetailedTexture(cells, textureSize, cellSize);
            var bmp = TextureHelper.TextureToBitmap(texture, textureSize);
            bmp.Save("Test.bmp");

            using var engine = new Engine();

            var propsGenerator = new PropsGenerator()
                .PushCellAlgorithm(new PanelsGeneratorAlgorithm(extrusion, SelectPanelMaterial))
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
            roomRenderer.Material = ModelMaterial;

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);
            engine.Run();
        }
    }
}