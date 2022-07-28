using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace GameEngine.Components
{
    public class MeshRenderComponent : Component
    {
        private static readonly Shader _shader = new("Shaders/shader3d.vert", "Shaders/shader3d.frag");

        private int _vertexArrayObject;
        private int _vertexBufferObject;
        private int _vertexElementObject;

        private Mesh _shape = new(Enumerable.Empty<Vertex>(), Enumerable.Empty<int>());

        public Mesh Shape
        {
            get => _shape;
            set
            {
                if (_shape == value) return;
                _shape = value;
                Unregister();
                Register(GetVertices(Shape), Shape.Indices.ToArray());
            }
        }

        public Material Material { get; set; } = new();

        public MeshRenderComponent()
        {
            Register(GetVertices(Shape), Shape.Indices.ToArray());
        }

        public override void RenderUpdate(FrameEventArgs args)
        {
            SetupShader();
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
            }

            return result.ToArray();
        }

        private void SetupShader()
        {
            _shader.Use();
            _shader.SetMatrix4("model", GameObject!.GetModelMatrix());
            _shader.SetMatrix4("view", GameObject!.Engine.Camera.GetViewMatrix());
            _shader.SetMatrix4("projection", GameObject!.Engine.Camera.GetProjectionMatrix());
            _shader.SetVector3("viewPos", GameObject!.Engine.Camera.Position);

            _shader.SetVector3("material.ambient", Material.Ambient);
            _shader.SetVector3("material.diffuse", Material.Diffuse);
            _shader.SetVector3("material.specular", Material.Specular);
            _shader.SetFloat("material.shininess", Material.Shininess);

            _shader.SetVector3("light.position", GameObject!.Engine.Light.Position);
            _shader.SetVector3("light.ambient", GameObject!.Engine.Light.Ambient);
            _shader.SetVector3("light.diffuse", GameObject!.Engine.Light.Diffuse);
            _shader.SetVector3("light.specular", GameObject!.Engine.Light.Specular);
        }

        private void Register(float[] vertices, int[] indices)
        {
            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            _vertexElementObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _vertexElementObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            var positionLocation = _shader.GetAttribLocation("aPos");
            GL.EnableVertexAttribArray(positionLocation);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);

            var normalLocation = _shader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        private void Unregister()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteBuffer(_vertexElementObject);
            GL.DeleteVertexArray(_vertexArrayObject);
        }

        private void Render()
        {
            GL.BindVertexArray(_vertexArrayObject);

            SetupShader();
            // TODO: ???
            //GL.DrawArrays(PrimitiveType.Triangles, 0, _count);
            GL.DrawElements(PrimitiveType.Triangles, Shape.Indices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }
    }
}
