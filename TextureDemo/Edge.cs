using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace TextureDemo
{
    public class Edge
    {
        public readonly Vector3 A;
        public readonly Vector3 B;

        public Edge(Vector3 a, Vector3 b)
        {
            A = a;
            B = b;
        }
    }
}
