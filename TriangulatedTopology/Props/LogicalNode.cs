using OpenTK.Mathematics;
using TextureUtils;

namespace TriangulatedTopology.Props
{
    public class LogicalNode
    {
        private List<Vector3> _corners;
        private List<bool> _connections;

        public readonly Rule Rule;

        public IReadOnlyList<Vector3> Corners => _corners;
        public IReadOnlyList<bool> Connections => _connections;

        public LogicalNode(IEnumerable<Vector3> corners, Rule rule, IEnumerable<bool> connections)
        {
            Rule = rule;
            _corners = new List<Vector3>(corners);
            _connections = new List<bool>(connections);
        }
    }
}
