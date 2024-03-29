﻿using GameEngine.Graphics;
using GameEngine.Helpers;
using OpenTK.Mathematics;

namespace GameEngine.Mathematics
{
    public static class Mathematics
    {
        // NOTE: I use words "points" and "polygon" as synonyms.

        public static int GetDecimalPlaces(float n)
        {
            n = Math.Abs(n);
            n -= (int)n;
            int decimalPlaces = 0;

            while (n > 0)
            {
                decimalPlaces++;
                n *= 10;
                n -= (int)n;
            }

            return decimalPlaces;
        }

        public static int Euclid(int a, int b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }

            return a + b;
        }

        public static float Map(float x, float inMin, float inMax, float outMin, float outMax)
        {
            return (x - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
        }

        /// <summary>
        /// Method triangulate <see cref="Line"/>.
        /// Use "Ear clipping" method (https://en.wikipedia.org/wiki/Polygon_triangulation).
        /// </summary>
        /// <param name="points"><see cref="Line"/> for triangulation.</param>
        /// <returns>Indices, which defines triangles vertexes in original points vertexes.</returns>
        /// <exception cref="ArgumentException">Trow, if points has <= then three vertexes.</exception>
        public static int[] Triangulate(IReadOnlyList<Vector2> points)
        {
            if (points.Count < 3)
            {
                throw new ArgumentException("Should have more then 3 vertexes.");
            }

            var indexes = Enumerable.Range(0, points.Count).ToList();
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

                    Vector2 va = points[a];
                    Vector2 vb = points[b];
                    Vector2 vc = points[c];

                    Vector2 vectorVaVb = vb - va;
                    Vector2 vectorVaVc = vc - va;

                    // Is vertex convex?
                    if (Cross(vectorVaVb, vectorVaVc) > 0.0f)
                    {
                        continue;
                    }

                    bool isEar = true;

                    // Does potential ear contain any poly vertex?
                    for (int j = 0; j < points.Count; j++)
                    {
                        if (j == a || j == b || j == c)
                        {
                            continue;
                        }

                        Vector2 p = points[j];

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
        /// Method check, is points has collinear neighboring edges.
        /// This edges can be eliminated without effecting to original points.
        /// You should avoid collinear neighboring edges, because they can break some algorithms...
        /// </summary>
        /// <param name="vertices">Vertexes.</param>
        /// <returns>True, if vertexes contains neighboring edges and false in other case.</returns>
        public static bool IsContainsCollinearNeighboringEdges(IReadOnlyList<Vector2> vertices)
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
        public static bool IsPointInConvexPolygon(Vector2 p, IReadOnlyList<Vector2> polygon)
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
        public static bool IsPolygonInsideConvexPolygon(IReadOnlyList<Vector2> thisPolygon, IReadOnlyList<Vector2> otherPolygon) 
            => thisPolygon.All(p => IsPointInConvexPolygon(p, otherPolygon));

        /// <summary>
        /// Method check, is convex polygons intersects.
        /// </summary>
        /// <param name="thisPolygon">First convex polygon.</param>
        /// <param name="otherPolygon">Second convex polygon.</param>
        /// <returns>True if intersect, false in other case.</returns>
        public static bool IsConvexPolygonsIntersects(IReadOnlyList<Vector2> thisPolygon, IReadOnlyList<Vector2> otherPolygon) 
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

        public static int SumComponents(this Vector2i vector)
        {
            return vector.X + vector.Y;
        }

        public static int SumComponents(this Vector3i vector)
        {
            return vector.X + vector.Y + vector.Z;
        }

        public static float SumComponents(this Vector2 vector)
        {
            return vector.X + vector.Y;
        }

        public static float SumComponents(this Vector3 vector)
        {
            return vector.X + vector.Y + vector.Z;
        }

        public static double SumComponents(this Vector2d vector)
        {
            return vector.X + vector.Y;
        }

        public static double SumComponents(this Vector3d vector)
        {
            return vector.X + vector.Y + vector.Z;
        }

        public static Vector2 Rotate(Vector2 vector, float angle)
        {
            var vector3d = new Vector3(vector.X, vector.Y, 1);
            vector3d *= Matrix3.CreateRotationZ(MathHelper.DegreesToRadians(angle));
            return new Vector2(vector3d.X, vector3d.Y);
        }

        public static Vector2 GetCentroid(IReadOnlyList<Vector2> points)
        {
            return points.Aggregate((p1, p2) => p1 + p2) / points.Count;
        }

        public static Vector3 GetCentroid(IReadOnlyList<Vector3> points)
        {
            return points.Aggregate((p1, p2) => p1 + p2) / points.Count;
        }

        public static Vector3 GetNormal(IReadOnlyList<Vector3> face)
        {
            var a = Vector3.Normalize(face[0] - face[1]);
            var b = Vector3.Normalize(face[2] - face[1]);
            return Vector3.Cross(a, b).Normalized();
        }

        public static List<Vector3> GetSharedPoints(IEnumerable<Vector3> poly1, IEnumerable<Vector3> poly2, float epsilon)
        {
            var sharedPoints = new List<Vector3>();

            foreach (var a in poly1)
            {
                foreach (var b in poly2)
                {
                    if (ApproximatelyEqualEpsilon(a, b, epsilon))
                    {
                        sharedPoints.Add(a);
                    }
                }
            }

            return sharedPoints;
        }

        public static bool TryGetIntersactionPoint(
            Vector3 r1,
            Vector3 e1,
            Vector3 r2,
            Vector3 e2,
            float epsilon,
            out Vector3 p)
        {
            float distance = float.PositiveInfinity;
            var n = Vector3.Cross(e1, e2);
            p = Vector3.Zero;

            if (ApproximatelyEqualEpsilon(n, Vector3.Zero, epsilon))
            {
                return false;
            }

            distance = MathF.Abs(Vector3.Dot(n, r1 - r2)) / n.Length;

            if (distance > epsilon)
            {
                return false;
            }

            var squareN = Vector3.Dot(n, n);
            var direction = r2 - r1;

            var t1 = Vector3.Dot(Vector3.Cross(e2, n), direction) / squareN;
            var t2 = Vector3.Dot(Vector3.Cross(e1, n), direction) / squareN;

            var p1 = r1 + t1 * e1;
            var p2 = r2 + t2 * e2;

            p = (p1 + p2) / 2;
            return true;
        }

        public static bool IsPointLieOnLine(Vector3 r, Vector3 e, Vector3 p, float epsilon)
        {
            var distacne = DistanceToLine(r, e, p);

            if (ApproximatelyEqualEpsilon(distacne, 0.0f, epsilon))
            {
                return true;
            }

            return false;
        }

        public static float DistanceToLine(Vector3 r, Vector3 e, Vector3 p)
        {
            var toPoint = p - r;
            var projection = (Vector3.Dot(toPoint, e) * e) / e.LengthSquared;
            var perpendicular = toPoint - projection;
            return perpendicular.Length;
        }

        public static Vector2 GetBarycentric(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            Vector2 v0 = c - a;
            Vector2 v1 = b - a;
            Vector2 v2 = p - a;

            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            float inverseDenominator = 1.0f / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * inverseDenominator;
            float v = (dot00 * dot12 - dot01 * dot02) * inverseDenominator;

            return new Vector2(u, v);
        }

        public static bool ApproximatelyEqualEpsilon(float a, float b, float epsilon)
        {
            return MathF.Abs(a - b) <= epsilon;
        }

        public static bool ApproximatelyEqualEpsilon(Vector2 a, Vector2 b, float epsilon)
        {
            return ApproximatelyEqualEpsilon(a.X, b.X, epsilon) 
                && ApproximatelyEqualEpsilon(a.Y, b.Y, epsilon);
        }

        public static bool ApproximatelyEqualEpsilon(Vector3 a, Vector3 b, float epsilon)
        {
            return ApproximatelyEqualEpsilon(a.X, b.X, epsilon)
                && ApproximatelyEqualEpsilon(a.Y, b.Y, epsilon)
                && ApproximatelyEqualEpsilon(a.Z, b.Z, epsilon);
        }

        public static bool IsBoundingBoxesIntersects(
            Vector2 position1, float width1, float height1,
            Vector2 position2, float width2, float height2)
        {
            return Math.Abs(position1.X - position2.X) * 2 < (width1 + width2)
                && Math.Abs(position1.Y - position2.Y) * 2 < (height1 + height2);
        }

        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            from.Normalize();
            to.Normalize();

            if (ApproximatelyEqualEpsilon(from, to, float.Epsilon))
            {
                return Quaternion.Identity;
            }
            if (ApproximatelyEqualEpsilon(from, -to, float.Epsilon))
            {
                return Quaternion.FromEulerAngles(0, MathF.PI, 0);
            }

            float cosa = MathHelper.Clamp(Vector3.Dot(from, to), -1, 1);
            var axis = Vector3.Cross(from, to);
            float angle = MathF.Acos(cosa);
            return Quaternion.FromAxisAngle(axis, angle);
        }

        public static Quaternion FromToRotation(Vector3 axis, Vector3 from, Vector3 to)
        {
            from.Normalize();
            to.Normalize();

            if (Mathematics.ApproximatelyEqualEpsilon(from, to, float.Epsilon))
            {
                return Quaternion.Identity;
            }
            if (Mathematics.ApproximatelyEqualEpsilon(from, -to, float.Epsilon))
            {
                return Quaternion.FromAxisAngle(axis, MathF.PI);
            }

            float cosa = MathHelper.Clamp(Vector3.Dot(from, to), -1, 1);
            float angle = MathF.Acos(cosa);
            return Quaternion.FromAxisAngle(axis, angle);
        }
    }
}
