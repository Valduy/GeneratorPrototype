using OpenTK.Mathematics;

namespace GameEngine.Mathematics
{
    public static class Curves
    {
        public static Matrix4 M = new(
            new Vector4(1, 0, -3, 2),
            new Vector4(0, 0, 3, -2),
            new Vector4(0, 1, -2, 1),
            new Vector4(0, 0, -1, 1));

        public static Vector3 Hermite(Vector3 p1, Vector3 p2, Vector3 t1, Vector3 t2, float t)
        {
            return (1 - 3 * MathF.Pow(t, 2) + 2 * MathF.Pow(t, 3)) * p1 +
                   MathF.Pow(t, 2) * (3 - 2 * t) * p2 +
                   t * MathF.Pow(t - 1, 2) * t1 +
                   MathF.Pow(t, 2) * (t - 1) * t2;
        }
    }
}
