using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Window = GameEngine.Game.Window;

namespace GameEngine.Components
{
    public class OperatorComponent : Component
    {
        private readonly Window _window;

        public Camera Camera => _window.Renderer.Camera;
        public KeyboardState Inputs => _window.KeyboardState;
        public float CameraSpeed { get; set; } = 200f;
        public float ZoomScale { get; set; } = 10f;

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

            if (Inputs.IsKeyDown(Keys.W))
            {
                Camera.Position += Camera.Up * CameraSpeed * dt;
            }
            if (Inputs.IsKeyDown(Keys.S))
            {
                Camera.Position -= Camera.Up * CameraSpeed * dt;
            }
            if (Inputs.IsKeyDown(Keys.A))
            {
                Camera.Position -= Camera.Right * CameraSpeed * dt;
            }
            if (Inputs.IsKeyDown(Keys.D))
            {
                Camera.Position += Camera.Right * CameraSpeed * dt;
            }
        }

        private void OnWindowMouseWheel(MouseWheelEventArgs args)
        {
            //Camera.Position += Camera.Front * ZoomScale * args.OffsetY;
            Camera.Zoom += ZoomScale * args.OffsetY * 0.2f;
            //_window.Renderer.Camera.Zoom -= ZoomScale * args.OffsetY;
        }
    }
}
