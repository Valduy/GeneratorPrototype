using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace TextureDemo
{
    class Program
    {
        public static void Main(string[] args)
        {
            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);
            var m = Model.Load("Content/Pear.obj");

            var pearGo = engine.CreateGameObject();
            var render = pearGo.Add<MeshRenderComponent>();
            render.Model = Model.Load("Content/Pear.obj");
            render.Texture = Texture.LoadFromFile("Content/Pear_Diffuse.jpg");
            pearGo.Position = new Vector3(0, 1, 0);

            engine.Run();
        }
    }
}