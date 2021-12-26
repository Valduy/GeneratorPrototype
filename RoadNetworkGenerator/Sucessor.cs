using OpenTK.Mathematics;

namespace RoadNetworkGenerator
{
    public enum SucessorType
    {
        Pivot,
        Main,
        Branch,
    }

    public class Sucessor : ICloneable
    {
        /// <summary>
        /// Type of potential road segment.
        /// </summary>
        public SucessorType SucessorType;

        /// <summary>
        /// Actually, priority in sucessors priority queue.
        /// </summary>
        public int Time;

        /// <summary>
        /// Segment-ancestor of this sucessor.
        /// </summary>
        public RoadNode Parent;

        /// <summary>
        /// Destination point for current sucessor (local goal).
        /// </summary>
        public Vector2 LocalGoal;

        /// <summary>
        /// Pursued goal by current brunch (global goal).
        /// </summary>
        public Vector2 GlobalGoal;

        public object Clone() => new Sucessor()
        {
            SucessorType = SucessorType,
            Time = Time,
            Parent = Parent,
            LocalGoal = LocalGoal,
            GlobalGoal = GlobalGoal
        };
    }
}
