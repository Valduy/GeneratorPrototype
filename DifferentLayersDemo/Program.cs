using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace DifferentLayersDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var engine = new Engine();
            engine.Camera.Projection = Projection.Orthographic;

            CreateTriangle(engine, new Vector2(0), 0, 100, -10, Colors.Yellow);
            CreateTriangle(engine, new Vector2(0, -50), 180, 100, -9, Colors.Red);

            engine.Run();
        }

        public static GameObject CreateTriangle(Engine engine, Vector2 position, float rotation, float size, float distance, Vector3 color)
        {
            var go = engine.CreateGameObject();
            
            var render2d = go.Add<Render2DComponent>();
            render2d.Color = color;
            render2d.Shape = Shape.Triangle(size);

            go.Position = new Vector3(position.X, position.Y, distance);
            go.Rotation = new Vector3(0, 0, rotation);
            return go;
        }
    }
}

