using System.Collections;
using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Shape : IReadOnlyList<float>
    {
        private readonly List<float> _vertices;

        public int Count => _vertices.Count;

        public Shape(IEnumerable<float> vertices)
        {
            _vertices = new List<float>(vertices);
        }

        public float this[int index] 
            => _vertices[index];

        public IEnumerator<float> GetEnumerator() 
            => _vertices.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();

        public static Shape Line(Vector2 a, Vector2 b) 
            => new(GetVertices(new List<Vector2> { a, b }));

        public static Shape Triangle(float side)
        {
            var leg = side / 2;

            return new Shape(GetVertices(new List<Vector2>
            {
                new(-leg, -leg),
                new(leg, -leg),
                new(0, leg),
            }));
        }

        public static Shape Square(float side)
        {
            var half = side / 2;

            return new Shape(GetVertices(new List<Vector2>
            {
                new(-half, -half),
                new(half, -half),
                new(half, half),
                new(-half, half),
            }));
        }

        public static Shape Rectangle(float width, float height)
        {
            var halfWidth = width / 2;
            var halfHeight = height / 2;

            return new Shape(GetVertices(new List<Vector2>
            {
                new(-halfWidth, -halfHeight),
                new(halfWidth, -halfHeight),
                new(halfWidth, halfHeight),
                new(-halfWidth, halfHeight),
            }));
        }

        public static float[] GetVertices(IReadOnlyList<Vector2> points)
        {
            if (points.Count > 3)
            {
                var triangles = Mathematics.Mathematics.Triangulate(points);
                var result = new float[triangles.Length * 9];

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var offset = i * 9;
                    WriteVector2ToArray(new ArraySegment<float>(result, offset, 3), points[triangles[i]]);
                    WriteVector2ToArray(new ArraySegment<float>(result, offset + 3, 3), points[triangles[i + 1]]);
                    WriteVector2ToArray(new ArraySegment<float>(result, offset + 6, 3), points[triangles[i + 2]]);
                }

                return result;
            }
            else
            {
                var result = new float[points.Count * 3];

                for (int i = 0; i < points.Count; i++)
                {
                    WriteVector2ToArray(new ArraySegment<float>(result, i * 3, 3), points[i]);
                }

                return result;
            }
        }

        private static void WriteVector2ToArray(ArraySegment<float> segment, Vector2 vertex)
        {
            segment[0] = vertex.X;
            segment[1] = vertex.Y;
            segment[2] = 0;
        }
    }
}
