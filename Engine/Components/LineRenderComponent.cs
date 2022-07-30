using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace GameEngine.Components
{
    public class LineRenderComponent : Component
    {
        private static readonly Shader _shader = new("Shaders/Shape.vert", "Shaders/Solid.frag");
        
        private int _vertexArrayObject;
        private int _vertexBufferObject;
        private int _count;

        private Line _line = new(Enumerable.Empty<Vector3>());

        public Line Line
        {
            get => _line;
            set
            {
                if (_line == value) return;
                _line = value;
                Unregister();
                Register();
            }
        }

        public float Width { get; set; } = 1;

        public bool IsDashed { get; set; } = false;

        public Vector3 Color { get; set; } = Colors.Gray;

        public LineRenderComponent()
        {
            Register();
        }

        public override void RenderUpdate(FrameEventArgs args)
        {
            GL.LineWidth(Width);
            SetupShader();
            Render();
        }

        public override void Stop()
        {
            GL.UseProgram(0);
            GL.DeleteProgram(_shader.Handle);
        }

        private float[] GetVertices(Line line)
        {
            var result = new List<float>();

            foreach (var vertex in line)
            {
                result.Add(vertex.X);
                result.Add(vertex.Y);
                result.Add(vertex.Z);
            }

            return result.ToArray();
        }

        private void SetupShader()
        {
            _shader.Use();
            _shader.SetMatrix4("model", GameObject!.GetModelMatrix());
            _shader.SetMatrix4("view", GameObject!.Engine.Camera.GetViewMatrix());
            _shader.SetMatrix4("projection", GameObject!.Engine.Camera.GetProjectionMatrix());
            _shader.SetVector3("color", Color);
        }

        private void Register()
        {
            float[] vertices = GetVertices(_line);

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            var location = _shader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(location);
            GL.VertexAttribPointer(location, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            GL.EnableVertexAttribArray(0);
            _count = vertices.Length / 3;
        }

        private void Unregister()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);
        }

        private void Render()
        {
            GL.BindVertexArray(_vertexArrayObject);
            GL.DrawArrays(IsDashed ? PrimitiveType.Lines : PrimitiveType.LineStrip, 0, _count);
            GL.BindVertexArray(0);
        }
    }
}
