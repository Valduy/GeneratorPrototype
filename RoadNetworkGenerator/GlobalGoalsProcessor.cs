using OpenTK.Mathematics;
using Math = GameEngine.Mathematics.Math;

namespace RoadNetworkGenerator
{
    public class GlobalGoalsProcessor
    {
        private readonly Vector2[] _pivots;
        public readonly InputData InputData;

        public GlobalGoalsProcessor(InputData inputData)
        {
            InputData = inputData;
            _pivots = inputData.Goals.Append(inputData.Start).ToArray();
        }

        // TODO: don't forget set right time for segments!!! 
        // TODO: don't forget provide relationships!!!
        public (Sucessor? a, Sucessor? b, Sucessor? right) Process(Sucessor sucessor, RoadSegment segment) => sucessor.RoadType switch
        {
            RoadType.Main => ProcessMain(sucessor, segment),
            RoadType.Branch => ProcessBranch(sucessor),
            _ => throw new ArgumentOutOfRangeException()
        };

        private (Sucessor? a, Sucessor? b, Sucessor? c) ProcessMain(Sucessor sucessor, RoadSegment segment)
        {
            Sucessor? a = null;
            Sucessor? b = null;
            Sucessor? c = null;

            // If: goal achieved.
            if (IsGoalAchieved(sucessor))
            {
                // Then: choose new goals and create for them sucessors.
                return ChooseNewGoals(sucessor)
                    .Select(p => CreateNewGlobalBranch(sucessor, segment, p))
                    .ToTriple();
            }

            // Else: just branch and grow
            a = GrowMain(sucessor, segment);
            b = BrunchMain(sucessor, segment, 90);
            c = BrunchMain(sucessor, segment, -90);

            return (a, b, c);
        }

        private (Sucessor? a, Sucessor? b, Sucessor? c) ProcessBranch(Sucessor sucessor)
        {
            throw new NotImplementedException();
        }

        private bool IsGoalAchieved(Sucessor sucessor) 
            => _pivots.Any(pivot => Math.Equal(pivot, sucessor.LocalGoal, Constants.FloatEpsilon));

        private Vector2[] ChooseNewGoals(Sucessor sucessor) => _pivots
            .Except(new[] { sucessor.BranchStart, sucessor.LocalGoal }, new Vector2Comparer())
            .OrderBy(p => Vector2.Distance(p, sucessor.LocalGoal))
            .Take(System.Math.Min(Constants.Brunches, _pivots.Length - 2))
            .ToArray();

        private Sucessor CreateNewGlobalBranch(Sucessor sucessor, RoadSegment segment, Vector2 newGlobalGoal) => new()
        {
            RoadType = RoadType.Main,
            Time = sucessor.Time + 1,
            Parent = segment,
            BranchStart = segment.End,
            LocalGoal = InputData.SegmentLength * (newGlobalGoal - segment.End).Normalized(),
            GlobalGoal = newGlobalGoal,
        };

        private Sucessor GrowMain(Sucessor sucessor, RoadSegment segment)
        {
            var mainSucessor = (Sucessor)sucessor.Clone();
            mainSucessor.Parent = segment;
            mainSucessor.LocalGoal = InputData.SegmentLength * (mainSucessor.GlobalGoal - mainSucessor.Parent.End).Normalized();
            return mainSucessor;
        }

        private Sucessor BrunchMain(Sucessor sucessor, RoadSegment segment, float angle)
        {
            var branchDirection = new Vector3(sucessor.LocalGoal.X, sucessor.LocalGoal.Y, 1);
            branchDirection *= Matrix3.CreateRotationZ(MathHelper.DegreesToRadians(angle));
            branchDirection.Normalize();
            branchDirection *= InputData.SegmentLength;
            var offset = new Vector2(branchDirection.X, branchDirection.Y);
            var localGoal = sucessor.LocalGoal + offset;
            
            return new Sucessor()
            {
                RoadType = RoadType.Branch,
                Time = sucessor.Time + 1, // TODO: Set relevant time...
                Parent = segment,
                BranchStart = segment.End,
                LocalGoal = localGoal,
                //GlobalGoal = TODO: Rich main road, for example...
            };
        }
    }
}
