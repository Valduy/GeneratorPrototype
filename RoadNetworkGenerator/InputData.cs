using OpenTK.Mathematics;

namespace RoadNetworkGenerator
{
    public class InputData
    {
        public Vector2 Start;
        public Vector2[] Goals;
        public Vector2[] ImportantPoints;
        public float SegmentLength;
        public float PerceptionRadius => SegmentLength * 2;
    }
}
