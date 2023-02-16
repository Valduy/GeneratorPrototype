using OpenTK.Mathematics;
using System.Collections;

namespace GameEngine.Helpers
{
    public class MatrixView<T> : IReadOnlyList<T>
    {
        private T[,] _matrix;
        private Vector2i _from;
        private Vector2i _to;
        private Vector2i _axis;
        private int _count;

        public int Count => _count;

        public MatrixView(T[,] matrix, Vector2i from, Vector2i to)
        {
            if (!IsAxisAligned(from, to))
            {
                throw new ArgumentException($"{nameof(from)} and {nameof(to)} is not axis aligned.");
            }
            if (!IsInRange(Vector2.Zero, new Vector2(matrix.GetLength(0), matrix.GetLength(1)), from))
            {
                throw new ArgumentException($"{nameof(from)} is out of range.");
            }
            if (!IsInRange(Vector2.Zero, new Vector2(matrix.GetLength(0), matrix.GetLength(1)), to))
            {
                throw new ArgumentException($"{nameof(to)} is out of range.");
            }

            _matrix = matrix;
            _from = from;
            _to = to;
            _axis = to - from;
            _axis.X = MathF.Sign(_axis.X);
            _axis.Y = MathF.Sign(_axis.Y);
            _count = (_to - _from).ManhattanLength;
        }

        public T this[int index]
        {
            get
            {
                var accessor = _from + _axis * index;
                return _matrix[accessor.X, accessor.Y];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var temp = _from; temp != _to; temp += _axis)
            {
                yield return _matrix[temp.X, temp.Y];
            }

            yield return _matrix[_to.X, _to.Y];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool IsAxisAligned(Vector2i a, Vector2i b)
        {
            return a.X == b.X || a.X == b.Y || a.Y == b.X || a.Y == b.Y;
        }

        private bool IsInRange(Vector2 aa, Vector2 bb, Vector2 point)
        {
            return point.X >= aa.X && point.Y >= aa.X && point.X < bb.X && point.Y < bb.Y;
        }
    }
}
