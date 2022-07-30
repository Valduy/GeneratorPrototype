using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using OpenTK.Mathematics;

namespace Pipes.Utils
{
    public class FlexiblePipesBuilder
    {
        private static readonly Model RingModel = Model.Load("Content/Ring.obj");

        private Engine _engine;
        private Vector3? _prevPosition;
        private Vector3? _prevDirection;

        public FlexiblePipesBuilder(Engine engine)
        {
            _engine = engine;
        }

        public void Reset()
        {
            _prevPosition = null;
            _prevDirection = null;
        }

        public void CreatePipeSegment(Vector3 position)
        {
            var lineGo = _engine.CreateGameObject();
            var render = lineGo.Add<LineRenderComponent>();
            render.Color = Colors.Green;

            if (_prevPosition == null)
            {
                _prevPosition = position;
                _prevDirection = Vector3.Zero;
                return;
            }

            Vector3 prevPosition = _prevPosition!.Value;
            Vector3 prevDirection = _prevDirection!.Value;
            Vector3 currentDirection = position - prevPosition;

            var points = GetSegmentPoints(prevPosition, prevDirection, position, currentDirection);
            render.Line = new Line(points);
            SpawnRings(points);

            _prevPosition = position;
            _prevDirection = currentDirection;
        }

        private void SpawnRings(IList<Vector3> points)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                var ringGo = _engine.CreateGameObject();
                var render = ringGo.Add<MaterialRenderComponent>();
                render.Model = RingModel;
                render.Material.Color = new Vector3(1.0f, 0.5f, 0.31f);
                render.Material.Specular = 0.0f;
                render.Material.Shininess = 32.0f;
                ringGo.Position = points[i];

                var direction = new Vector3(points[i + 1] - points[i]).Normalized();

                // crutch for (0, -1, 0) case...
                if (direction == -Vector3i.UnitY)
                {
                    ringGo.Euler = new Vector3(180, 0, 0);
                }
                else
                {
                    var to = new Vector3(direction).Normalized();
                    ringGo.Rotation = Mathematics.GetRotation(Vector3.UnitY, to);
                }
            }
        }

        private List<Vector3> GetSegmentPoints(
            Vector3 prevPosition,
            Vector3 prevDirection,
            Vector3 currentPosition,
            Vector3 currentDirection)
        {
            var result = new List<Vector3>();
            int pointsPerSegment = 10;

            Vector3 p1 = prevPosition;
            Vector3 p2 = currentPosition;
            Vector3 t1 = prevDirection;
            Vector3 t2 = currentDirection;

            for (int i = 0; i <= pointsPerSegment; i++)
            {
                var t = (float)i / pointsPerSegment;
                result.Add(Curves.Hermite(p1, p2, t1, t2, t));
            }

            return result;
        }

        private float[] ToArray(IEnumerable<Vector3> points)
        {
            var result = new List<float>();

            foreach (var point in points)
            {
                result.Add(point.X);
                result.Add(point.Y);
                result.Add(point.Z);
            }

            return result.ToArray();
        }
    }
}
