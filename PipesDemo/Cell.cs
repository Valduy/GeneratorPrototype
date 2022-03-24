using OpenTK.Mathematics;
using static System.Single;

namespace PipesDemo
{
    public enum CellType
    {
        Empty,
        Wall,
        Pipe,
        Inside,
    }

    public class Cell
    {
        private CellType _type = CellType.Empty;
        private float _temperature = NaN;
        private Vector3i? _direction = null;

        public BuildingModel Model { get; }
        public Vector3i Position { get; }

        public CellType Type
        {
            get => _type;
            set
            {
                if (_type == value) return;
                _type = value;
                TypeChanged?.Invoke(this);
            }
        }

        public float Temperature
        {
            get => _temperature;
            set
            {
                if (MathHelper.ApproximatelyEqualEpsilon(_temperature, value, Epsilon)) return;
                _temperature = value;
                TemperatureChanged?.Invoke(this);
            }
        }

        public Vector3i? Direction
        {
            get => _direction;
            set
            {
                if (_direction == value) return;
                _direction = value;
                DirectionChanged?.Invoke(this);
            }
        }

        public event Action<Cell> TypeChanged;
        public event Action<Cell> TemperatureChanged;
        public event Action<Cell> DirectionChanged;

        public Cell(BuildingModel model, int x, int y, int z)
            : this(new Vector3i(x, y, z))
        {
            Model = model;
        }

        public Cell(Vector3i position)
        {
            Position = position;
        }
    }
}
