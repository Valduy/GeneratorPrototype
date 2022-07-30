using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace GameEngine.Utils
{
    public static class Debug
    {
        public static GameObject Grid(this Engine engine, int size)
        {
            var go = engine.CreateGameObject();
            var points = new List<Vector3>();

            for (int x = 0; x < size; x++)
            {
                points.Add(new Vector3(x - size / 2, 0, -size / 2                 ));
                points.Add(new Vector3(x - size / 2, 0,  size / 2 - (size + 1) % 2));
            }

            for (int z = 0; z < size; z++)
            {
                points.Add(new Vector3(-size / 2,                  0, z - size / 2));
                points.Add(new Vector3( size / 2 - (size + 1) % 2, 0, z - size / 2));
            }

            var render = go.Add<LineRenderComponent>();
            render.Color = Colors.White;
            render.IsDashed = true;
            render.Line = new Line(points);

            return go;
        }

        public static GameObject Axis(this Engine engine, float size = 1)
        {
            var go = engine.CreateGameObject();

            var x = engine.Axis(Vector3.UnitX * size, Colors.Red  );
            var y = engine.Axis(Vector3.UnitY * size, Colors.Green);
            var z = engine.Axis(Vector3.UnitZ * size, Colors.Blue );

            go.AddChild(x);
            go.AddChild(y);
            go.AddChild(z);

            return go;
        }

        private static GameObject Axis(this Engine engine, Vector3 direction, Vector3 color)
        {
            var axis = engine.CreateGameObject();
            var render = axis.Add<LineRenderComponent>();
            render.Color = color;
            render.Line = new Line(new List<Vector3> {Vector3.Zero, direction});
            return axis;
        }
    }
}
