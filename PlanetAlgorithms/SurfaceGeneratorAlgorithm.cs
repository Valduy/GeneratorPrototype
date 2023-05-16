using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using GameEngine.Utils;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;
using UVWfc.Helpers;
using UVWfc.Props;
using UVWfc.Props.Algorithms;
using Mesh = GameEngine.Graphics.Mesh;

namespace PlanetAlgorithms
{
    public class SurfaceGeneratorAlgorithm : ICellAlgorithm
    {
        private struct NodeVertex
        {
            public Color Color;
            public Vector3 Position;

            public NodeVertex(Color color, Vector3 position)
            {
                Color = color;
                Position = position;
            }
        }

        private struct Sequence
        {
            public Vector3i Color = new Vector3i(0, 0, 0);
            public List<Vector3> Vertices = new();

            public Sequence() { }
        }

        private struct BiomDescriptor
        {
            public float Height;
            public Material Material;

            public BiomDescriptor(float height, Material material)
            {
                Height = height;
                Material = material;
            }
        }

        private const int CornersCount = 4;
        private const int SequencesPerNodeMaxCount = 2;

        private static readonly Vector3i WaterColor     = new(112, 136, 227);
        private static readonly Vector3i GrassColor     = new(0, 215, 46);
        private static readonly Vector3i SandColor      = new(243, 248, 0);
        private static readonly Vector3i RockColor      = new(134, 134, 134);
        private static readonly Vector3i MountainColor  = new(191, 255, 243);
        private static readonly Vector3i IceColor       = new(255, 255, 255);

        private static readonly Material WaterMaterial = new()
        {
            Color = new Vector3(WaterColor) / 255,
            Shininess = 64.0f,
        };

        private static readonly Material GrassMaterial = new()
        {
            Color = new Vector3(GrassColor) / 255
        };

        private static readonly Material SandMaterial = new()
        {
            Color = new Vector3(SandColor) / 255
        };

        private static readonly Material RockMaterial = new()
        {
            Color = new Vector3(RockColor) / 255,
            Shininess = 10.0f,
            Specular = 0.1f,
        };

        private static readonly Material IceMaterial = new()
        {
            Color = new Vector3(IceColor) / 255,
            Shininess = 128.0f,
        };

        private static readonly Dictionary<Vector3i, BiomDescriptor> ColorsToDescriptor = new()
        {
            { WaterColor    , new BiomDescriptor(0.0f  , WaterMaterial ) },
            { GrassColor    , new BiomDescriptor(0.3f  , GrassMaterial ) },
            { SandColor     , new BiomDescriptor(0.2f  , SandMaterial  ) },
            { RockColor     , new BiomDescriptor(1.5f  , RockMaterial  ) },
            { MountainColor , new BiomDescriptor(2.5f  , IceMaterial   ) },
            { IceColor      , new BiomDescriptor(0.4f  , IceMaterial   ) },
        };

        private static readonly Model TreeModel = Model.Load("Content/Models/Tree.fbx");
        private static readonly Model PalmModel = Model.Load("Content/Models/Palm.fbx");
        private static readonly Model ShipModel = Model.Load("Content/Models/Ship.fbx");
        private static readonly Model TowerModel = Model.Load("Content/Models/Tower.fbx");

        private static Texture TreeTexture = Texture.LoadFromFile("Content/Models/Foliage.png");
        private static Texture PalmTexture = Texture.LoadFromFile("Content/Models/Palm.png");
        private static Texture ShipTexture = Texture.LoadFromFile("Content/Models/Ship.png");
        private static Texture TowerTexture = Texture.LoadFromFile("Content/Models/Tower.jpeg");

        private Random _random = new Random();

        public bool ProcessCell(Engine engine, LogicalNode node)
        {
            var vertices = ExtractColoredVertices(node);
            var sequences = ExtractSequences(vertices);
            GenerateSurfaces(engine, sequences);
            return true;
        }

        #region ForDebug

        private static void VisualizeOutlines(Engine engine, List<Sequence> sequences)
        {
            foreach (var sequence in sequences)
            {
                var color = new Vector3(sequence.Color) / 255;

                for (int i = 0; i < sequence.Vertices.Count; i++)
                {
                    engine.Line(sequence.Vertices.GetCircular(i - 1), sequence.Vertices[i], color, 5.0f);
                }
            }
        }

        #endregion ForDebug

