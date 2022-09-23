using GameEngine.Graphics;
using GameEngine.Helpers;
using OpenTK.Mathematics;

namespace PatternDemo
{
    public enum MeshOrientation
    {
        XY,
        XZ,
        YZ,
    }

    public static class MeshHelper
    {
        public static Mesh SortUVs(this Mesh mesh)
        {
            int polyVerticesCount = 4;
            var vertices = new List<Vertex>();

            for (int i = 0; i < mesh.Indices.Count; i += polyVerticesCount)
            {
                var (positions, normals, uvs) = mesh.ExtractComponents(i, polyVerticesCount);
                var positionCentroid = positions.Aggregate((a, b) => a + b) / positions.Count;
                var uvsCentroid = uvs.Aggregate((a, b) => a + b) / uvs.Count;
                bool isHorisontal = IsHorisontal(positions);

                var a = (positions[1] - positions[0]).Normalized();
                var b = (positions[2] - positions[0]).Normalized();
                var c = Vector3.Cross(a, b);

                var sortedUvs = new List<Vector2>();

                switch (GetPolyOrientation(positions))
                {
                    case MeshOrientation.XY:
                        foreach (var position in positions)
                        {
                            var uv = uvs.First(uv =>
                                position.X < positionCentroid.X == uv.X < uvsCentroid.X &&
                                position.Y > positionCentroid.Y == uv.Y < uvsCentroid.Y);

                            sortedUvs.Add(uv);
                        }
                        break;
                    case MeshOrientation.XZ:
                        foreach (var position in positions)
                        {
                            var uv = uvs.First(uv =>
                                position.X < positionCentroid.X == uv.X < uvsCentroid.X &&
                                position.Z < positionCentroid.Z == uv.Y < uvsCentroid.Y);

                            sortedUvs.Add(uv);
                        }
                        break;
                    case MeshOrientation.YZ:
                        foreach (var position in positions)
                        {
                            var uv = uvs.First(uv =>
                                position.Z < positionCentroid.Z == uv.X < uvsCentroid.X &&
                                position.Y > positionCentroid.Y == uv.Y < uvsCentroid.Y);

                            sortedUvs.Add(uv);
                        }
                        break;
                }

                var poly = positions.Zip(normals, sortedUvs, (p, n, uv) => new Vertex(p, n, uv));
                vertices.AddRange(poly);
            }

            return new Mesh(vertices, mesh.Indices);
        }

        public static Mesh SortVertices(this Mesh mesh)
        {
            var indices = new List<int>();

            for (int i = 0; i < mesh.Indices.Count; i += 4)
            {
                var (positions, normals, uvs) = mesh.ExtractComponents(i, 4);
                var polyIndices = new List<int> 
                {
                    mesh.Indices[i + 0],
                    mesh.Indices[i + 1],
                    mesh.Indices[i + 2],
                    mesh.Indices[i + 3],
                };                

                switch (GetPolyOrientation(positions))
                {
                    case MeshOrientation.XY:
                    case MeshOrientation.YZ:
                        {
                            var maxY = positions.Max(p => p.Y);
                        
                            while (mesh.Vertices[polyIndices[0]].Position.Y != maxY || 
                                    mesh.Vertices[polyIndices[1]].Position.Y != maxY)
                            {
                                polyIndices.ShiftRight();
                            }

                            break;
                        }
                    case MeshOrientation.XZ:
                        {
                            var maxZ = positions.Max(p => p.Z);

                            while (mesh.Vertices[polyIndices[0]].Position.Z != maxZ ||
                                   mesh.Vertices[polyIndices[1]].Position.Z != maxZ)
                            {
                                polyIndices.ShiftRight();
                            }

                            break;
                        }                                            
                }

                indices.AddRange(polyIndices);
            }

            return new Mesh(mesh.Vertices, indices);
        }   

        public static Mesh TriangulateQuadMesh(this Mesh mesh)
        {
            var indices = new List<int>();

            for (int i = 0; i < mesh.Indices.Count; i += 4)
            {
                indices.Add(mesh.Indices[i + 0]);
                indices.Add(mesh.Indices[i + 1]);
                indices.Add(mesh.Indices[i + 2]);

                indices.Add(mesh.Indices[i + 2]);
                indices.Add(mesh.Indices[i + 3]);
                indices.Add(mesh.Indices[i + 0]);
            }

            return new Mesh(mesh.Vertices, indices);
        }

        private static MeshOrientation GetPolyOrientation(IList<Vector3> positions)
        {
            if (positions.Count < 3)
            {
                throw new ArgumentException();
            }

            var a = (positions[1] - positions[0]).Normalized();
            var b = (positions[2] - positions[0]).Normalized();
            var c = Vector3.Cross(a, b);

            if (!MathHelper.ApproximatelyEqualEpsilon(c.Z, 0, float.Epsilon))
            {
                return MeshOrientation.XY;
            }
            if (!MathHelper.ApproximatelyEqualEpsilon(c.Y, 0, float.Epsilon))
            {
                return MeshOrientation.XZ;
            }

            return MeshOrientation.YZ;
        }

        private static (List<Vector3> Positions, List<Vector3> normals, List<Vector2> UVs) ExtractComponents(this Mesh mesh, int from, int count)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();

            for (int i = from; i < from + count; i++)
            {
                int index = mesh.Indices[i];
                var vertex = mesh.Vertices[index];
                positions.Add(vertex.Position);
                normals.Add(vertex.Normal);
                uvs.Add(vertex.TextureCoords);
            };

            return (positions, normals, uvs);
        }

        private static bool IsHorisontal(List<Vector3> positions)
        {
            var a = (positions[1] - positions[0]).Normalized();
            var b = (positions[2] - positions[0]).Normalized();
            var c = Vector3.Cross(a, b);
            return c.Y != 0;
        }

        private static bool IsVertical(List<Vector3> positions) 
            => !IsHorisontal(positions);

        private static bool IsSortedRightHorizontally(List<Vector3> positions, List<Vector2> uvs)
        {


            throw new NotImplementedException();
        }

        private static bool IsSortedRightVertically()
        {
            throw new NotImplementedException();
        }
    }
}
