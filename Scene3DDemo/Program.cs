using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
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
            var render = lineGo.Add<Render2DComponent>();
            render.IsLinear = true;
            render.Color = Colors.Green;
            render.Shape = new Shape(new float[]
            {
                0, 0, 0,
                5, 5, 5, 
                10, 5, 5,
                10, 5, 10,
                10, 10, 10,
            });

            engine.Run();
        }

        public static GameObject CreateCube(Engine engine, Vector3 position, Vector3 rotation, float scale)
        {
            var go = engine.CreateGameObject();
            go.Position = position;
            go.Rotation = rotation;
            go.Scale = new Vector3(scale);

            var render = go.Add<Render3DComponent>();
            render.Shape = Mesh.Cube;

            return go;
        }
    }
}