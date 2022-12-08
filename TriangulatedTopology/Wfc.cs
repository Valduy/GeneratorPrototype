using GameEngine.Helpers;
using MeshTopology;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;

namespace TriangulatedTopology
{
    public static class Wfc
    {
        public static void GridWfc(Dictionary<TopologyNode, Cell[,]> grids, List<Rule> rules)
        {
            foreach (var pair in grids)
            {
                var grid = pair.Value;
                var defaultRule = rules[33];
                var forRecalculation = new List<Vector2i>();

                if (grid.GetLength(0) <= 2 || grid.GetLength(1) <= 2)
                {
                    foreach (var cell in grid.Enumerate())
                    {
                        cell.Rules.Clear();
                        cell.Rules.Add(defaultRule);
                    }

                    continue;
                }

                foreach (var ceil in grid.Enumerate())
                {
                    ceil.Rules.Clear();
                    ceil.Rules.AddRange(rules);
                }

                // Tob and bottom borders
                for (int x = 1; x < grid.GetLength(0) - 1; x++)
                {
                    grid[x, 0].Rules.Clear();
                    grid[x, 0].Rules.Add(defaultRule);
                    forRecalculation.Add(new Vector2i(x, 1));

                    grid[x, grid.GetLength(1) - 1].Rules.Clear();
                    grid[x, grid.GetLength(1) - 1].Rules.Add(defaultRule);
                    forRecalculation.Add(new Vector2i(x, grid.GetLength(1) - 2));
                }

                // Left and right borders
                for (int y = 1; y < grid.GetLength(1) - 1; y++)
                {
                    grid[0, y].Rules.Clear();
                    grid[0, y].Rules.Add(defaultRule);
                    forRecalculation.Add(new Vector2i(1, y));

                    grid[grid.GetLength(0) - 1, y].Rules.Clear();
                    grid[grid.GetLength(0) - 1, y].Rules.Add(defaultRule);
                    forRecalculation.Add(new Vector2i(grid.GetLength(0) - 2, y));
                }

                // Corners
                grid[0, 0].Rules.Clear();
                grid[0, 0].Rules.Add(defaultRule);

                grid[0, grid.GetLength(1) - 1].Rules.Clear();
                grid[0, grid.GetLength(1) - 1].Rules.Add(defaultRule);

                grid[grid.GetLength(0) - 1, 0].Rules.Clear();
                grid[grid.GetLength(0) - 1, 0].Rules.Add(defaultRule);

                grid[grid.GetLength(0) - 1, grid.GetLength(1) - 1].Rules.Clear();
                grid[grid.GetLength(0) - 1, grid.GetLength(1) - 1].Rules.Add(defaultRule);

                while (true)
                {
                    while (forRecalculation.Count > 0)
                    {
                        var coords = forRecalculation[0];
                        forRecalculation.Remove(coords);
                        var cell = grid[coords.X, coords.Y];
                        var filtered = FilterGridPossible(grid, coords);

                        if (filtered.Count == 0)
                        {
                            cell.Rules.Clear();
                            cell.Rules.AddRange(rules);

                            foreach (var neighbourCoords in GetNeighboursCross(grid, coords, 1))
                            {
                                var neighbour = grid[neighbourCoords.X, neighbourCoords.Y];
                                neighbour.Rules.Clear();
                                neighbour.Rules.AddRange(rules);
                            }

                            continue;
                        }

                        if (cell.Rules.Count > filtered.Count)
                        {
                            forRecalculation.AddRange(GetNeighboursCross(grid, coords, 1));
                            cell.Rules.Clear();
                            cell.Rules.AddRange(filtered);
                        }
                    }

                    var maxCellCoord = new Vector2i(1, 1);
                    var maxCell = grid[maxCellCoord.X, maxCellCoord.Y];

                    for (int x = 1; x < grid.GetLength(0) - 1; x++)
                    {
                        for (int y = 1; y < grid.GetLength(1) - 1; y++)
                        {
                            var cell = grid[x, y];

                            if (cell.Rules.Count > maxCell.Rules.Count)
                            {
                                maxCellCoord = new Vector2i(x, y);
                                maxCell = cell;
                            }
                        }
                    }

                    if (maxCell.Rules.Count <= 1)
                    {
                        break;
                    }

                    var rule = maxCell.Rules.GetRandom();
                    maxCell.Rules.Clear();
                    maxCell.Rules.Add(rule);
                    forRecalculation.AddRange(GetNeighboursCross(grid, maxCellCoord, 1));
                }
            }
        }

