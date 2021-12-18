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
            triangle1Go.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Yellow,
                Layer = -10, // Further from camera.
                Shape = new Shape(new[]
                {
                    new Vector2(-50, -50),
                    new Vector2(50, -50),
                    new Vector2(0, 50),
                })
            });

            var triangle2Go = game.Engine.CreateGameObject();
            triangle2Go.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Red,
                Layer = -9, // Closer to camera.
                Shape = new Shape(new[]
                {
                    new Vector2(-50, -50),
                    new Vector2(50, -50),
                    new Vector2(0, 50),
                })
            });
            triangle2Go.Rotation = 180;
            triangle2Go.Position = -Vector2.UnitY * 50;

            game.Run();
        }
    }
}

