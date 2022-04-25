﻿using OpenTK.Mathematics;
using PipesDemo.Models;
using PipesDemo.Models.Utils;

namespace PipesDemo.Algorithms
{
    public static class FieldAlgorithms
    {
        public const float MaxTemperature = 1000;
        public const float TemperatureStep = 50;

        public static void CalculateWarm(this Grid grid, Vector3i hearth)
            => CalculateWarm(grid, hearth.X, hearth.Y, hearth.Z);

        public static void CalculateWarm(this Grid grid, int x, int y, int z)
        {
            grid.ClearWarm();
            var frontier = new Stack<Cell>();
            var start = grid[x, y, z];
            start.Temperature = MaxTemperature;
            frontier.Push(start);

            while (frontier.Any())
            {
                var temp = frontier.Pop();
                var reachable = grid.GetCube(temp).ToList();

                foreach (var adjacent in reachable)
                {
                    var k = adjacent.Type is CellType.Empty ? 0.95f : 0.55f;

                    if (float.IsNaN(adjacent.Temperature) || adjacent.Temperature < temp.Temperature * k)
                    {
                        adjacent.Temperature = temp.Temperature * k;
                        frontier.Push(adjacent);
                    }
                }
            }
        }

        public static void CalculateWarmBreathFirst(this Grid grid, Vector3i hearth)
            => grid.CalculateWarmBreathFirst(hearth.X, hearth.Y, hearth.Z);

        public static void CalculateWarmBreathFirst(this Grid grid, int x, int y, int z)
        {
            grid.ClearWarm();
            var frontier = new Queue<Cell>();
            var visited = new HashSet<Cell>();
            var start = grid[x, y, z];
            start.Temperature = MaxTemperature;
            frontier.Enqueue(start);
            visited.Add(start);

            while (frontier.Any())
            {
                var temp = frontier.Dequeue();
                var reachable = grid.GetCross(temp)
                    .Where(c => !visited.Contains(c))
                    .ToList();

                foreach (var adjacent in reachable)
                {
                    var k = adjacent.Type is CellType.Empty ? 0.95f : 0.55f;
                    adjacent.Temperature = temp.Temperature * k;
                    frontier.Enqueue(adjacent);
                    visited.Add(adjacent);
                }
            }
        }

        public static void CalculateWarmHeatTransfer(this Grid grid, Vector3i hearth)
            => grid.CalculateWarmHeatTransfer(hearth.X, hearth.Y, hearth.Z);

        public static void CalculateWarmHeatTransfer(this Grid grid, int x, int y, int z)
        {
            grid.ZeroWarm();
            grid[x, y, z].Temperature = MaxTemperature;

            for (int t = 1; t < 16 * 16 * 16; t++)
            {
                grid[x, y, z].Temperature = MaxTemperature;

                for (int i = 1; i < grid.Width - 1; i++)
                {
                    for (int j = 1; j < grid.Height - 1; j++)
                    {
                        for (int k = 1; k < grid.Depth - 1; k++)
                        {
                            grid[x, y, z].Temperature = MaxTemperature;
                            float coefficient = grid.GetCoefficient(i, j, k);
                            float tau = 100;
                            grid[i, j, k].Temperature += coefficient * tau * grid.Derivative2(i, j, k);
                        }
                    }
                }
            }
        }

        private static float GetCoefficient(this Grid grid, int x, int y, int z)
        {
            float coefficient;

            if (grid[x, y, z].IsFree())
            {
                coefficient = grid.GetCube(grid[x, y, z]).Any(c => c.IsWallOrPipe()) ? 0.01f : 0.0000001f;
            }
            else
            {
                coefficient = 0.0f;
            }

            return coefficient;
        }

        private static float Derivative2(this Grid grid, int x, int y, int z)
            => grid.DerivativeX2(x, y, z) + grid.DerivativeY2(x, y, z) + grid.DerivativeZ2(x, y, z);

        private static float DerivativeX2(this Grid grid, int x, int y, int z) 
            => (grid[x + 1, y, z].Temperature - 2 * grid[x, y, z].Temperature + grid[x - 1, y, z].Temperature) / 4;

        private static float DerivativeY2(this Grid grid, int x, int y, int z)
            => (grid[x, y + 1, z].Temperature - 2 * grid[x, y, z].Temperature + grid[x, y - 1, z].Temperature) / 4;

        private static float DerivativeZ2(this Grid grid, int x, int y, int z)
            => (grid[x, y, z + 1].Temperature - 2 * grid[x, y, z].Temperature + grid[x, y, z - 1].Temperature) / 4;

        public static void ZeroWarm(this Grid grid)
        {
            foreach (var cell in grid)
            {
                cell.Temperature = 0;
                cell.Direction = Vector3.Zero;
            }
        }

        public static void ClearWarm(this Grid grid)
        {
            foreach (var cell in grid)
            {
                cell.Temperature = float.NaN;
                cell.Direction = null;
            }
        }

        public static void CalculateVectors(this Grid grid)
        {
            var frontier = new Queue<Cell>();
            var visited = new HashSet<Cell>();
            var start = grid[0, 0, 0];
            frontier.Enqueue(start);
            visited.Add(start);

            while (frontier.Any())
            {
                var temp = frontier.Dequeue();
                temp.Direction = grid.GetGradient(temp.Position, 1.0f);
                var reachable = grid.GetCube(temp)
                    .Where(c => c.IsFree() && !visited.Contains(c))
                    .ToList();

                foreach (var adjacent in reachable)
                {
                    frontier.Enqueue(adjacent);
                    visited.Add(adjacent);
                }
            }
        }

        public static Vector3 GetGradient(this Grid grid, Vector3 args, float h)
            => grid.GetGradient(args.X, args.Y, args.Z, h);

        public static Vector3 GetGradient(this Grid grid, float x, float y, float z, float h)
        {
            return new Vector3(
                (grid.GetHeat(x + h, y, z) - grid.GetHeat(x - h, y, z)) / (2 * h),
                (grid.GetHeat(x, y + h, z) - grid.GetHeat(x, y - h, z)) / (2 * h),
                (grid.GetHeat(x, y, z + h) - grid.GetHeat(x, y, z - h)) / (2 * h));
        }

        public static float GetHeat(this Grid grid, Vector3 args)
            => grid.GetHeat(args.X, args.Y, args.Z);

        public static float GetHeat(this Grid grid, float x, float y, float z)
        {
            int xi = Math.Clamp((int)MathF.Floor(x), 0, grid.Width - 1);
            int yi = Math.Clamp((int)MathF.Floor(y), 0, grid.Height - 1);
            int zi = Math.Clamp((int)MathF.Floor(z), 0, grid.Depth - 1);

            int xc = Math.Clamp(xi + 1, xi, grid.Width - 1);
            int yc = Math.Clamp(yi + 1, yi, grid.Height - 1);
            int zc = Math.Clamp(zi + 1, zi, grid.Depth - 1);

            float denominator = (xc - xi) * (yc - yi) * (zc - zi);

            var result =
                grid[xi, yi, zi].Temperature / denominator * (xc - x) * (yc - y) * (zc - z) +
                grid[xc, yi, zi].Temperature / denominator * (x - xi) * (yc - y) * (zc - z) +
                grid[xc, yi, zc].Temperature / denominator * (x - xi) * (yc - y) * (z - zi) +
                grid[xc, yc, zi].Temperature / denominator * (x - xi) * (y - yi) * (zc - z) +
                grid[xc, yc, zc].Temperature / denominator * (x - xi) * (y - yi) * (z - zi);

            return result;
        }
    }
}
