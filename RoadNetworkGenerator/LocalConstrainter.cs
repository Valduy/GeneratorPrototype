namespace RoadNetworkGenerator
{
    public class LocalConstrainter
    {
        private readonly InputData _inputData;

        public LocalConstrainter(InputData inputData)
        {
            _inputData = inputData;
        }

        public bool Process(Sucessor sucessor)
        {
            if (sucessor.RoadType == RoadType.Branch)
            {
                //TODO: Now just deprecate brunching, but it,s nonsense...
                return false;
            }

            // TODO: correct according to road intersection possibilities

            throw new NotImplementedException();
        }
    }
}
