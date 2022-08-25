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
            var faces = mesh.ExtractFaces();

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

        public TopologyNode<T> this[int index] 
            => _nodes[index];

        public IEnumerator<TopologyNode<T>> GetEnumerator()
            => _nodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
