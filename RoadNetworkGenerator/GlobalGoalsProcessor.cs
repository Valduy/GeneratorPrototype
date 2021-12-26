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
        public (Sucessor? a, Sucessor? b, Sucessor? c) Process(Sucessor sucessor, RoadNode node) => sucessor.SucessorType switch
        {
            SucessorType.Pivot => SetNewGoals(sucessor, node),
            SucessorType.Main => ProcessMain(sucessor, node),
            SucessorType.Branch => ProcessBranch(sucessor),
            _ => throw new ArgumentOutOfRangeException()
        };

        private (Sucessor? a, Sucessor? b, Sucessor? right) SetNewGoals(Sucessor sucessor, RoadNode node) 
            => ChooseNewGoals(sucessor)
                .Select(p => CreateNewGlobalBranch(sucessor, node, p))
                .ToTriple();

        private (Sucessor? a, Sucessor? b, Sucessor? c) ProcessMain(Sucessor sucessor, RoadNode node)
        {
            (Sucessor? a, Sucessor? b, Sucessor? c) result = (null, null, null);

            // If: goal achieved.
            if (IsGoalAchieved(sucessor))
            {
                result.a = CreatePivot(sucessor, node);
            }
            // Else: just branch and grow
            else
            {
                result.a = GrowMain(sucessor, node);
                //result.b = BrunchMain(sucessor, segment, 90);
                //result.c = BrunchMain(sucessor, segment, -90);
            }

            return result;
        }

        private (Sucessor? a, Sucessor? b, Sucessor? c) ProcessBranch(Sucessor sucessor)
        {
            throw new NotImplementedException();
        }

        private bool IsGoalAchieved(Sucessor sucessor) 
            => _pivots.Any(pivot => Math.Equal(pivot, sucessor.LocalGoal, Constants.FloatEpsilon));

        // TODO: Try to achieve different goals and use this info for goals
        private IEnumerable<Vector2> ChooseNewGoals(Sucessor sucessor) => _pivots
            //.Except(new[] {sucessor.BranchStart, sucessor.LocalGoal}, new Vector2Comparer())
            .OrderBy(p => Vector2.Distance(p, sucessor.LocalGoal))
            .Take(Constants.Brunches);

        private Sucessor CreatePivot(Sucessor sucessor, RoadNode node) => new()
        {
            SucessorType = SucessorType.Pivot,
            Time = sucessor.Time + 1,
            Parent = node,
            LocalGoal = node.Position,
            GlobalGoal = node.Position
        };

        private Sucessor CreateNewGlobalBranch(Sucessor sucessor, RoadNode node, Vector2 newGlobalGoal) => new()
        {
            SucessorType = SucessorType.Main,
            Time = sucessor.Time + 1,
            Parent = node,
            LocalGoal = node.Position + InputData.SegmentLength * (newGlobalGoal - node.Position).Normalized(),
            GlobalGoal = newGlobalGoal,
        };

        private Sucessor GrowMain(Sucessor sucessor, RoadNode node)
        {
            var mainSucessor = (Sucessor)sucessor.Clone();
            mainSucessor.Parent = node;
            mainSucessor.LocalGoal = node.Position + InputData.SegmentLength * (mainSucessor.GlobalGoal - node.Position).Normalized();
            return mainSucessor;
        }

        //private Sucessor BrunchMain(Sucessor sucessor, RoadSegment segment, float angle)
        //{
        //    var branchDirection = new Vector3(sucessor.LocalGoal.X, sucessor.LocalGoal.Y, 1);
        //    branchDirection *= Matrix3.CreateRotationZ(MathHelper.DegreesToRadians(angle));
        //    branchDirection.Normalize();
        //    branchDirection *= InputData.SegmentLength;
        //    var offset = new Vector2(branchDirection.X, branchDirection.Y);
        //    var localGoal = sucessor.LocalGoal + offset;
            
        //    return new Sucessor()
        //    {
        //        SucessorType = SucessorType.Branch,
        //        Time = sucessor.Time + 1, // TODO: Set relevant time...
        //        Parent = segment,
        //        BranchStart = segment.End,
        //        LocalGoal = localGoal,
        //        //GlobalGoal = TODO: Rich main road, for example...
        //    };
        //}
    }
}
