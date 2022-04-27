using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Window = GameEngine.Core.Window;

namespace GameEngine.Components
{
    public class Operator2DComponent : Component
    {
        public float CameraSpeed { get; set; } = 200f;
        public float ZoomScale { get; set; } = 0.2f;

        private Window Window => GameObject!.Engine.Window;
        private Camera Camera => GameObject!.Engine.Camera;
        private KeyboardState Inputs => Window.KeyboardState;

        public override void Start()
        {
            Window.MouseWheel += OnWindowMouseWheel;
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (!Window.IsFocused)
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
            Camera.Zoom += ZoomScale * args.OffsetY * ZoomScale;
        }
    }
}
