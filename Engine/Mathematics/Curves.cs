using OpenTK.Mathematics;

namespace GameEngine.Mathematics
{
    public static class Curves
    {
        public static Vector3 Hermite(Vector3 p1, Vector3 p2, Vector3 t1, Vector3 t2, float t)
        {
            return (1 - 3 * MathF.Pow(t, 2) + 2 * MathF.Pow(t, 3)) * p1 +
                   MathF.Pow(t, 2) * (3 - 2 * t) * p2 +
                   t * MathF.Pow(t - 1, 2) * t1 +
                   MathF.Pow(t, 2) * (t - 1) * t2;
        }
    }
}
