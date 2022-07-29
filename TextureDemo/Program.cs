using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace TextureDemo
{
    class Program
    {
        public static byte[] GenerateRandomTexture(int size)
        {
            var result = new byte[4 * size * size];
            var random = new Random();

            for (int i = 0; i < result.Length; i++)
            {
                if (i % 3 == 0)
                {
                    result[i] = byte.MaxValue;
                }
                else
                {
                    result[i] = (byte)random.Next(0, byte.MaxValue);
                }
            }

            return result;
        }

        public static void Main(string[] args)
        {
            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var pearGo = engine.CreateGameObject();
            var pearRender = pearGo.Add<MeshRenderComponent>();
            pearRender.Model = Model.Load("Content/Pear.obj");
            //pearRender.Texture = Texture.LoadFromFile("Content/Pear_Diffuse.jpg");
            pearRender.Texture = Texture.LoadFromMemory(GenerateRandomTexture(128), 128, 128);
            pearGo.Position = new Vector3(5, 0, 0);

            var structureGo = engine.CreateGameObject();
            var structureRender = structureGo.Add<MeshRenderComponent>();
            structureRender.Model = Model.Load("Content/Structure.obj");
            structureRender.Texture = Texture.LoadFromFile("Content/Texture.png");
            //structureRender.Texture = Texture.LoadFromMemory(GenerateRandomTexture(128), 128, 128);
            structureGo.Position = new Vector3(-5, 1, 0);

            engine.Run();
        }
    }
}