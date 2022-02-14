using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace GameEngine.Components
{
    public class Render3DComponent : Component
    {
        private readonly Shader _shader = new("Shaders/shader3d.vert", "Shaders/shader3d.frag");

        private int _vertexArrayObject;
        private int _vertexBufferObject;
        private int _count;

        private Shape3D _shape = new(Enumerable.Empty<Vector3>(), Enumerable.Empty<Vector3>());

        public readonly Game.Game Game;

        public Shape3D Shape
        {
            get => _shape;
            set
            {
                if (_shape == value) return;
                _shape = value;
                Unregister();
                Register(EnumerateShape().ToArray());
            }
        }

        public Render3DComponent(Game.Game game)
        {
            Game = game;
            Register(EnumerateShape().ToArray());
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
            _shader.SetMatrix4("view", Game.Camera.GetViewMatrix());
            _shader.SetMatrix4("projection", Game.Camera.GetProjectionMatrix());
            _shader.SetVector3("viewPos", Game.Camera.Position);

            _shader.SetVector3("material.ambient", new Vector3(1.0f, 0.5f, 0.31f));
            _shader.SetVector3("material.diffuse", new Vector3(1.0f, 0.5f, 0.31f));
            _shader.SetVector3("material.specular", new Vector3(0.5f, 0.5f, 0.5f));
            _shader.SetFloat("material.shininess", 32.0f);

            _shader.SetVector3("light.position", new Vector3(0, 10, 0));
            _shader.SetVector3("light.ambient", new Vector3(0.2f));
            _shader.SetVector3("light.diffuse", new Vector3(0.5f));
            _shader.SetVector3("light.specular", new Vector3(1.0f, 1.0f, 1.0f));
        }

        private Matrix4 GetModelMatrix()
        {
            var model = Matrix4.Identity;
            model *= Matrix4.CreateScale(GameObject!.Scale.X, GameObject!.Scale.Y, GameObject.Scale.Z);
            model *= Matrix4.CreateFromQuaternion(Quaternion.FromEulerAngles(GameObject!.Rotation * MathHelper.Pi / 180));
            model *= Matrix4.CreateTranslation(GameObject.Position);
            return model;
        }

        private IEnumerable<float> EnumerateShape()
        {
            for (int i = 0; i < Shape.Count; i++)
            {
                var vertex = Shape.Vertices[i];
                yield return vertex.X;
                yield return vertex.Y;
                yield return vertex.Z;

                var normal = Shape.Normals[i];
                yield return normal.X;
                yield return normal.Y;
                yield return normal.Z;
            }
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

        public void Unregister()
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
