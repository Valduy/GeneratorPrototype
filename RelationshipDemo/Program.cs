using GameEngine.Components;
using GameEngine.Core;
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
            float angle = (float)(RotationSpeed * args.Time);
            GameObject!.Euler += new Vector3(GameObject.Euler.X, GameObject.Euler.Y, angle);
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
            var centerRender = centerGo.Add<SolidRenderComponent>();
            centerRender.Color = Colors.Lime;
            centerRender.Model = Model.Square(10);
            centerGo.Position = Vector3.UnitZ * distanceFromCamera;

            var axisGo = engine.CreateGameObject();
            var axisRender = axisGo.Add<LineRenderComponent>();
            axisRender.Color = Colors.Red;
            axisRender.Line = new Line(new List<Vector2>
            {
                new(0,   0), 
                new(200, 0),
            });
            axisGo.Add<RotationComponent>();
            axisGo.Position = new Vector3(50.0f, 50.0f, distanceFromCamera);

            var squareGo = engine.CreateGameObject();
            var squareRender = squareGo.Add<SolidRenderComponent>();
            squareRender.Color = Colors.Magenta;
            squareRender.Model = Model.Square(20);
            squareGo.Add<ZigZagComponent>();
            squareGo.Position = Vector3.UnitZ * distanceFromCamera;

            axisGo.AddChild(squareGo);

            engine.Run();
        }
    }
}
