using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace MeshTopology
{
    public enum FaceOrientation
    {
        Wall,
        Floor,
    }

    public static class FaceHelper
    {
        public static List<Face> ExtractFaces(this Mesh mesh)
        {
            var result = new List<Face>();

            for (int i = 0; i < mesh.Indices.Count; i += 4)
            {
                result.Add(new Face(new List<Vertex>
                {
                    mesh.Vertices[mesh.Indices[i + 0]],
                    mesh.Vertices[mesh.Indices[i + 1]],
                    mesh.Vertices[mesh.Indices[i + 2]],
                    mesh.Vertices[mesh.Indices[i + 3]],
                }));
            }

            return result;
        }

        public static Vector3 Centroid(this Face face)
            => face.Select(v => v.Position).Aggregate((p1, p2) => p1 + p2) / face.Count;

        public static FaceOrientation GetFaceOrientation(this Face face)
        {
            var centroid = face.Centroid();
            var a = (face[0].Position - centroid).Normalized();
            var b = (face[1].Position - centroid).Normalized();
            var c = Vector3.Cross(a, b);
            return (c.Y != 0) ? FaceOrientation.Floor : FaceOrientation.Wall;
        }

        public static IEnumerable<(Vector3 A, Vector3 B)> EnumerateEdges(this Face face)
        {
            for (int i = 0; i < face.Count; i++)
            {
                yield return (face[i].Position, face[(i + 1) % face.Count].Position);
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
            for (int i = 0; i < face.Count; i++)
            {
                Vector3 a = face[i].Position;
                Vector3 b = face[(i + 1) % face.Count].Position;

                if (IsEquivalent(a, b, edge.A, edge.B))
                {
                    return i;
                }
            }

            throw new InvalidOperationException("The face does not contain this edge.");
        }

        public static (Vector3 A, Vector3 B) GetEdgeByIndex(this Face face, int index)
        {
            Vector3 a = face[index].Position;
            Vector3 b = face[(index + 1) % face.Count].Position;
            return (a, b);
        }

        public static bool IsEquivalent((Vector3 A, Vector3 B) edge1, (Vector3 A, Vector3 B) edge2)
            => IsEquivalent(edge1.A, edge1.B, edge2.A, edge2.B);

        public static bool IsEquivalent(Vector3 a1, Vector3 b1, Vector3 a2, Vector3 b2)
            => a1 == a2 && b1 == b2 || a1 == b2 && b1 == a2;
    }
}
