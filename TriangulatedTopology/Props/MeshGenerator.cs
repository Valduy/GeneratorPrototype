using GameEngine.Graphics;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
