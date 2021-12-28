using Graph;
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
        /// Node-ancestor of this sucessor.
        /// </summary>
        public Node<Sucessor>? Parent;

        /// <summary>
        /// Destination point for current sucessor (local goal).
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Pursued goal by current brunch (global goal).
        /// </summary>
        public Vector2 Goal;

        public object Clone() => new Sucessor()
        {
            SucessorType = SucessorType,
            Time = Time,
            Parent = Parent,
            Position = Position,
            Goal = Goal
        };
    }
}