        private void GenerateSurfaces(Engine engine, List<Sequence> sequences)
        {
            if (sequences.Count == 1)
            {
                var sequence = sequences[0];
                var mesh = CreateMesh(sequence);
                var model = new Model(mesh);
                var material = ColorsToDescriptor[sequence.Color].Material;
                CreateSurfaceGameObject(engine, model, material);
                InstantiateObjectRandomly(engine, sequence);
            }
            else
            {
                for (int i = 0; i < sequences.Count; i++)
                {
                    var thisSequence = sequences[i];
                    var neighbourSequence = sequences.GetCircular(i + 1);
                    var meshes = CreateMeshes(thisSequence, neighbourSequence);
                    var model = new Model(meshes);
                    var material = ColorsToDescriptor[thisSequence.Color].Material;
                    CreateSurfaceGameObject(engine, model, material);
                }
            }
        }

        private void InstantiateObjectRandomly(Engine engine, Sequence sequence)
        {
            var height = ColorsToDescriptor[sequence.Color].Height;
            var normal = Mathematics.GetNormal(sequence.Vertices);
            var position = Mathematics.GetCentroid(sequence.Vertices) + height * normal;
            var rotation = GetRotation(normal);

            if (IsGrass(sequence))
            {
                if (_random.Next(10) <= 1)
                {
                    InstantiateTower(engine, position, rotation);
                    return;
                }

                InstantiateTree(engine, position, rotation);
            }
            else if (IsSand(sequence))
            {
                if (_random.Next(10) <= 1)
                {
                    InstantiateTower(engine, position, rotation);
                    return;
                }

                InstantiatePalm(engine, position, rotation);
            }
            else if (IsWater(sequence))
            {
                if (_random.Next(10) > 1)
                {
                    return;
                }

                InstantiateShip(engine, position, rotation);
            }
            else if (IsRock(sequence))
            {
                InstantiateTower(engine, position, rotation);
            }
        }

        private Quaternion GetRotation(Vector3 normal)
        {
            var rotation = Mathematics.FromToRotation(Vector3.UnitY, normal);
            var yAxis = Vector3.Transform(Vector3.UnitY, rotation);
            var randomAngle = MathHelper.DegreesToRadians(_random.Next(360));
            return Quaternion.FromAxisAngle(yAxis, randomAngle) * rotation;
        }

        private static void InstantiateTree(Engine engine, Vector3 position, Quaternion rotation)
        {
            var tree = engine.CreateModel(TreeModel, position, rotation, new Vector3(0.05f));
            tree.Get<MaterialRenderComponent>()!.Texture = TreeTexture;
        }

        private static void InstantiatePalm(Engine engine, Vector3 position, Quaternion rotation)
        {
            var palm = engine.CreateModel(PalmModel, position, rotation, new Vector3(0.004f));
            palm.Get<MaterialRenderComponent>()!.Texture = PalmTexture;
        }

        private static void InstantiateShip(Engine engine, Vector3 position, Quaternion rotation)
        {
            var ship = engine.CreateModel(ShipModel, position, rotation, new Vector3(0.0005f));
            var renderer = ship.Get<MaterialRenderComponent>()!;
            renderer.Material.Shininess = 1.0f;
            renderer.Material.Specular = 1.0f;
            renderer.Texture = ShipTexture;
        }

        private static void InstantiateTower(Engine engine, Vector3 position, Quaternion rotation)
        {
            var tower = engine.CreateModel(TowerModel, position, rotation, new Vector3(0.005f));
            tower.Get<MaterialRenderComponent>()!.Texture = TowerTexture;
        }

        private static void CreateSurfaceGameObject(Engine engine, Model model, Material material)
        {
            var surface = engine.CreateGameObject();
            var renderer = surface.Add<MaterialRenderComponent>();
            renderer.Model = model;
            renderer.Material = material;
        }

        private static Mesh CreateMesh(Sequence sequence)
        {            
            var height = ColorsToDescriptor[sequence.Color].Height;
            var positions = sequence.Vertices
                .Select(position => position + height * Vector3.Normalize(position))
                .ToList();

            return CreateMesh(positions);
        }

        private static Mesh CreateMesh(List<Vector3> positions)
        {
            var normal = Mathematics.GetNormal(positions);
            var vertices = positions
                .Select(position => new Vertex(position, normal, Vector2.Zero))
                .ToList();
            var indices = TriangulateConvexPolygon(vertices);

            return new Mesh(vertices, indices);
        }

