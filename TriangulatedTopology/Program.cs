using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using GameEngine.Utils;
using Graph;
using MeshTopology;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using TextureUtils;
using TriangulatedTopology.Geometry;
using TriangulatedTopology.RulesAdapters;
using TriangulatedTopology.TextureIsland;
using Mathematics = GameEngine.Mathematics.Mathematics;
using Mesh = GameEngine.Graphics.Mesh;
using Quaternion = OpenTK.Mathematics.Quaternion;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;

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

        public static readonly Color PipesColor = Color.FromArgb(255, 217, 0);
        public static readonly Color WireColor = Color.FromArgb(255, 0, 24);
        public static readonly Color VentilationColor = Color.FromArgb(255, 60, 246);

        public static Model RingModel = Model.Load("Content/Models/Ring.obj");
        public static Model PipeModel = Model.Load("Content/Models/PipeSegment.fbx");
        public static Model PipeSupportModel = Model.Load("Content/Models/PipeSupport.fbx");
        public static Model MonitorModel = Model.Load("Content/Models/Monitor.fbx");
        public static Model WireSupportModel = Model.Load("Content/Models/WireSupport.fbx");

        public static Texture MonitorTexture = Texture.LoadFromFile("Content/Textures/Monitor.png");

        public static Engine Engine;

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

        public static Vector3 GetNormal(IReadOnlyList<Vector3> face)
        {
            var a = Vector3.Normalize(face[0] - face[1]);
            var b = Vector3.Normalize(face[2] - face[1]);
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

            var origins = new CoordSystem[]
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
                        origins.ShiftRight();
                    }

                    break;
                case RotationDirection.Positive:
                    for (int i = 0; i < rotations; i++)
                    {
                        origins.ShiftLeft();
                    }

                    break;
            }

            origin = origins[0].Origin;
            xAxis = origins[0].XAxis;
            yAxis = origins[0].YAxis;

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

        public static List<Net<LogicalNode>> ExtractNets(Topology topology, List<Cell> cells, int size)
        {
            var cellToLogicalNode = CreateLogicalNodes(topology, cells, size);
            var net = ConnectLogicalNodes(cells, cellToLogicalNode);
            return net.GetSubNets().ToList();
        }

        public static Dictionary<Cell, LogicalNode> CreateLogicalNodes(Topology topology, List<Cell> cells, int size)
        {
            var cellToLogicalNode = new Dictionary<Cell, LogicalNode>();

            foreach (var cell in cells)
            {
                var rule = cell.Rules[0].Logical;

                if (rule.Enumerate().Any(c => c.IsSame(PipesColor)))
                {
                    var corners = GetNodeCorners(topology, cell, size);
                    var connections = GetConnections(cell, PipesColor);
                    cellToLogicalNode[cell] = new LogicalNode(corners, PipesColor, connections);
                }
                if (rule.Enumerate().Any(c => c.IsSame(WireColor)))
                {
                    var corners = GetNodeCorners(topology, cell, size);
                    var connections = GetConnections(cell, WireColor);
                    cellToLogicalNode[cell] = new LogicalNode(corners, WireColor, connections);
                }
                if (rule.Enumerate().Any(c => c.IsSame(VentilationColor)))
                {
                    var corners = GetNodeCorners(topology, cell, size);
                    var connections = GetConnections(cell, VentilationColor);
                    cellToLogicalNode[cell] = new LogicalNode(corners, VentilationColor, connections);
                }
            }

            return cellToLogicalNode;
        }

        public static List<Vector3> GetNodeCorners(Topology topology, Cell cell, int size)
        {
            var corners = new List<Vector3>();

            foreach (var uv in cell)
            {
                var point = GetPoint(topology, uv, size);
                corners.Add(point);
            }

            return corners;
        }

        public static bool[] GetConnections(Cell cell, Color color)
        {
            var connections = new bool[4];
            var rule = cell.Rules[0];

            for (int i = 0; i < Cell.NeighboursCount; i++)
            {
                var side = rule[i];
                connections[i] = side[1].IsSame(color) && side[2].IsSame(color);
            }

            return connections;
        }

        public static Net<LogicalNode> ConnectLogicalNodes(List<Cell> cells, Dictionary<Cell, LogicalNode> cellToLogicalNode)
        {
            var cellToNetNode = new Dictionary<Cell, Node<LogicalNode>>();
            var net = new Net<LogicalNode>();

            foreach (var cell in cells)
            {
                if (!cellToLogicalNode.TryGetValue(cell, out var thisLogicalNode))
                {
                    continue;
                }
                if (!cellToNetNode.TryGetValue(cell, out var thisNetNode))
                {
                    thisNetNode = net.CreateNode(thisLogicalNode);
                    cellToNetNode[cell] = thisNetNode;
                }

                for (int i = 0; i < cell.Neighbours.Length; i++)
                {
                    if (!thisLogicalNode.Connections[i])
                    {
                        continue;
                    }

                    var otherCell = cell.Neighbours[i]!.Cell;

                    if (!cellToLogicalNode.TryGetValue(otherCell, out var otherLogicalNode))
                    {
                        continue;
                    }
                    if (!cellToNetNode.TryGetValue(otherCell, out var otherNetNode))
                    {
                        otherNetNode = net.CreateNode(otherLogicalNode);
                        cellToNetNode[otherCell] = otherNetNode;                        
                    }

                    net.Connect(thisNetNode, otherNetNode);
                }

                var expected = thisLogicalNode.Connections.Count(o => o);
                var actual = thisNetNode.Neighbours.Count;
            }

            return net;
        }

        public static void VisualizeNets(Engine engine, List<Net<LogicalNode>> nets)
        {
            float extrusionFactor = 0.3f;

            foreach (var net in nets)
            {
                foreach (var node in net.GetNodes())
                {
                    var centroid = GetCentroid(node.Item.Corners);
                    var normal = GetNormal(node.Item.Corners);
                    var from = centroid + extrusionFactor * normal;
                    var scale = new Vector3(0.3f);                    

                    foreach (var neighbour in node.Neighbours)
                    {
                        var lineGo = engine.CreateGameObject();
                        var render = lineGo.Add<LineRenderComponent>();
                        render.Color = RgbaToVector3(node.Item.Color);

                        var neighbourNormal = GetNormal(neighbour.Item.Corners);
                        var extrusionDirection = Vector3.Lerp(normal, neighbourNormal, 0.5f).Normalized();
                        var sharedPoints = GetSharedPoints(node.Item.Corners, neighbour.Item.Corners);
                        var to = GetCentroid(sharedPoints) + extrusionFactor * extrusionDirection;

                        render.Line = new Line(from, to);
                        render.Width = 5.0f;
                    }

                    {
                        var lineGo = engine.CreateGameObject();
                        var render = lineGo.Add<LineRenderComponent>();
                        render.Line = new Line(centroid, centroid + normal);
                        render.Color = Colors.Red;
                    }

                    if (node.Neighbours.Count == 1)
                    {
                        var rotation = Mathematics.GetRotation(Vector3.UnitY, normal);
                        var cube = InstantiateCube(engine, from, rotation, scale, node.Item.Color);

                        var lineGo = engine.CreateGameObject();
                        var render = lineGo.Add<LineRenderComponent>();
                        render.Line = new Line(Vector3.Zero, Vector3.UnitY);
                        render.Color = Colors.Blue;
                        cube.AddChild(lineGo);
                    }
                    if (node.Neighbours.Count >= 3)
                    {
                        InstantiateSphere(engine, from, Quaternion.Identity, scale, node.Item.Color);
                    }
                }
            }
        }

        public static void VisualizeProps(Engine engine, List<Net<LogicalNode>> nets)
        {           
            foreach (var net in nets)
            {
                if (net.GetNodes().First().Item.Color.IsSame(VentilationColor))
                {
                    //VisualizePipes(engine, net);
                    var model = AlternativePipes(engine, net);
                    var go = engine.CreateGameObject();
                    var render = go.Add<MaterialRenderComponent>();
                    render.Model = model;
                }
                if (net.GetNodes().First().Item.Color.IsSame(WireColor))
                {
                    var models = AlternativeWires(engine, net);

                    foreach (var model in models)
                    {
                        var go = engine.CreateGameObject();
                        var render = go.Add<MaterialRenderComponent>();
                        render.Material.Color = new Vector3(0.1f, 0.7f, 0.4f);
                        render.Model = model;
                    }

                    continue;
                }
            }
        }

        public struct SplineVertex
        {
            public readonly Vector3 Position;
            public readonly Vector3 Up;
            public readonly Vector3 Forward;

            public SplineVertex(Vector3 position, Vector3 up, Vector3 forward)
            {
                Position = position;
                Up = up;
                Forward = forward;
            }
        }

        public static List<Model> AlternativeWires(Engine engine, Net<LogicalNode> net)
        {
            int resolution = 32;
            float radius = 0.1f;

            var models = new List<Model>();
            var nodes = GetNodesSequence(net);
            var pointsLines = GetWiresPointsLines(nodes, 3);
            
            foreach (var line in pointsLines)
            {
                var spline = GetSpline(line);

                if (spline.Count == 0)
                {
                    continue;
                }

                var model = CreateTubeFromSpline(spline, resolution, radius);
                models.Add(model);
            }

            var middle = pointsLines[pointsLines.Count / 2];
            PlaceSupports(engine, middle);

            var first = middle[0];
            var last = middle[middle.Count - 1];

            // Monitors
            if (MathHelper.ApproximatelyEqualEpsilon(Vector3.Dot(first.Up, Vector3.UnitY), 0.0f, 0.01f))
            {
                InstantiateMonitor(engine, first.Position + first.Up * 0.2f, Mathematics.GetRotation(Vector3.UnitZ, first.Up));
            }

            if (MathHelper.ApproximatelyEqualEpsilon(Vector3.Dot(last.Up, Vector3.UnitY), 0.0f, 0.01f))
            {
                InstantiateMonitor(engine, last.Position + last.Up * 0.2f, Mathematics.GetRotation(Vector3.UnitZ, last.Up));
            }            

            return models;
        }

        public static bool IsLoop(Net<LogicalNode> net)
        {
            return !net.GetNodes().Any(n => n.Neighbours.Count == 1);
        }

        public static List<LogicalNode> GetNodesSequence(Net<LogicalNode> net)
        {
            var nodes = new List<LogicalNode>();
            var unvisited = new HashSet<LogicalNode>(net.GetNodes().Select(n => n.Item));
            var temp = 
                net.GetNodes().FirstOrDefault(n => n.Neighbours.Count == 1) ?? 
                net.GetNodes().First();

            if (temp.Neighbours.Count > 2 || temp.Neighbours.Count <= 0)
            {
                throw new ArgumentException("Pipe should has 1 or 2 neighbours.");
            }

            nodes.Add(temp.Item);
            unvisited.Remove(temp.Item);

            while (unvisited.Any())
            {
                temp = temp.Neighbours.First(n => unvisited.Contains(n.Item));

                if (temp.Neighbours.Count > 2 || temp.Neighbours.Count <= 0)
                {
                    throw new ArgumentException("Pipe should has 1 or 2 neighbours.");
                }

                nodes.Add(temp.Item);
                unvisited.Remove(temp.Item);                
            }

            return nodes;
        }

        public static List<List<SplineVertex>> GetWiresPointsLines(List<LogicalNode> nodes, int count)
        {
            float extrusionFactor = 0.2f;
            float offset = 0.2f;
            int half = count / 2;

            var pointsLines = new List<List<SplineVertex>>();

            for (int i = 0; i < count; i++)
            {
                pointsLines.Add(new List<SplineVertex>());
            }

            AddFirstSplineVertex(pointsLines, nodes[0], nodes[1], extrusionFactor, offset, count);
            AddSharedSplineVertex(pointsLines, nodes[0], nodes[1], extrusionFactor, offset, count);

            for (int i = 1; i < nodes.Count - 1; i++)
            {
                var prev = nodes[i - 1];
                var temp = nodes[i];
                var next = nodes[i + 1];

                AddSplineVertexInsideNode(pointsLines, prev, temp, next, extrusionFactor, offset, count);
                AddSharedSplineVertex(pointsLines, temp, next, extrusionFactor, offset, count);
            }

            AddLastSplineVertex(pointsLines, nodes[nodes.Count - 2], nodes[nodes.Count - 1], extrusionFactor, offset, count);
            return pointsLines;
        }

        public static void AddFirstSplineVertex(
            List<List<SplineVertex>> pointsLines,
            LogicalNode temp,
            LogicalNode next,
            float extrusionFactor,
            float offset,
            int count)
        {
            int half = count / 2;
            var normal = GetNormal(temp.Corners);
            var shared = GetSharedPoints(temp.Corners, next.Corners);
            var centroid = GetCentroid(shared);
            var pivot = GetCentroid(temp.Corners);
            var direction = Vector3.Normalize(centroid - pivot);
            var right = Vector3.Cross(direction, normal);
            var position = pivot + extrusionFactor * normal;

            for (int i = 0; i < count; i++)
            {
                int factor = i - half;
                pointsLines[i].Add(new SplineVertex(position + offset * factor * right, normal, direction));
            }
        }

        public static void AddLastSplineVertex(
            List<List<SplineVertex>> pointsLines,
            LogicalNode prev,
            LogicalNode temp,
            float extrusionFactor,
            float offset,
            int count)
        {
            int half = count / 2;
            var normal = GetNormal(temp.Corners);

            var shared = GetSharedPoints(prev.Corners, temp.Corners);
            var centroid = GetCentroid(shared);
            var pivot = GetCentroid(temp.Corners);
            var direction = Vector3.Normalize(pivot - centroid);
            var right = Vector3.Cross(direction, normal);
            var position = pivot + extrusionFactor * normal;

            for (int i = 0; i < count; i++)
            {
                int factor = i - half;
                pointsLines[i].Add(new SplineVertex(position + offset * factor * right, normal, direction));
            }
        }

        public static void AddSplineVertexInsideNode(
            List<List<SplineVertex>> pointsLines,
            LogicalNode prev,
            LogicalNode temp, 
            LogicalNode next,
            float extrusionFactor,
            float offset,
            int count)
        {
            int half = count / 2;
            var normal = GetNormal(temp.Corners);

            var prevSharedPoints = GetSharedPoints(prev.Corners, temp.Corners);
            var nextSharedPoints = GetSharedPoints(temp.Corners, next.Corners);

            var pivot = GetCentroid(temp.Corners);
            var prevJoint = GetCentroid(prevSharedPoints);
            var nextJoint = GetCentroid(nextSharedPoints);

            var toPivotDirection = Vector3.Normalize(pivot - prevJoint);
            var fromPivotDirection = Vector3.Normalize(nextJoint - pivot);
            var blendedDirection = Vector3.Normalize(Vector3.Lerp(toPivotDirection, fromPivotDirection, 0.5f));

            var right = Vector3.Cross(blendedDirection, normal);
            var position = pivot + extrusionFactor * normal;

            for (int i = 0; i < count; i++)
            {
                int factor = i - half;
                pointsLines[i].Add(new SplineVertex(position + offset * factor * right, normal, blendedDirection));
            }
        }

        public static void AddSharedSplineVertex(
            List<List<SplineVertex>> pointsLines,
            LogicalNode prev,
            LogicalNode next,
            float extrusionFactor,
            float offset,
            int count)
        {
            int half = count / 2;
            var prevNormal = GetNormal(prev.Corners);
            var nextNormal = GetNormal(next.Corners);
            var normal = Vector3.Normalize(Vector3.Lerp(prevNormal, nextNormal, 0.5f));

            var prevPivot = GetCentroid(prev.Corners);
            var nextPivot = GetCentroid(next.Corners);

            var sharedPoints = GetSharedPoints(prev.Corners, next.Corners);
            var joint = GetCentroid(sharedPoints);

            var toJointDirection = Vector3.Normalize(joint - prevPivot);
            var fromJointDirection = Vector3.Normalize(nextPivot - joint);
            var blendedDirection = Vector3.Normalize(Vector3.Lerp(toJointDirection, fromJointDirection, 0.5f));

            var right = Vector3.Cross(blendedDirection, normal);
            var position = joint + extrusionFactor * normal;

            for (int i = 0; i < count; i++)
            {
                int factor = i - half;
                pointsLines[i].Add(new SplineVertex(position + offset * factor * right, normal, blendedDirection));
            }
        }

        public static List<SplineVertex> GetSpline(List<SplineVertex> points)
        {
            var spline = new List<SplineVertex>();
            spline.AddRange(GenerateInnerVertices(points[0], points[1]));

            for (int i = 0; i < points.Count - 3; i++)
            {
                spline.AddRange(GenerateInnerVertices(points[i], points[i + 1], points[i + 2], points[i + 3]));
            }

            spline.AddRange(GenerateInnerVertices(points[points.Count - 2], points[points.Count - 1]));
            return spline;
        }

        public static void PlaceSupports(Engine engine, List<SplineVertex> points)
        {
            for (int i = 2; i < points.Count - 1; i += 2)
            {
                var rotation = Mathematics.GetRotation(Vector3.UnitY, points[i].Up);
                var forward = Vector3.Transform(Vector3.UnitX, rotation);
                rotation = Mathematics.GetRotation(forward, points[i].Forward) * rotation;
                InstantiateWireSupport(engine, points[i].Position, rotation);
            }
        }

        public static List<SplineVertex> GenerateInnerVertices(SplineVertex a, SplineVertex b)
        {
            int resolution = 20;
            var inner = new List<SplineVertex>();

            for (int j = 0; j < resolution; j++)
            {
                float tempPercent = (float)j / resolution;
                float nextPercent = (float)(j + 1) / resolution;
                float coerce = 0.5f;

                var position = Curves.Hermite(
                    a.Position,
                    b.Position,
                    coerce * a.Forward,
                    coerce * b.Forward,
                    tempPercent);

                var nextPosition = Curves.Hermite(
                    a.Position,
                    b.Position,
                    coerce * a.Forward,
                    coerce * b.Forward,
                    nextPercent);

                var normal = Vector3.Lerp(a.Up, b.Up, tempPercent);
                var direction = Vector3.Normalize(nextPosition - position);
                inner.Add(new SplineVertex(position, normal, direction));
            }

            return inner;
        }

        public static List<SplineVertex> GenerateInnerVertices(
            SplineVertex p0,
            SplineVertex p1,
            SplineVertex p2,
            SplineVertex p3)
        {
            int resolution = 20;
            var inner = new List<SplineVertex>();

            for (int j = 0; j < resolution; j++)
            {
                float tempPercent = (float)j / resolution;
                float nextPercent = (float)(j + 1) / resolution;
                float alpha = 0.5f;

                var position = Curves.CatmullRom(
                    p0.Position,
                    p1.Position,
                    p2.Position,
                    p3.Position,
                    tempPercent,
                    alpha);

                var nextPosition = Curves.CatmullRom(
                    p0.Position,
                    p1.Position,
                    p2.Position,
                    p3.Position,
                    nextPercent,
                    alpha);

                var normal = Vector3.Lerp(p1.Up, p2.Up, tempPercent);
                var direction = Vector3.Normalize(nextPosition - position);
                inner.Add(new SplineVertex(position, normal, direction));
            }

            return inner;
        }

        public static Model AlternativePipes(Engine engine, Net<LogicalNode> net)
        {
            int resolution = 32;
            float radius = 0.3f;
            float minSegmentLength = 0.5f;

            var nodes = GetNodesSequence(net);
            var points = GetPipePoints(nodes, radius);
            var lines = ExtractStraightLines(points);           

            foreach (var line in lines)
            {
                var first = line[0];
                var last = line[line.Count - 1];

                if (Vector3.Distance(first.Position, last.Position) < minSegmentLength)
                {
                    //var position = (first.Position + last.Position) / 2;
                    //var axis = Vector3.Normalize(Vector3.Lerp(first.Up, last.Up, 0.5f));
                    //var rotation = GetRotation(axis, Vector3.UnitY, first.Forward);

                    //InstantiatePipeSupport(
                    //    engine,
                    //    position,
                    //    rotation);

                    //InstantiatePipeSupport(
                    //    engine,
                    //    position,
                    //    Quaternion.FromAxisAngle(axis, MathF.PI) * rotation);

                    continue;
                }

                engine.Line(first.Position, first.Position + 3 * first.Up, Colors.Red);
                engine.Line(last.Position, last.Position + 3 * last.Up, Colors.Red);

                engine.Line(first.Position, first.Position + 3 * first.Forward, Colors.Blue);
                engine.Line(last.Position, last.Position + 3 * last.Forward, Colors.Blue);

                engine.Line(first.Position, first.Position + 3 * Vector3.UnitY, Colors.Green);
                engine.Line(last.Position, last.Position + 3 * Vector3.UnitY, Colors.Green);
               
                if (first.Forward.Y == 0)
                {
                    var t = 0;
                }

                var firstRotation = GetRotation(first.Up, Vector3.UnitY, first.Forward);
                var lastRotation = GetRotation(last.Up, Vector3.UnitY, last.Forward);

                InstantiatePipeSupport(
                    engine,
                    first.Position,
                    firstRotation);

                InstantiatePipeSupport(
                    engine,
                    first.Position,
                    Quaternion.FromAxisAngle(first.Up, MathF.PI) * firstRotation);

                InstantiatePipeSupport(
                    engine,
                    last.Position,
                    lastRotation);

                InstantiatePipeSupport(
                    engine,
                    last.Position,
                    Quaternion.FromAxisAngle(last.Up, MathF.PI) * lastRotation);
            }

            //for (int i = 1; i < points.Count; i++)
            //{
            //    var prev = points[i - 1];
            //    var next = points[i];

            //    var line = engine.Line(prev.Position, next.Position, Colors.Green);
            //    line.Get<LineRenderComponent>()!.Width = 10;
            //}

            //for (int i = 0; i < points.Count; i++)
            //{
            //    var temp = points[i];

            //    var line = engine.Line(temp.Position, temp.Position + temp.Up * 3, Colors.Red);
            //    line.Get<LineRenderComponent>()!.Width = 1;
            //}

            if (points.Count > 0)
            {
                var model = CreateTubeFromSpline(points, resolution, radius);
                return model;
            }

            return Model.Empty;
        }

        public static List<List<SplineVertex>> ExtractStraightLines(List<SplineVertex> points)
        {
            float epsilon = 0.01f;
            var lines = new List<List<SplineVertex>>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                var prev = points[i];
                var next = points[i + 1];
                var cosa = Vector3.Dot(prev.Forward, next.Forward);

                if (Mathematics.ApproximatelyEqualEpsilon(cosa, 1, epsilon))
                {
                    var line = new List<SplineVertex>();

                    for (var j = i; j < points.Count - 1; j++)
                    {                        
                        prev = points[j];
                        next = points[j + 1];
                        cosa = Vector3.Dot(prev.Forward, next.Forward);

                        if (!Mathematics.ApproximatelyEqualEpsilon(cosa, 1, epsilon))
                        {
                            break; 
                        }

                        line.Add(prev);
                    }

                    line.Add(points[i + line.Count]);
                    lines.Add(line);
                    i += line.Count;
                }
            }
            return lines;
        }

        public static List<SplineVertex> GetPipePoints(List<LogicalNode> nodes, float radius)
        {
            var points = new List<SplineVertex>();
            var segments = new List<List<SplineVertex>>();
            float extrusionFactor = 0.4f;
            int resolution = 5;
            
            for (int i = 1; i < nodes.Count; i ++)
            {
                var prev = nodes[i - 1];
                var next = nodes[i];

                segments.Add(CreatePipePointsAroundJoint(prev, next, extrusionFactor, radius, resolution));
            }

            points.AddRange(CreatePipeBegin(nodes[0], nodes[1], extrusionFactor, radius, resolution));

            for (int i = 1; i < segments.Count; i++)
            {
                var prev = segments[i - 1];
                var next = segments[i];

                var inner = CreatePipeInnerPoints(prev[prev.Count - 1], next[0], resolution);
                points.AddRange(prev.Concat(inner));
            }

            points.AddRange(segments[segments.Count - 1]);
            points.AddRange(CreatePipeEnd(nodes[nodes.Count - 2], nodes[nodes.Count - 1], extrusionFactor, radius, resolution));
            return points;
        }

        public static List<SplineVertex> CreatePipePointsAroundJoint(
            LogicalNode prev,
            LogicalNode next,
            float extrusionFactor,
            float radius,
            int resolution)
        {
            float epsilon = 0.01f;
            var points = new List<SplineVertex>();

            var prevNormal = GetNormal(prev.Corners);
            var nextNormal = GetNormal(next.Corners);
            var blendedNormal = Vector3.Normalize(Vector3.Lerp(prevNormal, nextNormal, 0.5f));

            var prevPivot = GetCentroid(prev.Corners);
            var nextPivot = GetCentroid(next.Corners);

            var sharedPoints = GetSharedPoints(prev.Corners, next.Corners);
            var joint = GetCentroid(sharedPoints);

            var prevDirection = Vector3.Normalize(joint - prevPivot);
            var nextDirection = Vector3.Normalize(nextPivot - joint);
            var blendedDirection = Vector3.Normalize(Vector3.Lerp(prevDirection, nextDirection, 0.5f));

            var prevP1 = prevPivot + extrusionFactor * prevNormal;
            var prevP2 = joint + extrusionFactor * prevNormal;
            var e1 = prevP2 - prevP1;
            
            var nextP1 = nextPivot + extrusionFactor * nextNormal;
            var nextP2 = joint + extrusionFactor * nextNormal;
            var e2 = nextP2 - nextP1;

            if (GetIntersactionPoint(prevP1, e1, nextP1, e2, epsilon, out var p))
            {
                var cosa = Math.Clamp(Vector3.Dot(prevDirection, nextDirection), -1.0f, 1.0f);
                var acos = MathF.Acos(cosa);
                var b = MathF.PI - acos;
                var offset = MathF.Abs(radius / MathF.Tan(b / 2));

                var prevP = p - offset * prevDirection;
                var nextP = p + offset * nextDirection;

                if (!GetIntersactionPoint(prevP, prevNormal, nextP, nextNormal, epsilon, out var rotationPivot))
                {
                    throw new ArgumentException("Something damn happened.");
                }

                var sign = MathF.Sign(Vector3.Dot(p - rotationPivot, blendedNormal));

                for (int i = 0; i <= resolution; i++)
                {
                    var t = (float)i / resolution;
                    var normal = Vector3.Normalize(Vector3.Lerp(prevNormal, nextNormal, t));
                    var position = rotationPivot + sign * radius * normal;
                    var direction = Vector3.Normalize(Vector3.Lerp(prevDirection, nextDirection, t));
                    points.Add(new SplineVertex(position, normal, direction));
                }
            }
            else
            {
                points.Add(new SplineVertex(joint + extrusionFactor * blendedNormal, blendedNormal, blendedDirection));
            }

            return points;
        }

        public static List<SplineVertex> CreatePipeBegin(
            LogicalNode begin,
            LogicalNode next,
            float extrusionFactor,
            float radius, 
            int resolution)
        {
            var points = new List<SplineVertex>();

            var beginNormal = GetNormal(begin.Corners);      
            
            var pivot = GetCentroid(begin.Corners);

            var sharedPoints = GetSharedPoints(begin.Corners, next.Corners);
            var joint = GetCentroid(sharedPoints);

            var fromPivotDirection = beginNormal;
            var toJointDirection = Vector3.Normalize(joint - pivot);

            var p = pivot + extrusionFactor * beginNormal;
            var rotationPivot = p - radius * fromPivotDirection + radius * toJointDirection;

            points.Add(new SplineVertex(pivot, -toJointDirection, fromPivotDirection));

            for (int i = 0; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                var normal = Vector3.Normalize(Vector3.Lerp(-toJointDirection, beginNormal, t));
                var direction = Vector3.Normalize(Vector3.Lerp(fromPivotDirection, toJointDirection, t));
                var position = rotationPivot + radius * normal;
                points.Add(new SplineVertex(position, normal, direction));
            }

            return points;
        }

        public static List<SplineVertex> CreatePipeEnd(
            LogicalNode prev,
            LogicalNode end,
            float extrusionFactor,
            float radius,
            int resolution)
        {
            var points = new List<SplineVertex>();

            var endNormal = GetNormal(end.Corners);

            var pivot = GetCentroid(end.Corners);

            var sharedPoints = GetSharedPoints(prev.Corners, end.Corners);
            var joint = GetCentroid(sharedPoints);

            var fromJointDirection = Vector3.Normalize(pivot - joint);
            var toPivotDirection = -endNormal;

            var p = pivot + extrusionFactor * endNormal;
            var rotationPivot = p + radius * toPivotDirection - radius * fromJointDirection;

            for (int i = 0; i <= resolution; i++)
            {
                float t = (float)i / resolution;
                var normal = Vector3.Normalize(Vector3.Lerp(endNormal, fromJointDirection, t));
                var direction = Vector3.Normalize(Vector3.Lerp(fromJointDirection, toPivotDirection, t));
                var position = rotationPivot + radius * normal;
                points.Add(new SplineVertex(position, normal, direction));
            }

            points.Add(new SplineVertex(pivot, fromJointDirection, toPivotDirection));
            return points;
        }

        public static List<SplineVertex> CreatePipeInnerPoints(SplineVertex p1, SplineVertex p2, int resolution)
        {
            var points = new List<SplineVertex>();

            for (int i = 1; i < resolution; i++)
            {
                float t = (float)i / resolution;
                var position = Curves.Hermite(p1.Position, p2.Position, p1.Forward * 2, p2.Forward * 2, t);
                var normal = Vector3.Normalize(Vector3.Lerp(p1.Up, p2.Up, t));
                var direction = Vector3.Normalize(Vector3.Lerp(p1.Forward, p2.Forward, t));
                points.Add(new SplineVertex(position, normal, direction));
            }

            return points;
        }

        public static bool GetIntersactionPoint(
            Vector3 r1,
            Vector3 e1,
            Vector3 r2,
            Vector3 e2,
            float epsilon,
            out Vector3 p)
        {
            float distance = float.PositiveInfinity;            
            var n = Vector3.Cross(e1, e2);
            p = Vector3.Zero;

            if (Mathematics.ApproximatelyEqualEpsilon(n, Vector3.Zero, epsilon))
            {
                return false;
            }

            distance = MathF.Abs(Vector3.Dot(n, r1 - r2)) / n.Length;
            
            if (distance > epsilon)
            {
                return false;
            }

            var squareN = Vector3.Dot(n, n);
            var direction = r2 - r1;

            var t1 = Vector3.Dot(Vector3.Cross(e2, n), direction) / squareN;
            var t2 = Vector3.Dot(Vector3.Cross(e1, n), direction) / squareN;

            var p1 = r1 + t1 * e1;
            var p2 = r2 + t2 * e2;

            p = (p1 + p2) / 2;
            return true;
        }

        public static Model CreateTubeFromSpline(List<SplineVertex> spline, int resolution, float radius)
        {
            var circles = new List<List<Vertex>>();
            var meshes = new List<Mesh>();

            var first = spline[0];
            circles.Add(GenerateCircle(first, resolution, radius));

            for (int i = 1; i < spline.Count; i++)
            {
                var temp = spline[i];
                circles.Add(GenerateCircle(temp, resolution, radius));

                var previous = circles[i - 1];
                var current = circles[i];

                var vertices = new List<Vertex>();
                var indices = new List<int>();

                vertices.AddRange(previous);
                vertices.AddRange(current);

                for (int j = 1; j < resolution; j++)
                {
                    indices.Add(resolution + j - 1);
                    indices.Add(j - 1);
                    indices.Add(j);
                    indices.Add(j);
                    indices.Add(resolution + j);
                    indices.Add(resolution + j - 1);
                }

                indices.Add(resolution + resolution - 1);
                indices.Add(resolution - 1);
                indices.Add(0);
                indices.Add(0);
                indices.Add(resolution);
                indices.Add(resolution + resolution - 1);

                var mesh = new Mesh(vertices, indices);
                meshes.Add(mesh);
            }

            return new Model(meshes);
        }

        public static List<Vertex> GenerateCircle(SplineVertex splineVertex, int resolution, float radius)
        {
            var circle = new List<Vertex>();
            var initial = splineVertex.Up;
            var sector = MathHelper.DegreesToRadians(360.0f / resolution);
            
            for (int i = 0; i < resolution; i++)
            {
                var rotator = Matrix4.CreateFromAxisAngle(splineVertex.Forward, sector * i);
                var normal = Vector3.TransformVector(initial, rotator);
                var point = splineVertex.Position + radius * normal;
                circle.Add(new Vertex(point, normal, Vector2.Zero));
            }

            return circle;
        }

        public static void VisualizePipes(Engine engine, Net<LogicalNode> net)
        {
            float extrusionFactor = 0.5f;

            foreach (var node in net.GetNodes())
            {
                if (node.Neighbours.Count > 2 || node.Neighbours.Count <= 0)
                {
                    throw new ArgumentException("Pipe should has 1 or 2 neighbours.");
                }

                var centroid = GetCentroid(node.Item.Corners);
                var normal = GetNormal(node.Item.Corners);
                var pivot = centroid + extrusionFactor * normal;
                float k = 0.96f;

                if (node.Neighbours.Count == 2)
                {
                    var pipe = InstantiatePipe(engine, pivot, Quaternion.Identity);
                    var skeleton = pipe.Get<SkeletalMeshRenderComponent>()!.Model.Skeleton!;

                    GetPipeSideDeformations(
                        node.Item, node.Neighbours[0].Item,
                        pivot, normal, Vector3.UnitZ * k, extrusionFactor,
                        out var topSideDirection,
                        out var topSocketCoerce,
                        out var topSocketRotation);

                    var top = skeleton["Top"];
                    var topHand = skeleton["TopHand"];
                    top.Position = topSideDirection;
                    topHand.Position = topSocketCoerce;
                    topHand.Rotation = topSocketRotation;

                    GetPipeSideDeformations(
                        node.Item, node.Neighbours[1].Item,
                        pivot, normal, -Vector3.UnitZ * k, extrusionFactor,
                        out var bottomSideDirection,
                        out var bottomSocketCoerce,
                        out var bottomSocketRotation);

                    var bottom = skeleton["Bottom"];
                    var bottomHand = skeleton["BottomHand"];
                    bottom.Position = bottomSideDirection;
                    bottomHand.Position = bottomSocketCoerce;
                    bottomHand.Rotation = bottomSocketRotation;
                }
                else
                {
                    var pipe = InstantiatePipe(engine, pivot, Quaternion.Identity);
                    var skeleton = pipe.Get<SkeletalMeshRenderComponent>()!.Model.Skeleton!;

                    GetPipeSideDeformations(node.Item, node.Neighbours[0].Item,
                        pivot, normal, Vector3.UnitZ * k, extrusionFactor,
                        out var topSideDirection,
                        out var topSocketCoerce,
                        out var topSocketRotation);

                    var top = skeleton["Top"];
                    var topHand = skeleton["TopHand"];
                    top.Position = topSideDirection;
                    topHand.Position = topSocketCoerce;
                    topHand.Rotation = topSocketRotation;

                    var shared = GetSharedPoints(node.Item.Corners, node.Neighbours[0].Item.Corners);
                    var to = GetCentroid(shared) + extrusionFactor * normal;
                    var toNeighbour = to - pivot;
                    toNeighbour.Normalize();

                    GetPipeEndingDeformations(
                        centroid, pivot, -toNeighbour, -Vector3.UnitZ * k,
                        out var bottomSideDirection,
                        out var bottomSocketCoerce,
                        out var bottomSocketRotation);

                    var bottom = skeleton["Bottom"];
                    var bottomHand = skeleton["BottomHand"];
                    bottom.Position = bottomSideDirection;
                    bottomHand.Position = bottomSocketCoerce;
                    bottomHand.Rotation = bottomSocketRotation;
                }
            }
        }

        public static void GetPipeSideDeformations(
            LogicalNode node,
            LogicalNode neighbour,
            Vector3 pivot,
            Vector3 normal,
            Vector3 socketOffset,
            float extrusionFactor,
            out Vector3 sideDirection,
            out Vector3 socketCoerce,
            out Quaternion socketRotation)
        {
            var neighbourNormal = GetNormal(neighbour.Corners);
            var extrusionDirection = Vector3.Lerp(normal, neighbourNormal, 0.5f).Normalized();

            var sharedPoints = GetSharedPoints(node.Corners, neighbour.Corners);
            var centroid = GetCentroid(sharedPoints);

            var to = centroid + extrusionFactor * extrusionDirection;
            var forward = centroid + extrusionFactor * normal - pivot;
            sideDirection = to - pivot;

            var edgeAxis = sharedPoints[1] - sharedPoints[0];
            edgeAxis.Normalize();

            var socketDirection = Vector3.Cross(edgeAxis, extrusionDirection);
            socketDirection.Normalize();

            // Rough but ok...
            if (Vector3.Dot(socketDirection, sideDirection) < 0)
            {
                socketDirection = -socketDirection;
            }

            var normalRotation = Mathematics.GetRotation(forward, socketDirection);
            normal = Vector3.Transform(normal, normalRotation).Normalized();

            socketRotation = Mathematics.GetRotation(socketOffset, socketDirection);
            var unwinding = GetUnwinding(socketDirection, socketRotation, normal);
            var withoutUnwinding = socketRotation; // for debug            

            var socketTransform = Matrix4.CreateTranslation(socketOffset);
            socketTransform *= Matrix4.CreateFromQuaternion(socketRotation);
            socketCoerce = -socketTransform.ExtractTranslation();
            socketRotation = unwinding * socketRotation;

            //var a0 = to;
            //var b0 = a0 + Vector3.Transform(Vector3.UnitY * 2, withoutUnwinding);

            //var line0 = Engine.Line(a0, b0, Colors.Green);
            //line0.Get<LineRenderComponent>()!.Width = 2;

            //var a1 = to;
            //var b1 = a1 + Vector3.Transform(Vector3.UnitY * 2, socketRotation);

            //var line1 = Engine.Line(a1, b1, Colors.Green);
            //line1.Get<LineRenderComponent>()!.Width = 2;
        }

        public static void GetPipeEndingDeformations(
            Vector3 centroid,
            Vector3 pivot,
            Vector3 normal,
            Vector3 socketOffset,
            out Vector3 sideDirection,
            out Vector3 socketCoerce,
            out Quaternion socketRotation)
        {
            sideDirection = centroid - pivot;
            socketRotation = Mathematics.GetRotation(socketOffset, sideDirection);
            var unwinding = GetUnwinding(sideDirection, socketRotation, normal);
            var withoutUnwinding = socketRotation; // for debug           

            var socketTransform = Matrix4.CreateTranslation(socketOffset);
            socketTransform *= Matrix4.CreateFromQuaternion(socketRotation);
            socketCoerce = -socketTransform.ExtractTranslation();
            socketRotation = unwinding * socketRotation;

            //var a0 = centroid;
            //var b0 = a0 + Vector3.Transform(yAxis * 2, withoutUnwinding);

            //var line0 = Engine.Line(a0, b0, Colors.Green);
            //line0.Get<LineRenderComponent>()!.Width = 2;

            //var a1 = centroid;
            //var b1 = a1 + Vector3.Transform(yAxis * 2, socketRotation);

            //var line1 = Engine.Line(a1, b1, Colors.Red);
            //line1.Get<LineRenderComponent>()!.Width = 2;
        }

        public static Quaternion GetUnwinding(
            Vector3 sideDirection,
            Quaternion socketRotation,
            Vector3 normal)
        {
            var yAxis = Vector3.UnitY;
            var axis = sideDirection.Normalized();
            var up = Vector3.Transform(yAxis, socketRotation);
            var unwinding = GetRotation(axis, up, normal);

            var epsilon = 0.01f;
            var unwindedUp = Vector3.Transform(up, unwinding);
            var normalUpAngle = Vector3.Dot(unwindedUp, normal);

            // GetRotation return always positive value.
            // We invert unwinding rotation if we should use negative angle.
            if (!MathHelper.ApproximatelyEqualEpsilon(normalUpAngle, 1.0f, epsilon))
            {
                unwinding.Invert();
            }

            var socketRotationWithUnwinding = unwinding * socketRotation;
            var upWithUnwinding = Vector3.Transform(yAxis, socketRotationWithUnwinding);
            var angle = Vector3.Dot(upWithUnwinding.Normalized(), normal);

            // Fix not correct rotation for 90 degrees between up and normal case.
            if (MathHelper.ApproximatelyEqualEpsilon(angle, -1.0f, epsilon))
            {
                unwinding *= Quaternion.FromAxisAngle(axis, MathF.PI);
            }

            return unwinding;
        }

        public static Quaternion GetRotation(Vector3 axis, Vector3 from, Vector3 to)
        {
            from.Normalize();
            to.Normalize();

            if (Mathematics.ApproximatelyEqualEpsilon(from, to, float.Epsilon))
            {
                return Quaternion.Identity;
            }
            if (Mathematics.ApproximatelyEqualEpsilon(from, -to, float.Epsilon))
            {
                return Quaternion.FromAxisAngle(axis, MathF.PI);
            }
            
            float cosa = MathHelper.Clamp(Vector3.Dot(from, to), -1, 1);
            float angle = MathF.Acos(cosa);
            return Quaternion.FromAxisAngle(axis, angle);
        }

        public static Vector3 GetCentroid(IReadOnlyList<Vector3> points)
        {
            return points.Aggregate((p1, p2) => p1 + p2) / points.Count;
        }

        public static List<Vector3> GetSharedPoints(IEnumerable<Vector3> poly1, IEnumerable<Vector3> poly2)
        {
            var sharedPoints = new List<Vector3>();
            var epsilon = 0.01f; // Just great enough;

            foreach (var a in poly1)
            {
                foreach (var b in poly2)
                {
                    if (Mathematics.ApproximatelyEqualEpsilon(a, b, epsilon))
                    {
                        sharedPoints.Add(a);
                    }
                }
            }

            return sharedPoints;
        }

        //public static void GenerateDetails(Engine engine, Topology topology, List<Cell> cells, int size)
        //{
        //    foreach (var cell in cells)
        //    {
        //        BuildNet(engine, topology, cell, PipesColor, size);
        //        BuildNet(engine, topology, cell, WireColor, size);
        //        BuildNet(engine, topology, cell, VentilationColor, size);
        //    }
        //}

        public static Vector3 GetPoint(Topology topology, Vector2 uv, int size)
        {
            foreach (var node in topology)
            {
                Vertex a = node.Face[0];
                Vertex b = node.Face[1];
                Vertex c = node.Face[2];

                Vector2 barycentric = Mathematics.GetBarycentric(
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

        //public static void BuildNet(Engine engine, Topology topology, Cell cell, Color color, int size)
        //{
        //    if (!cell.Rules[0].Logical.Enumerate().Any(c => c.IsSame(color)))
        //    {
        //        return;
        //    }

        //    var scaleFactor = 2.2f;
        //    var scale = new Vector3(scaleFactor);            
        //    var rule = cell.Rules[0];

        //    var centroidUV = cell.Aggregate((p1, p2) => p1 + p2) / 4;
        //    var centroid = GetPoint(topology, centroidUV, size);
        //    float step = 0.1f;

        //    for (int i = 0; i < 4; i++)
        //    {
        //        var side = rule[i];

        //        if (side[1].IsSame(color) && side[2].IsSame(color))
        //        {                    
        //            var uv = (cell[i] + cell.GetCircular(i + 1)) / 2;
        //            uv += (centroidUV - uv).Normalized(); // Move inside face;
        //            var edgePoint = GetPoint(topology, uv, size);
        //            var direction = centroid - edgePoint;
        //            var length = direction.Length;
        //            direction.Normalize();

        //            var rotation = Mathematics.GetRotation(Vector3.UnitY, direction);

        //            for (float offset = 0; offset < length; offset += step)
        //            {
        //                var position = edgePoint + direction * offset + cell.Normal * 0.3f;
        //                InstantiateRing(engine, position, rotation, scale, color);
        //            }                    
        //        }
        //    }

        //    //InstantiateCube(engine, centroid, rotation, scale, color);
        //}

        public static GameObject InstantiateCube(
            Engine engine, 
            Vector3 position, 
            Quaternion rotation, 
            Vector3 scale,
            Color color)
        {
            var cube = engine.CreateCube(position, Quaternion.Identity, scale);
            var renderer = cube.Get<MaterialRenderComponent>();
            renderer!.Material.Color = RgbaToVector3(color);
            cube.Rotation = rotation;
            return cube;
        }

        public static GameObject InstantiateSphere(
            Engine engine,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Color color)
        {
            var cube = engine.CreateSphere(position, scale);
            var renderer = cube.Get<MaterialRenderComponent>();
            renderer!.Material.Color = RgbaToVector3(color);
            cube.Rotation = rotation;
            return cube;
        }

        public static GameObject InstantiateRing(
            Engine engine,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Color color)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer!.Model = RingModel;
            renderer!.Material.Color = RgbaToVector3(color);
            go.Position = position;
            go.Rotation = rotation;
            go.Scale = scale;
            return go;
        }

        public static GameObject InstantiatePipe(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<SkeletalMeshRenderComponent>();
            //renderer.Model = Model.Load("Content/Models/PipeSegment.fbx");
            renderer.Model = Model.Load("Content/Models/CurvePipe.fbx");
            go.Position = position;
            go.Rotation = rotation;
            return go;
        }

        public static GameObject InstantiateWire(Engine engine, Vector3 position, Quaternion rotation)
        {
            var wire = engine.CreateGameObject();
            var renderer = wire.Add<SkeletalMeshRenderComponent>();
            //renderer.Model = Model.Load("Content/Models/PipeSegment.fbx");
            renderer.Model = Model.Load("Content/Models/Wire.fbx");
            wire.Position = position;
            wire.Rotation = rotation;
            return wire;
        }

        public static GameObject InstantiateMonitor(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer.Model = MonitorModel;
            renderer.Texture = MonitorTexture;
            go.Position = position;
            go.Rotation = rotation;
            return go;
        }

        public static GameObject InstantiateWireSupport(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer.Model = WireSupportModel;
            go.Position = position;
            go.Rotation = rotation;
            return go;
        }

        public static GameObject InstantiatePipeSupport(Engine engine, Vector3 position, Quaternion rotation)
        {
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer.Model = PipeSupportModel;
            go.Position = position;
            go.Rotation = rotation;
            return go;
        }

        public static Vector3 RgbaToVector3(Color color)
        {
            return new Vector3((float)color.R / 255, (float)color.G / 255, (float)color.B / 255);
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
            //CollectionsHelper.UseSeed(1310155548);887817102
            //CollectionsHelper.UseSeed(887817102);

            using var engine = new Engine();
            Engine = engine;

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
            Wfc.GraphWfc(cells, wallRules, floorRules, ceilRules);
            var nets = ExtractNets(topology, cells, size);
            //VisualizeNets(engine, nets);
            VisualizeProps(engine, nets);
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