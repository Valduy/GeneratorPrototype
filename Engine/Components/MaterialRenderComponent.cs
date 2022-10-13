using GameEngine.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace GameEngine.Components
{
    public class MaterialRenderComponent : MeshRenderComponent
    {
        private const string FragmentShaderPath = "Shaders/Material.frag";

        public Texture Texture { get; set; } = Texture.Default;

        public Material Material { get; set; } = new();

        public MaterialRenderComponent() 
            : base(FragmentShaderPath)
        {}

        protected override void SetupFragmentShader()
        {
            Texture.Use(TextureUnit.Texture0);
            Shader.SetVector3("viewPosition", GameObject!.Engine.Camera.Position);

            Shader.SetVector3("material.color", Material.Color);
            Shader.SetFloat("material.ambient", Material.Ambient);
            Shader.SetFloat("material.shininess", Material.Shininess);
            Shader.SetFloat("material.specular", Material.Specular);

            Shader.SetVector3("light.position", GameObject!.Engine.Light.Position);
            Shader.SetVector3("light.color", GameObject!.Engine.Light.Color);
        }
    }
}
