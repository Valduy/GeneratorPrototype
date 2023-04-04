using MeshTopology;
using System.Diagnostics.CodeAnalysis;

namespace UVWfc.LevelGraph
{
    public class EdgeComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge? x, Edge? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is not null && y is not null)
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