        private static List<Mesh> CreateMeshes(Sequence thisSequence, Sequence neighbourSequence)
        {
            var meshes = new List<Mesh>();
            var surfaceHeight = ColorsToDescriptor[thisSequence.Color].Height;
            var borderHeight = ColorsToDescriptor[neighbourSequence.Color].Height;
            borderHeight = borderHeight > surfaceHeight ? surfaceHeight : borderHeight;

            if (thisSequence.Vertices.Count == 3)
            {
                var shore = new List<Vector3>()
                {
                    thisSequence.Vertices[0] + borderHeight * Vector3.Normalize(thisSequence.Vertices[0]),
                    thisSequence.Vertices[1] + surfaceHeight * Vector3.Normalize(thisSequence.Vertices[1]),
                    thisSequence.Vertices[2] + borderHeight * Vector3.Normalize(thisSequence.Vertices[2]),
                };

                meshes.Add(CreateMesh(shore));
                return meshes;
            }

            {
                var shore = new List<Vector3>()
                {
                    thisSequence.Vertices[0] + borderHeight * Vector3.Normalize(thisSequence.Vertices[0]),
                    thisSequence.Vertices[1] + surfaceHeight * Vector3.Normalize(thisSequence.Vertices[1]),
                    thisSequence.Vertices[thisSequence.Vertices.Count - 2] + surfaceHeight * Vector3.Normalize(thisSequence.Vertices[thisSequence.Vertices.Count - 2]),
                    thisSequence.Vertices[thisSequence.Vertices.Count - 1] + borderHeight * Vector3.Normalize(thisSequence.Vertices[thisSequence.Vertices.Count - 1]),
                };

                meshes.Add(CreateMesh(shore));
            }

            if (thisSequence.Vertices.Count <= 4)
            {
                return meshes;
            }

            var surface = thisSequence.Vertices
                .Skip(1)
                .SkipLast(1)
                .Select(position => position + surfaceHeight * Vector3.Normalize(position))
                .ToList();

            meshes.Add(CreateMesh(surface));
            return meshes;
        }

        private static List<int> TriangulateConvexPolygon(List<Vertex> vertices)
        {
            var indices = new List<int>();
            var trianglesCount = vertices.Count - 2;
            int a = 0;
            int b = 1;

            for (int i = 0; i < trianglesCount; i++)
            {
                int c = b + 1;
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                b = c;
            }

            return indices;
        }

        private static List<NodeVertex> ExtractColoredVertices(LogicalNode node)
        {
            var vertices = new List<NodeVertex>();

            var cornerVertices = new[]
            {
                new NodeVertex(node.Rule.Logical[1, 0], node.Corners[0]),
                new NodeVertex(node.Rule.Logical[0, 0], node.Corners[1]),
                new NodeVertex(node.Rule.Logical[0, 1], node.Corners[2]),
                new NodeVertex(node.Rule.Logical[1, 1], node.Corners[3]),
            };

            for (int i = 0; i < CornersCount; i++)
            {
                var prev = cornerVertices.GetCircular(i - 1);
                var next = cornerVertices[i];
                var centroid = (prev.Position + next.Position) / 2;

                vertices.Add(prev);                

                if (prev.Color != next.Color)
                {
                    vertices.Add(new NodeVertex(prev.Color, centroid));
                    vertices.Add(new NodeVertex(next.Color, centroid));
                } 
            }

            return vertices;
        }

        private static List<Sequence> ExtractSequences(List<NodeVertex> vertices)
        {
            int initial = 0;
            var sequences = new List<Sequence>();

            for (int i = 0; i < vertices.Count; i++)
            {
                var prev = vertices.GetCircular(i - 1);
                var next = vertices.GetCircular(i);

                if (prev.Color != next.Color)
                {
                    initial = i;
                    break;
                }
            }

            for (int i = 0; i < SequencesPerNodeMaxCount; i++)
            {
                var sequence = new Sequence();
                sequence.Color = vertices[initial].Color.RgbaToVector3i();
                sequences.Add(sequence);

                var from = vertices.GetCircular(initial);

                for (var counter = 0; counter < vertices.Count; counter++)
                {
                    var temp = vertices.GetCircular(initial + counter);

                    if (from.Color != temp.Color)
                    {
                        initial += counter;
                        break;
                    }

                    sequence.Vertices.Add(temp.Position);
                }

                if (sequence.Vertices.Count == vertices.Count)
                {
                    break;
                }
            }

            return sequences;
        }

        private static bool IsWater(Sequence sequence)
        {
            return sequence.Color == WaterColor;
        }

        private static bool IsGrass(Sequence sequence)
        {
            return sequence.Color == GrassColor;
        }

        private static bool IsSand(Sequence sequence)
        {
            return sequence.Color == SandColor;
        }

        private static bool IsRock(Sequence sequence)
        {
            return sequence.Color == RockColor;
        }

        private static bool IsMountain(Sequence sequence)
        {
            return sequence.Color == MountainColor;
        }

        private static bool IsIce(Sequence sequence)
        {
            return sequence.Color == IceColor;
        }
    }
}