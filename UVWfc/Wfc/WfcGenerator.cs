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

            foreach (var c in cells)
            {
                c.Rules.Clear();
                c.Rules.AddRange(SelectRuleSet(c, wallRules, floorRules, ceilRules));
            }

            {
                var initial = cells.GetRandom();
                var rule = initial.Rules.GetRandom();
                initial.Rules.Clear();
                initial.Rules.Add(rule);

                forRecalculation.AddRange(SelectNeighbourCellsForRecalculation(initial));
            }

            // TODO: Fire observation

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
                            // TODO: Fire deadlock event

                            foreach (var c in cells)
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
                            neighbour!.Cell.Rules.Clear();
                            neighbour.Cell.Rules.AddRange(SelectRuleSet(neighbour.Cell, wallRules, floorRules, ceilRules));
                        }

                        continue;
                    }

                    if (cell.Rules.Count > filtered.Count)
                    {
                        foreach (var neighbour in cell.Neighbours)
                        {
                            forRecalculation.Add(neighbour!.Cell);
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
                forRecalculation.AddRange(SelectNeighbourCellsForRecalculation(maxCell));
                // TODO: Fire observation
            }
        }

        private static IEnumerable<Cell> SelectNeighbourCellsForRecalculation(Cell cell)
        {
            return cell.Neighbours
                .Where(nd => nd != null)
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

        private static List<Rule> FilterPossible(Cell cell)
            => cell.Rules.Where(r => IsPossible(r, cell)).ToList();

        private static bool IsPossible(Rule rule, Cell cell)
        {
            for (int neighbourIndexInCell = 0; neighbourIndexInCell < cell.Neighbours.Length; neighbourIndexInCell++)
            {
                var neighbour = cell.Neighbours[neighbourIndexInCell];

                if (neighbour == null)
                {
                    continue;
                }

                var neighbourRules = neighbour.Cell.Rules;
                var neighbourAdapter = neighbour.Adapter;
                var cellIndexInNeighbour = (neighbourIndexInCell + 2) % 4;
                var cellSide = rule[neighbourIndexInCell];

                bool isPossible = false;

                foreach (var neighbourRule in neighbourRules)
                {
                    var neighbourSide = neighbourAdapter.GetSide(neighbourRule, cellIndexInNeighbour);

                    // If any rule in neighbour has same color sheme
                    // => current rule is posible for this neighbour.
                    if (IsSame(neighbourSide, cellSide))
                    {
                        isPossible = true;
                        break;
                    }
                }

                // If this rule inpossible for any neighbour
                // => this rule inpossible in principle.
                if (!isPossible)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSame(Color[] lhs, Color[] rhs)
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
