using GameEngine.Graphics;

namespace GameEngine.Helpers
{
    public static class MeshHelper
    {
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
    }
}
