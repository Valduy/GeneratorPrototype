using System.Collections;
using GameEngine.Graphics;

namespace TextureDemo
{
    public class Face : IReadOnlyCollection<Vertex>
    {
        private readonly List<Vertex> _vertices;

        public int Count => _vertices.Count;

        public Face(IEnumerable<Vertex> vertices)
        {
            _vertices = vertices.ToList();
        }

        public Vertex this[int index] 
            => _vertices[index];

        public IEnumerator<Vertex> GetEnumerator() 
            => _vertices.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }
}
