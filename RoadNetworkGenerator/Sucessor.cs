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
        public RoadSegment Parent;

        /// <summary>
        /// Initial point of current branch (one of pivot point).
        /// </summary>
        public Vector2 BranchStart;

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
            BranchStart = BranchStart,
            LocalGoal = LocalGoal,
            GlobalGoal = GlobalGoal
        };
    }
}
