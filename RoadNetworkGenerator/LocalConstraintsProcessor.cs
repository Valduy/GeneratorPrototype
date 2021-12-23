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

        public bool Process(Sucessor sucessor)
        {
            if (sucessor.SucessorType == SucessorType.Branch)
            {
                //TODO: Now just deprecate brunching, but it,s nonsense...
                return false;
            }

            if (sucessor.SucessorType == SucessorType.Pivot)
            {
                return true;
            }
            
            if (Vector2.Distance(sucessor.GlobalGoal, sucessor.LocalGoal) <= _inputData.SegmentLength)
            {
                sucessor.LocalGoal = sucessor.GlobalGoal;
                return true;
            }

            // TODO: correct according to road intersection possibilities
            // TODO: make "perception" to find roads for merge
            return true;
        }
    }
}
