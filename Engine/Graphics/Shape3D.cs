using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Shape3D
    {
        private readonly List<Vector3> _vertices;
        private readonly List<Vector3> _normals;

        public int Count => _vertices.Count;

        public IReadOnlyList<Vector3> Vertices => _vertices;
        public IReadOnlyList<Vector3> Normals => _normals;

        public Shape3D(IEnumerable<Vector3> vertices, IEnumerable<Vector3> normals)
        {
            _vertices = vertices.ToList();
            _normals = normals.ToList();
        }
    }
}
