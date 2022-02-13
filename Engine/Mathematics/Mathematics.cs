using GameEngine.Graphics;
using GameEngine.Helpers;
using OpenTK.Mathematics;

namespace GameEngine.Mathematics
{
    public static class Mathematics
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

                        if (IsPointInConvexPolygon(p, new []{vb, va, vc}))
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
        public static float Area(IList<Vector2> vertices)
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
        public static bool IsCounterClockWise(IList<Vector2> vertices) 
            => Area(vertices) > 0;

        /// <summary>
        /// Method check, is point is inside convex polygon.
        /// </summary>
        /// <param name="p">Point.</param>
        /// <param name="polygon">Polygon's points.</param>
        /// <returns>True if point inside polygon, false in other case.</returns>
        public static bool IsPointInConvexPolygon(Vector2 p, IList<Vector2> polygon)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                var edge = polygon.GetCircular(i + 1) - polygon[i];
                var toPoint = p - polygon[i];

                if (Cross(edge, toPoint) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Method check, is polygon inside other convex polygon.
        /// </summary>
        /// <param name="thisPolygon">Polygon that we check if it is inside another.</param>
        /// <param name="otherPolygon">Other convex polygon.</param>
        /// <returns>True, if polygon inside other convex polygon, else in other case.</returns>
        public static bool IsPolygonInsideConvexPolygon(IList<Vector2> thisPolygon, IList<Vector2> otherPolygon) 
            => thisPolygon.All(p => IsPointInConvexPolygon(p, otherPolygon));

        /// <summary>
        /// Method check, is convex polygons intersects.
        /// </summary>
        /// <param name="thisPolygon">First convex polygon.</param>
        /// <param name="otherPolygon">Second convex polygon.</param>
        /// <returns>True if intersect, false in other case.</returns>
        public static bool IsConvexPolygonsIntersects(IList<Vector2> thisPolygon, IList<Vector2> otherPolygon) 
            => thisPolygon.Any(p => IsPointInConvexPolygon(p, otherPolygon))
               || otherPolygon.Any(p => IsPointInConvexPolygon(p, thisPolygon));

        /// <summary>
        /// Calculate cross production for 2d vectors.
        /// </summary>
        /// <param name="a">Vector a.</param>
        /// <param name="b">Vector b.</param>
        /// <returns>Component z of cross production.</returns>
        public static float Cross(Vector2 a, Vector2 b) 
            => a.X * b.Y - a.Y * b.X;

        /// <summary>
        /// Calculate angle between 2 vectors.
        /// </summary>
        /// <param name="a">Vector a.</param>
        /// <param name="b">Vector b.</param>
        /// <returns>Angle between a and b in radians.</returns>
        public static float Angle(Vector2 a, Vector2 b)
        {
            return MathF.Acos(Math.Clamp(Vector2.Dot(a, b) / (a.Length * b.Length), -1, 1));
        }

        public static Vector2 Rotate(Vector2 vector, float angle)
        {
            var vector3d = new Vector3(vector.X, vector.Y, 1);
            vector3d *= Matrix3.CreateRotationZ(MathHelper.DegreesToRadians(angle));
            return new Vector2(vector3d.X, vector3d.Y);
        }

        public static bool Equal(Vector2 a, Vector2 b, float epsilon)
        {
            return MathHelper.ApproximatelyEqualEpsilon(a.X, b.X, epsilon) 
                   && MathHelper.ApproximatelyEqualEpsilon(a.Y, b.Y, epsilon);
        }

        public static bool IsBoundingBoxesIntersects(
            Vector2 position1, float width1, float height1,
            Vector2 position2, float width2, float height2)
        {
            return Math.Abs(position1.X - position2.X) * 2 < (width1 + width2)
                   && Math.Abs(position1.Y - position2.Y) * 2 < (height1 + height2);
        }
    }
}
