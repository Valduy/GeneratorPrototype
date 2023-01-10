using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using TextureUtils;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using Mathematics = GameEngine.Mathematics.Mathematics;
using Face = MeshTopology.Face;
using TriangulatedTopology.RulesAdapters;
using Assimp;
using System.Drawing;
using Quaternion = OpenTK.Mathematics.Quaternion;

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

        public static Dictionary<TopologyNode, Cell[,]> BuildCells(Topology topology, int size, int step)
        {
            var grids = new Dictionary<TopologyNode, Cell[,]>();

            foreach (var node in topology)
            {
                var normal = node.Face.GetNormal().Normalized();
                var prev = node.Face.GetCircular(0).TextureCoords * size;
                var from = node.Face.GetCircular(1).TextureCoords * size;
                var next = node.Face.GetCircular(2).TextureCoords * size;

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

                grids[node] = grid;
            }

            return grids;
        }

        public static void ConnectCells(Dictionary<TopologyNode, Cell[,]> grids, int size)
        {
            foreach (var pair in grids)
            {
                var node = pair.Key;
                var grid = pair.Value;

                ConnectCellsOnSameGrid(grid);
                ConnectGridWithNeighbours(grids, node, size);
            }
        }

        public static void ConnectCellsOnSameGrid(Cell[,] grid)
        {
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

            int lastX = grid.GetLength(0) - 1;
            int lastY = grid.GetLength(1) - 1;

            for (int x = 0; x < grid.GetLength(0) - 1; x++)
            {
                var cell = grid[x, lastY];
                var right = grid[x + 1, lastY];

                cell.Neighbours[3] = new NeighbourData(right, new RuleEmptyAdapter(LogicalResolution));
                right.Neighbours[1] = new NeighbourData(cell, new RuleEmptyAdapter(LogicalResolution));
            }

            for (int y = 0; y < grid.GetLength(1) - 1; y++)
            {
                var cell = grid[lastX, y];
                var bottom = grid[lastX, y + 1];

                cell.Neighbours[2] = new NeighbourData(bottom, new RuleEmptyAdapter(LogicalResolution));
                bottom.Neighbours[0] = new NeighbourData(cell, new RuleEmptyAdapter(LogicalResolution));
            }
        }

        public static void ConnectGridWithNeighbours(
            Dictionary<TopologyNode, Cell[,]> grids, 
            TopologyNode node, 
            int size)
        {
            var grid = grids[node];

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

        public static void GenerateDetails(Engine engine, Topology topology, List<Cell> cells, int size)
        {
            foreach (var cell in cells)
            {
                BuildPipe(engine, topology, cell, size);
                //var centroid = cell.Aggregate((a, b) => a + b) / cell.Count;
                //var point = GetPoint(topology, centroid, size);
                //engine.CreateCube(point, new Vector3(0.1f));
            }
        }

        public static Vector3 GetPoint(Topology topology, Vector2 uv, int size)
        {
            foreach (var node in topology)
            {
                Vertex a = node.Face[0];
                Vertex b = node.Face[1];
                Vertex c = node.Face[2];

                Vector2 barycentric = GetBarycentric(
                    a.TextureCoords * size, 
                    b.TextureCoords * size, 
                    c.TextureCoords * size, 
                    uv);
                
                float u = barycentric.X;
                float v = barycentric.Y;

                // Face contain this uv
                if (u >= 0 && v >= 0 && u + v <= 1)
                {
                    Vector3 point = (1 - u - v) * a.Position + v * b.Position + u * c.Position;
                    return point;
                }
            }

            throw new ArgumentException();            
        }

        public static Vector2 GetBarycentric(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            Vector2 v0 = c - a;
            Vector2 v1 = b - a;
            Vector2 v2 = p - a;

            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            float inverseDenominator = 1.0f / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * inverseDenominator;
            float v = (dot00 * dot12 - dot01 * dot02) * inverseDenominator;

            return new Vector2(u, v);
        }

        public static void BuildPipe(Engine engine, Topology topology, Cell cell, int size)
        {            
            var rotation = Mathematics.GetRotation(Vector3.UnitX, cell.Normal);
            var scale = new Vector3(0.3f);
            var color = Color.FromArgb(255, 255, 217, 0);
            var rule = cell.Rules[0];

            var centroidUV = cell.Aggregate((p1, p2) => p1 + p2) / 4;
            var centroid = GetPoint(topology, centroidUV, size);

            bool isPipe = false;

            for (int i = 0; i < 4; i++)
            {
                var side = rule[i];

                if (side[1].IsSame(color) && side[2].IsSame(color))
                {                    
                    var uv = (cell[i] + cell.GetCircular(i + 1)) / 2;
                    uv += (centroidUV - uv).Normalized(); // Move inside right face;
                    var point = (GetPoint(topology, uv, size) + centroid) / 2;                    
                    InstantiateCube(engine, point, rotation, scale, color);
                    isPipe = true;
                }
            }

            if (isPipe)
            {
                InstantiateCube(engine, centroid, rotation, scale, color);
            }
        }

        private static GameObject InstantiateCube(
            Engine engine, 
            Vector3 position, 
            Quaternion rotation, 
            Vector3 scale,
            Color color)
        {
            var cube = engine.CreateCube(position, new Vector3(0.3f));
            var renderer = cube.Get<MaterialRenderComponent>();
            renderer!.Material.Color = new Vector3(color.R, color.G, color.B);
            cube.Rotation = rotation;
            return cube;
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

        public static List<Cell> GridsToCells(Dictionary<TopologyNode, Cell[,]> grids)
        {
            var cells = new List<Cell>();

            foreach (var node in grids)
            {
                var iland = node.Value;

                foreach (var cell in iland)
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        public static void Main(string[] args)
        {
            //var random = new Random();
            //int seed = random.Next();
            //Console.WriteLine(seed);
            //CollectionsHelper.UseSeed(seed);
            CollectionsHelper.UseSeed(404963574);

            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            //var model = Model.Load("Content/Models/Cube.obj", PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);
            //var model = Model.Load("Content/Models/Room.obj");
            //var model = Model.Load("Content/Models/Line.obj");
            //var model = Model.Load("Content/Models/Corner.obj", PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);
            //var model = Model.Load("Content/Models/Scene.obj", PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);
            var model = Model.Load("Content/Models/Tower.obj", PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);

            int size = 2048;
            int step = 32;

            //var topology = new Topology(model.Meshes[0], 3);
            //var dirtyPolies = ExtractPolies(topology);
            //var cleanPolies = CleanUpPolies(dirtyPolies);
            //var retopology = new Topology(cleanPolies);
            //var grids = BuildCells(retopology, size, step);
            //ConnectCells(grids, size);

            var topology = new Topology(model.Meshes[0], 4);
            var grids = BuildCells(topology, size, step);
            ConnectCells(grids, size);

            var roomGo = engine.CreateGameObject();
            var roomRenderer = roomGo.Add<MaterialRenderComponent>();
            var triangulatedModel = new Model(model.Meshes[0].TriangulateQuadMesh());            
            roomRenderer.Model = triangulatedModel;

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

            var cells = GridsToCells(grids);
            var triangulatedTopology = new Topology(triangulatedModel.Meshes[0], 3);
            Wfc.GraphWfc(cells, wallRules, floorRules, ceilRules);
            GenerateDetails(engine, triangulatedTopology, cells, size);

            var texture = TextureCreator.CreateDetailedTexture(grids, size, step);
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