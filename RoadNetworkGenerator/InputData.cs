using GameEngine.Mathematics;
using OpenTK.Mathematics;

namespace RoadNetworkGenerator
{
    public class InputData
    {
        public Rectangle Area;
        public Vector2 Start;
        public Vector2[] Goals;
        public float SegmentLength;
        public float MinBranchesAngle;
    }
}
