using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Game;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace RelationshipDemo
{
    public class RotationComponent : Component
    {
        public float RotationSpeed { get; set; } = 10;

        public override void GameUpdate(FrameEventArgs args)
        {
            GameObject!.Rotation += (float) (RotationSpeed * args.Time);
        }
    }

    public class ZigZagComponent : Component
    {
        private int _factor = 1;

        public float Speed { get; set; } = 30;

        public override void GameUpdate(FrameEventArgs args)
        {
            if (GameObject!.LocalPosition.X <= 0)
            {
                _factor = 1;
            }
            else if (GameObject.LocalPosition.X >= 200)
            {
                _factor = -1;
            }

            GameObject.LocalPosition = new Vector2(
                GameObject.LocalPosition.X + (float) (_factor * Speed * args.Time), 
                GameObject.LocalPosition.Y);
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            using var game = new Game();

            var centerGo = game.Engine.CreateGameObject();
            centerGo.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Lime,
                Shape = Shape.Square(10),
            });
            centerGo.Position = Vector2.Zero;

            var axisGo = game.Engine.CreateGameObject();
            axisGo.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Red,
                Shape = Shape.Line(new Vector2(0, 0), new Vector2(200, 0)),
            });
            axisGo.Add<RotationComponent>();
            axisGo.Position = new Vector2(50);

            var squareGo = game.Engine.CreateGameObject();
            squareGo.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Color = Colors.Magenta,
                Shape = Shape.Square(20),
            });
            squareGo.Add<ZigZagComponent>();
            squareGo.Position = Vector2.Zero;

            axisGo.AddChild(squareGo);

            game.Run();
        }
    }
}
