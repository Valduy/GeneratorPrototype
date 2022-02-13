using GameEngine.Game;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace GameEngine.Graphics
{
    public class Renderer
    {
        private readonly Window _window;
        public readonly Camera Camera;

        internal Renderer(Window window)
        {
            _window = window;
            Camera = new Camera(_window);
            _window.Resize += OnWindowResized;
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
        
        public void Render(RenderContext context)
        {
            GL.BindVertexArray(context.VertexArrayObject);

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
            
        }

        internal void StartRender()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        internal void StopRender()
        {
            _window.SwapBuffers();
        }

        private void OnWindowResized(ResizeEventArgs args)
        {
            GL.Viewport(0, 0, _window.Size.X, _window.Size.Y);
        }
    }
}
