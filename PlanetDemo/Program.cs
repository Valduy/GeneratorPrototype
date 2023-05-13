using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using PlanetAlgorithms;
using TextureUtils;
using UVWfc.LevelGraph;
using UVWfc.Props;
using UVWfc.Textures;
using UVWfc.Wfc;

namespace SphereDemo
{
    public class Program
    {
        public const int LogicalResolution = 2;
        public const int DetailedResolution = 2;

        private const float PoleTrashold = 25.0f;

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
            return angle < PoleTrashold;
        }

        private static bool IsFloor(Vector3 normal)
        {
            var cosa = Vector3.Dot(Vector3.UnitY, normal);
            var acos = MathF.Acos(cosa);
            var angle = MathHelper.RadiansToDegrees(acos);
            return angle < PoleTrashold;
        }

        private static List<Rule> SelectRuleSet(
            Cell cell,
            List<Rule> wallRules,
            List<Rule> poleRules)
        {
            if (IsFloor(cell.Normal) || IsCeil(cell.Normal))
            {
                return poleRules;
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

            //CollectionsHelper.UseSeed(375065103);

            var model = Model.Load("Content/Models/Sphere.obj");
            var topology = new Topology(model.Meshes[0], 3);

            var wallRules = RulesLoader.CreateRules(
                "Content/Rules/WallTileSet.png",
                "Content/Rules/WallTileSet.png",
                LogicalResolution,
                DetailedResolution);

            var poleRules = RulesLoader.CreateRules(
                "Content/Rules/PoleTileSet.png",
                "Content/Rules/PoleTileSet.png",
                LogicalResolution,
                DetailedResolution);

            var cells = LevelGraphCreator.CreateGraph(topology, LogicalResolution, textureSize, cellSize);

            var wfcGenerator = new WfcGenerator();
            wfcGenerator.Wfc(cells, cell => SelectRuleSet(cell, wallRules, poleRules));

            var texture = TextureCreator.CreateDetailedTexture(cells, textureSize, cellSize);
            var bmp = TextureHelper.TextureToBitmap(texture, textureSize);
            bmp.Save("Test.bmp");

            using var engine = new Engine();

            var propsGenerator = new PropsGenerator()
                .PushCellAlgorithm(new SurfaceGeneratorAlgorithm());
            propsGenerator.Generate(engine, topology, cells, textureSize);

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            //var planet = engine.CreateGameObject();
            //renderer = planet.Add<MaterialRenderComponent>();
            //renderer.Model = model;
            //renderer.Texture = Texture.LoadFromMemory(texture, textureSize, textureSize);

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);
            engine.Run();
        }
    }
}