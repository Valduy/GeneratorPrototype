using GameEngine.Helpers;
using OpenTK.Mathematics;

namespace GameEngine.Mathematics
{
    public static class Math
    {
        // TODO: simple polygon, order and colinear vertices check
        public static int[] Triangulate(IReadOnlyList<Vector2> vertices)
        {
            if (vertices.Count < 3)
            {
                throw new ArgumentException("Should have more then 3 vertices.");
            }

            var indexes = Enumerable.Range(0, vertices.Count).ToList();
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

                    Vector2 va = vertices[a];
                    Vector2 vb = vertices[b];
                    Vector2 vc = vertices[c];

                    Vector2 vectorVaVb = vb - va;
                    Vector2 vectorVaVc = vc - va;

                    // Is vertex convex?
                    if (Cross(vectorVaVb, vectorVaVc) > 0.0f)
                    {
                        continue;
                    }

                    bool isEar = true;

                    // Does potential ear contain any poly vertex?
                    for (int j = 0; j < vertices.Count; j++)
                    {
                        if (j == a || j == b || j == c)
                        {
                            continue;
                        }

                        Vector2 p = vertices[j];
                        
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
