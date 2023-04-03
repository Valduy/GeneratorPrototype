using OpenTK.Mathematics;

namespace TriangulatedTopology.Props
{
    public struct SplineVertex
    {
        public readonly Vector3 Position;
        public readonly Vector3 Up;
        public readonly Vector3 Forward;

        public SplineVertex(Vector3 position, Vector3 up, Vector3 forward)
        {
            Position = position;
            Up = up;
            Forward = forward;
        }
    }
}
