using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;
using TriangulatedTopology.RulesAdapters;
using TriangulatedTopology.TextureIsland;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using TriangulatedTopology.Wfc;
using TriangulatedTopology.Props;
using TriangulatedTopology.Props.Algorithms;

namespace TriangulatedTopology
{
    public enum RotationDirection
    {
        Positive,
        Negative,
    }

    public class CoordSystem
    {
        public Vector2i Origin;
        public Vector2i XAxis;
        public Vector2i YAxis;

        public CoordSystem(Vector2i origin, Vector2i xAxis, Vector2i yAxis)
        {
            Origin = origin;
            XAxis = xAxis;
            YAxis = yAxis;
        }
    }

    public class Program
    {
        public const int LogicalResolution = 4;
        public const int DetailedResolution = 20;

        public static List<Island> CreateIslands(Topology topology, int size, int step)
        {
            var islands = new List<Island>();
            var groups = topology.ExtractFacesGroups((reference, node)
                => reference.Face.IsSharedUVEdgeExist(node.Face));

            foreach (var group in groups)
            {
                var edges = ExtractOuterEdges(group);
                var loop = ConnectIntoLoop(edges);
                var sides = LoopToSides(loop);
                var corners = ExtractCorners(sides);
                var grid = CreateGrid(corners, size, step);
                var island = new Island(grid, sides.ToArray());
                islands.Add(island);
            }

            ConnectCells(islands, size, step);
            return islands;
        }

        public static HashSet<Edge> ExtractOuterEdges(List<TopologyNode> island)
        {
            var repeates = new HashSet<Edge>(new EdgeComparer());
            var edges = new HashSet<Edge>(new EdgeComparer());

            foreach (var node in island)
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
            return edges;
        }

        public static List<Edge> ConnectIntoLoop(HashSet<Edge> edges)
        {
            var loop = new List<Edge>();
            var temp = edges.First();

            while (edges.Count > 0)
            {
                edges.Remove(temp);
                loop.Add(temp);

                foreach (var other in edges)
                {
                    if (other.A == temp.B)
                    {
                        temp = other;
                        break;
                    }
                }
            }

            return loop;
        }

        public static List<Side> LoopToSides(List<Edge> loop)
        {
            var sides = new List<Side>();
            int initial = 0;

            // Find any corner.
            for (; initial < loop.Count; initial++)
            {
                var prev = loop.GetCircular(initial);
                var next = loop.GetCircular(initial + 1);
                var prevDirection = Vector2.Normalize(prev.B.TextureCoords - prev.A.TextureCoords);
                var nextDirection = Vector2.Normalize(next.B.TextureCoords - next.A.TextureCoords);
                
                if (prevDirection != nextDirection)
                {
                    break;
                }
            }

            initial += 1;

            // Create sides.
            var edges = new List<Edge>();

            for (int i = 0; i < loop.Count; i++)
            {
                var prev = loop.GetCircular(initial + i);
                var next = loop.GetCircular(initial + i + 1);
                var prevDirection = Vector2.Normalize(prev.B.TextureCoords - prev.A.TextureCoords);
                var nextDirection = Vector2.Normalize(next.B.TextureCoords - next.A.TextureCoords);

                edges.Add(prev);

                if (prevDirection != nextDirection)
                {
                    var side = new Side(edges);
                    sides.Add(side);
                    edges.Clear();
                }
            }

            return sides;
        }

        public static List<Vertex> ExtractCorners(List<Side> sides)
        {
            var corners = new List<Vertex>();

            foreach (var side in sides)
            {
                corners.Add(side[0].A);
            }

            return corners;
        }

        public static Cell[,] CreateGrid(IReadOnlyList<Vertex> corners, int size, int step)
        {
            var normal = GetNormal(corners);
            var prev = corners[0].TextureCoords * size;
            var from = corners[1].TextureCoords * size;
            var next = corners[2].TextureCoords * size;

            var xDirection = prev - from;
            var yDirection = next - from;

            var xLength = xDirection.Length;
            var yLength = yDirection.Length;

            var xAxis = xDirection.Normalized();
            var yAxis = yDirection.Normalized();

            int width = (int)MathHelper.Ceiling(xLength / step);
            int height = (int)MathHelper.Ceiling(yLength / step);
            var grid = new Cell[width, height];

            var dx = step * xAxis;
            var dy = step * yAxis;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pivot = from + (step * x * xAxis) + (step * y * yAxis);
                    var vertices = new List<Vector2>()
                    {
                        pivot + dx,
                        pivot,
                        pivot + dy,
                        pivot + dx + dy,
                    };

                    grid[x, y] = new Cell(vertices, normal);
                }
            }

