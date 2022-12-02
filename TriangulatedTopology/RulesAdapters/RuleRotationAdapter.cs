using GameEngine.Helpers;
using MeshTopology;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;

namespace TriangulatedTopology.RulesAdapters
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

    public class RuleRotationAdapter : RuleAdapter
    {
        private int _factor;
        private bool _isShouldTranspose;

        private Vector2i _origin;
        private Vector2i _xAxis;
        private Vector2i _yAxis;

        public readonly TopologyNode Pivot;
        public readonly TopologyNode Neighbour;

        public RuleRotationAdapter(TopologyNode pivot, TopologyNode neighbour, int size) 
            : base(size)
        {
            if (!pivot.Neighbours.Contains(neighbour))
            {
                throw new ArgumentException();
            }

            Pivot = pivot;
            Neighbour = neighbour;

            var pivotSharedEdge = pivot.Face.GetSharedEdge(neighbour.Face);
            // We can use edge index, because it is the same as correspondent intial vertex index.
            var pivotVertexIndex = pivot.Face.GetEdgeIndex(e => e.HasSamePositions(pivotSharedEdge));

            var expectedAnchorNeighbourVertexIndex = GetExpectedNeighbourVertexIndex(pivotVertexIndex);
            var anchorNeighbourVertex = neighbour.Face.First(v => v.Position == pivotSharedEdge.A.Position);
            var anchorNeighbourVertexIndex = neighbour.Face.IndexOf(anchorNeighbourVertex);

            var secondaryNeighbourVertex = neighbour.Face.First(v => v.Position == pivotSharedEdge.B.Position);
            var secondaryNeighbourVertexIndex = neighbour.Face.IndexOf(secondaryNeighbourVertex);

            bool isShouldTranspose = (secondaryNeighbourVertexIndex + 1) % 4 != anchorNeighbourVertexIndex;
            int factor = anchorNeighbourVertexIndex - expectedAnchorNeighbourVertexIndex;
            int rotations = MathHelper.Abs(factor);
            var direction = factor < 0
                ? RotationDirection.Negative
                : RotationDirection.Positive;

            // For debugging
            _isShouldTranspose = isShouldTranspose;
            _factor = factor;

            var origins = new CoordSystem[]
            {
                new(new Vector2i(0, 0), 
                    new Vector2i(1, 0), 
                    new Vector2i(0, 1)),
                new(new Vector2i(0, Size - 1), 
                    new Vector2i(0, -1), 
                    new Vector2i(1, 0)),
                new(new Vector2i(Size - 1, Size - 1), 
                    new Vector2i(-1, 0), 
                    new Vector2i(0, -1)),
                new(new Vector2i(Size - 1, 0), 
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

            _origin = origins[0].Origin;
            _xAxis = origins[0].XAxis;
            _yAxis = origins[0].YAxis;

            if (isShouldTranspose)
            {
                var temp = _xAxis;
                _xAxis = _yAxis;
                _yAxis = temp;
            }
        }

        public override Color Access(Rule rule, int x, int y)
        {
            if (x < 0 || x >= Size)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (y < 0 || y >= Size)
            {
                throw new ArgumentOutOfRangeException(nameof(y));
            }

            var accessor = _origin + x * _xAxis + y * _yAxis;
            return rule.Logical[accessor.X, accessor.Y];
        }

        private int GetExpectedNeighbourVertexIndex(int pivonVertexIndex)
        {
            switch (pivonVertexIndex)
            {
                case 0:
                    return 3;
                case 1:
                    return 0;
                case 2:
                    return 1;
                case 3:
                    return 2;
                default:
                    throw new IndexOutOfRangeException(nameof(pivonVertexIndex));
            }
        }
    }
}
