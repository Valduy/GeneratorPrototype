using OpenTK.Mathematics;
using System.Drawing;

namespace TriangulatedTopology.Geometry
{
    public class LogicalNode
    {
        private List<Vector3> _corners;
        private List<bool> _connections;

        public readonly Color Color;

        public IReadOnlyList<Vector3> Corners => _corners;
        public IReadOnlyList<bool> Connections => _connections;

        public LogicalNode(IEnumerable<Vector3> corners, Color color, IEnumerable<bool> connections)
        {
            Color = color;
            _corners = new List<Vector3>(corners);
            _connections = new List<bool>(connections);
        }
    }
}
