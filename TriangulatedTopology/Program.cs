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
using System.Drawing;
using TextureUtils;
using TriangulatedTopology.Geometry;
using TriangulatedTopology.RulesAdapters;
using TriangulatedTopology.TextureIsland;
using Mathematics = GameEngine.Mathematics.Mathematics;
using Mesh = GameEngine.Graphics.Mesh;
using Quaternion = OpenTK.Mathematics.Quaternion;

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
                // Search for purple lines.
                if (net.GetNodes().First().Item.Color.IsSame(VentilationColor))
                {
                    //VisualizePipes(engine, net);
                    var model = AlternativePipes(engine, net);
                    var go = engine.CreateGameObject();
                    var render = go.Add<MaterialRenderComponent>();
                    render.Model = model;
                    continue;
                }
                //if (net.GetNodes().First().Item.Color.IsSame(WireColor))
                //{
                //    VisualizeWires(engine, net);
                //    continue;
                //}
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

        public static Model AlternativePipes(Engine engine, Net<LogicalNode> net)
        {
            var nodes = GetNodesSequence(net);
            var spline = GetSpline(nodes);

            if (spline.Count == 0 || spline.Count == 1)
            {
                return Model.Empty;
            }

            //var line = engine.Line(spline[0].Position, spline[1].Position, Colors.Navy);
            //var renderer = line.Get<LineRenderComponent>()!;
            //renderer.Width = 10;

            //for (int i = 1; i < spline.Count; i++)
            //{
            //    line = engine.Line(spline[i - 1].Position, spline[i].Position, Colors.Navy);
            //    renderer = line.Get<LineRenderComponent>()!;
            //    renderer.Width = 10;
            //}

            //return Model.Empty;

            int resolution = 32;
            float radius = 0.1f;
            var model = CreateTubeFromSpline(spline, resolution, radius);

            return model;
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

        public static List<SplineVertex> GetSpline(List<LogicalNode> nodes)
        {
            float extrusionFactor = 0.15f;
            var points = new List<SplineVertex>();
            var spline = new List<SplineVertex>();

            //{ // First
            //    // Nodes
            //    var current = nodes[0];
            //    var next = nodes[1];

            //    // Normals
            //    var currentNormal = GetNormal(current.Corners);
            //    var nextNormal = GetNormal(next.Corners);
            //    var blendedNormal = Vector3.Normalize(Vector3.Lerp(currentNormal, nextNormal, 0.5f));

            //    // Pivots
            //    var currentJoint = GetCentroid(current.Corners);
            //    var currentPivot = currentJoint + extrusionFactor * currentNormal;

            //    // Shared points
            //    var sharedPoints = GetSharedPoints(current.Corners, next.Corners);
            //    var sharedPointsCentroid = GetCentroid(sharedPoints);

            //    // Joints
            //    var nextJoint = sharedPointsCentroid + extrusionFactor * blendedNormal;

            //    // Directions
            //    var toNextDirection = Vector3.Normalize(nextJoint - currentPivot);
            //    var currentJointDirection = currentNormal;                
            //    var pivotDirection = Vector3.Normalize(Vector3.Lerp(currentJointDirection, toNextDirection, 0.5f));

            //    // Up
            //    var currentJointUp = Vector3.Normalize(currentJoint - sharedPointsCentroid);
            //    var pivotUp = Vector3.Normalize(Vector3.Lerp(currentJointUp, currentNormal, 0.5f));

            //    // Spline
            //    spline.Add(new SplineVertex(currentJoint, currentJointUp, currentNormal));
            //    spline.Add(new SplineVertex(currentPivot, pivotUp, pivotDirection));
            //}

            //for (int i = 1; i < nodes.Count - 1; i++)
            //{
            //    // Nodes
            //    var previous = nodes[i - 1];
            //    var current = nodes[i];
            //    var next = nodes[i + 1];

            //    // Normals
            //    var previousNormal = GetNormal(previous.Corners);
            //    var currentNormal = GetNormal(current.Corners);
            //    var nextNormal = GetNormal(next.Corners);
            //    var previousBlendedNormal = Vector3.Normalize(Vector3.Lerp(previousNormal, currentNormal, 0.5f));
            //    var nextBlendedNormal = Vector3.Normalize(Vector3.Lerp(currentNormal, nextNormal, 0.5f));

            //    // Pivots
            //    var previousPivot = GetCentroid(previous.Corners) + extrusionFactor * previousNormal;
            //    var currentPivot = GetCentroid(current.Corners) + extrusionFactor * currentNormal;

            //    // Shared points
            //    var previousSharedPoints = GetSharedPoints(previous.Corners, current.Corners);
            //    var nextSharedPoints = GetSharedPoints(current.Corners, next.Corners);

            //    // Joints
            //    var previousJoint = GetCentroid(previousSharedPoints) + extrusionFactor * previousBlendedNormal;                
            //    var nextJoint = GetCentroid(nextSharedPoints) + extrusionFactor * nextBlendedNormal;

            //    // Directions
            //    var toPreviousJointDirection = Vector3.Normalize(previousJoint - previousPivot);
            //    var toCurrentPivotDirection = Vector3.Normalize(currentPivot - previousJoint);
            //    var toNextJointDirection = Vector3.Normalize(nextJoint - currentPivot);
            //    var previousBlendedDirection = Vector3.Normalize(Vector3.Lerp(toPreviousJointDirection, toCurrentPivotDirection, 0.5f));
            //    var nextBlendedDirection = Vector3.Normalize(Vector3.Lerp(toCurrentPivotDirection, toNextJointDirection, 0.5f));

            //    var sharedShared = GetSharedPoints(previousSharedPoints, nextSharedPoints);

            //    if (sharedShared.Any())
            //    {
            //        var ellipseCenter = sharedShared.First();

            //        var first = new SplineVertex(previousJoint, previousBlendedNormal, previousBlendedDirection);
            //        var last = new SplineVertex(nextJoint, nextBlendedNormal, toNextJointDirection);

            //        var line0 = Engine.Line(first.Position, first.Position + 3 * first.Forward, Colors.Green);
            //        var line1 = Engine.Line(last.Position, last.Position + 3 * last.Forward, Colors.Green);

            //        spline.Add(first);

            //        int resolution = 10;

            //        for (int j = 0; j < resolution; j++)
            //        {
            //            float percent = (float)j / resolution;

            //            //var position = ellipseCenter + Slerp(first.Position - ellipseCenter, last.Position - ellipseCenter, percent);
            //            //var position = Vector3.Lerp(first.Position, last.Position, MathF.Sin(percent * (MathF.PI / 2)));
            //            var position = Curves.Hermite(first.Position, last.Position, 3 * first.Forward, 3 * last.Forward, percent);
            //            var normal = Vector3.Lerp(first.Up, last.Up, percent);
            //            var direction = Vector3.Lerp(first.Forward, last.Forward, percent);

            //            spline.Add(new SplineVertex(position, normal, direction));
            //        }                    
            //    }
            //    else
            //    {
            //        // Spline
            //        spline.Add(new SplineVertex(previousJoint, previousBlendedNormal, previousBlendedDirection));
            //        spline.Add(new SplineVertex(currentPivot, currentNormal, nextBlendedDirection));
            //    }
            //}

            //{ // last
            //    // Nodes
            //    var previous = nodes[nodes.Count - 2];
            //    var current = nodes[nodes.Count - 1];

            //    // Normals
            //    var previousNormal = GetNormal(previous.Corners);
            //    var currentNormal = GetNormal(current.Corners);
            //    var blendedNormal = Vector3.Normalize(Vector3.Lerp(previousNormal, currentNormal, 0.5f));

            //    // Pivots
            //    var currentJoint = GetCentroid(current.Corners);
            //    var previousPivot = GetCentroid(previous.Corners) + extrusionFactor * previousNormal;
            //    var currentPivot = currentJoint + extrusionFactor * currentNormal;

            //    // Shared points
            //    var sharedPoints = GetSharedPoints(previous.Corners, current.Corners);
            //    var sharedPointsCentroid = GetCentroid(sharedPoints);

            //    // Joints
            //    var previousJoint = sharedPointsCentroid + extrusionFactor * blendedNormal;

            //    // Directions                
            //    var toPreviousJointDirection = Vector3.Normalize(previousJoint - previousPivot);
            //    var toCurrentPivotDirection = Vector3.Normalize(currentPivot - previousJoint);
            //    var currentJointDirection = -currentNormal;
            //    var previousBlendedDirection = Vector3.Normalize(Vector3.Lerp(toPreviousJointDirection, toCurrentPivotDirection, 0.5f));                
            //    var currentBlendedDirection = Vector3.Normalize(Vector3.Lerp(toCurrentPivotDirection, currentJointDirection, 0.5f));

            //    // Up
            //    var currentJointUp = Vector3.Normalize(currentJoint - sharedPointsCentroid);
            //    var currentPivotUp = Vector3.Normalize(Vector3.Lerp(currentNormal, currentJointUp, 0.5f));

            //    // Spline
            //    spline.Add(new SplineVertex(previousJoint, blendedNormal, previousBlendedDirection));
            //    spline.Add(new SplineVertex(currentPivot, currentPivotUp, currentBlendedDirection));
            //    spline.Add(new SplineVertex(currentJoint, currentJointUp, currentJointDirection));
            //}

            // return spline;

            { // First 
                var temp = nodes[0];
                var next = nodes[1];

                var normal = GetNormal(temp.Corners);
                var shared = GetSharedPoints(temp.Corners, next.Corners);
                var centroid = GetCentroid(shared);
                var pivot = GetCentroid(temp.Corners);
                var direction = Vector3.Normalize(centroid - pivot);

                points.Add(new SplineVertex(pivot, normal, direction));
            }

            for (int i = 1; i < nodes.Count - 1; i++)
            {
                var prev = nodes[i - 1];
                var temp = nodes[i];
                var next = nodes[i + 1];

                var normal = GetNormal(temp.Corners);

                var prevSharedPoints = GetSharedPoints(prev.Corners, temp.Corners);
                var nextSharedPoints = GetSharedPoints(temp.Corners, next.Corners);

                var pivot = GetCentroid(temp.Corners);
                var prevJoint = GetCentroid(prevSharedPoints);
                var nextJoint = GetCentroid(nextSharedPoints);

                var toPivotDirection = Vector3.Normalize(pivot - prevJoint);
                var fromPivotDirection = Vector3.Normalize(nextJoint - pivot);
                var blendedDirection = Vector3.Normalize(Vector3.Lerp(toPivotDirection, fromPivotDirection, 0.5f));

                points.Add(new SplineVertex(pivot + extrusionFactor * normal, normal, blendedDirection));
            }

            { // Last
                var prev = nodes[nodes.Count - 2];
                var temp = nodes[nodes.Count - 1];
                var normal = GetNormal(temp.Corners);

                var shared = GetSharedPoints(prev.Corners, temp.Corners);
                var centroid = GetCentroid(shared);
                var pivot = GetCentroid(temp.Corners);
                var direction = Vector3.Normalize(pivot - centroid);

                points.Add(new SplineVertex(pivot, normal, direction));
            }

            if (points.Count == 0)
            {
                return spline;
            }

            //Engine
            //    .Line(points[0].Position, points[0].Position + 3 * points[0].Forward, Colors.Green)
            //    .Get<LineRenderComponent>()!.Width = 4.0f;

            //Engine
            //    .CreateCube(points[0].Position, Quaternion.Identity, new Vector3(0.3f))
            //    .Get<MaterialRenderComponent>()!.Material.Color = Colors.Red;

            for (int i = 1; i < points.Count; i++)
            {
                var previous = points[i - 1];
                var next = points[i];              
               
                //Engine
                //    .Line(next.Position, next.Position + 3 * next.Forward, Colors.Green)
                //    .Get<LineRenderComponent>()!.Width = 4.0f;

                //Engine
                //    .CreateCube(next.Position, Quaternion.Identity, new Vector3(0.3f))
                //    .Get<MaterialRenderComponent>()!.Material.Color = Colors.Red;

                int resolution = 10;

                for (int j = 0; j < resolution; j++)
                {
                    float percent = (float)j / resolution;
                    float nextPercent = (float)(j + 1) / resolution;
                    float coerce = 3.0f;

                    var position = Curves.Hermite(
                        previous.Position, 
                        next.Position, 
                        coerce * previous.Forward, 
                        coerce * next.Forward, 
                        percent);
                    
                    var nextPosition = Curves.Hermite(
                        previous.Position, 
                        next.Position, 
                        coerce * previous.Forward,
                        coerce * next.Forward, 
                        nextPercent);
                   
                    var normal = Vector3.Lerp(previous.Up, next.Up, percent);
                    var direction = Vector3.Normalize(nextPosition - position);

                    spline.Add(new SplineVertex(position, normal, direction));
                }
            }

            spline.Add(new SplineVertex(
                points[points.Count - 1].Position, 
                points[points.Count - 1].Up, 
                points[points.Count - 1].Forward));

            return spline;
        }

        public static Vector3 Slerp(Vector3 start, Vector3 end, float percent)
        {
            float dot = Vector3.Dot(start, end);
            dot = Math.Clamp(dot, -1.0f, 1.0f);

            float theta = MathF.Acos(dot) * percent;
            Vector3 relativeVec = end - start * dot;
            relativeVec.Normalize();

            return (start * MathF.Cos(theta)) + (relativeVec * MathF.Sin(theta));
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

        public static void VisualizeWires(Engine engine, Net<LogicalNode> net)
        {
            float extrusionFactor = 0.2f;

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
                    var pipe = InstantiateWire(engine, pivot, Quaternion.Identity);
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
                    //var pipe = InstantiateWire(engine, pivot, Quaternion.Identity);
                    //var skeleton = pipe.Get<SkeletalMeshRenderComponent>()!.Model.Skeleton!;

                    //GetPipeSideDeformations(node.Item, node.Neighbours[0].Item,
                    //    pivot, normal, Vector3.UnitZ * k, extrusionFactor,
                    //    out var topSideDirection,
                    //    out var topSocketCoerce,
                    //    out var topSocketRotation);

                    //var top = skeleton["Top"];
                    //var topHand = skeleton["TopHand"];
                    //top.Position = topSideDirection;
                    //topHand.Position = topSocketCoerce;
                    //topHand.Rotation = topSocketRotation;

                    //var shared = GetSharedPoints(node.Item.Corners, node.Neighbours[0].Item.Corners);
                    //var to = GetCentroid(shared) + extrusionFactor * normal;
                    //var toNeighbour = to - pivot;
                    //toNeighbour.Normalize();

                    //GetPipeEndingDeformations(
                    //    centroid, pivot, -toNeighbour, -Vector3.UnitZ * k,
                    //    out var bottomSideDirection,
                    //    out var bottomSocketCoerce,
                    //    out var bottomSocketRotation);

                    //var bottom = skeleton["Bottom"];
                    //var bottomHand = skeleton["BottomHand"];
                    //bottom.Position = bottomSideDirection;
                    //bottomHand.Position = bottomSocketCoerce;
                    //bottomHand.Rotation = bottomSocketRotation;
                }
            }
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
            var ring = engine.CreateGameObject();
            var renderer = ring.Add<MaterialRenderComponent>();
            renderer!.Model = RingModel;
            renderer!.Material.Color = RgbaToVector3(color);
            ring.Position = position;
            ring.Rotation = rotation;
            ring.Scale = scale;
            return ring;
        }

        public static GameObject InstantiatePipe(Engine engine, Vector3 position, Quaternion rotation)
        {
            var pipe = engine.CreateGameObject();
            var renderer = pipe.Add<SkeletalMeshRenderComponent>();
            //renderer.Model = Model.Load("Content/Models/PipeSegment.fbx");
            renderer.Model = Model.Load("Content/Models/CurvePipe.fbx");
            pipe.Position = position;
            pipe.Rotation = rotation;
            return pipe;
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

            //CollectionsHelper.UseSeed(1628667546);
            //CollectionsHelper.UseSeed(1145917631);
            //CollectionsHelper.UseSeed(56224625);
            //CollectionsHelper.UseSeed(935418399);
            //CollectionsHelper.UseSeed(1310155548);

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