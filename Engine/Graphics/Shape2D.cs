using System.Collections;
using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Shape2D : IReadOnlyList<Vector2>
    {
        private readonly List<Vector2> _vertices;

        public int Count => _vertices.Count;

        // TODO: simple/complex polygon check
        public Shape2D(IEnumerable<Vector2> vertices)
        {
            _vertices = new List<Vector2>(vertices);

            if (Count > 3 && !Mathematics.Mathematics.IsCounterClockWise(_vertices))
            {
                throw new ArgumentException("Vertices should be counter clock wise.");
            }

            if (Count > 4 && Mathematics.Mathematics.IsContainsCollinearNeighboringEdges(_vertices))
            {
                throw new ArgumentException("Poly should not contain collinear neighboring edges.");
            }
        }

        public Vector2 this[int index] 
            => _vertices[index];

        public IEnumerator<Vector2> GetEnumerator() 
            => _vertices.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();

        public static Shape2D Line(Vector2 a, Vector2 b) 
            => new(new[] {a, b});

        public static Shape2D Triangle(float side)
        {
            var leg = side / 2;

            return new Shape2D(new[]
            {
                new Vector2(-leg, -leg),
                new Vector2(leg, -leg),
                new Vector2(0, leg),
            });
        }

        public static Shape2D Square(float side)
        {
            var half = side / 2;

            return new Shape2D(new[]
            {
                new Vector2(-half, -half),
                new Vector2(half, -half),
                new Vector2(half, half),
                new Vector2(-half, half),
            });
        }

        public static Shape2D Rectangle(float width, float height)
        {
            var halfWidth = width / 2;
            var halfHeight = height / 2;

            return new Shape2D(new[]
            {
                new Vector2(-halfWidth, -halfHeight),
                new Vector2(halfWidth, -halfHeight),
                new Vector2(halfWidth, halfHeight),
                new Vector2(-halfWidth, halfHeight),
            });
        }

        // TODO: circle, may be...
    }
}
