﻿using GameEngine.Graphics;
using GameEngine.Helpers;
using OpenTK.Mathematics;

namespace GameEngine.Mathematics
{
    public static class Math
    {
        // NOTE: I use words "shape" and "polygon" as synonyms.

        /// <summary>
        /// Method triangulate <see cref="Shape"/>.
        /// Use "Ear clipping" method (https://en.wikipedia.org/wiki/Polygon_triangulation).
        /// </summary>
        /// <param name="shape"><see cref="Shape"/> for triangulation.</param>
        /// <returns>Indices, which defines triangles vertexes in original shape vertexes.</returns>
        /// <exception cref="ArgumentException">Trow, if shape has <= then three vertexes.</exception>
        public static int[] Triangulate(Shape shape)
        {
            if (shape.Count < 3)
            {
                throw new ArgumentException("Should have more then 3 vertexes.");
            }

            var indexes = Enumerable.Range(0, shape.Count).ToList();
            int trianglesCount = indexes.Count - 2;
            var triangles = new int[trianglesCount * 3];
            int currentTriangleIndex = 0;

            while (indexes.Count > 3)
            {
                for (int i = 0; i < indexes.Count; i++)
                {
                    int a = indexes[i];
                    int b = indexes.GetCircular(i - 1);
                    int c = indexes.GetCircular(i + 1);

                    Vector2 va = shape[a];
                    Vector2 vb = shape[b];
                    Vector2 vc = shape[c];

                    Vector2 vectorVaVb = vb - va;
                    Vector2 vectorVaVc = vc - va;

                    // Is vertex convex?
                    if (Cross(vectorVaVb, vectorVaVc) > 0.0f)
                    {
                        continue;
                    }

                    bool isEar = true;

                    // Does potential ear contain any poly vertex?
                    for (int j = 0; j < shape.Count; j++)
                    {
                        if (j == a || j == b || j == c)
                        {
                            continue;
                        }

                        Vector2 p = shape[j];
                        
                        if (IsPointInTriangle(p, vb, va, vc))
                        {
                            isEar = false;
                            break;
                        }
                    }

                    if (isEar)
                    {
                        triangles[currentTriangleIndex++] = b;
                        triangles[currentTriangleIndex++] = a;
                        triangles[currentTriangleIndex++] = c;

                        indexes.RemoveAt(i);
                        break;
                    }
                }
            }

            triangles[currentTriangleIndex++] = indexes[0];
            triangles[currentTriangleIndex++] = indexes[1];
            triangles[currentTriangleIndex] = indexes[2];
            return triangles;
        }

        /// <summary>
        /// Method check, is shape has collinear neighboring edges.
        /// This edges can be eliminated without effecting to original shape.
        /// You should avoid collinear neighboring edges, because they can break some algorithms...
        /// </summary>
        /// <param name="vertices">Vertexes.</param>
        /// <returns>True, if vertexes contains neighboring edges and false in other case.</returns>
        public static bool IsContainsCollinearNeighboringEdges(IList<Vector2> vertices)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 va = vertices[i];
                Vector2 vb = vertices.GetCircular(i - 1);
                Vector2 vc = vertices.GetCircular(i + 1);

                Vector2 vectorVaVb = vb - va;
                Vector2 vectorVaVc = vc - va;

                if (Cross(vectorVaVb, vectorVaVc) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Method calculate area of simple polygon.
        /// </summary>
        /// <param name="vertices">Vertexes of polygon.</param>
        /// <returns>Area of polygon.</returns>
        public static float Area(IReadOnlyList<Vector2> vertices)
        {
            float area = 0;

            for (int i = 0; i < vertices.Count; i++)
            {
                int j = (i + 1) % vertices.Count;
                area += vertices[i].X * vertices[j].Y;
                area -= vertices[j].X * vertices[i].Y;
            }

            return area;
        }

        /// <summary>
        /// Check, is simple polygon is counter clock wise oriented.
        /// Note, that you should not use this algorithm for complex polygons.
        /// </summary>
        /// <param name="vertices">Vertexes of polygon.</param>
        /// <returns>True, if polygon counter clock wise oriented and false in other case.</returns>
        public static bool IsCounterClockWise(IReadOnlyList<Vector2> vertices) 
            => Area(vertices) > 0;

        /// <summary>
        /// Method check, is triangle <see cref="a"/>, <see cref="b"/>, <see cref="c"/> contains point <see cref="p"/>.
        /// </summary>
        /// <param name="p">Point.</param>
        /// <param name="a">Vertex a.</param>
        /// <param name="b">Vertex b.</param>
        /// <param name="c">Vertex c.</param>
        /// <returns>True if contains, false in other case.</returns>
        public static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var ab = b - a;
            var bc = c - b;
            var ca = a - c;

            var ap = p - a;
            var bp = p - b;
            var cp = p - c;

            if (Cross(ab, ap) < 0 || Cross(bc, bp) < 0 || Cross(ca, cp) < 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculate cross production for 2d vectors.
        /// </summary>
        /// <param name="a">Vector a.</param>
        /// <param name="b">Vector b.</param>
        /// <returns>Component z of cross production.</returns>
        public static float Cross(Vector2 a, Vector2 b) 
            => a.X * b.Y - a.Y * b.X;
        
        public static bool Equal(Vector2 a, Vector2 b, float epsilon) 
            => Equal(a.X, b.X, epsilon) && Equal(a.Y, b.Y, epsilon);

        public static bool Equal(float a, float b, float epsilon)
        {
            const float floatNormal = (1 << 23) * float.Epsilon;
            float absA = System.Math.Abs(a);
            float absB = System.Math.Abs(b);
            float diff = System.Math.Abs(a - b);

            if (a == b)
            {
                // Shortcut, handles infinities
                return true;
            }

            if (a == 0.0f || b == 0.0f || diff < floatNormal)
            {
                // a or b is zero, or both are extremely close to it.
                // relative error is less meaningful here
                return diff < (epsilon * floatNormal);
            }

            // use relative error
            return diff / System.Math.Min((absA + absB), float.MaxValue) < epsilon;
        }
    }
}
