using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using OpenTK.Mathematics;

namespace SplineDemo
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

            var points = new List<float>();
            var last = new Vector3(0, 0, 0);

            foreach (var segment in SegmentsEnumerator())
            {
                CreateCube(engine, segment.p1, Vector3.Zero, 0.1f);
                last = segment.p2;
                int pointsPerSegment = 10;

                for (int i = 0; i <= pointsPerSegment; i++)
                {
                    var t = (float) i / pointsPerSegment;
                    var point = Curves.Hermite(segment.p1, segment.p2, segment.t1, segment.t2, t);
                    points.Add(point.X);
                    points.Add(point.Y);
                    points.Add(point.Z);
                }
            }

            CreateCube(engine, last, Vector3.Zero, 0.1f);

            var lineGo = engine.CreateGameObject();
            var render = lineGo.Add<ShapeRenderComponent>();
            render.IsLinear = true;
            render.Color = Colors.Green;
            render.Shape = new Shape(points);

            engine.Run();
        }

        public static GameObject CreateCube(Engine engine, Vector3 position, Vector3 rotation, float scale)
        {
            var go = engine.CreateGameObject();
            go.Position = position;
            go.Euler = rotation;
            go.Scale = new Vector3(scale);

            var render = go.Add<MeshRenderComponent>();
            render.Shape = Model.Cube.Meshes[0];

            return go;
        }

        static IEnumerable<(Vector3 p1, Vector3 p2, Vector3 t1, Vector3 t2)> SegmentsEnumerator()
        {
            yield return (
                new Vector3(0, 0, 0),
                new Vector3(1, 1, 0),
                new Vector3(1, 1, 0),
                new Vector3(1, 1, 0));
            yield return (
                new Vector3(1, 1, 0),
                new Vector3(1, 2, 0),
                new Vector3(1, 1, 0),
                new Vector3(0, 1, 0));
            yield return (
                new Vector3(1, 2, 0),
                new Vector3(2, 2, 0),
                new Vector3(0, 1, 0),
                new Vector3(1, 0, 0));
            yield return (
                new Vector3(2, 2, 0),
                new Vector3(2, 2, 1),
                new Vector3(1, 0, 0),
                new Vector3(0, 0, 1));
            yield return (
                new Vector3(2, 2, 1),
                new Vector3(2, 2, 2),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1));
        }
    }
}