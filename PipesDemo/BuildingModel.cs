using System.Collections;
using System.Drawing;
using OpenTK.Mathematics;

namespace PipesDemo
{
    public class BuildingModel
    {
        public const int FloorsInRow = 8;
        public const int FloorsInColumn = 8;

        public const int FloorWidth = 16;
        public const int FloorDepth = 16;
        public const int BuildingHeight = 64;

        public const int WallSpacing = 1;

        public const float MaxTemperature = 100000;
        public const float TemperatureStep = 10;

        private Cell[,,] _cells = new Cell[
            FloorWidth + WallSpacing * 2,
            BuildingHeight + WallSpacing * 2,
            FloorDepth + WallSpacing * 2];

        public int Width => _cells.GetLength(0);
        public int Height => _cells.GetLength(1);
        public int Depth => _cells.GetLength(2);

        public event Action<Cell> WallCreated;
        public event Action<Cell> TemperatureChanged;
        public event Action<Cell> PipeCreated;

        public BuildingModel()
        {
            for (int i = 0; i < _cells.GetLength(0); i++)
            {
                for (int j = 0; j < _cells.GetLength(1); j++)
                {
                    for (int k = 0; k < _cells.GetLength(2); k++)
                    {
                        _cells[i, j, k] = new Cell(i, j, k);
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

        public IEnumerable GeneratePipes(Vector3i from, Vector3i to)
        {
            if (_cells[from.X, from.Y, from.Z].Type != CellType.Empty)
            {
                throw new ArgumentException("Cell is not empty.");
            }
            if (_cells[to.X, to.Y, to.Z].Type != CellType.Empty)
            {
                throw new ArgumentException("Cell is not empty.");
            }

            CalculateWarm(to.X, to.Y, to.Z);
            return BuildPipe(from, to);
        }

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
                        WallCreated?.Invoke(cell);
                    }
                }
            }
        }

        private void CalculateWarm(int x, int y, int z)
        {
            var cell = _cells[x, y, z];
            cell.Temperature = MaxTemperature;
            TemperatureChanged?.Invoke(cell);
            var stack = new Stack<Cell>();
            stack.Push(cell);
            
            while (stack.Any())
            {
                var temp = stack.Pop();
                var temperature = temp.Temperature - TemperatureStep;
                var neigbours = GetNeigbours(temp)
                    .Where(c => float.IsNaN(c.Temperature) || (c.Temperature < temperature && c.Type is CellType.Empty))
                    .ToList();

                foreach (var neigbour in neigbours)
                {
                    if (neigbour.Type == CellType.Wall)
                    {
                        neigbour.Temperature = float.NegativeInfinity;
                    }
                    else
                    {
                        neigbour.Temperature = temperature;
                    }

                    stack.Push(neigbour);
                    TemperatureChanged?.Invoke(cell);
                }
            }
        }

        private IEnumerable BuildPipe(Vector3i from, Vector3i to)
        {
            var current = from;

            while (current != to)
            {
                _cells[current.X, current.Y, current.Z].Type = CellType.Pipe;
                PipeCreated?.Invoke(_cells[current.X, current.Y, current.Z]);
                current = GetNeigbours(current.X, current.Y, current.Z)
                    .Where(c => c.Type == CellType.Empty)
                    .OrderByDescending(c => c.Temperature)
                    .First().Position;

                yield return null;
            }

            Console.WriteLine("Pipe generation end.");
        }

        private IEnumerable<Cell> GetNeigbours(Cell cell) =>
            GetNeigbours(cell.Position.X, cell.Position.Y, cell.Position.Z);

        private IEnumerable<Cell> GetNeigbours(int x, int y, int z)
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
    }
}
