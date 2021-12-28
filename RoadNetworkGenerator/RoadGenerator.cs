using Graph;
using OpenTK.Mathematics;

namespace RoadNetworkGenerator
{
    public class RoadGenerator
    {
        private readonly PriorityQueue<Sucessor, int> _sucessors = new();
        private readonly InputData _inputData;
        private readonly GlobalGoalsProcessor _globalGoals;
        private readonly LocalConstraintsProcessor _localConstraints;

        public readonly Net<Sucessor> Net = new();

        public RoadGenerator(InputData data)
        {
            _inputData = data;
            _globalGoals = new GlobalGoalsProcessor(_inputData, Net);
            _localConstraints = new LocalConstraintsProcessor(_inputData, Net);

            var sucessor = new Sucessor()
            {
                SucessorType = SucessorType.Main,
                Time = 0,
                Parent = null,
                Position = data.Start,
                Goal = data.Start,
            };

            _sucessors.Enqueue(sucessor, sucessor.Time);
        }

        public bool Iterate()
        {
            if (_sucessors.Count == 0) return false;

            var sucessor = _sucessors.Dequeue();
            var node = _localConstraints.Process(sucessor);

            if (node != null)
            {
                var (a, b, c) = _globalGoals.Process(node);
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