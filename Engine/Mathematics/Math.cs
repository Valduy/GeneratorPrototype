using GameEngine.Graphics;
using GameEngine.Helpers;
using OpenTK.Mathematics;

namespace GameEngine.Mathematics
{
    public static class Math
    {
        public static int[] Triangulate(Shape shape)
        {
            if (shape.Count < 3)
            {
                throw new ArgumentException("Should have more then 3 shape.");
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

        public static bool IsCounterClockWise(IReadOnlyList<Vector2> vertices) 
            => Area(vertices) > 0;

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

        public static float Cross(Vector2 a, Vector2 b) 
            => a.X * b.Y - a.Y * b.X;
    }
}
