using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace GameEngine.Components
{
    public class Render2DComponent : Component
    {
        private readonly Shader _shader = new("Shaders/shader2d.vert", "Shaders/shader2d.frag");
        
        private int _vertexArrayObject;
        private int _vertexBufferObject;
        private int _count;

        private Shape2D _shape = new(Enumerable.Empty<Vector2>());

        public readonly Game.Game Game;

        public Shape2D Shape
        {
            get => _shape;
            set
            {
                if (_shape == value) return;
                _shape = value;
                Unregister();
                Register(GetVertices());
            }
        }
        
        public Vector3 Color { get; set; } = Colors.Gray;

        public List<Vector2> Points
        {
            get
            {
                var model = GetModelMatrix();
                return Shape.Select(p => (new Vector4(p.X, p.Y, 0.0f , 1.0f) * model).Xy).ToList();
            }
        }

        public Render2DComponent(Game.Game game)
        {
            Game = game;
            Register(GetVertices());
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

        private float[] GetVertices()
        {
            if (Shape.Count > 3)
            {
                var triangles = Mathematics.Mathematics.Triangulate(Shape);
                var result = new float[triangles.Length * 9];

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var offset = i * 9;
                    WriteVector2ToArray(new ArraySegment<float>(result, offset, 3), Shape[triangles[i]]);
                    WriteVector2ToArray(new ArraySegment<float>(result, offset + 3, 3), Shape[triangles[i + 1]]);
                    WriteVector2ToArray(new ArraySegment<float>(result, offset + 6, 3), Shape[triangles[i + 2]]);
                }

                return result;
            }
            else
            {
                var result = new float[Shape.Count * 3];

                for (int i = 0; i < Shape.Count; i++)
                {
                    WriteVector2ToArray(new ArraySegment<float>(result, i * 3, 3), Shape[i]);
                }

                return result;
            }
        }

        private void WriteVector2ToArray(ArraySegment<float> segment, Vector2 vertex)
        {
            segment[0] = vertex.X;
            segment[1] = vertex.Y;
            segment[2] = 0;
        }

        private void SetupShader()
        {
            _shader.Use();
            _shader.SetMatrix4("model", GetModelMatrix());
            _shader.SetMatrix4("view", Game.Camera.GetViewMatrix());
            _shader.SetMatrix4("projection", Game.Camera.GetProjectionMatrix());
            _shader.SetVector3("color", Color);
        }

        private Matrix4 GetModelMatrix()
        {
            var model = Matrix4.Identity;
            model *= Matrix4.CreateScale(GameObject!.Scale.X, GameObject!.Scale.Y, 1.0f);
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

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            
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
                    GL.DrawArrays(PrimitiveType.Triangles, 0, _count);
                    break;
            }

            GL.BindVertexArray(0);
        }
    }
}
