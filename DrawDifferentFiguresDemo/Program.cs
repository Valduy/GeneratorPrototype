using GameEngine.Components;
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
            float leftTopCornerX = -Game.WindowWidth / 2;
            float leftTopCornerY = Game.WindowHeight / 2;

            var lineGo = game.Engine.CreateGameObject();
            lineGo.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Yellow,
                Shape = Shape.Line(new Vector2(-50, 0), new Vector2(50, 0))
            });
            lineGo.Position = new Vector2(leftTopCornerX + 100, leftTopCornerY - 100);

            var triangleGo = game.Engine.CreateGameObject();
            triangleGo.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Red,
                Shape = Shape.Triangle(100)
            });
            triangleGo.Position = new Vector2(leftTopCornerX + 250, leftTopCornerY - 100);

            var squareGo = game.Engine.CreateGameObject();
            squareGo.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Lime,
                Shape = Shape.Square(100)
            });
            squareGo.Position = new Vector2(leftTopCornerX + 400, leftTopCornerY - 100);

            var convexGo = game.Engine.CreateGameObject();
            convexGo.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Blue,
                Shape = new Shape(new[]
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
            convexGo.Position = new Vector2(leftTopCornerX + 550, leftTopCornerY - 100);

            // TODO: Well may be later....
            //var nonConvexGo = game.Engine.CreateGameObject();
            //nonConvexGo.Add(() => new RenderComponent(game.Window.Renderer)
            //{
            //    Color = new Vector3(0, 1, 1),
            //    Shape = new Shape(new[]
            //    {
            //        new Vector2(-25, -50),
            //        new Vector2(0, -50),
            //        new Vector2(0, 0),
            //        new Vector2(50, 0),
            //        new Vector2(50, 25),
            //        new Vector2(25, 50),
            //        new Vector2(-25, 50),
            //        new Vector2(-50, 25),
            //        new Vector2(-50, -25),
            //    })
            //});
            //nonConvexGo.Position = new Vector2(leftTopCornerX + 550, leftTopCornerY - 100);

            game.Run();
        }
    }
}
