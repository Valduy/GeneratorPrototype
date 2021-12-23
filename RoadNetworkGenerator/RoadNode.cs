using OpenTK.Mathematics;

namespace RoadNetworkGenerator
{
    public class RoadNode
    {
        public readonly List<RoadNode> Neighbours = new();
        public Vector2 Position { get; set; }
    }
}
