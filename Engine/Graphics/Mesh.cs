using System.Collections;

namespace GameEngine.Graphics
{
    public class Mesh : IReadOnlyList<float>
    {
        private readonly float[] _vertices;

        public int Count => _vertices.Length;
        public IReadOnlyList<float> Vertices => _vertices;

        public Mesh(float[] vertices)
        {
            _vertices = vertices;
        }

        public IEnumerator<float> GetEnumerator()
        {
            foreach (var vertex in _vertices)
            {
                yield return vertex;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
        
        public float this[int index] => _vertices[index];
    }
}
