using System.Drawing;

namespace TextureUtils
{
    public static class ColorHelper
    {
        public static bool IsSame(this Color lhs, Color rhs) =>
            lhs.A == rhs.A &&
            lhs.R == rhs.R &&
            lhs.G == rhs.G &&
            lhs.B == rhs.B;

        public static bool IsTransparent(this Color color) => color.A == 0;
    }
}