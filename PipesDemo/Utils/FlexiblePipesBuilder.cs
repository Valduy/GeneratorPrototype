using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using OpenTK.Mathematics;

namespace PipesDemo.Utils
{
    public class FlexiblePipesBuilder
    {
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
            var render = lineGo.Add<ShapeRenderComponent>();
            render.IsLinear = true;
            render.Color = Colors.Green;

            Vector3 prevPosition = _prevPosition ?? position;
            Vector3 prevDirection = _prevDirection ?? Vector3.Zero;
            Vector3 currentDirection = position - prevPosition;

            render.Shape = new Shape(GetSegmentPoints(
                prevPosition, prevDirection, position, currentDirection));

            _prevDirection = currentDirection;
            _prevPosition = position;
        }

        private float[] GetSegmentPoints(
            Vector3 prevPosition,
            Vector3 prevDirection,
            Vector3 currentPosition,
            Vector3 currentDirection)
        {
            var result = new List<float>();
            int pointsPerSegment = 10;

            Vector3 p1 = prevPosition;
            Vector3 p2 = currentPosition;
            Vector3 t1 = prevDirection;
            Vector3 t2 = currentDirection;

            for (int i = 0; i <= pointsPerSegment; i++)
            {
                var t = (float)i / pointsPerSegment;
                var point = Curves.Hermite(p1, p2, t1, t2, t);
                result.Add(point.X);
                result.Add(point.Y);
                result.Add(point.Z);
            }

            return result.ToArray();
        }
    }
}
