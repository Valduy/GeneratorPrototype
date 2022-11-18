using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using TextureUtils;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using System.Drawing;
using Mathematics = GameEngine.Mathematics.Mathematics;
using Face = MeshTopology.Face;
using TriangulatedTopology.RulesAdapters;
using GameEngine.Mathematics;

namespace TriangulatedTopology
{
    public class Program
    {
        public const int LogicalResolution = 4;
        public const int DetailedResolution = 20;

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

        //public static float GetTileSize(Topology topology, float size)
        //{
        //    var lengths = new List<int>();
            
        //    foreach (var node in topology)
        //    {
        //        foreach (var edge in node.Face.EnumerateEdges())
        //        {
        //            var difference = edge.B.TextureCoords - edge.A.TextureCoords;
        //            var length = (int)(difference.Length * size);
        //            lengths.Add(length);
        //        }
        //    }

        //    for (int i = 1; i < lengths.Count; i++)
        //    {
        //        var a = lengths[i - 1];
        //        var b = lengths[i - 0];
        //        var gcd = Mathematics.Euclid(a, b);
        //        lengths[i] = gcd;
        //    }

        //    return lengths.Last();
        //}

        public static Dictionary<TopologyNode, Vertex> SliceSurfaces(Topology topology)
        {
            var initials = new Dictionary<TopologyNode, Vertex>();
            var visited = new HashSet<TopologyNode> { topology[0] };
            var queue = new Queue<TopologyNode>();
            queue.Enqueue(topology[0]);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                SliceSurface(node, initials);

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

        public static void SliceSurface(TopologyNode node, Dictionary<TopologyNode, Vertex> initials)
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
                    var prev = node.Face.GetCircular(initialIndex - 1).TextureCoords * size;
                    var from = node.Face.GetCircular(initialIndex).TextureCoords * size;
                    var next = node.Face.GetCircular(initialIndex + 1).TextureCoords * size;
                    var to = node.Face.GetCircular(initialIndex + 2).TextureCoords * size;

                    var direction = to - from;
                    var altBounds = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));

                    var x_direction = prev - from;
                    var y_direction = next - from;

                    var x_length = x_direction.Length;
                    var y_length = y_direction.Length;

                    var x_axis = x_direction.Normalized();
                    var y_axis = y_direction.Normalized();

                    int width = (int)MathHelper.Ceiling(x_length / step);
                    int height = (int)MathHelper.Ceiling(y_length / step);
                    var grid = new Cell[width, height];

                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            Vector2 aa_x = step * x * x_axis;
                            Vector2 aa_y = step * y * y_axis;
                            Vector2 bb_x = (x + 1 < width) ? step * (x + 1) * x_axis : x_length * x_axis;
                            Vector2 bb_y = (y + 1 < height) ? step * (y + 1) * y_axis : y_length * y_axis;

                            var aa = from + aa_x + aa_y;
                            var bb = from + bb_x + bb_y;                            
                            
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
                        ? Orientation.Сounterclockwise
                        : Orientation.Clockwise;

