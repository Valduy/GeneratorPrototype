using System.Collections;
using System.Drawing;
using OpenTK.Mathematics;
using static System.Single;

namespace PipesDemo.Models
{
    public class BuildingModel : IEnumerable<Cell>
    {
        public const int FloorsInRow = 8;
        //public const int FloorsInColumn = 8;
        public const int FloorsInColumn = 2;

        public const int FloorWidth = 16;
        public const int FloorDepth = 16;
        public const int BuildingHeight = FloorsInRow * FloorsInColumn;

        public const int WallSpacing = 5;

        public const float MaxTemperature = 100000;
        public const float WallFactor = 50;
        public const float InsideFactor = 1000;
        public const float PipeFactor = 1000;
        public const float TemperatureStep = 50;

        public const float SplineStep = 0.1f;

        private Cell[,,] _cells = new Cell[
            FloorWidth + WallSpacing * 2,
            BuildingHeight + WallSpacing * 2,
            FloorDepth + WallSpacing * 2];

        public int Width => _cells.GetLength(0);
        public int Height => _cells.GetLength(1);
        public int Depth => _cells.GetLength(2);

        public event Action WallsCreated;
        public event Action TemperatureCalculated;
        public event Action VectorsCalculated;
        public event Action<List<Cell>> AStarPipeCreated;

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

            WallsCreated?.Invoke();
        }

