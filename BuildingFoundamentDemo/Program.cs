﻿using GameEngine.Components;
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

            var squareGo = game.Engine.CreateGameObject();
            squareGo.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Lime,
                Shape = Shape.Square(100)
            });
            squareGo.Add(() => new BoundsComponent(new GameEngine.Bounds.RectangleBounds()
            {
                Size = new Vector2(100)
            }));
            squareGo.Position = new Vector2(leftTopCornerX + 400, leftTopCornerY - 100);

            var squareGo_1 = game.Engine.CreateGameObject();
            squareGo_1.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Magenta,
                Shape = Shape.Square(100)
            });
            squareGo_1.Add(() => new BoundsComponent(new GameEngine.Bounds.RectangleBounds()
            {
                Size = new Vector2(100)
            }));
            squareGo_1.Position = new Vector2(leftTopCornerX + 301, leftTopCornerY - 100);

            Console.WriteLine(GameEngine.Mathematics.Math.IsAABBIntersects(squareGo.Get<BoundsComponent>().Position, squareGo.Get<BoundsComponent>().Width, squareGo.Get<BoundsComponent>().Height, squareGo_1.Get<BoundsComponent>().Position, squareGo_1.Get<BoundsComponent>().Width, squareGo_1.Get<BoundsComponent>().Height));

            game.Run();
        }
    }
}   