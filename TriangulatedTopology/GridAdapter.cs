using GameEngine.Helpers;
using MeshTopology;
using OpenTK.Mathematics;

namespace TriangulatedTopology
{
    public class GridAdapter
    {
        private Cell[,] _grid;
        private Vector2i _gridPivot;
        private RotationDirection _rotationDirection;
        private int _rotationCount;

        public readonly TopologyNode Pivot;
        public readonly TopologyNode Neighbour;

        public int Width => _rotationCount % 2 == 0
            ? _grid.GetLength(0)
            : _grid.GetLength(1);

        public int Height => _rotationCount % 2 == 0
            ? _grid.GetLength(1)
            : _grid.GetLength(0);

        public GridAdapter(TopologyNode pivot, TopologyNode neighbour, Cell[,] grid)
        {
            if (!pivot.Neighbours.Contains(neighbour))
            {
                throw new ArgumentException();
            }

            _grid = grid;
            Pivot = pivot;
            Neighbour = neighbour;            

            var sharedEdge = Neighbour.Face.GetSharedEdge(Pivot.Face);
            int pivotSharedIndex = Pivot.Face.GetEdgeIndex(e => e.HasSamePositions(sharedEdge));
            int neighbourSharedIndex = Neighbour.Face.GetEdgeIndex(e => e.HasSamePositions(sharedEdge));

            var vertexAdapter = new VertexAdapter(pivot, neighbour);
            var topEdge = neighbour.Face.GetEdgeByIndex(0);
            var adjustment = vertexAdapter.IndexOf(topEdge.B);

            switch (pivotSharedIndex)
            {
                case 0:
                    switch (neighbourSharedIndex)
                    {
                        case 0:
                            _grid = _grid
                                .RotateMatrixClockwise()
                                .RotateMatrixClockwise();
                            break;
                        case 1:
                            _grid = _grid.RotateMatrixClockwise();
                            break;
                        case 2:
                            break;
                        case 3:
                            _grid = _grid.RotateMatrixCounterClockwise();
                            break;
                    }

                    break;
                case 1:
                    switch (neighbourSharedIndex)
                    {
                        case 0:
                            _grid = _grid
                                .RotateMatrixClockwise()
                                .RotateMatrixClockwise();
                            break;
                        case 1:
                            _grid = _grid.RotateMatrixClockwise();
                            break;
                        case 2:
                            break;
                        case 3:
                            break;
                    }

                    break;
                case 2:
                    break;
                case 3:

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pivotSharedIndex));
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
    }
}
