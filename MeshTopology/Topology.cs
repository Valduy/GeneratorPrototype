using GameEngine.Graphics;
using System.Collections;

namespace MeshTopology
{
    public class Topology<T> : IReadOnlyList<TopologyNode<T>>
    {
        private List<TopologyNode<T>> _nodes = new();

        public int Count => _nodes.Count;

        public Topology(Mesh mesh)
        {
            var faces = ExtractFaces(mesh);

            foreach (var face in faces)
            {
                _nodes.Add(new TopologyNode<T>(face));
            }

            foreach (var node in _nodes)
            {
                foreach (var neighbour in _nodes)
                {
                    if (node == neighbour)
                    {
                        continue;
                    }

                    if (node.Face.IsSharedEdgeExist(neighbour.Face))
                    {
                        node._neighbours.Add(neighbour);
                    }
                }
            }

            foreach (var node in _nodes)
            {
                node._neighbours.Sort((n1, n2) =>
                {
                    int i1 = node.Face.GetEdgeIndex(n1.Face.GetSharedEdge(node.Face));
                    int i2 = node.Face.GetEdgeIndex(n2.Face.GetSharedEdge(node.Face));
                    return i1.CompareTo(i2);
                });
            }
        }

        private List<Face> ExtractFaces(Mesh mesh)
        {
            var result = new List<Face>();

            for (int i = 0; i < mesh.Indices.Count; i += 4)
            {
                result.Add(new Face(new List<Vertex>
                {
                    mesh.Vertices[i + 0],
                    mesh.Vertices[i + 1],
                    mesh.Vertices[i + 2],
                    mesh.Vertices[i + 3]
                }));
            }

            return result;
        }

        public TopologyNode<T> this[int index] 
            => _nodes[index];

        public IEnumerator<TopologyNode<T>> GetEnumerator()
            => _nodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