        public static void GraphWfc(
            List<Cell> cells, 
            List<Rule> rules, 
            List<Rule> horizontalRules, 
            List<Rule> verticalRules)
        {
            var forRecalculation = new List<Cell>();
            var defaultRule = rules[33];

            foreach (var cell in cells)
            {
                cell.Rules.Clear();

                if (cell.Neighbours.All(n => n != null))
                {                   
                    cell.Rules.AddRange(rules);
                }
                else
                {
                    cell.Rules.Add(defaultRule);
                    cell.Define();
                }
            }

            foreach (var cell in cells.Where(c => c.IsDefined()))
            {
                foreach (var neighbour in cell.Neighbours)
                {
                    if (neighbour != null && !neighbour.Cell.IsDefined())
                    {
                        forRecalculation.Add(neighbour.Cell);
                    }
                }
            }

            // If there is no defined tiles we choose tile randomly.
            if (!forRecalculation.Any())
            {
                var initial = cells.GetRandom();
                var rule = initial.Rules.GetRandom();
                initial.Rules.Clear();
                initial.Rules.Add(rule);

                foreach (var neighbour in initial.Neighbours)
                {
                    if (neighbour != null)
                    {
                        forRecalculation.Add(neighbour.Cell);
                    }
                }
            }

            while (true)
            {
                while (forRecalculation.Count > 0)
                {
                    var cell = forRecalculation[0];
                    forRecalculation.Remove(cell);
                    var filtered = FilterGraphPossible(cell);

                    // Deadlock resoultion.
                    if (filtered.Count == 0)
                    {
                        cell.Rules.Clear();
                        cell.Rules.AddRange(rules);

                        foreach (var neighbour in cell.Neighbours)
                        {
                            if (neighbour != null && !neighbour.Cell.IsDefined())
                            {
                                neighbour.Cell.Rules.Clear();
                                neighbour.Cell.Rules.AddRange(rules);
                            }
                        }

                        continue;
                    }

                    if (cell.Rules.Count > filtered.Count)
                    {
                        foreach (var neighbour in cell.Neighbours)
                        {
                            if (neighbour != null && !neighbour.Cell.IsDefined())
                            {
                                forRecalculation.Add(neighbour.Cell);
                            }
                        }

                        cell.Rules.Clear();
                        cell.Rules.AddRange(filtered);
                    }
                }

                var maxCell = cells.First();
                int maxPossibilities = maxCell.Rules.Count;

                foreach (var cell in cells)
                {
                    if (cell.Rules.Count > maxPossibilities)
                    {
                        maxCell = cell;
                        maxPossibilities = maxCell.Rules.Count;
                    }
                }

                if (maxPossibilities <= 1)
                {
                    break;
                }

                var rule = maxCell.Rules.GetRandom();
                maxCell.Rules.Clear();
                maxCell.Rules.Add(rule);

                foreach (var neighbour in maxCell.Neighbours)
                {
                    if (neighbour != null && !neighbour.Cell.IsDefined())
                    {
                        forRecalculation.Add(neighbour.Cell);
                    }
                }
            }
        }

        private static IEnumerable<Vector2i> GetNeighboursCross<T>(T[,] matrix, Vector2i coords, int padding = 0)
        {
            if (coords.Y > padding)
            {
                yield return new Vector2i(coords.X, coords.Y - 1);
            }
            if (coords.X > padding)
            {
                yield return new Vector2i(coords.X - 1, coords.Y);
            }
            if (coords.Y < matrix.GetLength(1) - 1 - padding)
            {
                yield return new Vector2i(coords.X, coords.Y + 1);
            }
            if (coords.X < matrix.GetLength(0) - 1 - padding)
            {
                yield return new Vector2i(coords.X + 1, coords.Y);
            }
        }

        private static List<Rule> FilterGridPossible(Cell[,] grid, Vector2i coords)
        {
            var cell = grid[coords.X, coords.Y];
            var neighboursCoords = GetNeighboursCross(grid, coords).ToList();
            return cell.Rules.Where(r => IsPossibleInGrid(grid, r, neighboursCoords)).ToList();
        }

        private static List<Rule> FilterGraphPossible(Cell cell) 
            => cell.Rules.Where(r => IsPossibleInGraph(r, cell)).ToList();

        private static bool IsPossibleInGrid(Cell[,] grid, Rule rule, List<Vector2i> neighboursCoords)
        {
            for (int neighbourIndex = 0; neighbourIndex < neighboursCoords.Count; neighbourIndex++)
            {
                var coords = neighboursCoords[neighbourIndex];
                var neighbour = grid[coords.X, coords.Y];
                var rules = neighbour.Rules;
                var nodeIndex = (neighbourIndex + 2) % 4;

                if (rules.All(r => !IsSame(r[nodeIndex], rule[neighbourIndex])))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPossibleInGraph(Rule rule, Cell cell)
        {
            for (int neighbourIndex = 0; neighbourIndex < cell.Neighbours.Count(); neighbourIndex++)
            {
                var neighbour = cell.Neighbours[neighbourIndex];

                if (neighbour == null)
                {
                    continue;
                }

                var rules = neighbour.Cell.Rules;
                var adapter = neighbour.Adapter;
                var nodeIndex = (neighbourIndex + 2) % 4;

                if (rules.All(r => !IsSame(adapter.GetSide(r, nodeIndex), rule[neighbourIndex])))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsSame(Color[] lhs, Color[] rhs)
        {
            if (lhs.Length != rhs.Length)
            {
                throw new ArgumentException();
            }

            for (int i = 0; i < lhs.Length; i++)
            {
                if (!lhs[i].IsSame(rhs[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static List<Rule> ChooseRuleSet()
        {
            throw new NotImplementedException();
        }
    }
}
