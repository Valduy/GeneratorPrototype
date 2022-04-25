using OpenTK.Mathematics;
using PipesDemo.Models;
using PipesDemo.Models.Utils;

namespace PipesDemo.Algorithms
{
    public static class PipesAlgorithms
    {
        #region RigidPipes

        public static IEnumerator<Cell> GenerateRigidPipe(this Grid grid, Vector3i from, Vector3i to)
        {
            if (!grid[from].IsFree())
            {
                throw new ArgumentException("Cell is not empty.");
            }
            if (!grid[to].IsFree())
            {
                throw new ArgumentException("Cell is not empty.");
            }

            var current = from;
            grid[current].Type = CellType.Pipe;
            yield return grid[current];

            while (current != to)
            {
                grid[current].Type = CellType.Pipe;
                current = grid.GetCross(current)
                    .Where(GridHelper.IsFree)
                    .OrderByDescending(c => c.Temperature)
                    .First().Position;

                yield return grid[current.X, current.Y, current.Z];
            }
        }

        #endregion

        #region FlexiblePipe

        public const float SplineStep = 0.5f;

        public static IEnumerator<Vector3> GenerateFlexiblePipe(this Grid grid, Vector3i from, Vector3i to)
        {
            if (!grid[from].IsFree())
            {
                throw new ArgumentException("Cell is not empty.");
            }
            if (!grid[to].IsFree())
            {
                throw new ArgumentException("Cell is not empty.");
            }

            Vector3 current = from;
            grid[from].Type = CellType.Pipe;
            yield return current;
            
            while (new Vector3i((int)Math.Round(current.X), (int)Math.Round(current.Y), (int)Math.Round(current.Z)) != to)
            {
                Vector3 direction = grid.Bilinear(current);

                if (MathHelper.ApproximatelyEqualEpsilon(direction.Length, 0, float.Epsilon))
                {
                    direction *= 100000000;
                }

                Console.WriteLine(direction);
                current += direction.Normalized() * SplineStep;

                yield return current;
            }

            yield return to;
        }

        static Vector3 Bilinear(this Grid grid, Vector3 point)
        {
            int xi = Math.Clamp((int)MathF.Floor(point.X), 0, grid.Width - 1);
            int yi = Math.Clamp((int)MathF.Floor(point.Y), 0, grid.Height - 1);
            int zi = Math.Clamp((int)MathF.Floor(point.Z), 0, grid.Depth - 1);

            int xc = Math.Clamp(xi + 1, xi, grid.Width - 1);
            int yc = Math.Clamp(yi + 1, yi, grid.Height - 1);
            int zc = Math.Clamp(zi + 1, zi, grid.Depth - 1);

            float dx = point.X - xi;
            float dy = point.Y - yi;
            float dz = point.Z - zi;

            float h = 1.0f;

            Vector3 c000 = grid.GetGradient(xi, yi, zi, h);
            Vector3 c001 = grid.GetGradient(xi, yi, zc, h);
            Vector3 c010 = grid.GetGradient(xi, yc, zi, h);
            Vector3 c011 = grid.GetGradient(xi, yc, zc, h);
            Vector3 c100 = grid.GetGradient(xc, yi, zi, h);
            Vector3 c101 = grid.GetGradient(xc, yi, zc, h);
            Vector3 c110 = grid.GetGradient(xc, yc, zi, h);
            Vector3 c111 = grid.GetGradient(xc, yc, zc, h);

            Vector3 result = 
                c000 * (1 - dx) * (1 - dy) * (1 - dz) +
                c001 * (1 - dx) * (1 - dy) * (dz)     +
                c010 * (1 - dx) * (dy)     * (1 - dz) +
                c011 * (1 - dx) * (dy)     * (dz) +
                c100 * (dx)     * (1 - dy) * (1 - dz) +
                c101 * (dx)     * (1 - dy) * (dz) +
                c110 * (dx)     * (dy)     * (1 - dz) +
                c111 * (dx)     * (dy)     * (dz);

            return result;
        }

        #endregion

        #region AStar

        public static List<Cell> GenerateAStarPipe(this Grid grid, Vector3i from, Vector3i to)
        {
            var path = AStar(grid, from, to);
            path.ForEach(p => p.Type = CellType.Pipe);
            return path;
        }

        private static List<Cell> AStar(Grid grid, Vector3i from, Vector3i to)
        {
            var start = grid[from];
            var goal = grid[to];

            // Set all nodes costs as infinity.
            foreach (var cell in grid)
            {
                cell.Temperature = float.PositiveInfinity;
                cell.Prev = null;
            }

            start.Temperature = 0;
            var reachable = new HashSet<Cell> { start };
            var explored = new HashSet<Cell>();

            while (reachable.Any())
            {
                var node = ChooseNode(reachable, goal);
                if (node == goal) return BuildPath(node);

                reachable.Remove(node!);
                explored.Add(node!);

                var newReachable = new HashSet<Cell>(grid.GetCross(node!).Where(GridHelper.IsFree));
                newReachable.ExceptWith(explored);

                foreach (var adjacent in newReachable)
                {
                    // Add new reachable.
                    if (!reachable.Contains(adjacent))
                    {
                        adjacent.Direction = node!.Position - adjacent.Position;
                        reachable.Add(adjacent);
                    }

                    float cost = GetCost(grid, node!, adjacent);

                    // Cost recalculation.
                    if (node!.Temperature + cost < adjacent.Temperature)
                    {
                        adjacent.Prev = node;
                        adjacent.Temperature = node.Temperature + cost;
                    }
                }
            }

            throw new ArgumentException("Path not found.");
        }

        static float GetCost(Grid grid, Cell node, Cell adjacent)
        {
            float modifier = 1;

            if (node.Prev != null &&
                MathHelper.ApproximatelyEqualEpsilon((node.Prev.Position - node.Position).EuclideanLength, 1.0f, float.Epsilon) &&
                node.Prev.Position - node.Position == node.Position - adjacent.Position)
            {
                modifier = 0.5f;
            }

            if (grid.GetCube(adjacent).All(GridHelper.IsFree))
            {
                return 10 * modifier;
            }
            if (grid.GetCross(adjacent).All(GridHelper.IsFree))
            {
                return 5 * modifier;
            }

            return 1 * modifier;
        }

        private static Cell? ChooseNode(IEnumerable<Cell> reachable, Cell goal)
        {
            float minCost = float.PositiveInfinity;
            Cell? best = null;

            foreach (var node in reachable)
            {
                float costFromStart = node.Temperature;
                float costToGoal = ManhattanLength(node, goal);
                float totalCost = costFromStart + costToGoal;

                if (minCost > totalCost)
                {
                    minCost = totalCost;
                    best = node;
                }
            }

            return best;
        }

        private static float ManhattanLength(Cell a, Cell b) =>
            Math.Abs(a.Position.X - b.Position.X) +
            Math.Abs(a.Position.Y - b.Position.Y) +
            Math.Abs(a.Position.Z - b.Position.Z);

        private static List<Cell> BuildPath(Cell node)
        {
            var path = new List<Cell>();

            while (node.Prev != null)
            {
                path.Add(node);
                node = node.Prev;
            }

            path.Add(node);
            return path;
        }

        #endregion
    }
}
