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
            var possibilities = new List<int>[4];

            for (int i = 0; i < node.Neighbours.Count; i++)
            {
                var neighbour = node.Neighbours[i];

                if (initials.TryGetValue(neighbour, out var initial))
                {
                    var sharedEdge = node.Face.GetSharedEdge(neighbour.Face);
                    var neighbourSharedEdgeIndex = neighbour.Face.GetEdgeIndex(e => e.HasSamePositions(sharedEdge));
                    
                    var neighbourGuidesEdges = GetGuidesEdges(neighbour, neighbourSharedEdgeIndex);
                    var guidesEdges = GetGuidesEdges(node, i);

                    var neighbourGuide = neighbourGuidesEdges.First(e => e.HasVertex(initial));
                    var guide = guidesEdges.First(e => e.IsSharedVertexExist(neighbourGuide));

                    possibilities[i] = new List<int>()
                    {
                        node.Face.IndexOf(guide.A),
                        node.Face.IndexOf(guide.B),
                    };
                }
                else
                {
                    possibilities[i] = new List<int>() { 0, 1, 2, 3 };
                }
            }

            try
            {
                var possibleInitials = possibilities[0];

                for (int i = 1; i < possibilities.Length; i++)
                {
                    possibleInitials = possibleInitials.Intersect(possibilities[i]).ToList();
                }

                initials[node] = node.Face[possibleInitials.First()];
            }
            catch (Exception)
            {
                Console.WriteLine("Can not resolve this surface");
            }
        }

        public static List<Edge> GetGuidesEdges(TopologyNode node, int index)
        {
            var result = new List<Edge>();

            if (index % 2 == 0)
            {
                for (int i = 1; i < node.Neighbours.Count; i += 2)
                {
                    result.Add(node.Face.GetEdgeByIndex(i));
                }
            }
            else
            {
                for (int i = 0; i < node.Neighbours.Count; i += 2)
                {
                    result.Add(node.Face.GetEdgeByIndex(i));
                }
            }

            return result;
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

                        cell.Neighbours[3] = new NeighbourData(right, new RuleEmptyAdapter(LogicalResolution));
                        right.Neighbours[1] = new NeighbourData(cell, new RuleEmptyAdapter(LogicalResolution));

                        cell.Neighbours[2] = new NeighbourData(bottom, new RuleEmptyAdapter(LogicalResolution));
                        bottom.Neighbours[0] = new NeighbourData(cell, new RuleEmptyAdapter(LogicalResolution));
                    }
                }

                for (int x = 0; x < grid.GetLength(0) - 1; x++)
                {
                    int y = grid.GetLength(1) - 1;
                    var cell = grid[x, y];
                    var right = grid[x + 1, y];

                    cell.Neighbours[3] = new NeighbourData(right, new RuleEmptyAdapter(LogicalResolution));
                    right.Neighbours[1] = new NeighbourData(cell, new RuleEmptyAdapter(LogicalResolution));
                }

                for (int y = 0; y < grid.GetLength(1) - 1; y++)
                {
                    int x = grid.GetLength(0) - 1;
                    var cell = grid[x, y];
                    var bottom = grid[x, y + 1];

                    cell.Neighbours[2] = new NeighbourData(bottom, new RuleEmptyAdapter(LogicalResolution));
                    bottom.Neighbours[0] = new NeighbourData(cell, new RuleEmptyAdapter(LogicalResolution));
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
                            cell.Neighbours[i] = new NeighbourData(link, new RuleRotationAdapter(node, neighbour, LogicalResolution));
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

        public static void OrientationDebug(Dictionary<TopologyNode, Cell[,]> grids)
        {
            var pallete = new Color[]
            {
                Color.Blue,
                Color.Purple,
                Color.Magenta,
                Color.Coral,
                Color.Red,
                Color.Orange,
                Color.Yellow,
                Color.Lime,
                Color.Green,
                Color.Aqua,
                Color.Cyan,
            };

            foreach (var pair in grids)
            {
                var grid = pair.Value;

                foreach (var cell in grid)
                {
                    var defenition = new Color[LogicalResolution, LogicalResolution];

                    for (int x = 0; x < defenition.GetLength(0); x++)
                    {
                        for (int y = 0; y < defenition.GetLength(1); y++)
                        {
                            defenition[x, y] = Color.White;
                        }
                    }

                    cell.Rules.Add(new Rule(defenition, defenition));
                }
            }

            foreach (var pair in grids)
            {
                var node = pair.Key;
                var grid = pair.Value;

                if (grids.ContainsKey(node.Neighbours[0]))
                {
                    for (int x = 0; x < grid.GetLength(0); x++)
                    {
                        var cell = grid[x, 0];
                        var cellRule = cell.Rules[0];

                        var link = cell.Neighbours[0];
                        var adapter = link!.Adapter;
                        var linkRule = link!.Cell.Rules[0];

                        var semple = adapter.GetSide(linkRule, 2);

                        if (semple.All(c => c.IsSame(Color.White)))
                        {
                            for (int i = 0; i < cellRule.Logical.GetLength(0); i++)
                            {
                                cellRule.Logical[i, 0] = pallete.GetRandom();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < semple.Length; i++)
                            {
                                cellRule.Logical[i, 0] = semple[i];
                            }
                        }
                    }
                }

                if (grids.ContainsKey(node.Neighbours[1]))
                {
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        var cell = grid[0, y];
                        var cellRule = cell.Rules[0];

                        var link = cell.Neighbours[1];
                        var adapter = link!.Adapter;
                        var linkRule = link!.Cell.Rules[0];

                        var semple = adapter.GetSide(linkRule, 3);

                        if (semple.All(c => c.IsSame(Color.White)))
                        {
                            for (int i = 0; i < cellRule.Logical.GetLength(1); i++)
                            {
                                cellRule.Logical[0, i] = pallete.GetRandom();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < semple.Length; i++)
                            {
                                cellRule.Logical[0, i] = semple[i];
                            }
                        }
                    }
                }

                if (grids.ContainsKey(node.Neighbours[2]))
                {
                    for (int x = 0; x < grid.GetLength(0); x++)
                    {
                        var cell = grid[x, grid.GetLength(1) - 1];
                        var cellRule = cell.Rules[0];

                        var link = cell.Neighbours[2];
                        var adapter = link!.Adapter;
                        var linkRule = link!.Cell.Rules[0];

                        var semple = adapter.GetSide(linkRule, 0);

                        if (semple.All(c => c.IsSame(Color.White)))
                        {
                            for (int i = 0; i < cellRule.Logical.GetLength(0); i++)
                            {
                                cellRule.Logical[i, cellRule.Logical.GetLength(1) - 1] = pallete.GetRandom();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < semple.Length; i++)
                            {
                                cellRule.Logical[i, cellRule.Logical.GetLength(1) - 1] = semple[i];
                            }
                        }
                    }
                }

                if (grids.ContainsKey(node.Neighbours[3]))
                {
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        var cell = grid[grid.GetLength(0) - 1, y];
                        var cellRule = cell.Rules[0];

                        var link = cell.Neighbours[3];
                        var adapter = link!.Adapter;
                        var linkRule = link!.Cell.Rules[0];

                        var semple = adapter.GetSide(linkRule, 1);

                        if (semple.All(c => c.IsSame(Color.White)))
                        {
                            for (int i = 0; i < cellRule.Logical.GetLength(1); i++)
                            {
                                cellRule.Logical[cellRule.Logical.GetLength(0) - 1, i] = pallete.GetRandom();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < semple.Length; i++)
                            {
                                cellRule.Logical[cellRule.Logical.GetLength(0) - 1, i] = semple[i];
                            }
                        }
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            //CollectionsHelper.UseSeed(96350589);

            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            //var model = Model.Load("Content/Cube.obj");
            //var model = Model.Load("Content/Room.obj");
            //var model = Model.Load("Content/Line.obj");
            var model = Model.Load("Content/Corner.obj");

            int size = 2048;
            int step = 32;

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

            //var texture = TextureCreator.CreateDebugStitchesTexture(grids, initials, size, step);
            //roomRenderer.Texture = Texture.LoadFromMemory(texture, size, size);
            //var bmp = TextureHelper.TextureToBitmap(texture, size);
            //bmp.Save("Test.bmp");

            // WFC on graph
            var rules = RulesLoader.CreateRules(
                "Content/WallLogical.png",
                "Content/WallDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var horizontalRules = RulesLoader.CreateRules(
                "Content/HorizontalLogical.png",
                "Content/HorizontalDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var verticalRules = RulesLoader.CreateRules(
                "Content/VerticalLogical.png",
                "Content/VerticalDetailed.png",
                LogicalResolution,
                DetailedResolution);

            var cells = new List<Cell>();

            foreach (var node in grids)
            {
                var iland = node.Value;

                foreach (var cell in iland)
                {
                    cells.Add(cell);
                }
            }

            Wfc.GraphWfc(cells, rules, horizontalRules, verticalRules);

            var texture = TextureCreator.CreateDetailedTexture(grids, initials, size, step);
            roomRenderer.Texture = Texture.LoadFromMemory(texture, size, size);
            var bmp = TextureHelper.TextureToBitmap(texture, size);
            bmp.Save("Test.bmp");

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);
            engine.Run();
        }
    }
}