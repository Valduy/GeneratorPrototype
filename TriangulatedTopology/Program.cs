using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using TextureUtils;
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
            var topAdapter = new VertexAdapter(node, node.Neighbours[0]);
            var bottomAdapter = new VertexAdapter(node, node.Neighbours[2]);
            var leftAdapter = new VertexAdapter(node, node.Neighbours[1]);            
            var rightAdapter = new VertexAdapter(node, node.Neighbours[3]);

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
            catch (Exception)
            {
                Console.WriteLine("Can not resolve this surface");
            }
        }

        public static List<int> GetPossibleInitialsHorizontal(VertexAdapter adapter, Dictionary<TopologyNode, Vertex> initials)
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

        public static List<int> GetPossibleInitialsVertical(VertexAdapter adapter, Dictionary<TopologyNode, Vertex> initials)
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

        public static Dictionary<TopologyNode, Cell[,]> BuildCells(
            Topology topology, 
            Dictionary<TopologyNode, Vertex> initials,
            int size,
            int step)
        {
            var grids = new Dictionary<TopologyNode, Cell[,]>();

            foreach (var node in topology)
            {
                if (initials.TryGetValue(node, out var initial))
                {
                    var iland = node.Face.Select(v => v.TextureCoords * size).ToList();

                    var initialIndex = node.Face.IndexOf(initial);
                    var from = node.Face[initialIndex].TextureCoords * size;
                    var to = node.Face.GetCircular(initialIndex + 2).TextureCoords * size;
                    var direction = to - from;
                    var bounds = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));
                    var axis = new Vector2i(MathF.Sign(direction.X), MathF.Sign(direction.Y));

                    int width = (int)MathHelper.Ceiling(bounds.X / step);
                    int height = (int)MathHelper.Ceiling(bounds.Y / step);
                    var grid = new Cell[width, height];

                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float aa_x = step * x * axis.X;
                            float aa_y = step * y * axis.Y;
                            float bb_x = (x + 1 < width) ? step * (x + 1) * axis.X : bounds.X * axis.X;
                            float bb_y = (y + 1 < height) ? step * (y + 1) * axis.Y : bounds.Y * axis.Y;

                            var aa = from + new Vector2(aa_x, aa_y);
                            var bb = from + new Vector2(bb_x, bb_y);                            
                            
                            var vertices = new List<Vector2>() 
                            {
                                aa, new Vector2(bb.X, aa.Y),
                                bb, new Vector2(aa.X, bb.Y),
                            };

                            var sortedVertices = new List<Vector2>(vertices);

                            for (int i = 0; i < iland.Count; i++)
                            {
                                double distance = Vector2d.Distance(vertices[0], iland[i]);
                                Vector2 vertex = vertices[0];

                                for (int j = 1; j < vertices.Count; j++)
                                {
                                    double tempDistance = Vector2d.Distance(vertices[j], iland[i]);

                                    if (tempDistance < distance)
                                    {
                                        vertex = vertices[j];
                                        distance = tempDistance;
                                    }
                                }

                                sortedVertices[i] = vertex;
                            }

                            grid[x, y] = new Cell(sortedVertices);
                        }
                    }

                    int expectedIndex = 1;
                    int actualIndex = node.Face.IndexOf(initial);
                    int factor = actualIndex - expectedIndex;

                    int rotationCount = MathHelper.Abs(factor);
                    var rotationDirection = factor < 0 
                        ? RotationDirection.Сounterclockwise
                        : RotationDirection.Clockwise;

                    switch (rotationDirection)
                    {
                        case RotationDirection.Clockwise:
                            for (int i = 0; i < rotationCount; i++)
                            {
                                grid = grid.RotateMatrixClockwise();
                            }

                            break;
                        case RotationDirection.Сounterclockwise:
                            for (int i = 0; i < rotationCount; i++)
                            {
                                grid = grid.RotateMatrixCounterClockwise();
                            }

                            break;
                    }

                    grids[node] = grid;
                }
            }

            return grids;
        }

        public static void ConnectCells(Dictionary<TopologyNode, Cell[,]> grids)
        {
            foreach (var grid in grids.Values)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        var cell = grid[x, y];
                        var right = (x + 1 < grid.GetLength(0)) ? grid[x + 1, y] : null;
                        var bottom = (y + 1 < grid.GetLength(1)) ? grid[x, y + 1] : null;
                        cell.Right = right;
                        cell.Bottom = bottom;

                        if (right != null)
                        {
                            right.Left = cell;
                        }                       

                        if (bottom != null)
                        {
                            bottom.Top = cell;
                        }                        
                    }
                }

                var t = 0;
            }

            // TODO: use cell adapters for cells (to rotate cells)
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

        public static byte[] CreateDebugTexture(
            Topology topology,
            Dictionary<TopologyNode, Vertex> initials,
            Dictionary<TopologyNode, Cell[,]> grids, 
            int size, 
            int step)
        {
            var texture = new byte[size * size * 4];

            foreach (var node in topology)
            {
                var from = node.Face[0].TextureCoords * size;
                var to = node.Face.GetCircular(2).TextureCoords * size;
                var direction = to - from;
                var bounds = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));
                var axis = new Vector2i(Math.Sign(direction.X), Math.Sign(direction.Y));

                for (int x = 0; x < bounds.X; x++)
                {
                    for (int y = 0; y < bounds.Y; y++)
                    {
                        var position = from + new Vector2(x * axis.X, y * axis.Y);
                        texture.SetColor(size, (int)position.X, (int)position.Y, Color.Purple);
                    }
                }

                if (grids.TryGetValue(node, out var grid))
                {

                    for (int x = 0; x < bounds.X; x++)
                    {
                        for (int y = 0; y < bounds.Y; y++)
                        {
                            var position = from + new Vector2(x * axis.X, y * axis.Y);
                            texture.SetColor(size, (int)position.X, (int)position.Y, Color.White);
                        }
                    }

                    var ilandBottomDirection = node.Face[2].TextureCoords - node.Face[1].TextureCoords;
                    var ilandBottomBound = (ilandBottomDirection * size).Length;
                    ilandBottomDirection.Normalize();

                    var ilandLeftDirection = node.Face[1].TextureCoords - node.Face[0].TextureCoords;
                    var rightBound = step / 8;
                    ilandLeftDirection.Normalize();

                    for (int x = 0; x < rightBound; x++)
                    {
                        for (int y = 0; y < ilandBottomBound; y++)
                        {
                            var position = from + x * ilandLeftDirection + y * ilandBottomDirection;
                            texture.SetColor(size, (int)position.X, (int)position.Y, Color.Red);
                        }
                    }

                    foreach (var cell in grid.Enumerate())
                    {
                        var cellBottomDirection = cell[3] - cell[0];
                        var cellBottomBound = cellBottomDirection.Length;
                        cellBottomDirection.Normalize();

                        var cellRightDirection = cell[0] - cell[1];
                        var cellLeftBound = step / 8;
                        cellRightDirection.Normalize();

                        for (int x = 0; x < cellLeftBound; x++)
                        {
                            for (int y = 0; y < cellBottomBound; y++)
                            {
                                var position = cell[1] + x * cellRightDirection + y * cellBottomDirection;
                                texture.SetColor(size, (int)position.X, (int)position.Y, Color.Green);
                            }
                        }
                    }

                    var initialIndex = node.Face.IndexOf(initials[node]);
                    from = node.Face[initialIndex].TextureCoords * size;
                    to = node.Face.GetCircular(initialIndex + 2).TextureCoords * size;
                    direction = to - from;
                    bounds = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));
                    axis = new Vector2i(Math.Sign(direction.X), Math.Sign(direction.Y));

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
            }

            return texture;
        }

        public static byte[] CreateTexture(Topology topology, Dictionary<TopologyNode, Vertex> initials, int size, int step)
        {
            var texture = new byte[size * size * 4];

            foreach (var node in topology)
            {
                if (initials.TryGetValue(node, out var initial))
                {
                    var initialIndex = node.Face.IndexOf(initial);
                    var from = node.Face[initialIndex].TextureCoords * size;
                    var to = node.Face.GetCircular(initialIndex + 2).TextureCoords * size;
                    var direction = to - from;
                    var bounds = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));
                    var axis = new Vector2i(Math.Sign(direction.X), Math.Sign(direction.Y));

                    for (int x = 0; x < bounds.X; x++)
                    {
                        for (int y = 0; y < bounds.Y; y++)
                        {
                            var position = from + new Vector2(x * axis.X, y * axis.Y);
                            texture.SetColor(size, (int)position.X, (int)position.Y, Color.White);
                        }
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
                else
                {
                    var from = node.Face[0].TextureCoords * size;
                    var to = node.Face[2].TextureCoords * size;
                    var direction = to - from;
                    var bounds = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));
                    var axis = new Vector2i(Math.Sign(direction.X), Math.Sign(direction.Y));

                    for (int x = 0; x < bounds.X; x++)
                    {
                        for (int y = 0; y < bounds.Y; y++)
                        {
                            var position = from + new Vector2(x * axis.X, y * axis.Y);
                            texture.SetColor(size, (int)position.X, (int)position.Y, Color.Red);
                        }
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

            int size = 2048;
            int step = 40;

            var topology = new Topology(model.Meshes[0], 3);
            var dirtyPolies = ExtractPolies(topology);
            var cleanPolies = CleanUpPolies(dirtyPolies);
            var retopology = new Topology(cleanPolies);
            var initials = SliceSurfaces(retopology);
            var grids = BuildCells(retopology, initials, size, step);
            ConnectCells(grids);

            var roomGo = engine.CreateGameObject();
            var roomRenderer = roomGo.Add<MaterialRenderComponent>();
            roomRenderer.Model = model;
            //roomRenderer.Texture = Texture.LoadFromMemory(CreateTexture(retopology, initials, size, step), size, size);
            var texture = CreateDebugTexture(retopology, initials, grids, size, step);
            roomRenderer.Texture = Texture.LoadFromMemory(texture, size, size);
            roomGo.Position = 5 * Vector3.UnitY;

            var bmp = TextureHelper.TextureToBitmap(texture, size);
            bmp.Save("Test.bmp");

            //var visualization = CreatePoliesVisualization(engine, retopology);
            //visualization.Position = 5 * Vector3.UnitY;

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);

            engine.Run();
        }
    }
}