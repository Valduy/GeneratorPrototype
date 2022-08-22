namespace MeshTopology
{
    public class TopologyNode<T>
    {
        internal List<TopologyNode<T>> _neighbours = new();

        public T? Data { get; set; }
        public Face Face { get; }
        public IReadOnlyList<TopologyNode<T>> Neighbours => _neighbours;

        public TopologyNode(Face face, T data) : this(face)
        {
            Data = data;
        }

        public TopologyNode(Face face)
        {
            Face = face;            
        }
    }
}