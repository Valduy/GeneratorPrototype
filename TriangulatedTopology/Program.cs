using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using TextureUtils;
using Mathematics = GameEngine.Mathematics.Mathematics;

namespace TriangulatedTopology
{
    public class EdgeComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge? x, Edge? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x != null && y != null)
            {
                return x.HasSamePositions(y);
            }

            return false;
        }

        public int GetHashCode([DisallowNull] Edge edge)
        {
            return edge.A.Position.GetHashCode() ^ edge.B.Position.GetHashCode();
        }
    }

    public class Program
    {
        public const float SideSize = 1.0f;

        public static List<Face> ExtractPolies(Topology topology)
        {
            var groups = topology.ExtractFacesGroups((reference, node)
                => reference.Face.IsSharedUVEdgeExist(node.Face));

            var polies = new List<Face>();

            foreach (var group in groups)
            {
                var repeates = new HashSet<Edge>(new EdgeComparer());
                var edges = new HashSet<Edge>(new EdgeComparer());

                foreach (var node in group)
                {
                    foreach (var edge in node.Face.EnumerateEdges())
                    {
                        if (edges.Contains(edge))
                        {
                            repeates.Add(edge);
                        }
                        else
                        {
                            edges.Add(edge);
                        }
                    }
                }

                edges.ExceptWith(repeates);

                while (edges.Count > 0)
                {
                    var poly = new List<Vertex>() { edges.First().B };
                    edges.Remove(edges.First());

                    while (edges.Any(e => e.A.Position == poly[poly.Count - 1].Position))
                    {
                        var edge = edges.First(e => e.A.Position == poly[poly.Count - 1].Position);
                        poly.Add(edge.B);
                        edges.Remove(edge);
                    }

                    polies.Add(new Face(poly));
                }            
            }

            return polies;
        }

        public static List<Face> CleanUpPolies(List<Face> polies)
        {
            var result = new List<Face>();

            foreach (var poly in polies)
            {
                var cleanPoly = GetCorners(poly);
                result.Add(new Face(cleanPoly));
            }

            return result;
        }

        public static List<Vertex> GetCorners(Face poly)
        {
            var cleanPoly = new List<Vertex>();

            for (int i = 0; i < poly.Count; i++)
            {
                var prev = poly.GetCircular(i - 1);
                var current = poly.GetCircular(i);
                var next = poly.GetCircular(i + 1);

                var axis1 = prev.Position - current.Position;
                var axis2 = next.Position - current.Position;
                var cross = Vector3.Cross(axis1, axis2);

                if (!Mathematics.ApproximatelyEqualEpsilon(cross, Vector3.Zero, float.Epsilon))
                {
                    cleanPoly.Add(current);
                }
            }

            return cleanPoly;
        }
        
        public static Dictionary<TopologyNode, Vertex> SliceSurfaces(Topology topology)
        {
            var initials = new Dictionary<TopologyNode, Vertex>();
            var visited = new HashSet<TopologyNode> { topology[0] };
            var queue = new Queue<TopologyNode>();
            queue.Enqueue(topology[0]);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                Slice(node, initials);

                foreach (var neighbour in node.Neighbours)
                {
                    if (!visited.Contains(neighbour))
                    {
                        queue.Enqueue(neighbour);
                        visited.Add(neighbour);
                    }
                }
            }

            return initials;
        }

        public static void Slice(TopologyNode node, Dictionary<TopologyNode, Vertex> initials)
        {
            var topAdapter = new Adapter(node, node.Neighbours[0]);
            var bottomAdapter = new Adapter(node, node.Neighbours[2]);
            var leftAdapter = new Adapter(node, node.Neighbours[1]);            
            var rightAdapter = new Adapter(node, node.Neighbours[3]);

            var top = GetPossibleInitialsHorizontal(topAdapter, initials);
            var bottom = GetPossibleInitialsHorizontal(bottomAdapter, initials);
            var left = GetPossibleInitialsVertical(leftAdapter, initials);
            var right = GetPossibleInitialsVertical(rightAdapter, initials);

            try
            {
                var initialIndex = top
                    .Intersect(bottom)
                    .Intersect(left)
                    .Intersect(right)
                    .First();


                initials[node] = node.Face[initialIndex];
            }
            catch (Exception ex)
            {

            }
        }

        public static List<int> GetPossibleInitialsHorizontal(Adapter adapter, Dictionary<TopologyNode, Vertex> initials)
        {
            if (initials.TryGetValue(adapter.Neighbour, out var initial))
            {
                int adaptedIndex = adapter.IndexOf(initial);

                if (adaptedIndex == 0 || adaptedIndex == 3)
                {
                    return new List<int> { 0, 3 };
                }
                else
                {
                    return new List<int> { 1, 2 };
                }
            }

            return new List<int> { 0, 1, 2, 3 };
        }

        public static List<int> GetPossibleInitialsVertical(Adapter adapter, Dictionary<TopologyNode, Vertex> initials)
        {
            if (initials.TryGetValue(adapter.Neighbour, out var initial))
            {
                int adaptedIndex = adapter.IndexOf(initial);

                if (adaptedIndex == 0 || adaptedIndex == 1)
                {
                    return new List<int> { 0, 1 };
                }
                else
                {
                    return new List<int> { 2, 3 };
                }
            }

            return new List<int> { 0, 1, 2, 3 };
        }

        public static GameObject CreatePoliesVisualization(Engine engine, Topology topology)
        {
            var go = engine.CreateGameObject();

            foreach (var node in topology)
            {
                var polyGo = engine.CreateGameObject();

                foreach (var edge in node.Face.EnumerateEdges())
                {
                    var edgeGo = engine.Line(edge.A.Position, edge.B.Position, Colors.Green);
                    polyGo.AddChild(edgeGo);
                }

                go.AddChild(polyGo);
            }

            return go;
        }

        public static byte[] CreateTexture(Topology topology, Dictionary<TopologyNode, Vertex> initials, int size)
        {
            var texture = new byte[size * size * 4];
            int step = 40;

            foreach (var node in topology)
            {
                bool isErrorFlag = !initials.ContainsKey(node);

                var initialIndex = isErrorFlag ? 0 : node.Face.IndexOf(initials[node]);
                var from = node.Face[initialIndex].TextureCoords * size;
                var to = node.Face.GetCircular(initialIndex + 2).TextureCoords * size;
                var direction = to - from;
                var bounds = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));
                var axis = new Vector2i(Math.Sign(direction.X), Math.Sign(direction.Y));

                var backgroundColor = isErrorFlag ? Color.Red : Color.White;

                for (int x = 0; x < bounds.X; x++)
                {
                    for (int y = 0; y < bounds.Y; y ++)
                    {
                        var position = from + new Vector2(x * axis.X, y * axis.Y);
                        texture.SetColor(size, (int)position.X, (int)position.Y, backgroundColor);
                    }
                }

                if (isErrorFlag)
                {
                    continue;
                }

                for (int x = 0; x < bounds.X; x += step)
                {
                    for (int y = 0; y < bounds.Y; y++)
                    {
                        var position = from + new Vector2(x * axis.X, y * axis.Y);
                        texture.SetColor(size, (int)position.X, (int)position.Y, Color.Black);
                    }
                }

                for (int x = 0; x < bounds.X; x++)
                {
                    for (int y = 0; y < bounds.Y; y += step)
                    {
                        var position = from + new Vector2(x * axis.X, y * axis.Y);
                        texture.SetColor(size, (int)position.X, (int)position.Y, Color.Black);
                    }
                }
            }

            return texture;
        }

        public static void Main(string[] args)
        {
            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var model = Model.Load("Content/Room.obj");

            //var roomGo = engine.CreateGameObject();
            //var roomRenderer = roomGo.Add<MaterialRenderComponent>();
            //roomRenderer.Model = model;

            //TODO: simplify ilands and check, that they have rectangular shape.

            var topology = new Topology(model.Meshes[0], 3);
            var dirtyPolies = ExtractPolies(topology);
            var cleanPolies = CleanUpPolies(dirtyPolies);
            var retopology = new Topology(cleanPolies);
            var initials = SliceSurfaces(retopology);

            var roomGo = engine.CreateGameObject();
            var roomRenderer = roomGo.Add<MaterialRenderComponent>();
            roomRenderer.Model = model;
            int size = 2048;
            roomRenderer.Texture = Texture.LoadFromMemory(CreateTexture(retopology, initials, size), size, size);
            roomGo.Position = 5 * Vector3.UnitY;

            //var visualization = CreatePoliesVisualization(engine, retopology);
            //visualization.Position = 5 * Vector3.UnitY;

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);

            engine.Run();
        }
    }
}