using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Mesh
    {
        public static readonly Mesh Empty = new(
            Enumerable.Empty<Vertex>(), 
            Enumerable.Empty<int>(),
            Enumerable.Empty<VertexWeights>());

        public readonly IReadOnlyList<Vertex> Vertices;
        public readonly IReadOnlyList<int> Indices;
        public readonly IReadOnlyList<VertexWeights> Weights;

        public Mesh(IEnumerable<Vertex> vertices, IEnumerable<int> indices)
        {
            Vertices = vertices.ToList();
            Indices = indices.ToList();
            Weights = new List<VertexWeights>();
        }

        public Mesh(IEnumerable<Vertex> vertices, IEnumerable<int> indices, IEnumerable<VertexWeights> weights)
        {
            Vertices = vertices.ToList();
            Indices = indices.ToList();
            Weights = weights.ToList();
        }
    }
}
