namespace GridDemo.Graph
{
    public class UndirectedGraph<T>
    {
        private readonly HashSet<GraphNode<T>> _nodes = new();

        public IReadOnlyCollection<GraphNode<T>> Nodes => _nodes;

        public bool IsLinked(GraphNode<T> lhs, GraphNode<T> rhs)
        {
            return lhs.Linked.Contains(rhs) && rhs.Linked.Contains(lhs);
        }

        public GraphNode<T> Add(T data)
        {
            var node = new GraphNode<T>(data);
            Add(node);
            return node;
        }

        public void Add(GraphNode<T> node)
        {
            if (node.Owner != null)
            {
                throw new InvalidOperationException();
            }

            node.Owner = this;
            _nodes.Add(node);
        }

        public void Remove(GraphNode<T> node)
        {
            if (node.Owner != this)
            {
                throw new InvalidOperationException();
            }
            if (!_nodes.Contains(node))
            {
                throw new InvalidOperationException();
            }

            foreach (var neighbour in node.Linked)
            {
                Delink(node, neighbour);
            }

            _nodes.Remove(node);
        }

        public void Link(GraphNode<T> lhs, GraphNode<T> rhs)
        {
            if (lhs.Owner != this || rhs.Owner != this)
            {
                throw new InvalidOperationException();
            }
            if (!(_nodes.Contains(lhs) && _nodes.Contains(rhs)))
            {
                throw new InvalidOperationException();
            }

            lhs.LinkedNodes.Add(rhs);
            rhs.LinkedNodes.Add(lhs);
        }

        public void Delink(GraphNode<T> lhs, GraphNode<T> rhs)
        {
            if (lhs.Owner != this || rhs.Owner != this)
            {
                throw new InvalidOperationException();
            }
            if (!(lhs.Linked.Contains(rhs) && rhs.Linked.Contains(lhs)))
            {
                throw new InvalidOperationException();
            }
            if (!(_nodes.Contains(lhs) && _nodes.Contains(rhs)))
            {
                throw new InvalidOperationException();
            }

            lhs.LinkedNodes.Remove(rhs);
            rhs.LinkedNodes.Remove(lhs);
        }

        public IEnumerable<(GraphNode<T>, GraphNode<T>)> GetEdges()
        {
            var visited = new HashSet<GraphNode<T>>();

            foreach (var node in _nodes)
            {
                foreach (var linked in node.Linked.Except(visited))
                {
                    yield return (node, linked);
                }

                visited.Add(node);
            }
        }
    }
}
