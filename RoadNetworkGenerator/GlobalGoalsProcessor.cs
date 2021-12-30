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
            SucessorType.Branch => ProcessBranch(node),
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
                node.Item.SucessorType = SucessorType.Pivot;
                result = SetNewGoals(node);
            }
            // Else: just branch and grow
            else
            {
                var prevDirection = node.Item.Position - node.Item.Parent!.Item.Position;
                var desiredDirection = TryGetInfluencer(node, out var influencer) 
                    ? (influencer!.Value - node.Item.Position) 
                    : node.Item.Goal - node.Item.Position;

                int sign = System.Math.Sign(Math.Cross(prevDirection, desiredDirection));
                var angle = MathHelper.RadiansToDegrees(Math.Angle(prevDirection, desiredDirection));
                angle = System.Math.Min(angle, Constants.MaxAngle) * sign;
  
                var forward = prevDirection.Normalized();
                forward *= Constants.SegmentLength;
                forward = Math.Rotate(forward, angle);

                result.a = GrowMain(node, forward + node.Item.Position);

                if (IsCanBrunch(node))
                {
                    var left = Math.Rotate(forward, 90);
                    result.b = CreateBrunch(node, left + node.Item.Position);

                    var right = Math.Rotate(forward, -90);
                    result.c = CreateBrunch(node, right + node.Item.Position);
                }
            }

            return result;
        }

        private (Sucessor? a, Sucessor? b, Sucessor? c) ProcessBranch(Node<Sucessor> node)
        {
            (Sucessor? a, Sucessor? b, Sucessor? c) result = (null, null, null);

            var forward = (node.Item.Position - node.Item.Parent!.Item.Position).Normalized();
            forward *= Constants.SegmentLength;
            result.a = GrowBrunch(node, forward + node.Item.Position);

            if (IsCanBrunch(node))
            {
                var left = Math.Rotate(forward, 90);
                result.b = GrowBrunch(node, left + node.Item.Position);

                var right = Math.Rotate(forward, -90);
                result.c = GrowBrunch(node, right + node.Item.Position);
            }

            return result;
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
            Position = node.Item.Position + Constants.SegmentLength * (newGlobalGoal - node.Item.Position).Normalized(),
            Goal = newGlobalGoal,
        };

        private Sucessor GrowMain(Node<Sucessor> node, Vector2 position)
        {
            var mainSucessor = (Sucessor)node.Item.Clone();
            mainSucessor.Parent = node;
            mainSucessor.Position = position;
            return mainSucessor;
        }

        private Sucessor CreateBrunch(Node<Sucessor> node, Vector2 position) => new()
        {
            SucessorType = SucessorType.Branch,
            Time = 1000 + 1, // TODO: think about it...
            Parent = node,
            Goal = position, // TODO: and about it...
            Position = position,
        };

        private Sucessor GrowBrunch(Node<Sucessor> node, Vector2 position) => new()
        {
            SucessorType = SucessorType.Branch,
            Time = node.Item.Time + 1,
            Parent = node,
            Goal = position, // TODO: and about it...
            Position = position,
        };

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

        private bool TryGetInfluencer(Node<Sucessor> node, out Vector2? influencer)
        {
            influencer = InputData.ImportantPoints
                .OrderBy(p => Vector2.Distance(node.Item.Position, p))
                .FirstOrDefault();
            
            return Vector2.Distance(node.Item.Position, influencer.Value) <= Constants.InfluenceRadius
                && Vector2.Dot(node.Item.Position - node.Item.Parent!.Item.Position, influencer.Value - node.Item.Position) > 0;
        }

        private bool IsCanBrunch(Node<Sucessor> node)
        {
            for (int i = 0; i < Constants.BrunchingDistance; i++)
            {
                if (node.Neighbours.Count > 2 || node.Item.Parent == null)
                {
                    return false;                    
                }

                node = node.Item.Parent;
            }

            return true;
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
