using OpenTK.Graphics.OpenGL4;

namespace GameEngine.Graphics
{
    public static class Layouts
    {
        public static MeshBuffers DescribeStaticMeshLayout(Shader shader, Mesh mesh)
        {
            float[] vertices = GetVertices(mesh);
            int[] indices = mesh.Indices.ToArray();
            var meshBuffers = new MeshBuffers();

            meshBuffers.VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(meshBuffers.VertexArrayObject);

            meshBuffers.VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, meshBuffers.VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            meshBuffers.VertexElementObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, meshBuffers.VertexElementObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            var positionLocation = shader.GetAttribLocation("vertexPosition");
            GL.EnableVertexAttribArray(positionLocation);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            var normalLocation = shader.GetAttribLocation("vertexNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            var textureLocation = shader.GetAttribLocation("vertexTextureCoord");
            GL.EnableVertexAttribArray(textureLocation);
            GL.VertexAttribPointer(textureLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            return meshBuffers;
        }

        private static float[] GetVertices(Mesh mesh)
        {
            var result = new List<float>();

            foreach (var vertex in mesh.Vertices)
            {
                result.Add(vertex.Position.X);
                result.Add(vertex.Position.Y);
                result.Add(vertex.Position.Z);
                result.Add(vertex.Normal.X);
                result.Add(vertex.Normal.Y);
                result.Add(vertex.Normal.Z);
                result.Add(vertex.TextureCoords.X);
                result.Add(vertex.TextureCoords.Y);
            }

            return result.ToArray();
        }
    }
}
