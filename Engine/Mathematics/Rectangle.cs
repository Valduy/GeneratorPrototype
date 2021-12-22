using OpenTK.Mathematics;

namespace GameEngine.Mathematics
{
    public class Rectangle
    {
        public Vector2 Position { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Rectangle() {}

        public Rectangle(Vector2 position, int width, int height)
        {
            Position = position;
            Width = width;
            Height = height;
        }
    }
}