        public IEnumerator<Cell> GenerateRigidPipe(Vector3i from, Vector3i to)
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
                cell.Temperature = NaN;
                cell.Direction = null;
            }

            CalculateWarm(to.X, to.Y, to.Z);
            TemperatureCalculated?.Invoke();
            return CreateRigidPipeSegment(from, to);
        }

        public IEnumerator<Vector3> GenerateFlexiblePipe(Vector3i from, Vector3i to)
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
            //CalculateVectors();
            //VectorsCalculated?.Invoke();
            return CreateFlexiblePipeSegment(from, to);
        }

        public List<Cell> GenerateAStarPipe(Vector3i from, Vector3i to)
        {
            var path = AStar(this[from], this[to]);
            path.ForEach(p => p.Type = CellType.Pipe);
            return path;
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

        private void CalculateWarm(int x, int y, int z)
        {
            ClearWarm();
            var cell = _cells[x, y, z];
            cell.Temperature = MaxTemperature;
            var stack = new Stack<Cell>();
            stack.Push(cell);
            
            while (stack.Any())
            {
                var temp = stack.Pop();
                var temperature = temp.Temperature - TemperatureStep;
                
                var neigbours = GetCross(temp)
                    .Where(c => IsNaN(c.Temperature) || (c.Temperature < temperature /* && c.Type is CellType.Empty or CellType.Inside*/))
                    .ToList();

                foreach (var neigbour in neigbours)
                {
                    //if (neigbour.Type is CellType.Wall or CellType.Pipe)
                    //{
                    //    neigbour.Temperature = 0;
                    //}
                    //else
                    //{
                    //    neigbour.Temperature = temperature;
                    //    stack.Push(neigbour);
                    //}
                    neigbour.Temperature = temperature;
                    stack.Push(neigbour);
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

            foreach (var c in this.Where(c => c.Type is CellType.Wall or CellType.Pipe))
            {
                var coldest = GetCube(c)
                    .Where(c => c.Type is not (CellType.Wall or CellType.Pipe))
                    .OrderBy(c => c.Temperature)
                    .FirstOrDefault();

                if (coldest != null)
                {
                    c.Temperature = coldest.Temperature/* - TemperatureStep*/;
                }
            }

            //foreach (var c in this.Where(c => c.Type is CellType.Empty or CellType.Inside))
            //{
            //if (!GetCube(c).Any(n => n.Type is (CellType.Wall or CellType.Pipe)))
            //{
            //    c.Temperature -= WallFactor;
            //}

            //if (GetCross(c).Any(n => n.Type is CellType.Inside))
            //{
            //    c.Temperature -= WallFactor;
            //}

            //if (GetCross(c).Any(n => n.Type is CellType.Pipe))
            //{
            //    c.Temperature -= PipeFactor;
            //}
            //}
        }

        private void ClearWarm()
        {
            foreach (var cell in _cells)
            {
                cell.Temperature = NaN;
                cell.Direction = null;
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
                //var next = GetCube(temp)
                //    .OrderByDescending(c => c.Temperature)
                //    .First();

                //temp.Direction = next.Temperature >= temp.Temperature
                //    ? next.Position - temp.Position
                //    : new Vector3i(0);

                //WallAdjustment(temp);

                //int xi = temp.Position.X;
                //int yi = temp.Position.Y;
                //int zi = temp.Position.Z;

                //int xc = xi + 1;
                //int yc = yi + 1;
                //int zc = zi + 1;

                //var grad = new Vector3(
                //    (GetHeat(xc, yi, zi) - GetHeat(xi - 1, yi, zi)) / (xc - xi + 1),
                //    (GetHeat(xi, yc, zi) - GetHeat(xi, yi - 1, zi)) / (yc - yi + 1),
                //    (GetHeat(xi, yi, zc) - GetHeat(xi, yi, zi - 1)) / (zc - zi + 1));

                //temp.Direction = grad;

                var grad = GetGradient(temp.Position, 1.0f);

                temp.Direction = grad;

                var neighbours = GetCube(temp)
                    .Where(c => c.Direction == null);

                foreach (var neighbour in neighbours)
                {
                    stack.Push(neighbour);
                }
            }
        }

        //private void WallAdjustment(Cell cell)
        //{
        //    const int scale = 5;

        //    if (this[cell.Position + new Vector3i(cell.Direction!.Value.X, 0, 0)].Type != CellType.Wall)
        //    {
        //        cell.Direction = cell.Direction.Value * new Vector3i(scale, 1, 1);
        //    }
        //    if (this[cell.Position + new Vector3i(0, cell.Direction!.Value.Y, 0)].Type != CellType.Wall)
        //    {
        //        cell.Direction = cell.Direction.Value * new Vector3i(1, scale, 1);
        //    }
        //    if (this[cell.Position + new Vector3i(0, 0, cell.Direction!.Value.Z)].Type != CellType.Wall)
        //    {
        //        cell.Direction = cell.Direction.Value * new Vector3i(1, 1, scale);
        //    }
        //}

        private IEnumerator<Cell> CreateRigidPipeSegment(Vector3i from, Vector3i to)
        {
            var current = from;
            this[current].Type = CellType.Pipe;
            yield return this[current];

            while (current != to)
            {
                this[current].Type = CellType.Pipe;
                current = GetCross(current)
                    .Where(c => c.Type is CellType.Empty or CellType.Inside)
                    .OrderByDescending(c => c.Temperature)
                    .First().Position;

                yield return _cells[current.X, current.Y, current.Z];
            }
        }

        private IEnumerator<Vector3> CreateFlexiblePipeSegment(Vector3i from, Vector3i to)
        {
            Vector3 current = from;
            this[from].Type = CellType.Pipe;
            yield return current;

            while (new Vector3i((int)Math.Round(current.X), (int)Math.Round(current.Y), (int)Math.Round(current.Z)) != to)
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

                //Vector3 main = Vector3.Zero;
                ////Vector3 main = _cells[xi, yi, zi].Direction.Value;

                //Vector3 direction =
                //    new Vector3(_cells[xi, yi, zi].Direction ?? main) * (1 - dx) * (1 - dy) * (1 - dz) +
                //    new Vector3(_cells[xi, yi, zc].Direction ?? main) * (1 - dx) * (1 - dy) * (dz) +
                //    new Vector3(_cells[xi, yc, zi].Direction ?? main) * (1 - dx) * (dy) * (1 - dz) +
                //    new Vector3(_cells[xi, yc, zc].Direction ?? main) * (1 - dx) * (dy) * (dz) +
                //    new Vector3(_cells[xc, yi, zi].Direction ?? main) * (dx) * (1 - dy) * (1 - dz) +
                //    new Vector3(_cells[xc, yi, zc].Direction ?? main) * (dx) * (1 - dy) * (dz) +
                //    new Vector3(_cells[xc, yc, zi].Direction ?? main) * (dx) * (dy) * (1 - dz) +
                //    new Vector3(_cells[xc, yc, zc].Direction ?? main) * (dx) * (dy) * (dz);

                //current += direction.Normalized() * SplineStep;

                float h = 1.0f;

                Vector3 direction =
                    new Vector3(GetGradient(xi, yi, zi, h)) * (1 - dx) * (1 - dy) * (1 - dz) +
                    new Vector3(GetGradient(xi, yi, zc, h)) * (1 - dx) * (1 - dy) * (dz) +
                    new Vector3(GetGradient(xi, yc, zi, h)) * (1 - dx) * (dy) * (1 - dz) +
                    new Vector3(GetGradient(xi, yc, zc, h)) * (1 - dx) * (dy) * (dz) +
                    new Vector3(GetGradient(xc, yi, zi, h)) * (dx) * (1 - dy) * (1 - dz) +
                    new Vector3(GetGradient(xc, yi, zc, h)) * (dx) * (1 - dy) * (dz) +
                    new Vector3(GetGradient(xc, yc, zi, h)) * (dx) * (dy) * (1 - dz) +
                    new Vector3(GetGradient(xc, yc, zc, h)) * (dx) * (dy) * (dz);

                Console.WriteLine(direction);
                current += direction.Normalized() * SplineStep;

                yield return current;
            }

            yield return to;
        }

        Vector3 GetGradient(Vector3 args, float h) 
            => GetGradient(args.X, args.Y, args.Z, h);

        Vector3 GetGradient(float x, float y, float z, float h)
        {
            return new Vector3(
                (GetHeat(x + h, y, z) - GetHeat(x - h, y, z)) / (2 * h),
                (GetHeat(x, y + h, z) - GetHeat(x, y - h, z)) / (2 * h),
                (GetHeat(x, y, z + h) - GetHeat(x, y, z - h)) / (2 * h));
        }

        float GetHeat(Vector3 args) 
            => GetHeat(args.X, args.Y, args.Z);

        float GetHeat(float x, float y, float z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= Width - 1 || y >= Height - 1 || z >= Depth - 1)
            {
                return 0;
            }

            int xf = (int)MathF.Floor(x);
            int yf = (int)MathF.Floor(y);
            int zf = (int)MathF.Floor(z);

            int xc = xf + 1;
            int yc = yf + 1;
            int zc = zf + 1;

            float denominator = (xc - xf) * (yc - yf) * (zc - zf);

            var result =
                _cells[xf, yf, zf].Temperature / denominator * (xc - x) * (yc - y) * (zc - z) +
                _cells[xc, yf, zf].Temperature / denominator * (x - xf) * (yc - y) * (zc - z) +
                _cells[xc, yf, zc].Temperature / denominator * (x - xf) * (yc - y) * (z - zf) +
                _cells[xc, yc, zf].Temperature / denominator * (x - xf) * (y - yf) * (zc - z) +
                _cells[xc, yc, zc].Temperature / denominator * (x - xf) * (y - yf) * (z - zf);

            return result;

            //return _cells[(int) Math.Round(x), (int) Math.Round(y), (int) Math.Round(z)].Temperature;
        }

        //float GetHeat(int x, int y, int z)
        //{
        //    if (x < 0 || y < 0 || z < 0 || x >= Width || y >= Height || z >= Depth)
        //    {
        //        return 0;
        //    }

        //    return _cells[x, y, z].Temperature;
        //}

        private List<Cell> AStar(Cell start, Cell goal)
        {
            // Set all nodes costs as infinity.
            foreach (var cell in _cells)
            {
                cell.Temperature = PositiveInfinity;
                cell.Prev = null;
            }

            start.Temperature = 0;
            var reachable = new HashSet<Cell> { start };
            var explored = new HashSet<Cell>();

            while (reachable.Any())
            {
                var node = ChooseNode(reachable, goal);
                if (node == goal) return BuildPath(node);

                reachable.Remove(node!);
                explored.Add(node!);

                var newReachable = new HashSet<Cell>(GetCross(node!).Where(IsSuitableForPipe));
                newReachable.ExceptWith(explored);

                foreach (var adjacent in newReachable)
                {
                    // Add new reachable.
                    if (!reachable.Contains(adjacent))
                    {
                        adjacent.Direction = node!.Position - adjacent.Position;
                        reachable.Add(adjacent);
                    }

                    float cost = GetCost(node!, adjacent);

                    // Cost recalculation.
                    if (node!.Temperature + cost < adjacent.Temperature)
                    {
                        adjacent.Prev = node;
                        adjacent.Temperature = node.Temperature + cost;
                    }
                }
            }

            throw new ArgumentException("Path not found.");
        }

        float GetCost(Cell node, Cell cell)
        {
            float modifier = 1;

            if (node.Prev != null && 
                MathHelper.ApproximatelyEqualEpsilon((node.Prev.Position - node.Position).EuclideanLength, 1.0f, Epsilon) &&
                node.Prev.Position - node.Position == node.Position - cell.Position)
            {
                modifier = 0.5f;
            }

            if (GetCube(cell).All(IsSuitableForPipe))
            {
                return 10 * modifier;
            }
            if (GetCross(cell).All(IsSuitableForPipe))
            {
                return 5 * modifier;
            }

            return 1 * modifier;
        }

        bool IsSuitableForPipe(Cell cell) => 
            cell.Type is not CellType.Wall
            && cell.Type is not CellType.Pipe;
            //&& !IsInsideBuilding(cell);

        private Cell? ChooseNode(IEnumerable<Cell> reachable, Cell goal)
        {
            float minCost = PositiveInfinity;
            Cell? best = null;

            foreach (var node in reachable)
            {
                float costFromStart = node.Temperature;
                float costToGoal = ManhattanLength(node, goal);
                float totalCost = costFromStart + costToGoal;

                if (minCost > totalCost)
                {
                    minCost = totalCost;
                    best = node;
                }
            }

            return best;
        }

        private float ManhattanLength(Cell a, Cell b) =>
            Math.Abs(a.Position.X - b.Position.X) +
            Math.Abs(a.Position.Y - b.Position.Y) +
            Math.Abs(a.Position.Z - b.Position.Z);

        private List<Cell> BuildPath(Cell node)
        {
            var path = new List<Cell>();

            while (node.Prev != null)
            {
                path.Add(node);
                node = node.Prev;
            }

            path.Add(node);
            return path;
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
