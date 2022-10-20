using GameEngine.Graphics;
using System.Collections;

namespace MeshTopology
{
    public class MeshTopology : IReadOnlyList<TopologyNode>
    {
        private List<TopologyNode> _nodes = new();

        public int VerticesPerFace;
        public int Count => _nodes.Count;

        public MeshTopology(Mesh mesh, int verticesPerFace)
        {
            VerticesPerFace = verticesPerFace;
            var faces = mesh.ExtractFaces(VerticesPerFace);

            foreach (var face in faces)
            {
                _nodes.Add(new TopologyNode(face));
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

        public TopologyNode this[int index] 
            => _nodes[index];

        public IEnumerator<TopologyNode> GetEnumerator()
            => _nodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
