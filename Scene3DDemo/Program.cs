using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Utils;
using OpenTK.Mathematics;

namespace Scene3DDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var engine = new Engine();
  
            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 1, 1);

            CreateCube(engine, new Vector3(0), Vector3.UnitY * 45, 1);
            CreateCube(engine, new Vector3(2, 0, 0), Vector3.Zero, 1);
            CreateCube(engine, new Vector3(2, 0, 2), Vector3.Zero, 1);
            
            var lineGo = engine.CreateGameObject();
            var render = lineGo.Add<LineRenderComponent>();
            render.Color = Colors.Green;
            render.Line = new Line(new List<Vector3>
            {
                new(0,  0,  0 ),
                new(5,  5,  5 ), 
                new(10, 5,  5 ),
                new(10, 5,  10),
                new(10, 10, 10),
            });

            var gird = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11.0f, 0.0f, - 11.0f);
            engine.Run();
        }

        public static GameObject CreateCube(Engine engine, Vector3 position, Vector3 rotation, float scale)
        {
            var go = engine.CreateGameObject();
            go.Position = position;
            go.Euler = rotation;
            go.Scale = new Vector3(scale);

            var render = go.Add<MaterialRenderComponent>();
            render.Model = Model.Cube;

            return go;
        }
    }
}