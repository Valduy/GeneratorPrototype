using GameEngine.Game;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace GameEngine.Graphics
{
    public class Renderer
    {
        private readonly Window _window;
        private Shader _shader;

        public readonly Camera Camera = new();

        internal Renderer(Window window)
        {
            _window = window;
            _window.Resize += OnWindowResized;
            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
        }

        public RenderContext Register(float[] vertices)
        {
            var vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayObject);

            var vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            return new RenderContext(vertexArrayObject, vertexBufferObject, vertices.Length / 3);
        }
        
        public void Render(RenderContext context, Vector3 color, Vector2 scale, float rotation, Vector2 position, int layer)
        {
            GL.BindVertexArray(context.VertexArrayObject);
            SetupShader(color, scale, rotation, position, layer);
            Render(context);
            GL.BindVertexArray(0);
        }

        public void Unregister(RenderContext context)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            GL.DeleteBuffer(context.VertexBufferObject);
            GL.DeleteVertexArray(context.VertexArrayObject);
        }

        internal void Initialize()
        {
            GL.ClearColor(0, 0, 0, 1);
            GL.Enable(EnableCap.DepthTest);
        }

        internal void Terminate()
        {
            GL.UseProgram(0);
            GL.DeleteProgram(_shader.Handle);
        }

        internal void StartRender()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        internal void StopRender()
        {
            _window.SwapBuffers();
        }

        private void SetupShader(Vector3 color, Vector2 scale, float rotation, Vector2 position, int layer)
        {
            _shader.Use();
            _shader.SetMatrix4("model", GetModelMatrix(scale, rotation, position, layer));
            _shader.SetMatrix4("view", GetViewMatrix());
            _shader.SetMatrix4("projection", GetProjectionMatrix());
            _shader.SetVector3("color", color);
        }

        private Matrix4 GetModelMatrix(Vector2 scale, float rotation, Vector2 position, int layer)
        {
            var model = Matrix4.Identity;
            model *= Matrix4.CreateScale(scale.X, scale.Y, 1.0f);
            model *= Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotation));
            model *= Matrix4.CreateTranslation(position.X, position.Y, layer);
            return model;
        }

        private Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(new Vector3(Camera.Position.X, Camera.Position.Y, 0), new Vector3(Camera.Position.X, Camera.Position.Y, -1), Vector3.UnitY);
        }

        // TODO: Fix zoom!!!
        private Matrix4 GetProjectionMatrix()
        {
            var widht = _window.Size.X + Camera.Zoom;
            var height = _window.Size.Y + _window.Size.X / _window.Size.Y * Camera.Zoom;
            return Matrix4.CreateOrthographic(widht, height, 0.1f, 100.0f);
        }

        private void Render(RenderContext context)
        {
            switch (context.Count)
            {
                case 0:
                case 1:
                    return;
                case 2:
                    GL.DrawArrays(PrimitiveType.Lines, 0, context.Count);
                    break;
                default:
                    GL.DrawArrays(PrimitiveType.Triangles, 0, context.Count);
                    break;
            }
        }

        private void OnWindowResized(ResizeEventArgs args)
        {
            GL.Viewport(0, 0, _window.Size.X, _window.Size.Y);
        }
    }
}
