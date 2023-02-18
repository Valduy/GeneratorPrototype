using System.Collections;
using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Line : IReadOnlyList<Vector3>
    {
        private readonly List<Vector3> _points;

        public int Count => _points.Count;

        public Line(IEnumerable<Vector3> points)
        {
            _points = points.ToList();
        }

        public Line(IEnumerable<Vector2> points)
        {
            _points = new List<Vector3>(points.Select(p => new Vector3(p.X, p.Y, 0.0f)));
        }

        public Line(params Vector3[] points)
        {
            _points = points.ToList();
        }

        public Vector3 this[int index] 
            => _points[index];

        public IEnumerator<Vector3> GetEnumerator() 
            => _points.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }
}
