namespace Graph
{
    public class Node<T>
    {
        private List<Node<T>> _neighbours = new();

        public Net<T> Net { get; }
        public T Item { get; }
        public IReadOnlyList<Node<T>> Neighbours => _neighbours;

        internal Node(Net<T> net, T item)
        {
            Net = net;
            Item = item;
        }

        internal void Add(Node<T> node) => _neighbours.Add(node);
        internal bool Remove(Node<T> node) => _neighbours.Remove(node);
    }
}
