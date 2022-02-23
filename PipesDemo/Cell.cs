using OpenTK.Mathematics;

namespace PipesDemo
{
    public enum CellType
    {
        Empty,
        Wall,
        Pipe,
    }

    public class Cell
    {
        public Vector3i Position { get; }
        public CellType Type { get; set; } = CellType.Empty;
        public float Temperature { get; set; } = float.NaN;
        public Vector3i? Direction { get; set; } = null;

        public Cell(int x, int y, int z)
            : this(new Vector3i(x, y, z))
        { }

        public Cell(Vector3i position)
        {
            Position = position;
        }
    }
}
