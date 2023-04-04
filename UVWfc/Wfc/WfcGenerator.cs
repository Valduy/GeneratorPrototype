using GameEngine.Helpers;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;
using UVWfc.LevelGraph;

namespace UVWfc.Wfc
{
    public static class WfcGenerator
    {
        private const float FloorTrashold = 45.0f;
        private const float CeilTrashold = 45.0f;

        public static void GraphWfc(
            List<Cell> cells,
            List<Rule> wallRules,
            List<Rule> floorRules,
            List<Rule> ceilRules)
        {
            var forRecalculation = new List<Cell>();

            //var defaultRule = rules[33];

            //foreach (var cell in cells)
            //{
            //    cell.Rules.Clear();

            //    if (cell.Neighbours.All(n => n != null))
            //    {                   
            //        cell.Rules.AddRange(rules);
            //    }
            //    else
            //    {
            //        cell.Rules.Add(defaultRule);
            //        cell.Define();
            //    }
            //}

            //foreach (var c in cells)
            //{
            //    c.Rules.Clear();
            //    c.Rules.AddRange(SelectRuleSet(c, wallRules, floorRules, ceilRules));

            //    if (c.Rules.Count == 1)
            //    {
            //        c.Define();
            //    }
            //}

            //foreach (var cell in cells.Where(c => c.IsDefined()))
            //{
            //    forRecalculation.AddRange(SelectNeighbourCellsForRecalculation(cell));
            //}

            foreach (var c in cells.Where(c => !c.IsDefined()))
            {
                c.Rules.Clear();
                c.Rules.AddRange(SelectRuleSet(c, wallRules, floorRules, ceilRules));
            }

            // If there is no defined tiles we choose tile randomly.
            if (!forRecalculation.Any())
            {
                var initial = cells.GetRandom();

                var rule = initial.Rules.GetRandom();
                initial.Rules.Clear();
                initial.Rules.Add(rule);

                forRecalculation.AddRange(SelectNeighbourCellsForRecalculation(initial));
            }

            while (true)
            {
                int trashold = 1000;
                int failes = 0;

                while (forRecalculation.Count > 0)
                {
                    var cell = forRecalculation[0];
                    forRecalculation.Remove(cell);
                    var filtered = FilterPossible(cell);

                    // Deadlock resoultion.
                    if (filtered.Count == 0)
                    {
                        failes += 1;

                        if (failes >= trashold)
                        {
                            foreach (var c in cells.Where(c => !c.IsDefined()))
                            {
                                c.Rules.Clear();
                                c.Rules.AddRange(SelectRuleSet(c, wallRules, floorRules, ceilRules));
                            }

                            forRecalculation.Clear();
                            break;
                        }

                        cell.Rules.Clear();
                        cell.Rules.AddRange(SelectRuleSet(cell, wallRules, floorRules, ceilRules));

                        foreach (var neighbour in cell.Neighbours)
                        {
                            if (neighbour != null && !neighbour.Cell.IsDefined())
                            {
                                neighbour.Cell.Rules.Clear();
                                neighbour.Cell.Rules.AddRange(SelectRuleSet(neighbour.Cell, wallRules, floorRules, ceilRules));
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

                int collapsed = 0;

                foreach (var cell in cells)
                {
                    if (cell.Rules.Count > maxPossibilities)
                    {
                        maxCell = cell;
                        maxPossibilities = maxCell.Rules.Count;
                    }

                    if (cell.Rules.Count == 1)
                    {
                        collapsed += 1;
                    }
                }

                Console.WriteLine(collapsed);

                if (maxPossibilities <= 1)
                {
                    break;
                }

                var rule = maxCell.Rules.GetRandom();
                maxCell.Rules.Clear();
                maxCell.Rules.Add(rule);

                forRecalculation.AddRange(SelectNeighbourCellsForRecalculation(maxCell));
            }
        }

        private static IEnumerable<Cell> SelectNeighbourCellsForRecalculation(Cell cell)
        {
            return cell.Neighbours
                .Where(nd => nd != null && !nd.Cell.IsDefined())
                .Select(nd => nd!.Cell);
        }

        private static List<Rule> SelectRuleSet(
            Cell cell,
            List<Rule> wallRules,
            List<Rule> floorRules,
            List<Rule> ceilRules)
        {
            var floorFactor = MathHelper.RadiansToDegrees(MathHelper.Acos(Vector3.Dot(Vector3.UnitY, cell.Normal)));

            if (floorFactor < FloorTrashold)
            {
                return floorRules;
            }

            var ceilFactor = MathHelper.RadiansToDegrees(MathHelper.Acos(Vector3.Dot(-Vector3.UnitY, cell.Normal)));

            if (ceilFactor < CeilTrashold)
            {
                return ceilRules;
            }

            return wallRules;
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

        private static List<Rule> FilterPossible(Cell cell)
            => cell.Rules.Where(r => IsPossible(r, cell)).ToList();

        private static bool IsPossible(Rule rule, Cell cell)
        {
            for (int neighbourIndex = 0; neighbourIndex < cell.Neighbours.Length; neighbourIndex++)
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
    }
}
