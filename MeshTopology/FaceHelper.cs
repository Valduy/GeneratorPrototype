using OpenTK.Mathematics;

namespace MeshTopology
{
    public static class FaceHelper
    {
        public static IEnumerable<(Vector3 A, Vector3 B)> EnumerateEdges(this Face face)
        {
            for (int i = 0; i < face.Vertices.Count; i++)
            {
                yield return (face.Vertices[i].Position, face.Vertices[(i + 1) % face.Vertices.Count].Position);
            }
        }

        public static bool IsSharedEdgeExist(this Face face, Face other)
        {
            foreach (var edge1 in face.EnumerateEdges())
            {
                foreach (var edge2 in other.EnumerateEdges())
                {
                    if (IsEquivalent(edge1, edge2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static (Vector3 A, Vector3 B) GetSharedEdge(this Face face, Face other)
        {
            foreach (var edge1 in face.EnumerateEdges())
            {
                foreach (var edge2 in other.EnumerateEdges())
                {
                    if (IsEquivalent(edge1, edge2))
                    {
                        return edge1;
                    }
                }
            }

            throw new InvalidOperationException("There is no shared edge.");
        }

        public static int GetEdgeIndex(this Face face, (Vector3 A, Vector3 B) edge)
        {
            for (int i = 0; i < face.Vertices.Count; i++)
            {
                Vector3 a = face.Vertices[i].Position;
                Vector3 b = face.Vertices[(i + 1) % face.Vertices.Count].Position;

                if (IsEquivalent(a, b, edge.A, edge.B))
                {
                    return i;
                }
            }

            throw new InvalidOperationException("The face does not contain this edge.");
        }

        public static (Vector3 A, Vector3 B) GetEdgeByIndex(this Face face, int index)
        {
            Vector3 a = face.Vertices[index].Position;
            Vector3 b = face.Vertices[(index + 1) % face.Vertices.Count].Position;
            return (a, b);
        }

        public static bool IsEquivalent((Vector3 A, Vector3 B) edge1, (Vector3 A, Vector3 B) edge2)
            => IsEquivalent(edge1.A, edge1.B, edge2.A, edge2.B);

        public static bool IsEquivalent(Vector3 a1, Vector3 b1, Vector3 a2, Vector3 b2)
            => a1 == a2 && b1 == b2 || a1 == b2 && b1 == a2;
    }
}
