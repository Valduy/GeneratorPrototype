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
        public float RotationSpeed { get; set; } = 1;

        public override void GameUpdate(FrameEventArgs args)
        {
            float angle = (float)(RotationSpeed * args.Time);
            GameObject!.Rotation += new Vector3(GameObject.Rotation.X, GameObject.Rotation.Y, angle);
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

            float offset = (float) (_factor * Speed * args.Time);

            GameObject.LocalPosition = new Vector3(
                GameObject.LocalPosition.X + offset, 
                GameObject.LocalPosition.Y,
                GameObject.LocalPosition.Z);
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            using var game = new Game();
            game.Window.Renderer.Camera.Projection = Projection.Orthographic;
            float distanceFromCamera = -10.0f;

            var centerGo = game.Engine.CreateGameObject();
            centerGo.Add(() => new Render2DComponent(game.Window.Renderer)
            {
                Color = Colors.Lime,
                Shape = Shape2D.Square(10),
            });
            centerGo.Position = Vector3.UnitZ * distanceFromCamera;

            var axisGo = game.Engine.CreateGameObject();
            axisGo.Add(() => new Render2DComponent(game.Window.Renderer)
            {
                Color = Colors.Red,
                Shape = Shape2D.Line(new Vector2(0, 0), new Vector2(200, 0)),
            });
            axisGo.Add<RotationComponent>();
            axisGo.Position = new Vector3(50.0f, 50.0f, distanceFromCamera);

            var squareGo = game.Engine.CreateGameObject();
            squareGo.Add(() => new Render2DComponent(game.Window.Renderer)
            {
                Color = Colors.Magenta,
                Shape = Shape2D.Square(20),
            });
            squareGo.Add<ZigZagComponent>();
            squareGo.Position = Vector3.UnitZ * distanceFromCamera;

            axisGo.AddChild(squareGo);

            game.Run();
        }
    }
}
