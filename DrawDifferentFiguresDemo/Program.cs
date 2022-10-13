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
            var lineRender = lineGo.Add<LineRenderComponent>();
            lineRender.Color = Colors.Yellow;
            lineRender.Line = new Line(new List<Vector2>
            {
                new(-50.0f, 0.0f ), 
                new( 50.0f, 0.0f ), 
            });
            lineGo.Position = new Vector3(leftTopCornerX + 100, leftTopCornerY - 100, distanceFromCamera);

            var triangleGo = engine.CreateGameObject();
            var triangleRender = triangleGo.Add<SolidRenderComponent>();
            triangleRender.Color = Colors.Red;
            triangleRender.Model = Model.Triangle(100);
            triangleGo.Position = new Vector3(leftTopCornerX + 250, leftTopCornerY - 100, distanceFromCamera);

            var squareGo = engine.CreateGameObject();
            var squareRender = squareGo.Add<SolidRenderComponent>();
            squareRender.Color = Colors.Lime;
            squareRender.Model = Model.Square(100);
            squareGo.Position = new Vector3(leftTopCornerX + 400, leftTopCornerY - 100, distanceFromCamera);

            var convexGo = engine.CreateGameObject();
            var convexRender = convexGo.Add<SolidRenderComponent>();
            convexRender.Color = Colors.Blue;
            convexRender.Model = Model.FromPoly(new List<Vector2>
            {
                new(-25, -50),
                new( 25, -50),
                new( 50, -25),
                new( 50,  25),
                new( 25,  50),
                new(-25,  50),
                new(-50,  25),
                new(-50, -25),
            });
            convexGo.Position = new Vector3(leftTopCornerX + 550, leftTopCornerY - 100, distanceFromCamera);

            var nonConvexGo = engine.CreateGameObject();
            var nonConvexRender = nonConvexGo.Add<SolidRenderComponent>();
            nonConvexRender.Color = Colors.Cyan;
            nonConvexRender.Model = Model.FromPoly(new List<Vector2>
            {
                new(-25, -50),
                new( 0,  -50),
                new( 0,   0 ),
                new( 50,  0 ),
                new( 50,  25),
                new( 25,  50),
                new(-25,  50),
                new(-50,  25),
                new(-50, -25),
            });
            nonConvexGo.Position = new Vector3(leftTopCornerX + 700, leftTopCornerY - 100, distanceFromCamera);

            var complexGo = engine.CreateGameObject();
            var complexRender = complexGo.Add<SolidRenderComponent>();
            complexRender.Color = Colors.Green;
            complexRender.Model = Model.FromPoly(new List<Vector2>
            {
                new(-20,  10),
                new(-60,  0 ),
                new( 0,  -50),
                new( 30,  30),
                new( 50, -60),
                new( 70,  0 ),
                new( 20,  50),
                new( 0,   20),
                new(-40,  60),
            });
            complexGo.Position = Vector3.UnitZ * distanceFromCamera;

            engine.Run();
        }
    }
}
