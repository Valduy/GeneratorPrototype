using GameEngine.Helpers;
using GameEngine.Graphics;
using System.Collections;
using MeshTopology;
using OpenTK.Mathematics;

namespace TriangulatedTopology
{
    public class VertexAdapter : IReadOnlyList<Vertex>
    {
        private int _adjustment;

        public readonly TopologyNode Pivot;
        public readonly TopologyNode Neighbour;

        public int Count => Neighbour.Face.Count;

        public Vertex this[int index]
        {
            get
            {
                if (index < 0 || index >= 4)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return Neighbour.Face.GetCircular(_adjustment + index);
            }
        }

        public Cell this[int x, int y]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public VertexAdapter(TopologyNode pivot, TopologyNode neighbour)
        {
            if (!pivot.Neighbours.Contains(neighbour))
            {
                throw new ArgumentException();
            }

            Pivot = pivot;
            Neighbour = neighbour;

            var sharedEdge = Neighbour.Face.GetSharedEdge(Pivot.Face);
            var sharedIndex = Pivot.Face.GetEdgeIndex(e => e.HasSamePositions(sharedEdge));

            _adjustment = Neighbour.Face.IndexOf(v => v.Position == sharedEdge.A.Position);

            switch (sharedIndex)
            {
                case 0:
                    _adjustment -= 2;
                    break;
                case 1:
                    _adjustment += 1;
                    break;
                case 2:                
                    break;
                case 3:
                    _adjustment -= 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sharedIndex));
            }
        }

        public IEnumerator<Vertex> GetEnumerator()
        {
            return Neighbour.Face.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
