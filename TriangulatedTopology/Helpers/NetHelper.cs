using Graph;

namespace TriangulatedTopology.Helpers
{
    public static class NetHelper
    {
        public static List<T> ToList<T>(this Net<T> net)
        {
            var nodes = new List<T>();
            var unvisited = new HashSet<Node<T>>(net.GetNodes());
            var temp =
                net.GetNodes().FirstOrDefault(n => n.Neighbours.Count == 1) ??
                net.GetNodes().First();

            if (temp.Neighbours.Count > 2 || temp.Neighbours.Count <= 0)
            {
                throw new ArgumentException("List node should has 1 or 2 neighbours.");
            }

            nodes.Add(temp.Item);
            unvisited.Remove(temp);

            while (unvisited.Any())
            {
                temp = temp.Neighbours.First(n => unvisited.Contains(n));

                if (temp.Neighbours.Count > 2 || temp.Neighbours.Count <= 0)
                {
                    throw new ArgumentException("List node should has 1 or 2 neighbours.");
                }

                nodes.Add(temp.Item);
                unvisited.Remove(temp);
            }

            return nodes;
        }
    }
}
