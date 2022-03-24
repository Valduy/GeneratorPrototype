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
        public const float WallFactor = 100;
        public const float InsideFactor = 1000;
        public const float PipeFactor = 1000;
        public const float TemperatureStep = 50;

        public const float SplineStep = 0.5f;

        private Cell[,,] _cells = new Cell[
            FloorWidth + WallSpacing * 2,
            BuildingHeight + WallSpacing * 2,
            FloorDepth + WallSpacing * 2];

        public int Width => _cells.GetLength(0);
        public int Height => _cells.GetLength(1);
        public int Depth => _cells.GetLength(2);

        public event Action<Cell> WallCreated;
        public event Action<Cell> PipeCreated;
        public event Action<Vector3> SegmentCreated;
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

            foreach (var cell in _cells)
            {
                cell.Temperature = float.NaN;
                cell.Direction = null;
            }

            CalculateWarm(to.X, to.Y, to.Z);
            TemperatureCalculated?.Invoke();
            CalculateVectors();
            VectorsCalculated?.Invoke();
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
            TemperatureCalculated?.Invoke();
            CalculateVectors();
            VectorsCalculated?.Invoke();
            return BuildSpline(from, to);
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
                    .Where(c => float.IsNaN(c.Temperature) || (c.Temperature < temperature && c.Type is CellType.Empty or CellType.Inside))
                    .ToList();

                foreach (var neigbour in neigbours)
                {
                    if (neigbour.Type is CellType.Wall or CellType.Pipe)
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

            //foreach (var c in this.Where(c => c.Type is CellType.Empty or CellType.Inside))
            //{
            //    if (IsInsideBuilding(c))
            //    {
            //        c.Type = CellType.Inside;
            //        c.Temperature -= InsideFactor;
            //    }
            //}

            foreach (var c in this.Where(c => c.Type is CellType.Empty or CellType.Inside))
            {
                if (!GetCube(c).Any(n => n.Type is CellType.Wall))
                {
                    c.Temperature -= WallFactor;
                }

                //if (GetCross(c).Any(n => n.Type is CellType.Inside))
                //{
                //    c.Temperature -= WallFactor;
                //}

                if (GetCross(c).Any(n => n.Type is CellType.Pipe))
                {
                    c.Temperature -= PipeFactor;
                }
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
                    .OrderByDescending(c => c.Temperature)
                    .First();

                if (next.Temperature >= temp.Temperature)
                    temp.Direction = next.Position - temp.Position;
                else
                    temp.Direction = new Vector3i(0);

                var neighbours = GetCube(temp)
                    .Where(c => c.Direction == null);

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
                    .Where(c => c.Type is CellType.Empty or CellType.Inside)
                    .OrderByDescending(c => c.Temperature)
                    .First().Position;

                yield return null;
            }

            Console.WriteLine("Pipe generation end.");
        }

        private IEnumerable BuildSpline(Vector3i from, Vector3i to)
        {
            Vector3 current = from;

            while (new Vector3i((int)current.X, (int)current.Y, (int)current.Z) != to)
            {
                int xi = Math.Clamp((int)MathF.Floor(current.X), 0, Width - 1);
                int yi = Math.Clamp((int)MathF.Floor(current.Y), 0, Height - 1);
                int zi = Math.Clamp((int)MathF.Floor(current.Z), 0, Depth - 1);

                int xc = Math.Clamp(xi + 1, 0, Width - 1);
                int yc = Math.Clamp(yi + 1, 0, Height - 1);
                int zc = Math.Clamp(zi + 1, 0, Depth - 1);

                float dx = current.X - xi;
                float dy = current.Y - yi;
                float dz = current.Z - zi;

                var cell = _cells[xi, yi, zi];
                cell.Type = CellType.Pipe;

                Vector3 main = Vector3.Zero;

                
                Vector3 direction =
                    new Vector3(_cells[xi, yi, zi].Direction ?? main) * (1 - dx) * (1 - dy) * (1 - dz) +
                    new Vector3(_cells[xi, yi, zc].Direction ?? main) * (1 - dx) * (1 - dy) * (dz) +
                    new Vector3(_cells[xi, yc, zi].Direction ?? main) * (1 - dx) * (dy)     * (1 - dz) +
                    new Vector3(_cells[xi, yc, zc].Direction ?? main) * (1 - dx) * (dy)     * (dz) +
                    new Vector3(_cells[xc, yi, zi].Direction ?? main) * (dx)     * (1 - dy) * (1 - dz) +
                    new Vector3(_cells[xc, yi, zc].Direction ?? main) * (dx)     * (1 - dy) * (dz) +
                    new Vector3(_cells[xc, yc, zi].Direction ?? main) * (dx)     * (dy)     * (1 - dz) +
                    new Vector3(_cells[xc, yc, zc].Direction ?? main) * (dx)     * (dy)     * (dz);

                current += direction.Normalized() * SplineStep;
                SegmentCreated?.Invoke(current);

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

        private bool IsInsideBuilding(Cell cell) 
            => IsInsideBuilding(cell.Position);

        private bool IsInsideBuilding(Vector3i position)
        {
            if (_cells[position.X, position.Y, position.Z].Type == CellType.Wall)
            {
                return false;
            }

            int wallsCount = 0;

            for (int x = position.X; x < Width; x++)
            {
                if (_cells[x, position.Y, position.Z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }
            for (int x = 0; x < position.X; x++)
            {
                if (_cells[x, position.Y, position.Z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }
            for (int y = position.Y; y < Height; y++)
            {
                if (_cells[position.X, y, position.Z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }
            for (int y = 0; y < position.Y; y++)
            {
                if (_cells[position.X, y, position.Z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }
            for (int z = position.Z; z < Depth; z++)
            {
                if (_cells[position.X, position.Y, z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }
            for (int z = 0; z < position.Z; z++)
            {
                if (_cells[position.X, position.Y, z].Type == CellType.Wall)
                {
                    wallsCount++;
                    break;
                }
            }

            return wallsCount > 2;
        }
    }
}
