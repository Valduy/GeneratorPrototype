using GameEngine.Helpers;
using GameEngine.Mathematics;
using MeshTopology;
using OpenTK.Mathematics;

namespace TriangulatedTopology
{
    public class GridAdapter
    {
        private int _expected;
        private int _actual;

        private int _factor;
        private Cell[,] _grid;
        private Vector2i _original;
        private Vector2i _axis;

        public readonly TopologyNode Pivot;
        public readonly TopologyNode Neighbour;

        public int Width => _factor % 2 == 0
            ? _grid.GetLength(0)
            : _grid.GetLength(1);

        public int Height => _factor % 2 == 0
            ? _grid.GetLength(1)
            : _grid.GetLength(0);

        public Cell this[int x, int y]
        {
            get
            {
                var accessor = _original + new Vector2i(x, y) * _axis;
                return _grid[accessor.X, accessor.Y];
            }
        }

        public GridAdapter(TopologyNode pivot, TopologyNode neighbour, Cell[,] grid)
        {
            if (!pivot.Neighbours.Contains(neighbour))
            {
                throw new ArgumentException();
            }

            Pivot = pivot;
            Neighbour = neighbour;

            _grid = grid;
            _original = Vector2i.Zero;
            _axis = Vector2i.One;

            var pivotNeighbourIndex = pivot.Neighbours.IndexOf(neighbour);
            var sharedEdge = pivot.Face.GetSharedEdge(neighbour.Face);
            var neighbourSharedEdgeIndex = neighbour.Face.GetEdgeIndex(e => e.HasSamePositions(sharedEdge));
            var expectedSharedEdgeIndex = GetExpectedSharedEdgeIndex(pivotNeighbourIndex);

            var originals = new List<Vector2i> 
            { 
                new Vector2i(0, 0),
                new Vector2i(0, grid.GetLength(1) - 1),
                new Vector2i(grid.GetLength(0) - 1, grid.GetLength(1) - 1),
                new Vector2i(grid.GetLength(0) - 1, 0),
            };

            _expected = expectedSharedEdgeIndex;
            _actual = neighbourSharedEdgeIndex;

            _factor = neighbourSharedEdgeIndex - expectedSharedEdgeIndex;
            var rotationCount = MathHelper.Abs(_factor);
            var rotationDirection = _factor > 0
                ? Orientation.Сounterclockwise
                : Orientation.Clockwise;
            
            switch (rotationDirection)
            {
                case Orientation.Clockwise:
                    for (int i = 0; i < rotationCount; i++)
                    {
                        _axis = RotateClockwise(_axis);
                        originals.ShiftLeft();
                    }

                    _original = originals[0];
                    break;
                case Orientation.Сounterclockwise:
                    for (int i = 0; i < rotationCount; i++)
                    {
                        _axis = RotateCounterclockwise(_axis);
                        originals.ShiftRight();
                    }

                    _original = originals[0];
                    break;
            }
        }

        Vector2i RotateClockwise(Vector2i indices)
        {
            return new Vector2i(indices.Y, -indices.X);
        }

        Vector2i RotateCounterclockwise(Vector2i indices)
        {
            return new Vector2i(-indices.Y, indices.X);
        }

        private int GetExpectedSharedEdgeIndex(int pivotNeighbourIndex)
        {
            switch (pivotNeighbourIndex)
            {
                case 0:
                    return 2;
                case 1:
                    return 3;
                case 2:
                    return 0;
                case 3:
                    return 1;
                default:
                    throw new IndexOutOfRangeException(nameof(pivotNeighbourIndex));
            }
        }
    }
}
