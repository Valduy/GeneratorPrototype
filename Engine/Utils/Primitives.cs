using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace GameEngine.Utils
{
    public static class Primitives
    {
        public static GameObject CreateCube(this Engine engine, Vector3 position) 
            => engine.CreateCube(position, new Vector3(1.0f));

        public static GameObject CreateCube(this Engine engine, Vector3 position, Vector3 scale)
        {
            var cube = engine.CreateGameObject();
            var render = cube.Add<MaterialRenderComponent>();
            render.Model = Model.Cube;
            render.Material.Color = Colors.Gray;
            cube.Position = position;
            cube.Scale = scale;
            return cube;
        }
    }
}
