namespace RoadNetworkGenerator
{
    public class RoadGenerator
    {
        private readonly PriorityQueue<Sucessor, int> _sucessors = new();
        private readonly InputData _inputData;
        private readonly GlobalGoalsProcessor _globalGoals;
        private readonly LocalConstrainter _localConstraints;

        public readonly RoadSegment Initial;

        public RoadGenerator(InputData data)
        {
            _inputData = data;
            _globalGoals = new GlobalGoalsProcessor(_inputData);
            _localConstraints = new LocalConstrainter(_inputData);

            Initial = new RoadSegment(data.Start, data.Start);

            var sucessor = new Sucessor()
            {
                RoadType = RoadType.Main,
                Time = 0,
                Parent = Initial,
                BranchStart = data.Start,
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

            if (_localConstraints.Process(sucessor))
            {
                newSegment = new RoadSegment(sucessor.Parent.End, sucessor.LocalGoal);
                sucessor.Parent.Children.Add(newSegment);
                var (a, b, c) = _globalGoals.Process(sucessor, newSegment);
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