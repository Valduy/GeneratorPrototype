using GameEngine.Components;
using GameEngine.Game;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace DifferentLayersDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var game = new Game();

            var triangle1Go = game.Engine.CreateGameObject();
            float distanceFromCamera1 = -10.0f;
            triangle1Go.Add(() => new Render2DComponent(game.Window.Renderer)
            {
                Color = Colors.Yellow,
                Shape = Shape2D.Triangle(100)
            });
            triangle1Go.Position = Vector3.UnitZ * distanceFromCamera1;

            var triangle2Go = game.Engine.CreateGameObject();
            float distanceFromCamera2 = -9.0f;
            triangle2Go.Add(() => new Render2DComponent(game.Window.Renderer)
            {
                Color = Colors.Red,
                Shape = Shape2D.Triangle(100)
            });
            triangle2Go.Rotation = new Vector3(0.0f, 0.0f, 180);
            triangle2Go.Position = new Vector3(0.0f, -50, distanceFromCamera2);

            game.Run();
        }
    }
}

