using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace TriangulatedTopology.Props
{
    public static class MeshGenerator
    {
        public static Model GenerateTubeFromSpline(List<SplineVertex> spline, int resolution, float radius)
        {
            var circles = new List<List<Vertex>>();
            var meshes = new List<Mesh>();

            var first = spline[0];
            circles.Add(GenerateCircle(first, resolution, radius));

            for (int i = 1; i < spline.Count; i++)
            {
                var temp = spline[i];
                circles.Add(GenerateCircle(temp, resolution, radius));

                var previous = circles[i - 1];
                var current = circles[i];

                var vertices = new List<Vertex>();
                var indices = new List<int>();

                vertices.AddRange(previous);
                vertices.AddRange(current);

                for (int j = 1; j < resolution; j++)
                {
                    indices.Add(resolution + j - 1);
                    indices.Add(j - 1);
                    indices.Add(j);
                    indices.Add(j);
                    indices.Add(resolution + j);
                    indices.Add(resolution + j - 1);
                }

                indices.Add(resolution + resolution - 1);
                indices.Add(resolution - 1);
                indices.Add(0);
                indices.Add(0);
                indices.Add(resolution);
                indices.Add(resolution + resolution - 1);

                var mesh = new Mesh(vertices, indices);
                meshes.Add(mesh);
            }

            return new Model(meshes);
        }

        public static Model GenerateTubeFromSpline(List<SplineVertex> spline, float side)
        {
            var squares = new List<List<List<Vertex>>>();
            var meshes = new List<Mesh>();

            var first = spline[0];
            squares.Add(GenerateSquare(first, side));

            for (int i = 1; i < spline.Count; i++)
            {
                var temp = spline[i];
                squares.Add(GenerateSquare(temp, side));

                var previous = squares[i - 1];
                var current = squares[i];

                var vertices = new List<Vertex>();
                var indices = new List<int>();

                for (int j = 0; j < 4; j++)
                {
                    var edge1 = previous[j];
                    var edge2 = current[j];

                    vertices.Add(edge1[0]);
                    vertices.Add(edge2[0]);
                    vertices.Add(edge2[1]);
                    vertices.Add(edge1[1]);

                    indices.Add(4 * j + 1);
                    indices.Add(4 * j);
                    indices.Add(4 * j + 3);
                    indices.Add(4 * j + 3);
                    indices.Add(4 * j + 2);
                    indices.Add(4 * j + 1);
                }

                var mesh = new Mesh(vertices, indices);
                meshes.Add(mesh);
            }

            return new Model(meshes);
        }

        private static List<Vertex> GenerateCircle(SplineVertex splineVertex, int resolution, float radius)
        {
            var circle = new List<Vertex>();
            var initial = splineVertex.Up;
            var sector = MathHelper.DegreesToRadians(360.0f / resolution);

            for (int i = 0; i < resolution; i++)
            {
                var rotator = Matrix4.CreateFromAxisAngle(splineVertex.Forward, sector * i);
                var normal = Vector3.TransformVector(initial, rotator);
                var point = splineVertex.Position + radius * normal;
                circle.Add(new Vertex(point, normal, Vector2.Zero));
            }

            return circle;
        }

        private static List<List<Vertex>> GenerateSquare(SplineVertex splineVertex, float side)
        {
            var half = side / 2;
            var square = new List<List<Vertex>>();
            var right = Vector3.Normalize(Vector3.Cross(splineVertex.Up, splineVertex.Forward));

            var a = splineVertex.Position + half * splineVertex.Up - half * right;
            var b = splineVertex.Position + half * splineVertex.Up + half * right;
            var c = splineVertex.Position - half * splineVertex.Up + half * right;
            var d = splineVertex.Position - half * splineVertex.Up - half * right;

            square.Add(new List<Vertex>
            {
                new Vertex(a, splineVertex.Up, Vector2.Zero),
                new Vertex(b, splineVertex.Up, Vector2.Zero),
            });
            square.Add(new List<Vertex>
            {
                new Vertex(b, right, Vector2.Zero),
                new Vertex(c, right, Vector2.Zero),
            });
            square.Add(new List<Vertex>
            {
                new Vertex(c, -splineVertex.Up, Vector2.Zero),
                new Vertex(d, -splineVertex.Up, Vector2.Zero),
            });
            square.Add(new List<Vertex>
            {
                new Vertex(d, -right, Vector2.Zero),
                new Vertex(a, -right, Vector2.Zero),
            });

            return square;
        }
    }
}
