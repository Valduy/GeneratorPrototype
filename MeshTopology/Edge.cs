using GameEngine.Graphics;
using GameEngine.Mathematics;

namespace MeshTopology
{
    public class Edge
    {
        public readonly Vertex A;
        public readonly Vertex B;

        public Edge(Vertex a, Vertex b)
        {
            A = a;
            B = b;
        }

        public bool HasSamePositions(Edge other)
        {
            return this.A.Position == other.A.Position && this.B.Position == other.B.Position
                || this.A.Position == other.B.Position && this.B.Position == other.A.Position;
            //return Mathematics.ApproximatelyEqualEpsilon(this.A.Position, other.A.Position, 0.1f)
            //    && Mathematics.ApproximatelyEqualEpsilon(this.B.Position, other.B.Position, 0.1f)
            //    || Mathematics.ApproximatelyEqualEpsilon(this.A.Position, other.B.Position, 0.1f)
            //    && Mathematics.ApproximatelyEqualEpsilon(this.B.Position, other.A.Position, 0.1f);
        }

        public bool HasSameUV(Edge other)
        {
            return this.A.TextureCoords == other.A.TextureCoords && this.B.TextureCoords == other.B.TextureCoords
                || this.A.TextureCoords == other.B.TextureCoords && this.B.TextureCoords == other.A.TextureCoords;
        }
    }
}
