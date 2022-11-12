using MeshTopology;
using System.Diagnostics.CodeAnalysis;

namespace TriangulatedTopology
{
    public class EdgeComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge? x, Edge? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x != null && y != null)
            {
                return x.HasSamePositions(y);
            }

            return false;
        }

        public int GetHashCode([DisallowNull] Edge edge)
        {
            return edge.A.Position.GetHashCode() ^ edge.B.Position.GetHashCode();
        }
    }

}
