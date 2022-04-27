namespace RoadNetworkGenerator
{
    // TODO: May be move to input data.
    public static class Constants
    {
        public const int Brunches = 3;
        public const int BrunchingDistance = 3;
        public const float BruncingAngle = 88;
        public const float FloatEpsilon = 0.01f;
        public const float FrontArc = 60;
        public const float SegmentLength = 30;
        public const float PerceptionRadius = SegmentLength * 2;
        public const float InfluenceRadius = SegmentLength * 5;
        public const float MaxAngle = 10;
    }
}
