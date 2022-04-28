using OpenTK.Mathematics;

namespace Pipes.Models.Utils
{
    public static class GridHelper
    {
        public static bool IsFree(this Cell cell)
            => cell.Type is CellType.Empty or CellType.Inside;

        public static bool IsOccupied(this Cell cell) 
            => !cell.IsFree();

        public static bool IsPipe(this Cell cell) 
            => cell.Type is CellType.Pipe;

        public static bool IsWall(this Cell cell)
            => cell.Type is CellType.Wall;

        public static bool IsWallOrPipe(this Cell cell)
            => cell.IsWall() || cell.IsPipe();

        public static bool IsNearWall(this Grid grid, Cell cell)
            => grid.IsNearWall(cell.Position);

        public static bool IsNearWall(this Grid grid, Vector3i position) 
            => grid.GetCross(position).Any(IsWall);

        public static bool IsInsideBuilding(this Grid grid, Cell cell)
            => grid.IsInsideBuilding(cell.Position);

        public static bool IsInsideBuilding(this Grid grid, Vector3i position)
        {
            if (grid[position.X, position.Y, position.Z].Type == CellType.Wall)
            {
                return false;
            }

            int wallsCount = 0;

            for (int x = position.X; x < grid.Width; x++)
            {
                if (grid[x, position.Y, position.Z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }
            for (int x = 0; x < position.X; x++)
            {
                if (grid[x, position.Y, position.Z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }
            for (int y = position.Y; y < grid.Height; y++)
            {
                if (grid[position.X, y, position.Z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }
            for (int y = 0; y < position.Y; y++)
            {
                if (grid[position.X, y, position.Z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }
            for (int z = position.Z; z < grid.Depth; z++)
            {
                if (grid[position.X, position.Y, z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }
            for (int z = 0; z < position.Z; z++)
            {
                if (grid[position.X, position.Y, z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }

            return wallsCount > 2;
        }
    }
}
