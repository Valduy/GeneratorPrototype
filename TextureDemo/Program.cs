using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using GameEngine.Utils;
using OpenTK.Mathematics;
using Mesh = GameEngine.Graphics.Mesh;

namespace TextureDemo
{
    class Program
    {
        public class ColorsRing
        {
            private readonly List<Color> _colors;

            public ColorsRing(int size)
            {
                _colors = new List<Color>();
                var random = new Random();

                for (int i = 0; i < size; i++)
                {
                    _colors.Add(new Color
                    {
                        R = (byte) random.Next(0, 256),
                        G = (byte) random.Next(0, 256),
                        B = (byte) random.Next(0, 256),
                        A = 255,
                    });
                }
            }

            public Color this[int index] 
                => _colors.GetCircular(index);
        }

        public class EdgeComparer : IEqualityComparer<Edge>
        {
            public bool Equals(Edge? lhs, Edge? rhs)
            {
                return lhs!.A == rhs!.A && lhs!.B == rhs!.B
                       || lhs!.A == rhs!.B && lhs!.B == rhs!.A;
            }

            public int GetHashCode(Edge item)
            {
                return item.A.GetHashCode() ^ item.B.GetHashCode();
            }
        }

        public const int TextureSize = 1000;

        public static Random GlobalRandom;
        public static ColorsRing RandomColors;

        public static List<Face> GetFaces(Mesh mesh)
        {
            var faces = new List<Face>();

            for (int i = 0; i < mesh.Indices.Count; i += 4)
            {
                var face = new Face(new List<Vertex>()
                {
                    mesh.Vertices[mesh.Indices[i + 0]],
                    mesh.Vertices[mesh.Indices[i + 1]],
                    mesh.Vertices[mesh.Indices[i + 2]],
                    mesh.Vertices[mesh.Indices[i + 3]],
                });
                faces.Add(face);
            }

            return faces;
        }

        public static List<Edge> GetEdges(List<Face> faces)
        {
            var edges = new HashSet<Edge>(new EdgeComparer());

            foreach (var face in faces)
            {
                foreach (var edge in face.EnumerateFace())
                {
                    edges.Add(edge);
                }
            }

            return edges.ToList();
        }

        static List<Face> GetAdjacentFaces(List<Face> faces, Edge shared)
        {
            var result = new List<Face>();
            var comparer = new EdgeComparer();

            foreach (var face in faces)
            {
                foreach (var edge in face.EnumerateFace())
                {
                    if (comparer.Equals(edge, shared))
                    {
                        result.Add(face);
                        break;
                    }
                }
            }

            return result;
        }

        public static List<(Vertex, Vertex)> GetEdgeVertices(List<Face> faces, Edge edge)
        {
            var result = new List<(Vertex, Vertex)>();

            foreach (var face in faces)
            {
                Vertex a = face.First(v => v.Position == edge.A);
                Vertex b = face.First(v => v.Position == edge.B);
                result.Add((a, b));
            }

            return result;
        }

        public static List<Vector2> GetDirections(List<Face> faces, List<(Vertex A, Vertex B)> vertices)
        {
            var result = new List<Vector2>();

            for (int i = 0; i < faces.Count; i++)
            {
                var face = faces[i];
                var edge = vertices[i];

                var otherEdges = face
                    .Except(new List<Vertex> {edge.A, edge.B})
                    .ToList();
               
                var from = (edge.A.TextureCoords + edge.B.TextureCoords) / 2;
                var to = otherEdges
                    .Select(v => v.TextureCoords)
                    .Aggregate((v1, v2) => v1 + v2) / otherEdges.Count;

                result.Add((to - from).Normalized());
            }

            return result;
        }

        public static void GenerateEdgePixels(
            byte[] data, 
            List<(Vertex A, Vertex B)> vertices, 
            List<Vector2> directions)
        {
            var color = GlobalRandom.Next();
            
            for (int i = 0; i < vertices.Count; i++)
            {
                var (a, b) = vertices[i];
                var direction = directions[i];
                var colorTemp = color;

                var from = a.TextureCoords * TextureSize;
                var to = b.TextureCoords * TextureSize;
                var step = (to - from).Normalized();

                for (var temp = from; !Mathematics.ApproximatelyEqualEpsilon(to, temp, 0.001f); temp += step)
                {
                    data.SetColor(TextureSize, temp, RandomColors[colorTemp]);
                    data.SetColor(TextureSize, temp + direction, RandomColors[colorTemp]);
                    colorTemp += 1;
                }
            }
            
        }

        public static byte[] GenerateTexture(Model model)
        {
            byte[] data = new byte[TextureSize * TextureSize * 4];
            var faces = GetFaces(model.Meshes[0]);
            var edges = GetEdges(faces);

            foreach (var edge in edges)
            {
                var adjacent = GetAdjacentFaces(faces, edge);
                var vertices = GetEdgeVertices(adjacent, edge);
                var directions = GetDirections(adjacent, vertices);
                GenerateEdgePixels(data, vertices, directions);
            }

            return data;
        }

        public static byte[] GenerateRandomTexture(int size)
        {
            var result = new byte[4 * size * size];
            var random = new Random();

            for (int i = 0; i < result.Length; i++)
            {
                if (i % 3 == 0)
                {
                    result[i] = byte.MaxValue;
                }
                else
                {
                    result[i] = (byte)random.Next(0, byte.MaxValue);
                }
            }

            return result;
        }

        public static byte[] GenerateLinearTexture(int size)
        {
            var result = new byte[4 * size * size];
            bool flag = true;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    result[4 * x + 4 * size * y + 0] = (byte)(flag ? 255 : 0);
                    result[4 * x + 4 * size * y + 1] = (byte)(flag ? 0 : 255);
                    result[4 * x + 4 * size * y + 2] = 0;
                    result[4 * x + 4 * size * y + 3] = 255;
                }

                flag = !flag;
            }

            return result;
        }

        public static void Main(string[] args)
        {
            GlobalRandom = new();
            RandomColors = new(1000);

            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var pearGo = engine.CreateGameObject();
            var pearRender = pearGo.Add<MaterialRenderComponent>();
            pearRender.Model = Model.Load("Content/Pear.obj");
            //pearRender.Texture = Texture.LoadFromFile("Content/Pear_Diffuse.jpg");
            pearRender.Texture = Texture.LoadFromMemory(GenerateRandomTexture(128), 128, 128);
            pearGo.Position = new Vector3(5, 0, 0);

            var quadModel = Model.Load("Content/Structure.obj", PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);
            var texture = GenerateTexture(quadModel);

            var structureGo = engine.CreateGameObject();
            var structureRender = structureGo.Add<MaterialRenderComponent>();
            structureRender.Model = Model.Load("Content/Structure.obj");
            //structureRender.Texture = Texture.LoadFromFile("Content/Texture.png");
            structureRender.Texture = Texture.LoadFromMemory(texture, TextureSize, TextureSize);
            structureGo.Position = new Vector3(-5, 1, 0);
            structureGo.AddChild(engine.Axis(5));

            var planeGo = engine.CreateGameObject();
            var planeRender = planeGo.Add<MaterialRenderComponent>();
            planeRender.Model = Model.Square(1);
            planeRender.Texture = Texture.LoadFromMemory(GenerateLinearTexture(TextureSize), TextureSize, TextureSize);

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11.0f, 0.0f, -11.0f);
            
            engine.Run();
        }
    }
}