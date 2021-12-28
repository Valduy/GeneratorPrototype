﻿using Net;

namespace Graph
{
    public class Net<T>
    {
        private List<Node<T>> _nodes = new();

        public EventHandler<ConnectionEventArgs<T>>? Connected;

        public Node<T> CreateNode(T item)
        {
            var node = new Node<T>(this, item);
            _nodes.Add(node);
            return node;
        }

        public bool RemoveNode(Node<T> node)
        {
            if (node.Net != this)
            {
                throw new InvalidOperationException("Node not belong to this net.");
            }

            foreach (var neighbour in node.Neighbours)
            {
                neighbour.Remove(node);
            }

            return _nodes.Remove(node);
        }

        public bool Connect(Node<T> node1, Node<T> node2)
        {
            if (node1.Net != this)
            {
                throw new InvalidOperationException("Node 1 not belong to this net.");
            }

            if (node2.Net != this)
            {
                throw new InvalidOperationException("Node 2 not belong to this net.");
            }

            if (node1 == node2)
            {
                throw new InvalidOperationException("Can't connect node with yourself.");
            }

            if (node1.Neighbours.Contains(node2))
            {
                return false;
            }

            node1.Add(node2);
            node2.Add(node1);
            Connected?.Invoke(this, new ConnectionEventArgs<T>(node1, node2));
            return true;
        }

        public IEnumerable<Node<T>> GetNodes() => _nodes;

        public IEnumerable<(Node<T>, Node<T>)> GetEdges()
        {
            var visited = new HashSet<Node<T>>();

            foreach (var node in _nodes)
            {
                foreach (var neighbour in node.Neighbours.Except(visited))
                {
                    yield return (node, neighbour);
                }

                visited.Add(node);
            }
        }
    }
}