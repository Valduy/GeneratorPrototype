using OpenTK.Mathematics;
using System.Collections;

namespace TriangulatedTopology
{
    public class Cell : IReadOnlyList<Vector2>
    {
        private List<Vector2> _points = new();

        public int Count => _points.Count;

        public Cell? Top;
        public Cell? Left;
        public Cell? Bottom;
        public Cell? Right;

        public Vector2 this[int index] => _points[index];

        public Cell(IEnumerable<Vector2> points)
        {
            _points = points.ToList();
        }

        public IEnumerator<Vector2> GetEnumerator()
        {
            return _points.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
