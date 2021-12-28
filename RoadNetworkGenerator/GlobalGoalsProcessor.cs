using Graph;
using OpenTK.Mathematics;
using Math = GameEngine.Mathematics.Math;

namespace RoadNetworkGenerator
{
    public class GlobalGoalsProcessor
    {
        private readonly Vector2[] _pivots;
        public readonly InputData InputData;
        public readonly Net<Sucessor> Net;

        public GlobalGoalsProcessor(InputData inputData, Net<Sucessor> net)
        {
            InputData = inputData;
            Net = net;
            _pivots = inputData.Goals.Append(inputData.Start).ToArray();
        }

        // TODO: don't forget set right time for segments!!! 
        // TODO: don't forget provide relationships!!!
        public (Sucessor? a, Sucessor? b, Sucessor? c) Process(Node<Sucessor> node) => node.Item.SucessorType switch
        {
            SucessorType.Main => ProcessMain(node),
            SucessorType.Branch => ProcessBranch(node.Item),
            _ => throw new ArgumentOutOfRangeException()
        };

        private (Sucessor? a, Sucessor? b, Sucessor? right) SetNewGoals(Node<Sucessor> node) => _pivots
                .Except(GetOneTransitReachablePivots(node))
                .OrderBy(p => Vector2.Distance(p, node.Item.Position))
                .Take(Constants.Brunches)
                .Select(p => CreateNewGlobalBranch(node, p))
                .ToTriple();

        private (Sucessor? a, Sucessor? b, Sucessor? c) ProcessMain(Node<Sucessor> node)
        {
            (Sucessor? a, Sucessor? b, Sucessor? c) result = (null, null, null);

            if (IsGoalAchievable(node))
            {
                return result;
            }
            if (IsGoalAchieved(node.Item))
            {
                //if (TryFindPivotNode(node.Item.Position, out var pivot))
                //{
                //    Net.Connect(node, pivot);
                //}
                //else
                //{
                //    node.Item.SucessorType = SucessorType.Pivot;
                //    result = SetNewGoals(node);
                //}
                node.Item.SucessorType = SucessorType.Pivot;
                result = SetNewGoals(node);
            }
            // Else: just branch and grow
            else
            {
                result.a = GrowMain(node.Item, node);
                //result.b = BrunchMain(sucessor, segment, 90);
                //result.c = BrunchMain(sucessor, segment, -90);
            }

            return result;
        }

        private (Sucessor? a, Sucessor? b, Sucessor? c) ProcessBranch(Sucessor sucessor)
        {
            throw new NotImplementedException();
        }

        private bool IsGoalAchievable(Node<Sucessor> node)
        {
            var visited = new List<Node<Sucessor>> { node };

            foreach (var neighbour in node.Neighbours)
            {
                switch (neighbour.Item.SucessorType)
                {
                    case SucessorType.Pivot:
                        return Math.Equal(neighbour.Item.Position, node.Item.Goal, Constants.FloatEpsilon);
                    case SucessorType.Main when IsGoalAchievable(node.Item.Goal, visited, neighbour):
                        return true;
                }
            }

            return false;
        }

        private bool IsGoalAchievable(Vector2 goal, List<Node<Sucessor>> visited, Node<Sucessor> node)
        {
            visited.Add(node);

            foreach (var neighbour in node.Neighbours.Except(visited))
            {
                switch (neighbour.Item.SucessorType)
                {
                    case SucessorType.Pivot:
                        return Math.Equal(neighbour.Item.Position, goal, Constants.FloatEpsilon);
                    case SucessorType.Main when IsGoalAchievable(goal, visited, neighbour):
                        return true;
                }
            }

            return false;
        }

        private bool IsGoalAchieved(Sucessor sucessor) 
            => _pivots.Any(pivot => Math.Equal(pivot, sucessor.Position, Constants.FloatEpsilon));

        private Sucessor CreateNewGlobalBranch(Node<Sucessor> node, Vector2 newGlobalGoal) => new()
        {
            SucessorType = SucessorType.Main,
            Time = node.Item.Time + 1,
            Parent = node,
            Position = node.Item.Position + InputData.SegmentLength * (newGlobalGoal - node.Item.Position).Normalized(),
            Goal = newGlobalGoal,
        };

        private Sucessor GrowMain(Sucessor sucessor, Node<Sucessor> node)
        {
            var mainSucessor = (Sucessor)sucessor.Clone();
            mainSucessor.Parent = node;
            mainSucessor.Position = node.Item.Position + InputData.SegmentLength * (mainSucessor.Goal - node.Item.Position).Normalized();
            return mainSucessor;
        }

        private IEnumerable<Vector2> GetOneTransitReachablePivots(Node<Sucessor> node)
        {
            var pivots = new List<Vector2> { node.Item.Position };
            var visited = new List<Node<Sucessor>> { node };

            foreach (var neighbour in node.Neighbours)
            {
                if (neighbour.Item.SucessorType == SucessorType.Main)
                {
                    GetOneTransitReachablePivots(pivots, visited, neighbour);
                }
            }

            return pivots;
        }

        private void GetOneTransitReachablePivots(List<Vector2> pivots, List<Node<Sucessor>> visited, Node<Sucessor> node)
        {
            visited.Add(node);

            if (node.Item.SucessorType == SucessorType.Pivot)
            {
                pivots.Add(node.Item.Position);
                return;
            }

            foreach (var neighbour in node.Neighbours.Except(visited))
            {
                if (neighbour.Item.SucessorType is SucessorType.Main or SucessorType.Pivot)
                {
                    GetOneTransitReachablePivots(pivots, visited, neighbour);
                }
            }
        }

        private bool TryFindPivotNode(Vector2 position, out Node<Sucessor>? pivot)
        {
            pivot = Net
                .GetNodes()
                .FirstOrDefault(n => n.Item.Position == position && n.Item.SucessorType == SucessorType.Pivot);

            return pivot != null;
        }

        //private Sucessor BrunchMain(Sucessor sucessor, RoadSegment segment, float angle)
        //{
        //    var branchDirection = new Vector3(sucessor.Position.X, sucessor.Position.Y, 1);
        //    branchDirection *= Matrix3.CreateRotationZ(MathHelper.DegreesToRadians(angle));
        //    branchDirection.Normalize();
        //    branchDirection *= InputData.SegmentLength;
        //    var offset = new Vector2(branchDirection.X, branchDirection.Y);
        //    var localGoal = sucessor.Position + offset;
            
        //    return new Sucessor()
        //    {
        //        SucessorType = SucessorType.Branch,
        //        Time = sucessor.Time + 1, // TODO: Set relevant time...
        //        Parent = segment,
        //        BranchStart = segment.End,
        //        Position = localGoal,
        //        //Goal = TODO: Rich main road, for example...
        //    };
        //}
    }
}
