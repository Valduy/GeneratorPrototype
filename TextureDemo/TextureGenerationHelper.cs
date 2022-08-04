using OpenTK.Mathematics;

namespace TextureDemo
{
    public static class TextureGenerationHelper
    {
        public static IEnumerable<Edge> EnumerateFace(this Face face)
        {
            for (int i = 0; i < face.Count; i++)
            {
                Vector3 a = face[i].Position;
                Vector3 b = face[(i + 1) % face.Count].Position;
                yield return new Edge(a, b);
            }
        }

        public static void SetColor(this byte[] data, int size, Vector2 position, Color color)
        {
            int x = (int)Math.Clamp(position.X, 0, size - 1);
            int y = (int)Math.Clamp(position.Y, 0, size - 1);

            data[4 * x + 4 * size * y + 0] = color.R;
            data[4 * x + 4 * size * y + 1] = color.G;
            data[4 * x + 4 * size * y + 2] = color.B;
            data[4 * x + 4 * size * y + 3] = color.A;
        }
    }
}
