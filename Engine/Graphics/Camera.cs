using GameEngine.Game;
using OpenTK.Mathematics;

namespace GameEngine.Graphics
{
    public class Camera
    {
        private readonly Window _window;

        public Vector2 Position { get; set; }

        public float Zoom { get; set; }

        public Camera(Window window)
        {
            _window = window;
        }

        public Matrix4 GetViewMatrix() 
            => Matrix4.LookAt(
                new Vector3(Position.X, Position.Y, 0), 
                new Vector3(Position.X, Position.Y, -1), 
                Vector3.UnitY);

        // TODO: Fix zoom!!!
        public Matrix4 GetProjectionMatrix()
        {
            var widht = _window.Size.X + Zoom;
            var height = _window.Size.Y + _window.Size.X / _window.Size.Y * Zoom;
            return Matrix4.CreateOrthographic(widht, height, 0.1f, 100.0f);
        }
    }
}
