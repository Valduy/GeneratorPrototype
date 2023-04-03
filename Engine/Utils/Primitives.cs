using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace GameEngine.Utils
{
    public static class Primitives
    {
        public static GameObject CreateCube(
            this Engine engine, 
            Vector3 position, 
            Quaternion rotation, 
            Vector3 scale)
        {
            return engine.CreateCube(position, rotation, scale, Colors.Gray);
        }

        public static GameObject CreateCube(
            this Engine engine,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Vector3 color)
        {
            return engine.CreateModel(Model.Cube, position, rotation, scale, color);
        }

        public static GameObject CreateSphere(
            this Engine engine, 
            Vector3 position, 
            Quaternion rotation, 
            Vector3 scale)
        {
            return engine.CreateSphere(position, rotation, scale, Colors.Gray);
        }

        public static GameObject CreateSphere(
            this Engine engine, 
            Vector3 position,
            Quaternion rotation, 
            Vector3 scale,
            Vector3 color)
        {
            return engine.CreateModel(Model.Sphere, position, rotation, scale, color);
        }

        public static GameObject CreateModel(
            this Engine engine,
            Model model,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            return engine.CreateModel(model, position, rotation, scale, Colors.Gray);
        }

        public static GameObject CreateModel(
            this Engine engine,
            Model model,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Vector3 color)
        {
            var sphere = engine.CreateGameObject();
            var render = sphere.Add<MaterialRenderComponent>();
            render.Model = model;
            render.Material.Color = color;
            sphere.Position = position;
            sphere.Rotation = rotation;
            sphere.Scale = scale;
            return sphere;
        }
    }
}
