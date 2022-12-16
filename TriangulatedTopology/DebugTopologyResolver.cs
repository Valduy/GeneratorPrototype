using GameEngine.Helpers;
using MeshTopology;
using System.Drawing;
using TextureUtils;

namespace TriangulatedTopology
{
    public class DebugTopologyResolver
    {
        public static void OrientationDebug(Dictionary<TopologyNode, Cell[,]> grids, int logicalResolution)
        {
            var pallete = new Color[]
            {
                Color.Blue,
                Color.Purple,
                Color.Magenta,
                Color.Coral,
                Color.Red,
                Color.Orange,
                Color.Yellow,
                Color.Lime,
                Color.Green,
                Color.Aqua,
                Color.Cyan,
            };

            foreach (var pair in grids)
            {
                var grid = pair.Value;

                foreach (var cell in grid)
                {
                    var defenition = new Color[logicalResolution, logicalResolution];

                    for (int x = 0; x < defenition.GetLength(0); x++)
                    {
                        for (int y = 0; y < defenition.GetLength(1); y++)
                        {
                            defenition[x, y] = Color.White;
                        }
                    }

                    cell.Rules.Add(new Rule(defenition, defenition));
                }
            }

            foreach (var pair in grids)
            {
                var node = pair.Key;
                var grid = pair.Value;

                if (grids.ContainsKey(node.Neighbours[0]))
                {
                    for (int x = 0; x < grid.GetLength(0); x++)
                    {
                        var cell = grid[x, 0];
                        var cellRule = cell.Rules[0];

                        var link = cell.Neighbours[0];
                        var adapter = link!.Adapter;
                        var linkRule = link!.Cell.Rules[0];

                        var semple = adapter.GetSide(linkRule, 2);

                        if (semple.All(c => c.IsSame(Color.White)))
                        {
                            for (int i = 0; i < cellRule.Logical.GetLength(0); i++)
                            {
                                cellRule.Logical[i, 0] = pallete.GetRandom();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < semple.Length; i++)
                            {
                                cellRule.Logical[i, 0] = semple[i];
                            }
                        }
                    }
                }

                if (grids.ContainsKey(node.Neighbours[1]))
                {
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        var cell = grid[0, y];
                        var cellRule = cell.Rules[0];

                        var link = cell.Neighbours[1];
                        var adapter = link!.Adapter;
                        var linkRule = link!.Cell.Rules[0];

                        var semple = adapter.GetSide(linkRule, 3);

                        if (semple.All(c => c.IsSame(Color.White)))
                        {
                            for (int i = 0; i < cellRule.Logical.GetLength(1); i++)
                            {
                                cellRule.Logical[0, i] = pallete.GetRandom();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < semple.Length; i++)
                            {
                                cellRule.Logical[0, i] = semple[i];
                            }
                        }
                    }
                }

                if (grids.ContainsKey(node.Neighbours[2]))
                {
                    for (int x = 0; x < grid.GetLength(0); x++)
                    {
                        var cell = grid[x, grid.GetLength(1) - 1];
                        var cellRule = cell.Rules[0];

                        var link = cell.Neighbours[2];
                        var adapter = link!.Adapter;
                        var linkRule = link!.Cell.Rules[0];

                        var semple = adapter.GetSide(linkRule, 0);

                        if (semple.All(c => c.IsSame(Color.White)))
                        {
                            for (int i = 0; i < cellRule.Logical.GetLength(0); i++)
                            {
                                cellRule.Logical[i, cellRule.Logical.GetLength(1) - 1] = pallete.GetRandom();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < semple.Length; i++)
                            {
                                cellRule.Logical[i, cellRule.Logical.GetLength(1) - 1] = semple[i];
                            }
                        }
                    }
                }

                if (grids.ContainsKey(node.Neighbours[3]))
                {
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        var cell = grid[grid.GetLength(0) - 1, y];
                        var cellRule = cell.Rules[0];

                        var link = cell.Neighbours[3];
                        var adapter = link!.Adapter;
                        var linkRule = link!.Cell.Rules[0];

                        var semple = adapter.GetSide(linkRule, 1);

                        if (semple.All(c => c.IsSame(Color.White)))
                        {
                            for (int i = 0; i < cellRule.Logical.GetLength(1); i++)
                            {
                                cellRule.Logical[cellRule.Logical.GetLength(0) - 1, i] = pallete.GetRandom();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < semple.Length; i++)
                            {
                                cellRule.Logical[cellRule.Logical.GetLength(0) - 1, i] = semple[i];
                            }
                        }
                    }
                }
            }
        }
    }
}
