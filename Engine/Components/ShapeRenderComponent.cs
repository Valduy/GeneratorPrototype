using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace GameEngine.Components
{
    public class ShapeRenderComponent : Component
    {
        private static readonly Shader _shader = new("Shaders/shader2d.vert", "Shaders/shader2d.frag");
        
        private int _vertexArrayObject;
        private int _vertexBufferObject;
        private int _count;

        private Shape _shape = new(Enumerable.Empty<float>());

        public Shape Shape
        {
            get => _shape;
            set
            {
                if (_shape == value) return;
                _shape = value;
                Unregister();
                Register(_shape.ToArray());
            }
        }
        
        public Vector3 Color { get; set; } = Colors.Gray;

        public bool IsLinear { get; set; }

        public ShapeRenderComponent()
        {
            Register(Shape.ToArray());
        }

        public override void RenderUpdate(FrameEventArgs args)
        {
            SetupShader();
            Render();
        }

        public override void Stop()
        {
            GL.UseProgram(0);
            GL.DeleteProgram(_shader.Handle);
        }

        private void SetupShader()
        {
            _shader.Use();
            _shader.SetMatrix4("model", GetModelMatrix());
            _shader.SetMatrix4("view", GameObject!.Engine.Camera.GetViewMatrix());
            _shader.SetMatrix4("projection", GameObject!.Engine.Camera.GetProjectionMatrix());
            _shader.SetVector3("color", Color);
        }

        private Matrix4 GetModelMatrix()
        {
            var model = Matrix4.Identity;
            model *= Matrix4.CreateScale(GameObject!.Scale.X, GameObject!.Scale.Y, GameObject!.Scale.Z);
            model *= Matrix4.CreateFromQuaternion(Quaternion.FromEulerAngles(GameObject!.Rotation * MathHelper.Pi / 180));
            model *= Matrix4.CreateTranslation(GameObject.Position);
            return model;
        }

        private void Register(float[] vertices)
        {
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

            switch (_count)
            {
                case 0:
                case 1:
                    return;
                case 2:
                    GL.DrawArrays(PrimitiveType.Lines, 0, _count);
                    break;
                default:
                    GL.DrawArrays(IsLinear ? PrimitiveType.LineStrip : PrimitiveType.Triangles, 0, _count);
                    break;
            }

            GL.BindVertexArray(0);
        }
    }
}
