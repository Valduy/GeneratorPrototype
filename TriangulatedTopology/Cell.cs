using OpenTK.Mathematics;
using System.Collections;
using TextureUtils;
using TriangulatedTopology.RulesAdapters;

namespace TriangulatedTopology
{
    public class NeighbourData
    {
        public Cell Neighbour;
        public RuleAdapter Adapter;

        public NeighbourData(Cell neighbour, RuleAdapter adapter)
        {
            Neighbour = neighbour;
            Adapter = adapter;
        }
    }

    public class Cell : IReadOnlyList<Vector2>
    {
        public const int NeighboursCount = 4;

        private List<Vector2> _points = new();

        public int Count => _points.Count;

        public readonly NeighbourData?[] Neighbours = new NeighbourData?[NeighboursCount];
        public readonly List<Rule> Rules = new List<Rule>();

        public Vector2 this[int index] => _points[index];

        public Cell(IEnumerable<Vector2> points)
        {
            _points = points.ToList();
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
