using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace GameEngine.Components
{
    public class SolidRenderComponent : MeshRenderComponent
    {
        private const string FragmentShaderPath = "Shaders/Solid.frag";

        public Vector3 Color = Colors.Gray;

        public SolidRenderComponent() 
            : base(FragmentShaderPath)
        {}

        protected override void SetupFragmentShader()
        {
            Shader.SetVector3("color", Color);
        }
    }
}
