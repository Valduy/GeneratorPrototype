using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using TextureUnit = OpenTK.Graphics.OpenGL4.TextureUnit;

namespace GameEngine.Components
{
    public class MeshRenderComponent : Component
    {
        private struct MeshBuffers
        {
            public int VertexArrayObject { get; set; }
            public int VertexBufferObject { get; set; }
            public int VertexElementObject { get; set; }
        }

        private static readonly Shader Shader = new("Shaders/shader3d.vert", "Shaders/shader3d.frag");

        private List<MeshBuffers> _modelBuffers = new();

        private Model _model = Model.Empty;

        public Model Model
        {
            get => _model;
            set
            {
                if (_model == value) return;
                _model = value;
                Unregister();
                Register();
            }
        }

        public Texture Texture { get; set; } = Texture.Default;

        public Material Material { get; set; } = new();

        public MeshRenderComponent()
        {
            Register();
        }

        public override void RenderUpdate(FrameEventArgs args)
        {
            Setup();
            Render();
        }

        private float[] GetVertices(Mesh mesh)
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

        private void Setup()
        {
            Texture.Use(TextureUnit.Texture0);

            Shader.Use();
            Shader.SetMatrix4("transform.model", GameObject!.GetModelMatrix());
            Shader.SetMatrix4("transform.view", GameObject!.Engine.Camera.GetViewMatrix());
            Shader.SetMatrix4("transform.projection", GameObject!.Engine.Camera.GetProjectionMatrix());
            Shader.SetVector3("viewPosition", GameObject!.Engine.Camera.Position);

            Shader.SetVector3("material.color", Material.Color);
            Shader.SetFloat("material.ambient", Material.Ambient);
            Shader.SetFloat("material.shininess", Material.Shininess);
            Shader.SetFloat("material.specular", Material.Specular);
            
            Shader.SetVector3("light.position", GameObject!.Engine.Light.Position);
            Shader.SetVector3("light.color", GameObject!.Engine.Light.Color);
        }

        private void Register()
        {
            _modelBuffers.Clear();

            foreach (var mesh in _model.Meshes)
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

                var positionLocation = Shader.GetAttribLocation("vertexPosition");
                GL.EnableVertexAttribArray(positionLocation);
                GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

                var normalLocation = Shader.GetAttribLocation("vertexNormal");
                GL.EnableVertexAttribArray(normalLocation);
                GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

                var textureLocation = Shader.GetAttribLocation("vertexTextureCoord");
                GL.EnableVertexAttribArray(textureLocation);
                GL.VertexAttribPointer(textureLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

                GL.EnableVertexAttribArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);

                _modelBuffers.Add(meshBuffers);
            }
        }

        private void Unregister()
        {
            foreach (var meshBuffers in _modelBuffers)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);

                GL.DeleteBuffer(meshBuffers.VertexBufferObject);
                GL.DeleteBuffer(meshBuffers.VertexElementObject);
                GL.DeleteVertexArray(meshBuffers.VertexArrayObject);
            }

        }

        private void Render()
        {
            for (int i = 0; i < _modelBuffers.Count; i++)
            {
                GL.BindVertexArray(_modelBuffers[i].VertexArrayObject);
                Setup();
                GL.DrawElements(PrimitiveType.Triangles, _model.Meshes[i].Indices.Count, DrawElementsType.UnsignedInt, 0);
                GL.BindVertexArray(0);
            }
        }
    }
}
