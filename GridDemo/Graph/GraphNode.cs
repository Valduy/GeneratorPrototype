namespace GridDemo.Graph
{
    public class GraphNode<T>
    {
        internal object? Owner= null;
        internal List<GraphNode<T>> LinkedNodes = new();

        public readonly T Data;
        public IReadOnlyCollection<GraphNode<T>> Linked => LinkedNodes;

        public GraphNode(T data)
        {
            Data = data;
        }
    }
}
