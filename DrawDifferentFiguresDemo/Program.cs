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
            var lineRender = lineGo.Add<ShapeRenderComponent>();
            lineRender.Color = Colors.Yellow;
            lineRender.Shape = Shape.Line(new Vector2(-50, 0), new Vector2(50, 0));
            lineGo.Position = new Vector3(leftTopCornerX + 100, leftTopCornerY - 100, distanceFromCamera);

            var triangleGo = engine.CreateGameObject();
            var triangleRender = triangleGo.Add<ShapeRenderComponent>();
            triangleRender.Color = Colors.Red;
            triangleRender.Shape = Shape.Triangle(100);
            triangleGo.Position = new Vector3(leftTopCornerX + 250, leftTopCornerY - 100, distanceFromCamera);

            var squareGo = engine.CreateGameObject();
            var squareRender = squareGo.Add<ShapeRenderComponent>();
            squareRender.Color = Colors.Lime;
            squareRender.Shape = Shape.Square(100);
            squareGo.Position = new Vector3(leftTopCornerX + 400, leftTopCornerY - 100, distanceFromCamera);

            var convexGo = engine.CreateGameObject();
            var convexRender = convexGo.Add<ShapeRenderComponent>();
            convexRender.Color = Colors.Blue;
            convexRender.Shape = new Shape(Shape.GetVertices(new List<Vector2>
            {
                new(-25, -50),
                new(25, -50),
                new(50, -25),
                new(50, 25),
                new(25, 50),
                new(-25, 50),
                new(-50, 25),
                new(-50, -25),
            }));
            convexGo.Position = new Vector3(leftTopCornerX + 550, leftTopCornerY - 100, distanceFromCamera);

            var nonConvexGo = engine.CreateGameObject();
            var nonConvexRender = nonConvexGo.Add<ShapeRenderComponent>();
            nonConvexRender.Color = Colors.Cyan;
            nonConvexRender.Shape = new Shape(Shape.GetVertices(new List<Vector2>
            {
                new(-25, -50),
                new(0, -50),
                new(0, 0),
                new(50, 0),
                new(50, 25),
                new(25, 50),
                new(-25, 50),
                new(-50, 25),
                new(-50, -25),
            }));
            nonConvexGo.Position = new Vector3(leftTopCornerX + 700, leftTopCornerY - 100, distanceFromCamera);

            var complexGo = engine.CreateGameObject();
            var complexRender = complexGo.Add<ShapeRenderComponent>();
            complexRender.Color = Colors.Green;
            complexRender.Shape = new Shape(Shape.GetVertices(new List<Vector2>
            {
                new(-20, 10),
                new(-60, 0),
                new(0, -50),
                new(30, 30),
                new(50, -60),
                new(70, 0),
                new(20, 50),
                new(0, 20),
                new(-40, 60),
            }));
            complexGo.Position = Vector3.UnitZ * distanceFromCamera;

            engine.Run();
        }
    }
}
