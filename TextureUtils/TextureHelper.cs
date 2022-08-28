using System.Drawing;

namespace TextureUtils
{
    public static class TextureHelper
    {
        public static Color GetColor(this byte[] data, int size, int x, int y)
        {
            x = Math.Clamp(x, 0, size - 1);
            y = Math.Clamp(y, 0, size - 1);

            byte r = data[4 * x + 4 * size * y + 0];
            byte g = data[4 * x + 4 * size * y + 1];
            byte b = data[4 * x + 4 * size * y + 2];
            byte a = data[4 * x + 4 * size * y + 3];

            return Color.FromArgb(a, r, g, b);
        }

        public static void SetColor(this byte[] data, int size, int x, int y, Color color)
        {
            x = Math.Clamp(x, 0, size - 1);
            y = Math.Clamp(y, 0, size - 1);

            data[4 * x + 4 * size * y + 0] = color.R;
            data[4 * x + 4 * size * y + 1] = color.G;
            data[4 * x + 4 * size * y + 2] = color.B;
            data[4 * x + 4 * size * y + 3] = color.A;
        }
    }
}
