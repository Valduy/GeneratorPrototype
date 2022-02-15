using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace GameEngine.Components
{
    public class Render3DComponent : Component
    {
        private static readonly Shader _shader = new("Shaders/shader3d.vert", "Shaders/shader3d.frag");

        private int _vertexArrayObject;
        private int _vertexBufferObject;
        private int _count;

        private Mesh _shape = new(Array.Empty<float>());

        public Mesh Shape
        {
            get => _shape;
            set
            {
                if (_shape == value) return;
                _shape = value;
                Unregister();
                Register(Shape.ToArray());
            }
        }

        public Material Material { get; set; } = new();

        public Render3DComponent()
        {
            Register(Shape.ToArray());
        }

        public override void RenderUpdate(FrameEventArgs args)
        {
            SetupShader();
            Render();
        }

        private void SetupShader()
        {
            _shader.Use();
            _shader.SetMatrix4("model", GetModelMatrix());
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

        private Matrix4 GetModelMatrix()
        {
            var model = Matrix4.Identity;
            model *= Matrix4.CreateScale(GameObject!.Scale.X, GameObject!.Scale.Y, GameObject.Scale.Z);
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

            var positionLocation = _shader.GetAttribLocation("aPos");
            GL.EnableVertexAttribArray(positionLocation);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);

            var normalLocation = _shader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));

            GL.EnableVertexAttribArray(0);
            _count = vertices.Length / 6;
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

            SetupShader();
            GL.DrawArrays(PrimitiveType.Triangles, 0, _count);

            GL.BindVertexArray(0);
        }
    }
}
