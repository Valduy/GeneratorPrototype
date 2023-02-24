using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace GameEngine.Utils
{
    public static class Primitives
    {
        public static GameObject CreateCube(this Engine engine, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var cube = engine.CreateGameObject();
            var render = cube.Add<MaterialRenderComponent>();
            render.Model = Model.Cube;
            render.Material.Color = Colors.Gray;
            cube.Position = position;
            cube.Scale = scale;
            return cube;
        }

        public static GameObject CreateSphere(this Engine engine, Vector3 position, Vector3 scale)
        {
            var sphere = engine.CreateGameObject();
            var render = sphere.Add<MaterialRenderComponent>();
            render.Model = Model.Sphere;
            render.Material.Color = Colors.Gray;
            sphere.Position = position;
            sphere.Scale = scale;
            return sphere;
        }
    }
}
