using System.Collections;
using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Shape : IReadOnlyList<Vector2>
    {
        private readonly List<Vector2> _vertices;

        public int Count => _vertices.Count;
        
        public Shape(IEnumerable<Vector2> vertices)
        {
            _vertices = new List<Vector2>(vertices);
        }

        public Vector2 this[int index] 
            => _vertices[index];

        public IEnumerator<Vector2> GetEnumerator() 
            => _vertices.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();

        public static Shape Line(Vector2 a, Vector2 b) 
            => new(new[] {a, b});

        public static Shape Triangle(float side)
        {
            var leg = side / 2;

            return new Shape(new[]
            {
                new Vector2(-leg, -leg),
                new Vector2(leg, -leg),
                new Vector2(0, leg),
            });
        }

        public static Shape Square(float side)
        {
            var half = side / 2;

            return new Shape(new[]
            {
                new Vector2(-half, -half),
                new Vector2(half, -half),
                new Vector2(half, half),
                new Vector2(-half, half),
            });
        }

        public static Shape Rectangle(float width, float height)
        {
            var halfWidth = width / 2;
            var halfHeight = height / 2;

            return new Shape(new[]
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