                    switch (rotationDirection)
                    {
                        case Orientation.Clockwise:
                            for (int i = 0; i < rotationCount; i++)
                            {
                                grid = grid.RotateMatrixClockwise();
                            }

                            break;
                        case Orientation.Сounterclockwise:
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

        public static void ConnectCells(Dictionary<TopologyNode, Cell[,]> grids, int size)
        {
            foreach (var pair in grids)
            {
                var node = pair.Key;
                var grid = pair.Value;

                for (int x = 0; x < grid.GetLength(0) - 1; x++)
                {
                    for (int y = 0; y < grid.GetLength(1) - 1; y++)
                    {
                        var cell = grid[x, y];
                        var right = grid[x + 1, y];
                        var bottom = grid[x, y + 1];

                        cell.Neighbours[3] = new NeighbourData(right, new RuleEmptyAdapter());
                        right.Neighbours[1] = new NeighbourData(cell, new RuleEmptyAdapter());

                        cell.Neighbours[2] = new NeighbourData(bottom, new RuleEmptyAdapter());
                        bottom.Neighbours[0] = new NeighbourData(cell, new RuleEmptyAdapter());
                    }
                }

                for (int x = 0; x < grid.GetLength(0) - 1; x++)
                {
                    int y = grid.GetLength(1) - 1;
                    var cell = grid[x, y];
                    var right = grid[x + 1, y];

                    cell.Neighbours[3] = new NeighbourData(right, new RuleEmptyAdapter());
                    right.Neighbours[1] = new NeighbourData(cell, new RuleEmptyAdapter());
                }

                for (int y = 0; y < grid.GetLength(1) - 1; y++)
                {
                    int x = grid.GetLength(0) - 1;
                    var cell = grid[x, y];
                    var bottom = grid[x, y + 1];

                    cell.Neighbours[2] = new NeighbourData(bottom, new RuleEmptyAdapter());
                    bottom.Neighbours[0] = new NeighbourData(cell, new RuleEmptyAdapter());
                }

                for (int i = 0; i < node.Neighbours.Count; i++)
                {
                    var neighbour = node.Neighbours[i];

                    if (grids.TryGetValue(neighbour, out var neighbourGrid))
                    {
                        var neighbourSharedEdge = neighbour.Face.GetSharedEdge(node.Face);
                        var gridSharedEdge = node.Face.GetSharedEdge(neighbour.Face);

                        var fromTextureCoords = GetTextureCoords(neighbourSharedEdge, gridSharedEdge.A.Position);
                        var toTextureCoords = GetTextureCoords(neighbourSharedEdge, gridSharedEdge.B.Position);

                        var from = GetCornerByUV(neighbourGrid, fromTextureCoords * size);
                        var to = GetCornerByUV(neighbourGrid, toTextureCoords * size);
                        var temp = from;

                        var step = to - from;
                        step.X = MathHelper.Sign(step.X);
                        step.Y = MathHelper.Sign(step.Y);

                        foreach (var cell in EnumerateGridSide(grid, i))
                        {
                            var link = neighbourGrid[temp.X, temp.Y];
                            cell.Neighbours[i] = new NeighbourData(link, new RuleEmptyAdapter());
                            temp += step;
                        }
                    }
                }
            }
        }

        public static Vector2 GetTextureCoords(Edge edge, Vector3 position)
        {
            if (edge.A.Position == position)
            {
                return edge.A.TextureCoords;
            }
            if (edge.B.Position == position) 
            {
                return edge.B.TextureCoords;
            }

            throw new ArgumentException($"Edge is not contain vertex with position {position}.");
        }

        public static Vector2i GetCornerByUV(Cell[,] grid, Vector2 uv)
        {
            float epsilon = 0.1f;

            if (grid[0, 0].Any(p => (uv - p).Length < epsilon))
            {
                return new Vector2i(0, 0);
            }
            if (grid[grid.GetLength(0) - 1, 0].Any(p => (uv - p).Length < epsilon))
            {
                return new Vector2i(grid.GetLength(0) - 1, 0);
            }
            if (grid[grid.GetLength(0) - 1, grid.GetLength(1) - 1].Any(p => (uv - p).Length < epsilon))
            {
                return new Vector2i(grid.GetLength(0) - 1, grid.GetLength(1) - 1);
            }
            if (grid[0, grid.GetLength(1) - 1].Any(p => (uv - p).Length < epsilon))
            {
                return new Vector2i(0, grid.GetLength(1) - 1);
            }

            throw new ArgumentException($"There is no corner with uv {uv}.");
        }

        public static IEnumerable<Cell> EnumerateGridSide(Cell[,] grid, int side)
        {
            switch (side)
            {
                case 0:
                    for (int x = grid.GetLength(0) - 1; x >= 0; x--)
                    {
                        yield return grid[x, 0];
                    }
                    break;
                case 1:
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        yield return grid[0, y];
                    }
                    break;
                case 2:
                    for (int x = 0; x < grid.GetLength(0); x++)
                    {
                        yield return grid[x, grid.GetLength(1) - 1];
                    }
                    break;
                case 3:
                    for (int y = grid.GetLength(1) - 1; y >= 0; y--)
                    {
                        yield return grid[grid.GetLength(0) - 1, y];
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        public static void Wfc(Dictionary<TopologyNode, Cell[,]> grids, List<Rule> rules)
        {
            foreach (var pair in grids)
            {
                var grid = pair.Value;
                var defaultRule = rules[33];
                var forRecalculation = new List<Vector2i>();

                if (grid.GetLength(0) <= 2 || grid.GetLength(1) <= 2)
                {
                    foreach (var cell in grid.Enumerate())
                    {
                        cell.Rules.Clear();
                        cell.Rules.Add(defaultRule);
                    }

                    continue;
                }

                foreach (var ceil in grid.Enumerate())
                {
                    ceil.Rules.Clear();
                    ceil.Rules.AddRange(rules);
                }

                // Tob and bottom borders
                for (int x = 1; x < grid.GetLength(0) - 1; x++)
                {
                    grid[x, 0].Rules.Clear();
                    grid[x, 0].Rules.Add(defaultRule);
                    forRecalculation.Add(new Vector2i(x, 1));

                    grid[x, grid.GetLength(1) - 1].Rules.Clear();
                    grid[x, grid.GetLength(1) - 1].Rules.Add(defaultRule);
                    forRecalculation.Add(new Vector2i(x, grid.GetLength(1) - 2));
                }

                // Left and right borders
                for (int y = 1; y < grid.GetLength(1) - 1; y++)
                {
                    grid[0, y].Rules.Clear();
                    grid[0, y].Rules.Add(defaultRule);
                    forRecalculation.Add(new Vector2i(1, y));

                    grid[grid.GetLength(0) - 1, y].Rules.Clear();
                    grid[grid.GetLength(0) - 1, y].Rules.Add(defaultRule);
                    forRecalculation.Add(new Vector2i(grid.GetLength(0) - 2, y));
                }

                // Corners
                grid[0, 0].Rules.Clear();
                grid[0, 0].Rules.Add(defaultRule);

                grid[0, grid.GetLength(1) - 1].Rules.Clear();
                grid[0, grid.GetLength(1) - 1].Rules.Add(defaultRule);

                grid[grid.GetLength(0) - 1, 0].Rules.Clear();
                grid[grid.GetLength(0) - 1, 0].Rules.Add(defaultRule);

                grid[grid.GetLength(0) - 1, grid.GetLength(1) - 1].Rules.Clear();
                grid[grid.GetLength(0) - 1, grid.GetLength(1) - 1].Rules.Add(defaultRule);

                while (true)
                {
                    while (forRecalculation.Count > 0)
                    {
                        var coords = forRecalculation[0];
                        forRecalculation.Remove(coords);
                        var cell = grid[coords.X, coords.Y];
                        var filtered = FilterPossible(grid, coords);

                        if (filtered.Count == 0)
                        {
                            cell.Rules.Clear();
                            cell.Rules.AddRange(rules);

                            foreach (var neighbourCoords in GetNeighboursCoords(grid, coords, 1))
                            {
                                var neighbour = grid[neighbourCoords.X, neighbourCoords.Y];
                                neighbour.Rules.Clear();
                                neighbour.Rules.AddRange(rules);
                            }

                            continue;
                        }

                        if (cell.Rules.Count > filtered.Count)
                        {
                            forRecalculation.AddRange(GetNeighboursCoords(grid, coords, 1));
                            cell.Rules.Clear();
                            cell.Rules.AddRange(filtered);
                        }
                    }

                    var maxCellCoord = new Vector2i(1, 1);
                    var maxCell = grid[maxCellCoord.X, maxCellCoord.Y];

                    for (int x = 1; x < grid.GetLength(0) - 1; x++)
                    {
                        for (int y = 1; y < grid.GetLength(1) - 1; y++)
                        {
                            var cell = grid[x, y];

                            if (cell.Rules.Count > maxCell.Rules.Count)
                            {
                                maxCellCoord = new Vector2i(x, y);
                                maxCell = cell;
                            }
                        }
                    }

                    if (maxCell.Rules.Count <= 1)
                    {
                        break;
                    }

                    var rule = maxCell.Rules.GetRandom();
                    maxCell.Rules.Clear();
                    maxCell.Rules.Add(rule);
                    forRecalculation.AddRange(GetNeighboursCoords(grid, maxCellCoord, 1));
                }
            }
        }

        private static IEnumerable<Vector2i> GetNeighboursCoords<T>(T[,] matrix, Vector2i coords, int padding = 0)
        {
            if (coords.Y > padding)
            {
                yield return new Vector2i(coords.X, coords.Y - 1);
            }
            if (coords.X > padding)
            {
                yield return new Vector2i(coords.X - 1, coords.Y);
            }
            if (coords.Y + 1 < matrix.GetLength(1) - padding)
            {
                yield return new Vector2i(coords.X, coords.Y + 1);
            }
            if (coords.X + 1 < matrix.GetLength(0) - padding)
            {
                yield return new Vector2i(coords.X + 1, coords.Y);
            }
        }

        public static List<Rule> FilterPossible(Cell[,] grid, Vector2i coords)
        {
            var cell = grid[coords.X, coords.Y];
            var neighboursCoords = GetNeighboursCoords(grid, coords).ToList();
            return cell.Rules.Where(r => IsPossible(grid, r, neighboursCoords)).ToList();
        }           

        public static bool IsPossible(Cell[,] grid, Rule rule, List<Vector2i> neighboursCoords)
        {
            for (int neighbourIndex = 0; neighbourIndex < neighboursCoords.Count; neighbourIndex++)
            {
                var coords = neighboursCoords[neighbourIndex];                
                var neighbour = grid[coords.X, coords.Y];
                var rules = neighbour.Rules;
                var nodeIndex = (neighbourIndex + 2) % 4;

                if (rules.All(r => !IsSame(r[nodeIndex], rule[neighbourIndex])))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsSame(Color[] lhs, Color[] rhs)
        {
            if (lhs.Length != rhs.Length)
            {
                throw new ArgumentException();
            }

            for (int i = 0; i < lhs.Length; i++)
            {
                if (!lhs[i].IsSame(rhs[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static void Main(string[] args)
        {
            //CollectionsHelper.UseSeed(96350589);

            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var model = Model.Load("Content/Room.obj");
            //var model = Model.Load("Content/Cube.obj");

            int size = 2048;
            int step = 40;

            var topology = new Topology(model.Meshes[0], 3);
            var dirtyPolies = ExtractPolies(topology);
            var cleanPolies = CleanUpPolies(dirtyPolies);
            var retopology = new Topology(cleanPolies);
            var initials = SliceSurfaces(retopology);
            var grids = BuildCells(retopology, initials, size, step);
            ConnectCells(grids, size);

            var roomGo = engine.CreateGameObject();
            var roomRenderer = roomGo.Add<MaterialRenderComponent>();
            roomRenderer.Model = model;   
            roomGo.Position = 5 * Vector3.UnitY;

            //var texture = TextureCreator.CreateGridTexture(retopology, initials, size, step);
            //roomRenderer.Texture = Texture.LoadFromMemory(texture, size, size);
            //var bmp = TextureHelper.TextureToBitmap(texture, size);
            //bmp.Save("Test.bmp");

            //var texture = TextureCreator.CreateDebugGridTexture(retopology, initials, grids, size, step);
            //roomRenderer.Texture = Texture.LoadFromMemory(texture, size, size);
            //var bmp = TextureHelper.TextureToBitmap(texture, size);
            //bmp.Save("Test.bmp");

            //var texture = TextureCreator.CreateDebugCellTexture(grids, initials, size, step);
            //roomRenderer.Texture = Texture.LoadFromMemory(texture, size, size);
            //var bmp = TextureHelper.TextureToBitmap(texture, size);
            //bmp.Save("Test.bmp");

            var texture = TextureCreator.CreateDebugStitchesTexture(grids, initials, size, step);
            roomRenderer.Texture = Texture.LoadFromMemory(texture, size, size);
            var bmp = TextureHelper.TextureToBitmap(texture, size);
            bmp.Save("Test.bmp");

            //var rules = RulesLoader.CreateRules(
            //    "Content/WallLogical.png",
            //    "Content/WallLogical.png",
            //    LogicalResolution,
            //    LogicalResolution);

            //Wfc(grids, rules);

            //var texture = TextureCreator.CreateLogicalTexture(grids, initials, size, step);
            //roomRenderer.Texture = Texture.LoadFromMemory(texture, size, size);
            //var bmp = TextureHelper.TextureToBitmap(texture, size);
            //bmp.Save("Test.bmp");

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);

            engine.Run();
        }
    }
}