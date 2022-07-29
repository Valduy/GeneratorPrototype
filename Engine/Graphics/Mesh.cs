using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Mesh
    {
        public static readonly Mesh Empty = new(Enumerable.Empty<Vertex>(), Enumerable.Empty<int>());

        public readonly IReadOnlyList<Vertex> Vertices;
        public readonly IReadOnlyList<int> Indices;

        public Mesh(IEnumerable<Vertex> vertices, IEnumerable<int> indices)
        {
            Vertices = vertices.ToList();
            Indices = indices.ToList();
        }
    }
}
