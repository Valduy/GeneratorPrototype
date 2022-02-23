using System.Collections;
using System.Drawing;
using OpenTK.Mathematics;

namespace PipesDemo
{
    public class BuildingModel : IEnumerable<Cell>
    {
        public const int FloorsInRow = 8;
        //public const int FloorsInColumn = 8;
        public const int FloorsInColumn = 2;

        public const int FloorWidth = 16;
        public const int FloorDepth = 16;
        public const int BuildingHeight = FloorsInRow * FloorsInColumn;

        public const int WallSpacing = 1;

        public const float MaxTemperature = 100000;
        public const float WallFactor = 5;
        public const float TemperatureStep = 10;

        private Cell[,,] _cells = new Cell[
            FloorWidth + WallSpacing * 2,
            BuildingHeight + WallSpacing * 2,
            FloorDepth + WallSpacing * 2];

        public int Width => _cells.GetLength(0);
        public int Height => _cells.GetLength(1);
        public int Depth => _cells.GetLength(2);

        public event Action<Cell> WallCreated;
        public event Action<Cell> PipeCreated;
        public event Action TemperatureCalculated;
        public event Action VectorsCalculated;

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
            TemperatureCalculated?.Invoke();
            return BuildPipe(from, to);
        }

        public IEnumerable GenerateSpline(Vector3i from, Vector3i to)
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
            CalculateVectors();
            VectorsCalculated?.Invoke();
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
            var stack = new Stack<Cell>();
            stack.Push(cell);
            
            while (stack.Any())
            {
                var temp = stack.Pop();
                var temperature = temp.Temperature - TemperatureStep;
                
                var neigbours = GetCross(temp)
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
                        stack.Push(neigbour);
                    }
                }
            }

            foreach (var c in _cells)
            {
                if (GetCross(c).Any(c => c.Type == CellType.Wall))
                {
                    c.Temperature += WallFactor;
                }

                c.Temperature -= WallFactor;
            }
        }

        private void CalculateVectors()
        {
            var initial = _cells[0, 0, 0];
            var stack = new Stack<Cell>();
            stack.Push(initial);

            while (stack.Any())
            {
                var temp = stack.Pop();
                var next = GetCube(temp)
                    .Where(c => c.Type is CellType.Empty)
                    .OrderByDescending(c => c.Temperature)
                    .First();

                temp.Direction = next.Temperature >= temp.Temperature 
                    ? next.Position - temp.Position 
                    : Vector3.Zero;

                var neighbours = GetCube(temp)
                    .Where(c => c.Type is CellType.Empty && c.Direction == Vector3.NegativeInfinity);

                foreach (var neighbour in neighbours)
                {
                    stack.Push(neighbour);
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
                current = GetCross(current)
                    .Where(c => c.Type == CellType.Empty)
                    .OrderByDescending(c => c.Temperature)
                    .First().Position;

                yield return null;
            }

            Console.WriteLine("Pipe generation end.");
        }

        private IEnumerable<Cell> GetCross(Cell cell) =>
            GetCross(cell.Position);

        private IEnumerable<Cell> GetCross(Vector3i position)
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

        private IEnumerable<Cell> GetCube(Cell cell) 
            => GetCube(cell.Position);

        private IEnumerable<Cell> GetCube(Vector3i position)
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

        public IEnumerator<Cell> GetEnumerator()
        {
            foreach (var cell in _cells)
            {
                yield return cell;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }
}
