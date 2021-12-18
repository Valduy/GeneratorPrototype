using GameEngine.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Window = GameEngine.Game.Window;

namespace GameEngine.Components
{
    public class OperatorComponent : Component
    {
        private readonly Window _window;
 
        public float CameraSpeed { get; set; } = 200f;
        public float ZoomScale { get; set; } = 100f;

        public OperatorComponent(Window window)
        {
            _window = window;
            _window.MouseWheel += OnWindowMouseWheel;
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (!_window.IsFocused)
            {
                return;
            }

            var dt = (float) args.Time;
            var input = _window.KeyboardState;
            var camera = _window.Renderer.Camera;

            if (input.IsKeyDown(Keys.W))
            {
                camera.Position += Vector2.UnitY * CameraSpeed * dt;
            }
            if (input.IsKeyDown(Keys.S))
            {
                camera.Position -= Vector2.UnitY * CameraSpeed * dt;
            }
            if (input.IsKeyDown(Keys.A))
            {
                camera.Position -= Vector2.UnitX * CameraSpeed * dt;
            }
            if (input.IsKeyDown(Keys.D))
            {
                camera.Position += Vector2.UnitX * CameraSpeed * dt;
            }
        }

        private void OnWindowMouseWheel(MouseWheelEventArgs args)
        {
            _window.Renderer.Camera.Zoom -= ZoomScale * args.OffsetY;
        }
    }
}
