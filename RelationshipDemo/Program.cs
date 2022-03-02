using GameEngine.Components;
using GameEngine.Core;
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
            using var engine = new Engine();
            engine.Camera.Projection = Projection.Orthographic;
            float distanceFromCamera = -10.0f;

            var centerGo = engine.CreateGameObject();
            var centerRender = centerGo.Add<ShapeRenderComponent>();
            centerRender.Color = Colors.Lime;
            centerRender.Shape = Shape.Square(10);
            centerGo.Position = Vector3.UnitZ * distanceFromCamera;

            var axisGo = engine.CreateGameObject();
            var axisRender = axisGo.Add<ShapeRenderComponent>();
            axisRender.Color = Colors.Red;
            axisRender.Shape = Shape.Line(new Vector2(0, 0), new Vector2(200, 0));
            axisGo.Add<RotationComponent>();
            axisGo.Position = new Vector3(50.0f, 50.0f, distanceFromCamera);

            var squareGo = engine.CreateGameObject();
            var squareRender = squareGo.Add<ShapeRenderComponent>();
            squareRender.Color = Colors.Magenta;
            squareRender.Shape = Shape.Square(20);
            squareGo.Add<ZigZagComponent>();
            squareGo.Position = Vector3.UnitZ * distanceFromCamera;

            axisGo.AddChild(squareGo);

            engine.Run();
        }
    }
}
