using GameEngine.Graphics;
using MeshTopology;
using System.Collections;

namespace UVWfc.LevelGraph.TextureIsland
{
    public class Side : IReadOnlyList<Edge>
    {
        private readonly List<Edge> _segments;

        public int Count => _segments.Count;

        public Vertex A => _segments[0].A;
        public Vertex B => _segments[_segments.Count - 1].B;

        public Side(IEnumerable<Edge> edges)
        {
            _segments = new List<Edge>(edges);
        }

        public Edge this[int index] => _segments[index];

        public IEnumerator<Edge> GetEnumerator()
        {
            return _segments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
