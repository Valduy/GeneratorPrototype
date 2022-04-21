using System.Collections;
using System.Drawing;
using OpenTK.Mathematics;
using static System.Single;

namespace PipesDemo.Models
{
    public class Grid : IEnumerable<Cell>
    {
        public const int FloorsInRow = 8;
        //public const int FloorsInColumn = 8;
        public const int FloorsInColumn = 2;

        public const int FloorWidth = 16;
        public const int FloorDepth = 16;
        public const int BuildingHeight = FloorsInRow * FloorsInColumn;

        public const int WallSpacing = 5;

        private Cell[,,] _cells = new Cell[
            FloorWidth + WallSpacing * 2,
            BuildingHeight + WallSpacing * 2,
            FloorDepth + WallSpacing * 2];

        public int Width => _cells.GetLength(0);
        public int Height => _cells.GetLength(1);
        public int Depth => _cells.GetLength(2);

        public Grid()
        {
            for (int i = 0; i < _cells.GetLength(0); i++)
            {
                for (int j = 0; j < _cells.GetLength(1); j++)
                {
                    for (int k = 0; k < _cells.GetLength(2); k++)
                    {
                        _cells[i, j, k] = new Cell(this, i, j, k);
                    }
                }
            }
        }

        public void Load(string path)
        {
            using var reader = new StreamReader(path);
            var bmp = new Bitmap(reader.BaseStream);

            for (int i = 0; i < FloorsInRow; i++)
            {
                for (int j = 0; j < FloorsInColumn; j++)
                {
                    BuildFloor(bmp, i, j);
                }
            }
        }

        public Cell this[int x, int y, int z] => _cells[x, y, z];
        public Cell this[Vector3i xyz] => _cells[xyz.X, xyz.Y, xyz.Z];

        public IEnumerator<Cell> GetEnumerator()
        {
            foreach (var cell in _cells)
            {
                yield return cell;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private static bool IsNotEmpty(Color color) => color.A != 0;

        private void BuildFloor(Bitmap bmp, int i, int j)
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
                        var cell = _cells[x + WallSpacing, height + WallSpacing, y + WallSpacing];
                        cell.Type = CellType.Wall;
                    }
                }
            }
        }
        
        public IEnumerable<Cell> GetCross(Cell cell) =>
            GetCross(cell.Position);

        public IEnumerable<Cell> GetCross(Vector3i position)
        {
            if (position.X - 1 >= 0)
            {
                yield return _cells[position.X - 1, position.Y, position.Z];
            }
            if (position.X + 1 < Width)
            {
                yield return _cells[position.X + 1, position.Y, position.Z];
            }
            if (position.Y - 1 >= 0)
            {
                yield return _cells[position.X, position.Y - 1, position.Z];
            }
            if (position.Y + 1 < Height)
            {
                yield return _cells[position.X, position.Y + 1, position.Z];
            }
            if (position.Z - 1 >= 0)
            {
                yield return _cells[position.X, position.Y, position.Z - 1];
            }
            if (position.Z + 1 < Depth)
            {
                yield return _cells[position.X, position.Y, position.Z + 1];
            }
        }

        public IEnumerable<Cell> GetCube(Cell cell) 
            => GetCube(cell.Position);

        public IEnumerable<Cell> GetCube(Vector3i position)
        {
            var start = new Vector3i(
                Math.Max(0, position.X - 1),
                Math.Max(0, position.Y - 1),
                Math.Max(0, position.Z - 1));

            var end = new Vector3i(
                Math.Min(Width - 1, position.X + 1),
                Math.Min(Height - 1, position.Y + 1),
                Math.Min(Depth - 1, position.Z + 1));

            for (int x = start.X; x <= end.X; x++)
            {
                for (int y = start.Y; y <= end.Y; y++)
                {
                    for (int z = start.Z; z <= end.Z; z++)
                    {
                        if (new Vector3i(x, y, z) == position)
                        {
                            continue;
                        }

                        yield return _cells[x, y, z];
                    }
                }
            }
        }
    }
}