            return grid;
        }

        public static Vector3 GetNormal(IReadOnlyList<Vertex> face)
        {
            var a = Vector3.Normalize(face[0].Position - face[1].Position);
            var b = Vector3.Normalize(face[2].Position - face[1].Position);
            return Vector3.Cross(a, b).Normalized();
        }

        public static void ConnectCells(List<Island> islands, int size, int step)
        {
            foreach (var island in islands)
            {               
                ConnectCellsOnIsland(island);
                ConnectCellsBetweenIslands(island, islands, size, step);
            }
        }

        public static void ConnectCellsOnIsland(Island island)
        {
            for (int x = 0; x < island.Grid.GetLength(0) - 1; x++)
            {
                for (int y = 0; y < island.Grid.GetLength(1) - 1; y++)
                {
                    var cell = island.Grid[x, y];
                    var right = island.Grid[x + 1, y];
                    var bottom = island.Grid[x, y + 1];

                    cell.Neighbours[3] = new NeighbourData(right, new RuleEmptyAdapter(LogicalResolution));
                    right.Neighbours[1] = new NeighbourData(cell, new RuleEmptyAdapter(LogicalResolution));

                    cell.Neighbours[2] = new NeighbourData(bottom, new RuleEmptyAdapter(LogicalResolution));
                    bottom.Neighbours[0] = new NeighbourData(cell, new RuleEmptyAdapter(LogicalResolution));
                }
            }

            int lastX = island.Grid.GetLength(0) - 1;
            int lastY = island.Grid.GetLength(1) - 1;

            // Bottom side.
            for (int x = 0; x < island.Grid.GetLength(0) - 1; x++)
            {
                var cell = island.Grid[x, lastY];
                var right = island.Grid[x + 1, lastY];

                cell.Neighbours[3] = new NeighbourData(right, new RuleEmptyAdapter(LogicalResolution));
                right.Neighbours[1] = new NeighbourData(cell, new RuleEmptyAdapter(LogicalResolution));
            }

            // Right side.
            for (int y = 0; y < island.Grid.GetLength(1) - 1; y++)
            {
                var cell = island.Grid[lastX, y];
                var bottom = island.Grid[lastX, y + 1];

                cell.Neighbours[2] = new NeighbourData(bottom, new RuleEmptyAdapter(LogicalResolution));
                bottom.Neighbours[0] = new NeighbourData(cell, new RuleEmptyAdapter(LogicalResolution));
            }
        }

        public static void ConnectCellsBetweenIslands(Island island, List<Island> islands, int size, int step)
        {
            for (int sideIndex = 0; sideIndex < island.Sides.Length; sideIndex++)
            {
                var side = island.Sides[sideIndex];

                foreach (var segment in side)
                {
                    var thisCells = island.GetCorrespodingCells(segment.A.Position, segment.B.Position, size, step);
                    var neighbour = GetNeighbourIsland(island, islands, segment);
                    var neighbourCells = neighbour.GetCorrespodingCells(segment.A.Position, segment.B.Position, size, step);
                    GetAdapterBasis(sideIndex, segment, neighbour, out var origin, out var xAxis, out var yAxis);

                    for (int i = 0; i < thisCells.Count; i++)
                    {
                        var thisCell = thisCells[i];
                        var neighbourCell = neighbourCells[i];
                        thisCell.Neighbours[sideIndex] = new NeighbourData(neighbourCell, new RuleRotationAdapter(origin, xAxis, yAxis, LogicalResolution));
                    }
                }
            }
        }

        // This method is where all the magic happens...
        public static void GetAdapterBasis(
            int sideIndex, 
            Edge segment, 
            Island neighbour, 
            out Vector2i origin, 
            out Vector2i xAxis, 
            out Vector2i yAxis)
        {
            neighbour.TryGetSegmentAndSide(segment.A.Position, segment.B.Position, out var _, out var neighbourSide);
            var expectedAnchorNeighbourVertexIndex = (4 + (sideIndex - 1 % 4)) % 4;
            var (A, B) = GetExpectedNeighbourSideVerticesOrder(segment, neighbourSide!);

            var anchorNeighbourVertex = neighbour.Corners.First(v => v.Position == A);
            var anchorNeighbourVertexIndex = neighbour.Corners.IndexOf(anchorNeighbourVertex);

            var secondaryNeighbourVertex = neighbour.Corners.First(v => v.Position == B);
            var secondaryNeighbourVertexIndex = neighbour.Corners.IndexOf(secondaryNeighbourVertex);

            bool isShouldTranspose = (secondaryNeighbourVertexIndex + 1) % 4 != anchorNeighbourVertexIndex;
            int factor = anchorNeighbourVertexIndex - expectedAnchorNeighbourVertexIndex;
            int rotations = MathHelper.Abs(factor);
            var direction = factor < 0
                ? RotationDirection.Negative
                : RotationDirection.Positive;

            var bases = new CoordSystem[]
            {
                new(new Vector2i(0, 0),
                    new Vector2i(1, 0),
                    new Vector2i(0, 1)),
                new(new Vector2i(0, LogicalResolution - 1),
                    new Vector2i(0, -1),
                    new Vector2i(1, 0)),
                new(new Vector2i(LogicalResolution - 1, LogicalResolution - 1),
                    new Vector2i(-1, 0),
                    new Vector2i(0, -1)),
                new(new Vector2i(LogicalResolution - 1, 0),
                    new Vector2i(0, 1),
                    new Vector2i(-1, 0)),
            };

            switch (direction)
            {
                case RotationDirection.Negative:
                    for (int i = 0; i < rotations; i++)
                    {
                        bases.ShiftRight();
                    }

                    break;
                case RotationDirection.Positive:
                    for (int i = 0; i < rotations; i++)
                    {
                        bases.ShiftLeft();
                    }

                    break;
            }

            origin = bases[0].Origin;
            xAxis = bases[0].XAxis;
            yAxis = bases[0].YAxis;

            if (isShouldTranspose)
            {
                var temp = xAxis;
                xAxis = yAxis;
                yAxis = temp;
            }            
        }

        public static (Vector3 A, Vector3 B) GetExpectedNeighbourSideVerticesOrder(Edge pivotSegment, Side neighbourSide)
        {
            Vector3 A = neighbourSide.A.Position;
            Vector3 B = neighbourSide.B.Position;

            float aDistance = (neighbourSide.A.Position - pivotSegment.A.Position).Length;
            float bDistance = (neighbourSide.A.Position - pivotSegment.B.Position).Length;

            if (aDistance > bDistance)
            {
                var temp = A;
                A = B;
                B = temp;
            }

            return (A, B);
        }

        public static Island GetNeighbourIsland(Island island, List<Island> islands, Edge segment)
        {
            return islands.First(o => !o.Equals(island) && o.IsContainsSegment(segment.A.Position, segment.B.Position));
        }

        public static bool IsAllColorsSameInWindow(Color[,] rule, int x, int y, int size, Color color)
        {
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (!rule[x + i, y + j].IsSame(color))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static List<Cell> IslandsToCells(List<Island> islands)
        {
            var cells = new List<Cell>();

            foreach (var island in islands)
            {
                foreach (var cell in island.Grid.Enumerate())
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        public static void Main(string[] args)
        {
            var random = new Random();
            int seed = random.Next();
            Console.WriteLine(seed);
            CollectionsHelper.UseSeed(seed);

            //CollectionsHelper.UseSeed(1628667546);1176043099
            //CollectionsHelper.UseSeed(1145917631);
            //CollectionsHelper.UseSeed(56224625);
            //CollectionsHelper.UseSeed(935418399);
            //CollectionsHelper.UseSeed(1310155548);
            //CollectionsHelper.UseSeed(887817102);
            //CollectionsHelper.UseSeed(1902612879); 
            //CollectionsHelper.UseSeed(113611668); // Провод на лестнице
            //CollectionsHelper.UseSeed(580746622);2117749366
            //CollectionsHelper.UseSeed(2117749366);

            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var model = Model.Load("Content/Models/TriangulatedTower.obj");

            int size = 2048;
            int step = 32;

            var topology = new Topology(model.Meshes[0], 3);
            var islands = CreateIslands(topology, size, step);

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

            var cells = IslandsToCells(islands);
            WfcGenerator.GraphWfc(cells, wallRules, floorRules, ceilRules);

            float extrusion = 0.05f;

            var propsGenerator = new PropsGenerator()
                .PushCellAlgorithm(new PanelsGeneratorAlgorithm(extrusion))
                .PushNetAlgorithm(new WiresGeneratorAlgorithm(extrusion))
                .PushNetAlgorithm(new PipesGeneratorAlgorithm(extrusion))
                .PushNetAlgorithm(new VentilationGeneratorAlgorithm(extrusion));
            propsGenerator.Generate(engine, topology, cells, size);

            //var nets = ExtractNets(topology, cells, size);
            //VisualizeNets(engine, nets);
            //GenerateDetails(engine, topology, cells, size);

            var texture = TextureCreator.CreateDetailedTexture(cells, size, step);
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