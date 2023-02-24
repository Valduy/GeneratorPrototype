using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace GameEngine.Components
{
    public abstract class MeshRenderComponent : Component
    {
        public readonly Shader Shader;

        private readonly List<MeshBuffers> _modelBuffers = new();

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

        public MeshRenderComponent(string vertexShaderPath, string fragmentShaderPath)
        {
            Shader = ShaderManager.GetShader(vertexShaderPath, fragmentShaderPath);
            Register();
        }

        public override void RenderUpdate(FrameEventArgs args)
        {
            Setup();
            Render();            
        }

        protected abstract MeshBuffers DescribeLayout(Mesh mesh);
        protected abstract void SetupShader();

        private void Setup()
        {
            Shader.Use();
            SetupShader();
        }

        private void Register()
        {
            _modelBuffers.Clear();

            foreach (var mesh in _model.Meshes)
            {
                _modelBuffers.Add(DescribeLayout(mesh));
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
                GL.DrawElements(BeginMode.Triangles, _model.Meshes[i].Indices.Count, DrawElementsType.UnsignedInt, 0);
                GL.BindVertexArray(0);
            }
        }
    }
}
