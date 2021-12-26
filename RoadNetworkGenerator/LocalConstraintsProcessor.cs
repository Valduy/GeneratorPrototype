using OpenTK.Mathematics;

namespace RoadNetworkGenerator
{
    public class LocalConstraintsProcessor
    {
        private readonly InputData _inputData;

        public LocalConstraintsProcessor(InputData inputData)
        {
            _inputData = inputData;
        }

        public RoadNode? Process(Sucessor sucessor)
        {
            if (sucessor.SucessorType == SucessorType.Branch)
            {
                return null;
            }

            if (sucessor.SucessorType == SucessorType.Pivot)
            {
                return null;
            }

            if (Vector2.Distance(sucessor.GlobalGoal, sucessor.LocalGoal) <= _inputData.SegmentLength)
            {
                sucessor.LocalGoal = sucessor.GlobalGoal;
                var node = CreateNode(sucessor);
            }

            return CreateNode(sucessor);
        }

        private void ConnectWithNode()
        {

        }

        private void ConnectWithSegment()
        {

        }

        private RoadNode CreateNode(Sucessor sucessor)
        {
            var node = new RoadNode(sucessor.LocalGoal);
            node.Neighbours.Add(node);
            node.Neighbours.Add(sucessor.Parent);
            return node;
        }
    }
}
