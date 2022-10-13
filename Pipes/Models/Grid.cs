using System.Collections;
using System.Drawing;
using OpenTK.Mathematics;

namespace Pipes.Models
{
    public class Grid : IEnumerable<Cell>
    {
        private Cell[,,] _cells;

        public int Width => _cells.GetLength(0);
        public int Height => _cells.GetLength(1);
        public int Depth => _cells.GetLength(2);

        public Grid(int width, int height, int depth)
        {
            _cells = new Cell[width, height, depth];

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    for (int k = 0; k < Depth; k++)
                    {
                        _cells[i, j, k] = new Cell(this, i, j, k);
                    }
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

        public IEnumerable<Cell> GetCross(Cell cell) =>
            GetCross(cell.Position);

        public IEnumerable<Cell> GetCross(Vector3i position)
            => GetCross(position.X, position.Y, position.Z);

        public IEnumerable<Cell> GetCross(int x, int y, int z)
        {
            if (x - 1 >= 0)
            {
                yield return _cells[x - 1, y, z];
            }
            if (x + 1 < Width)
            {
                yield return _cells[x + 1, y, z];
            }
            if (y - 1 >= 0)
            {
                yield return _cells[x, y - 1, z];
            }
            if (y + 1 < Height)
            {
                yield return _cells[x, y + 1, z];
            }
            if (z - 1 >= 0)
            {
                yield return _cells[x, y, z - 1];
            }
            if (z + 1 < Depth)
            {
                yield return _cells[x, y, z + 1];
            }
        }

        public IEnumerable<Cell> GetCube(Cell cell) 
            => GetCube(cell.Position);

        public IEnumerable<Cell> GetCube(Vector3i position) 
            => GetCube(position.X, position.Y, position.Z);

        public IEnumerable<Cell> GetCube(int x, int y, int z)
        {
            var initial = new Vector3i(x, y, z);

            var start = new Vector3i(
                Math.Max(0, x - 1),
                Math.Max(0, y - 1),
                Math.Max(0, z - 1));

            var end = new Vector3i(
                Math.Min(Width - 1, x + 1),
                Math.Min(Height - 1, y + 1),
                Math.Min(Depth - 1, z + 1));

            for (x = start.X; x <= end.X; x++)
            {
                for (y = start.Y; y <= end.Y; y++)
                {
                    for (z = start.Z; z <= end.Z; z++)
                    {
                        if (new Vector3i(x, y, z) == initial)
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
