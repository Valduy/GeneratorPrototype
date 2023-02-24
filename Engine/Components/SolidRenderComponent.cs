using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using Assimp;
using Mesh = GameEngine.Graphics.Mesh;

namespace GameEngine.Components
{
    public class SolidRenderComponent : MeshRenderComponent
    {
        private const string VertexShaderPath = "Shaders/Mesh.vert";
        private const string FragmentShaderPath = "Shaders/Solid.frag";

        public Vector3 Color = Colors.Gray;

        public SolidRenderComponent() 
            : base(VertexShaderPath, FragmentShaderPath)
        {}

        protected override MeshBuffers DescribeLayout(Mesh mesh)
        {
            return Layouts.DescribeStaticMeshLayout(Shader, mesh);
        }

        protected override void SetupShader()
        {
            Shader.SetMatrix4("transform.model", GameObject!.GetModelMatrix());
            Shader.SetMatrix4("transform.view", GameObject!.Engine.Camera.GetViewMatrix());
            Shader.SetMatrix4("transform.projection", GameObject!.Engine.Camera.GetProjectionMatrix());

            Shader.SetVector3("color", Color);
        }
    }
}
