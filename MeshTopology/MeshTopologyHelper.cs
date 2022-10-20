using GameEngine.Graphics;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MeshTopology
{
    public enum FaceOrientation
    {
        XY,
        XZ,
        YZ,
    }

    public class VertexComparer : IEqualityComparer<Vertex>
    {
        public bool Equals(Vertex x, Vertex y)
        {
            return x.Position.Equals(y.Position);
        }

        public int GetHashCode([DisallowNull] Vertex obj)
        {
            return obj.Position.GetHashCode();
        }
    }

    public static class MeshTopologyHelper
    {
        public const float EdgeLength = 1;

        public static List<Face> ExtractFaces(this Mesh mesh, int verticesPerFace)
        {
            var result = new List<Face>();

            for (int i = 0; i < mesh.Indices.Count; i += verticesPerFace)
            {
                var vertices = new List<Vertex>();

                for (int j = 0; j < verticesPerFace; j++)
                {
                    vertices.Add(mesh.Vertices[mesh.Indices[i + j]]);
                }

                result.Add(new Face(vertices));
            }

            return result;
        }

        public static Vector3 Centroid(this Face face)
            => face.Select(v => v.Position).Aggregate((p1, p2) => p1 + p2) / face.Count;

        public static FaceOrientation GetFaceOrientation(this Face face)
        {
            if (face.Count < 3)
            {
                throw new ArgumentException();
            }

            var normal = face.GetNormal();

            if (!MathHelper.ApproximatelyEqualEpsilon(normal.Z, 0, float.Epsilon))
            {
                return FaceOrientation.XY;
            }
            if (!MathHelper.ApproximatelyEqualEpsilon(normal.Y, 0, float.Epsilon))
            {
                return FaceOrientation.XZ;
            }

            return FaceOrientation.YZ;
        }

        #region Edges

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

        public static bool IsEquivalent((Vector3 A, Vector3 B) pair1, (Vector3 A, Vector3 B) pair2)
            => IsEquivalent(pair1.A, pair1.B, pair2.A, pair2.B);

        public static bool IsEquivalent(Vector3 a1, Vector3 b1, Vector3 a2, Vector3 b2)
            => a1 == a2 && b1 == b2 || a1 == b2 && b1 == a2;

        #endregion Edges

        #region UVs

        public static IEnumerable<(Vector2 A, Vector2 B)> EnumerateUVEdges(this Face face)
        {
            for (int i = 0; i < face.Count; i++)
            {
                yield return (face[i].TextureCoords, face[(i + 1) % face.Count].TextureCoords);
            }
        }

        public static bool IsSharedUVEdgeExist(this Face face, Face other)
        {
            foreach (var edge1 in face.EnumerateUVEdges())
            {
                foreach (var edge2 in other.EnumerateUVEdges())
                {
                    if (IsEquivalent(edge1, edge2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static (Vector2 A, Vector2 B) GetSharedUVEdge(this Face face, Face other)
        {
            foreach (var edge1 in face.EnumerateUVEdges())
            {
                foreach (var edge2 in other.EnumerateUVEdges())
                {
                    if (IsEquivalent(edge1, edge2))
                    {
                        return edge1;
                    }
                }
            }

            throw new InvalidOperationException("There is no shared edge.");
        }

        public static int GetUVEdgeIndex(this Face face, (Vector2 A, Vector2 B) edge)
        {
            for (int i = 0; i < face.Count; i++)
            {
                Vector2 a = face[i].TextureCoords;
                Vector2 b = face[(i + 1) % face.Count].TextureCoords;

                if (IsEquivalent(a, b, edge.A, edge.B))
                {
                    return i;
                }
            }

            throw new InvalidOperationException("The face does not contain this edge.");
        }

        public static (Vector2 A, Vector2 B) GetUVEdgeByIndex(this Face face, int index)
        {
            Vector2 a = face[index].TextureCoords;
            Vector2 b = face[(index + 1) % face.Count].TextureCoords;
            return (a, b);
        }

        public static bool IsEquivalent((Vector2 A, Vector2 B) pair1, (Vector2 A, Vector2 B) pair2)
            => IsEquivalent(pair1.A, pair1.B, pair2.A, pair2.B);

        public static bool IsEquivalent(Vector2 a1, Vector2 b1, Vector2 a2, Vector2 b2)
            => a1 == a2 && b1 == b2 || a1 == b2 && b1 == a2;

        #endregion UVs

        public static List<TopologyNode?[,]> ExtractXyGroups(this MeshTopology topology)
        {
            var walls = new List<TopologyNode?[,]>();
            var groups = topology.ExtractFacesGroups(n => n.Face.GetFaceOrientation() == FaceOrientation.XY);

            foreach (var group in groups)
            {
                var (Min, Max) = GetMinMaxXy(group);
                var width = (int)MathF.Round((Max.X - Min.X) / EdgeLength);
                var height = (int)MathF.Round((Max.Y - Min.Y) / EdgeLength);
                var wall = new TopologyNode?[width, height];

                var notNullNode = group.First(n => n != null)!;
                bool isMirror = notNullNode.Face.GetNormal().Z < 0;

                foreach (var node in group)
                {
                    var i = (int)MathF.Round((node.Face.Min(v => v.Position.X) - Min.X) / EdgeLength);
                    var j = (int)MathF.Round(height - (node.Face.Min(v => v.Position.Y) - Min.Y) / EdgeLength - 1);
                    i = isMirror ? width - i - 1 : i;
                    wall[i, j] = node;
                }

                walls.Add(wall);
            }

            return walls;
        }

        public static List<TopologyNode?[,]> ExtractXzGroups(this MeshTopology topology)
        {
            var walls = new List<TopologyNode?[,]>();
            var groups = topology.ExtractFacesGroups(n => n.Face.GetFaceOrientation() == FaceOrientation.XZ);

            foreach (var group in groups)
            {
                var (Min, Max) = GetMinMaxXz(group);
                var width = (int)MathF.Round((Max.X - Min.X) / EdgeLength);
                var height = (int)MathF.Round((Max.Z - Min.Z) / EdgeLength);
                var wall = new TopologyNode?[width, height];

                var notNullNode = group.First(n => n != null)!;
                bool isMirror = notNullNode.Face.GetNormal().Y > 0;

                foreach (var node in group)
                {
                    var i = (int)MathF.Round((node.Face.Min(v => v.Position.X) - Min.X) / EdgeLength);
                    var j = (int)MathF.Round(height - (node.Face.Min(v => v.Position.Z) - Min.Z) / EdgeLength - 1);
                    i = isMirror ? width - i - 1 : i;
                    wall[i, j] = node;
                }

                walls.Add(wall);
            }

            return walls;
        }

        public static List<TopologyNode?[,]> ExtractYzGroups(this MeshTopology topology)
        {
            var walls = new List<TopologyNode?[,]>();
            var groups = topology.ExtractFacesGroups(n => n.Face.GetFaceOrientation() == FaceOrientation.YZ);

            foreach (var group in groups)
            {
                var (Min, Max) = GetMinMaxYz(group);
                var width = (int)MathF.Round((Max.Z - Min.Z) / EdgeLength);
                var height = (int)MathF.Round((Max.Y - Min.Y) / EdgeLength);
                var wall = new TopologyNode?[width, height];

                var notNullNode = group.First(n => n != null)!;
                bool isMirror = notNullNode.Face.GetNormal().X > 0;

                foreach (var node in group)
                {
                    var i = (int)MathF.Round((node.Face.Min(v => v.Position.Z) - Min.Z) / EdgeLength);
                    var j = (int)MathF.Round(height - (node.Face.Min(v => v.Position.Y) - Min.Y) / EdgeLength - 1);
                    i = isMirror ? width - i - 1 : i;
                    wall[i, j] = node;
                }

                walls.Add(wall);
            }

            return walls;
        }

        public static List<List<TopologyNode>> ExtractFacesGroups(this MeshTopology topology, Func<TopologyNode, bool> filter)
        {
            var groups = new List<List<TopologyNode>>();
            var faces = new HashSet<TopologyNode>(topology.Where(filter));

            while (faces.Any())
            {
                var initial = faces.First();
                var group =  ExtractGroup(initial, filter);
                faces.ExceptWith(group);
                groups.Add(group);
            }

            return groups;
        }

        public static List<List<TopologyNode>> ExtractFacesGroups(this MeshTopology topology, Func<TopologyNode, TopologyNode, bool> filter)
        {
            var groups = new List<List<TopologyNode>>();
            var faces = new HashSet<TopologyNode>(topology);

            while (faces.Any())
            {
                var initial = faces.First();
                var group = ExtractGroup(initial, filter);
                faces.ExceptWith(group);
                groups.Add(group);
            }

            return groups;
        }

        public static Vector3 GetNormal(this Face face)
        {
            var a = (face[1].Position - face[0].Position).Normalized();
            var b = (face[2].Position - face[0].Position).Normalized();
            return Vector3.Cross(a, b);
        }

        public static Mesh ToMesh(this IEnumerable<TopologyNode> nodes)
        {
            var verticesSet = new HashSet<Vertex>(new VertexComparer());

            foreach (var node in nodes)
            {
                var normal = node.Face.GetNormal().Normalized();

                foreach (var vertex in node.Face)
                {
                    verticesSet.Add(new Vertex(vertex.Position, normal, Vector2.Zero));
                }                
            }

            var vertices = verticesSet.ToList();
            var indices = new List<int>();

            foreach (var node in nodes)
            {
                foreach (var vertex in node.Face)
                {
                    int index = -1;

                    for(int i = 0; i < vertices.Count; i++)
                    {
                        if (vertices[i].Position.Equals(vertex.Position))
                        {
                            index = i;
                            break;
                        }
                    }

                    indices.Add(index);
                }                
            }

            return new Mesh(vertices, indices);
        }

        private static (Vector3 Min, Vector3 Max) GetMinMaxXy(List<TopologyNode> group)
        {
            var minX = group[0].Face[0].Position.X;
            var maxX = group[0].Face[0].Position.X;
            var minY = group[0].Face[0].Position.Y;
            var maxY = group[0].Face[0].Position.Y;

            foreach (var node in group)
            {
                foreach (var vertex in node.Face)
                {
                    minX = MathF.Min(vertex.Position.X, minX);
                    maxX = MathF.Max(vertex.Position.X, maxX);
                    minY = MathF.Min(vertex.Position.Y, minY);
                    maxY = MathF.Max(vertex.Position.Y, maxY);
                }
            }

            return (new Vector3(minX, minY, 0), new Vector3 (maxX, maxY, 0));
        }

        private static (Vector3 Min, Vector3 Max) GetMinMaxXz(List<TopologyNode> group)
        {
            var minX = group[0].Face[0].Position.X;
            var maxX = group[0].Face[0].Position.X;
            var minZ = group[0].Face[0].Position.Z;
            var maxZ = group[0].Face[0].Position.Z;

            foreach (var node in group)
            {
                foreach (var vertex in node.Face)
                {
                    minX = MathF.Min(vertex.Position.X, minX);
                    maxX = MathF.Max(vertex.Position.X, maxX);
                    minZ = MathF.Min(vertex.Position.Z, minZ);
                    maxZ = MathF.Max(vertex.Position.Z, maxZ);
                }
            }

            return (new Vector3(minX, 0, minZ), new Vector3(maxX, 0, maxZ));
        }

        private static (Vector3 Min, Vector3 Max) GetMinMaxYz(List<TopologyNode> group)
        {
            var minY = group[0].Face[0].Position.Y;
            var maxY = group[0].Face[0].Position.Y;
            var minZ = group[0].Face[0].Position.Z;
            var maxZ = group[0].Face[0].Position.Z;

            foreach (var node in group)
            {
                foreach (var vertex in node.Face)
                {
                    minY = MathF.Min(vertex.Position.Y, minY);
                    maxY = MathF.Max(vertex.Position.Y, maxY);
                    minZ = MathF.Min(vertex.Position.Z, minZ);
                    maxZ = MathF.Max(vertex.Position.Z, maxZ);
                }
            }

            return (new Vector3(0, minY, minZ), new Vector3(0, maxY, maxZ));
        }

        private static List<TopologyNode> ExtractGroup(
            TopologyNode initial, 
            Func<TopologyNode, bool> filter)
        {
            if (!filter(initial))
            {
                throw new ArgumentException();
            }

            var group = new HashSet<TopologyNode>();
            var visited = new HashSet<TopologyNode> { initial };
            var queue = new Queue<TopologyNode>();
            queue.Enqueue(initial);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                group.Add(node);

                foreach (var neighbour in node.Neighbours)
                {
                    if (!visited.Contains(neighbour) && filter(neighbour))
                    {
                        queue.Enqueue(neighbour);
                        visited.Add(neighbour);
                    }
                }
            }

            return group.ToList();
        }

        private static List<TopologyNode> ExtractGroup(
            TopologyNode initial,
            Func<TopologyNode, TopologyNode, bool> filter)
        {
            var group = new HashSet<TopologyNode>();
            var visited = new HashSet<TopologyNode> { initial };
            var queue = new Queue<TopologyNode>();
            queue.Enqueue(initial);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                group.Add(node);

                foreach (var neighbour in node.Neighbours)
                {
                    if (!visited.Contains(neighbour) && filter(node, neighbour))
                    {
                        queue.Enqueue(neighbour);
                        visited.Add(neighbour);
                    }
                }
            }

            return group.ToList();
        }
    }
}
