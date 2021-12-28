using Graph;

namespace Net
{
    public class ConnectionEventArgs<T> : EventArgs
    {
        public readonly Node<T> Node1;
        public readonly Node<T> Node2;

        public ConnectionEventArgs(Node<T> node1, Node<T> node2)
        {
            Node1 = node1;
            Node2 = node2;
        }
    }
}
