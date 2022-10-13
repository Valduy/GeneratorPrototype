using GameEngine.Graphics;
using System.Collections;

namespace MeshTopology
{
    public class Face : IReadOnlyList<Vertex>
    {
        private readonly List<Vertex> _vertices;

        public int Count => _vertices.Count;

        public Vertex this[int index] => _vertices[index];

        public Face(IEnumerable<Vertex> vertices)
        {
            _vertices = new List<Vertex>(vertices);
        }

        public IEnumerator<Vertex> GetEnumerator() => _vertices.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _vertices.GetEnumerator();
    }
}
