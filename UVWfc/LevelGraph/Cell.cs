using OpenTK.Mathematics;
using System.Collections;
using TextureUtils;
using UVWfc.RulesAdapters;

namespace UVWfc.LevelGraph
{
    public class NeighbourData
    {
        public Cell Cell;
        public RuleAdapter Adapter;

        public NeighbourData(Cell cell, RuleAdapter adapter)
        {
            Cell = cell;
            Adapter = adapter;
        }
    }

    public class Cell : IReadOnlyList<Vector2>
    {
        public const int NeighboursCount = 4;

        private List<Vector2> _points = new();

        public int Count => _points.Count;
        public Vector3 Normal;

        public readonly NeighbourData?[] Neighbours = new NeighbourData?[NeighboursCount];
        public readonly List<Rule> Rules = new();

        public Vector2 this[int index] => _points[index];

        public Cell(IEnumerable<Vector2> points, Vector3 normal)
        {
            _points = points.ToList();
            Normal = normal;
        }

        public IEnumerator<Vector2> GetEnumerator()
        {
            return _points.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
