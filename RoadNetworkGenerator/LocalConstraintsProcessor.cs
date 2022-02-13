using GameEngine.Mathematics;
using Graph;
using OpenTK.Mathematics;

namespace RoadNetworkGenerator
{
    public class LocalConstraintsProcessor
    {
        public readonly InputData InputData;
        public readonly Net<Sucessor> Net;

        public LocalConstraintsProcessor(InputData inputData, Net<Sucessor> net)
        {
            InputData = inputData;
            Net = net;
        }

        public Node<Sucessor>? Process(Sucessor sucessor)
        {
            //// now, just fail
            //if (sucessor.SucessorType == SucessorType.Branch)
            //{
            //    return null;
            //}
            
            if (sucessor.Parent != null && sucessor.Parent.Neighbours.Count >= 4)
            {
                return null;
            }

            if (Vector2.Distance(sucessor.Goal, sucessor.Position) <= Constants.SegmentLength)
            {
                sucessor.Position = sucessor.Goal;
            }

            var destinations = GetPotentialDestinations(sucessor).ToList();

            // TODO: connection with edges
            if (destinations.Any())
            {
                var destination = destinations.First();

                // TODO: now, but lately should do something smarter...
                if (destination.Neighbours.Count >= 4) return null;

                sucessor.Position = destination.Item.Position;
                Net.Connect(sucessor.Parent!, destination);
                return null;
            }

            return CreateNode(sucessor);
        }

        private Node<Sucessor> CreateNode(Sucessor sucessor)
        {
            var node = Net.CreateNode(sucessor);

            if (sucessor.Parent != null)
            {
                Net.Connect(sucessor.Parent, node);
            }

            return node;
        }

        private IEnumerable<Node<Sucessor>> GetPotentialDestinations(Sucessor sucessor)
        {
            if (sucessor.Parent == null) return Enumerable.Empty<Node<Sucessor>>();

            return Net
                .GetNodes()
                .Where(n => IsValidDestination(sucessor, n))
                .OrderBy(n => Vector2.Distance(sucessor.Parent.Item.Position, n.Item.Position));
        }

        private bool IsValidDestination(Sucessor sucessor, Node<Sucessor> destination) 
            => destination != sucessor.Parent
               //&& destination.Item.SucessorType is SucessorType.Main or SucessorType.Pivot
               && IsDestinationInPerceptionRadius(sucessor, destination)
               && IsDestinationInFrontArc(sucessor, destination);

        private bool IsDestinationInPerceptionRadius(Sucessor sucessor, Node<Sucessor> destination)
        {
            var sourcePosition = sucessor.Parent!.Item.Position;
            var destinationPosition = destination.Item.Position;
            return Vector2.Distance(sourcePosition, destinationPosition) < Constants.PerceptionRadius;
        }

        private bool IsDestinationInFrontArc(Sucessor sucessor, Node<Sucessor> destination)
        {
            var vector = sucessor.Position - sucessor.Parent!.Item.Position;
            var toOther = destination.Item.Position - sucessor.Parent.Item.Position;
            return System.Math.Abs(MathHelper.RadiansToDegrees(Mathematics.Angle(vector, toOther))) <= Constants.FrontArc / 2;
        }
    }
}
