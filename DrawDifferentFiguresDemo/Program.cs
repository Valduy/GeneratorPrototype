using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Game;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace DrawDifferentFiguresDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var game = new Game();
            game.Camera.Projection = Projection.Orthographic;
            float leftTopCornerX = -Game.WindowWidth / 2;
            float leftTopCornerY = Game.WindowHeight / 2;
            float distanceFromCamera = -10.0f;

            var lineGo = game.Engine.CreateGameObject();
            lineGo.Add(() => new Render2DComponent(game)
            {
                Color = Colors.Yellow,
                Shape = Shape2D.Line(new Vector2(-50, 0), new Vector2(50, 0))
            });
            lineGo.Position = new Vector3(leftTopCornerX + 100, leftTopCornerY - 100, distanceFromCamera);

            var triangleGo = game.Engine.CreateGameObject();
            triangleGo.Add(() => new Render2DComponent(game)
            {
                Color = Colors.Red,
                Shape = Shape2D.Triangle(100)
            });
            triangleGo.Position = new Vector3(leftTopCornerX + 250, leftTopCornerY - 100, distanceFromCamera);

            var squareGo = game.Engine.CreateGameObject();
            squareGo.Add(() => new Render2DComponent(game)
            {
                Color = Colors.Lime,
                Shape = Shape2D.Square(100)
            });
            squareGo.Position = new Vector3(leftTopCornerX + 400, leftTopCornerY - 100, distanceFromCamera);

            var convexGo = game.Engine.CreateGameObject();
            convexGo.Add(() => new Render2DComponent(game)
            {
                Color = Colors.Blue,
                Shape = new Shape2D(new[]
                {
                    new Vector2(-25, -50),
                    new Vector2(25, -50),
                    new Vector2(50, -25),
                    new Vector2(50, 25),
                    new Vector2(25, 50),
                    new Vector2(-25, 50),
                    new Vector2(-50, 25),
                    new Vector2(-50, -25),
                })
            });
            convexGo.Position = new Vector3(leftTopCornerX + 550, leftTopCornerY - 100, distanceFromCamera);

            var nonConvexGo = game.Engine.CreateGameObject();
            nonConvexGo.Add(() => new Render2DComponent(game)
            {
                Color = Colors.Cyan,
                Shape = new Shape2D(new[]
                {
                    new Vector2(-25, -50),
                    new Vector2(0, -50),
                    new Vector2(0, 0),
                    new Vector2(50, 0),
                    new Vector2(50, 25),
                    new Vector2(25, 50),
                    new Vector2(-25, 50),
                    new Vector2(-50, 25),
                    new Vector2(-50, -25),
                })
            });
            nonConvexGo.Position = new Vector3(leftTopCornerX + 700, leftTopCornerY - 100, distanceFromCamera);

            var complexGo = game.Engine.CreateGameObject();
            complexGo.Add(() => new Render2DComponent(game)
            {
                Color = Colors.Green,
                Shape = new Shape2D(new[]
                {
                    new Vector2(-20, 10),
                    new Vector2(-60, 0),
                    new Vector2(0, -50),
                    new Vector2(30, 30),
                    new Vector2(50, -60),
                    new Vector2(70, 0),
                    new Vector2(20, 50),
                    new Vector2(0, 20),
                    new Vector2(-40, 60),
                })
            });

            game.Run();
        }
    }
}
