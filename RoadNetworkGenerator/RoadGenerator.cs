namespace RoadNetworkGenerator
{
    public class RoadGenerator
    {
        private readonly PriorityQueue<Sucessor, int> _sucessors = new();
        private readonly InputData _inputData;
        private readonly GlobalGoalsProcessor _globalGoals;
        private readonly LocalConstraintsProcessor _localConstraints;

        public readonly RoadNode Initial;

        public RoadGenerator(InputData data)
        {
            _inputData = data;
            _globalGoals = new GlobalGoalsProcessor(_inputData);
            _localConstraints = new LocalConstraintsProcessor(_inputData);

            Initial = new RoadNode(data.Start);

            var sucessor = new Sucessor()
            {
                SucessorType = SucessorType.Main,
                Time = 0,
                Parent = Initial,
                LocalGoal = data.Start,
                GlobalGoal = data.Start,
            };

            _sucessors.Enqueue(sucessor, sucessor.Time);
        }

        public bool Iterate(out RoadSegment? newSegment)
        {
            newSegment = null;
            if (_sucessors.Count == 0) return false;

            var sucessor = _sucessors.Dequeue();
            var node = _localConstraints.Process(sucessor);

            if (node != null)
            {
                newSegment = new RoadSegment(sucessor.Parent.Position, node.Position);
                var (a, b, c) = _globalGoals.Process(sucessor, node);
                TryAddBranch(a);
                TryAddBranch(b);
                TryAddBranch(c);
            }

            return true;
        }

        private bool TryAddBranch(Sucessor? branch)
        {
            if (branch != null)
            {
                _sucessors.Enqueue(branch, branch.Time);
                return true;
            }

            return false;
        }
    }
}