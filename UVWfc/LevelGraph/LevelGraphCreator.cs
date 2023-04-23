using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using UVWfc.Helpers;
using UVWfc.LevelGraph.TextureIsland;
using UVWfc.RulesAdapters;

namespace UVWfc.LevelGraph
{
    public static class LevelGraphCreator
    {
        private enum RotationDirection
        {
            Positive,
            Negative,
        }

        private class CoordSystem
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

        public static List<Cell> CreateGraph(
            Topology topology,
            int logicalResolution,
            int textureSize,
            int cellSize)
        {
            var islands = CreateIslands(topology, logicalResolution, textureSize, cellSize);
            var cells = IslandsToCells(islands);
            return cells;
        }

        private static List<Island> CreateIslands(
            Topology topology,
            int logicalResolution,
            int textureSize,
            int cellSize)
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
                var grid = CreateGrid(corners, textureSize, cellSize);
                var island = new Island(grid, sides.ToArray());
                islands.Add(island);
            }

            ConnectCells(islands, logicalResolution, textureSize, cellSize);
            return islands;
        }

        private static HashSet<Edge> ExtractOuterEdges(List<TopologyNode> island)
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

        private static List<Edge> ConnectIntoLoop(HashSet<Edge> edges)
        {
            var loop = new List<Edge>();
            var temp = edges.First();

            while (edges.Count > 0)
            {                
                edges.Remove(temp);
                loop.Add(temp);
                bool isNotLooped = edges.Any();

                foreach (var other in edges)
                {
                    if (other.A.Position == temp.B.Position &&
                        other.A.TextureCoords == temp.B.TextureCoords)
                    {
                        temp = other;
                        isNotLooped = false;
                        break;
                    }
                }

                if (isNotLooped)
                {
                    throw new ArgumentException($"Edges do not create a loop.");
                }
            }

            return loop;
        }

        private static List<Side> LoopToSides(List<Edge> loop)
        {
            float epsilon = 0.01f;
            var sides = new List<Side>();
            int initial = 0;

            // Find any corner.
            for (; initial < loop.Count; initial++)
            {
                var prev = loop.GetCircular(initial);
                var next = loop.GetCircular(initial + 1);
                var prevDirection = Vector2.Normalize(prev.B.TextureCoords - prev.A.TextureCoords);
                var nextDirection = Vector2.Normalize(next.B.TextureCoords - next.A.TextureCoords);

                if (!Mathematics.ApproximatelyEqualEpsilon(prevDirection, nextDirection, epsilon))
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

                if (!Mathematics.ApproximatelyEqualEpsilon(prevDirection, nextDirection, epsilon))
                {
                    var side = new Side(edges);
                    sides.Add(side);
                    edges.Clear();
                }
            }

            return sides;
        }

        private static List<Vertex> ExtractCorners(List<Side> sides)
        {
            var corners = new List<Vertex>();

            foreach (var side in sides)
            {
                corners.Add(side[0].A);
            }

            return corners;
        }

        private static Cell[,] CreateGrid(IReadOnlyList<Vertex> corners, int textureSize, int cellStep)
        {
            var normal = GetNormal(corners);
            var prev = corners[0].TextureCoords * textureSize;
            var from = corners[1].TextureCoords * textureSize;
            var next = corners[2].TextureCoords * textureSize;

            var xDirection = prev - from;
            var yDirection = next - from;

            var xLength = xDirection.Length;
            var yLength = yDirection.Length;

            var xAxis = xDirection.Normalized();
            var yAxis = yDirection.Normalized();

            int width = (int)MathHelper.Round(xLength / cellStep);
            int height = (int)MathHelper.Round(yLength / cellStep);
            var grid = new Cell[width, height];

            var dx = cellStep * xAxis;
            var dy = cellStep * yAxis;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pivot = from + cellStep * x * xAxis + cellStep * y * yAxis;
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

        private static Vector3 GetNormal(IReadOnlyList<Vertex> face)
        {
            var a = Vector3.Normalize(face[0].Position - face[1].Position);
            var b = Vector3.Normalize(face[2].Position - face[1].Position);
            return Vector3.Cross(a, b).Normalized();
        }

        private static void ConnectCells(
            List<Island> islands,
            int logicalResolution,
            int textureSize,
            int cellSize)
        {
            foreach (var island in islands)
            {
                ConnectCellsOnIsland(island, logicalResolution);
                ConnectCellsBetweenIslands(island, islands, logicalResolution, textureSize, cellSize);
            }
        }

        private static void ConnectCellsOnIsland(Island island, int logicalResolution)
        {
            for (int x = 0; x < island.Grid.GetLength(0) - 1; x++)
            {
                for (int y = 0; y < island.Grid.GetLength(1) - 1; y++)
                {
                    var cell = island.Grid[x, y];
                    var right = island.Grid[x + 1, y];
                    var bottom = island.Grid[x, y + 1];

                    cell.Neighbours[3] = new NeighbourData(right, new RuleEmptyAdapter(logicalResolution));
                    right.Neighbours[1] = new NeighbourData(cell, new RuleEmptyAdapter(logicalResolution));

                    cell.Neighbours[2] = new NeighbourData(bottom, new RuleEmptyAdapter(logicalResolution));
                    bottom.Neighbours[0] = new NeighbourData(cell, new RuleEmptyAdapter(logicalResolution));
                }
            }

            int lastX = island.Grid.GetLength(0) - 1;
            int lastY = island.Grid.GetLength(1) - 1;

            // Bottom side.
            for (int x = 0; x < island.Grid.GetLength(0) - 1; x++)
            {
                var cell = island.Grid[x, lastY];
                var right = island.Grid[x + 1, lastY];

                cell.Neighbours[3] = new NeighbourData(right, new RuleEmptyAdapter(logicalResolution));
                right.Neighbours[1] = new NeighbourData(cell, new RuleEmptyAdapter(logicalResolution));
            }

            // Right side.
            for (int y = 0; y < island.Grid.GetLength(1) - 1; y++)
            {
                var cell = island.Grid[lastX, y];
                var bottom = island.Grid[lastX, y + 1];

                cell.Neighbours[2] = new NeighbourData(bottom, new RuleEmptyAdapter(logicalResolution));
                bottom.Neighbours[0] = new NeighbourData(cell, new RuleEmptyAdapter(logicalResolution));
            }
        }

        private static void ConnectCellsBetweenIslands(
            Island island,
            List<Island> islands,
            int logicalResolution,
            int textureSize,
            int cellSize)
        {
            for (int sideIndex = 0; sideIndex < island.Sides.Length; sideIndex++)
            {
                var side = island.Sides[sideIndex];

                foreach (var segment in side)
                {
                    var thisCells = island.GetCorrespodingCells(segment.A.Position, segment.B.Position, textureSize, cellSize);
                    var neighbour = GetNeighbourIsland(island, islands, segment);
                    var neighbourCells = neighbour.GetCorrespodingCells(segment.A.Position, segment.B.Position, textureSize, cellSize);
                    GetAdapterBasis(sideIndex, segment, neighbour, logicalResolution, out var origin, out var xAxis, out var yAxis);

                    for (int i = 0; i < thisCells.Count; i++)
                    {
                        var thisCell = thisCells[i];
                        var neighbourCell = neighbourCells[i];
                        var adapter = new RuleRotationAdapter(origin, xAxis, yAxis, logicalResolution);
                        thisCell.Neighbours[sideIndex] = new NeighbourData(neighbourCell, adapter);
                    }
                }
            }
        }

        // This method is where all the magic happens...
        private static void GetAdapterBasis(
            int sideIndex,
            Edge segment,
            Island neighbour,
            int logicalResolution,
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
                new(new Vector2i(0, logicalResolution - 1),
                    new Vector2i(0, -1),
                    new Vector2i(1, 0)),
                new(new Vector2i(logicalResolution - 1, logicalResolution - 1),
                    new Vector2i(-1, 0),
                    new Vector2i(0, -1)),
                new(new Vector2i(logicalResolution - 1, 0),
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

        private static (Vector3 A, Vector3 B) GetExpectedNeighbourSideVerticesOrder(
            Edge pivotSegment,
            Side neighbourSide)
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

        private static Island GetNeighbourIsland(Island island, List<Island> islands, Edge segment)
        {
            return islands.First(o => !o.Equals(island) && o.IsContainsSegment(segment.A.Position, segment.B.Position));
        }

        private static List<Cell> IslandsToCells(List<Island> islands)
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
    }
}
