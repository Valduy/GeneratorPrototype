using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace DrawDifferentFiguresDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var engine = new Engine();
            engine.Camera.Projection = Projection.Orthographic;
            float leftTopCornerX = -Engine.WindowWidth / 2;
            float leftTopCornerY = Engine.WindowHeight / 2;
            float distanceFromCamera = -10.0f;

            var lineGo = engine.CreateGameObject();
            var lineRender = lineGo.Add<Render2DComponent>();
            lineRender.Color = Colors.Yellow;
            lineRender.Shape = Shape2D.Line(new Vector2(-50, 0), new Vector2(50, 0));
            lineGo.Position = new Vector3(leftTopCornerX + 100, leftTopCornerY - 100, distanceFromCamera);

            var triangleGo = engine.CreateGameObject();
            var triangleRender = triangleGo.Add<Render2DComponent>();
            triangleRender.Color = Colors.Red;
            triangleRender.Shape = Shape2D.Triangle(100);
            triangleGo.Position = new Vector3(leftTopCornerX + 250, leftTopCornerY - 100, distanceFromCamera);

            var squareGo = engine.CreateGameObject();
            var squareRender = squareGo.Add<Render2DComponent>();
            squareRender.Color = Colors.Lime;
            squareRender.Shape = Shape2D.Square(100);
            squareGo.Position = new Vector3(leftTopCornerX + 400, leftTopCornerY - 100, distanceFromCamera);

            var convexGo = engine.CreateGameObject();
            var convexRender = convexGo.Add<Render2DComponent>();
            convexRender.Color = Colors.Blue;
            convexRender.Shape = new Shape2D(new[]
            {
                new Vector2(-25, -50),
                new Vector2(25, -50),
                new Vector2(50, -25),
                new Vector2(50, 25),
                new Vector2(25, 50),
                new Vector2(-25, 50),
                new Vector2(-50, 25),
                new Vector2(-50, -25),
            });
            convexGo.Position = new Vector3(leftTopCornerX + 550, leftTopCornerY - 100, distanceFromCamera);

            var nonConvexGo = engine.CreateGameObject();
            var nonConvexRender = nonConvexGo.Add<Render2DComponent>();
            nonConvexRender.Color = Colors.Cyan;
            nonConvexRender.Shape = new Shape2D(new[]
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
            });
            nonConvexGo.Position = new Vector3(leftTopCornerX + 700, leftTopCornerY - 100, distanceFromCamera);

            var complexGo = engine.CreateGameObject();
            var complexRender = complexGo.Add<Render2DComponent>();
            complexRender.Color = Colors.Green;
            complexRender.Shape = new Shape2D(new[]
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
            });
            complexGo.Position = Vector3.UnitZ * distanceFromCamera;

            engine.Run();
        }
    }
}
