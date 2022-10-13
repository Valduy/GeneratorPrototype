namespace MeshTopology
{
    public class TopologyNode
    {
        internal List<TopologyNode> _neighbours = new();

        public Face Face { get; }
        public IReadOnlyList<TopologyNode> Neighbours => _neighbours;

        public TopologyNode(Face face)
        {
            Face = face;            
        }
    }
}