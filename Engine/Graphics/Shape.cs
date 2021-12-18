using System.Collections;
using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Shape : IReadOnlyList<Vector2>
    {
        private readonly List<Vector2> _vertices;

        public int Count => _vertices.Count;

        public Shape(IEnumerable<Vector2> vertices)
        {
            _vertices = new List<Vector2>(vertices);
        }

        public Vector2 this[int index] 
            => _vertices[index];

        public IEnumerator<Vector2> GetEnumerator() 
            => _vertices.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }
}
