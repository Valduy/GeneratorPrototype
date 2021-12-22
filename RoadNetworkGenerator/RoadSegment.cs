using OpenTK.Mathematics;

namespace RoadNetworkGenerator
{
    public class RoadSegment
    {
        public readonly List<RoadSegment> Children = new();

        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        
        public RoadSegment() {}

        public RoadSegment(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
    }
}
