using GameEngine.Graphics;

namespace MeshTopology
{
    public class Face
    {
        private readonly List<Vertex> _vertices;

        public IReadOnlyList<Vertex> Vertices => _vertices;

        public Face(IEnumerable<Vertex> vertices)
        {
            _vertices = new List<Vertex>(vertices);
        }
    }
}
