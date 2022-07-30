using System.Collections;
using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Line : IReadOnlyList<Vector3>
    {
        private readonly List<Vector3> _vertices;

        public int Count => _vertices.Count;

        public Line(IEnumerable<Vector3> vertices)
        {
            _vertices = new List<Vector3>(vertices);
        }

        public Line(IEnumerable<Vector2> points)
        {
            _vertices = new List<Vector3>(points.Select(p => new Vector3(p.X, p.Y, 0.0f)));
        }

        public Vector3 this[int index] 
            => _vertices[index];

        public IEnumerator<Vector3> GetEnumerator() 
            => _vertices.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }
}
