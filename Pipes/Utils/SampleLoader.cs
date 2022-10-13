using System.Drawing;
using Pipes.Models;

namespace Pipes.Utils
{
    public static class SampleLoader
    {
        public const int FloorsInRow = 8;
        //public const int FloorsInColumn = 8;
        public const int FloorsInColumn = 2;

        public const int FloorWidth = 16;
        public const int FloorDepth = 16;
        public const int BuildingHeight = FloorsInRow * FloorsInColumn;

        public const int WallSpacing = 5;

        public static void Load(this Grid grid, string path)
        {
            using var reader = new StreamReader(path);
            var bmp = new Bitmap(reader.BaseStream);

            for (int i = 0; i < FloorsInRow; i++)
            {
                for (int j = 0; j < FloorsInColumn; j++)
                {
                    grid.BuildFloor(bmp, i, j);
                }
            }
        }

        private static void BuildFloor(this Grid grid, Bitmap bmp, int i, int j)
        {
            int pivotX = FloorWidth * i;
            int pivotY = FloorDepth * j;

            for (int x = 0; x < FloorWidth; x++)
            {
                for (int y = 0; y < FloorDepth; y++)
                {
                    if (IsNotEmpty(bmp.GetPixel(pivotX + x, pivotY + y)))
                    {
                        int height = FloorsInRow * j + i;
                        var cell = grid[x + WallSpacing, height + WallSpacing, y + WallSpacing];
                        cell.Type = CellType.Wall;
                    }
                }
            }
        }

        private static bool IsNotEmpty(Color color) => color.A != 0;
    }
}
