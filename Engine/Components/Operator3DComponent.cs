using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Window = GameEngine.Core.Window;

namespace GameEngine.Components
{
    public class Operator3DComponent : Component
    {
        private bool _firstMove;
        private Vector2 _lastPos;

        public float CameraSpeed { get; set; } = 1.5f;
        public float Sensitivity { get; set; } = 0.2f;

        private Window Window => GameObject!.Engine.Window;
        private KeyboardState Inputs => Window.KeyboardState;
        private MouseState Mouse => Window.MouseState;
        private Camera Camera => GameObject!.Engine.Camera;

        public override void Start()
        {
            GameObject!.Engine.Camera.Position = GameObject!.Position;
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (!GameObject!.Engine.Window.IsFocused)
            {
                return;
            }

            ProcessKeyboardInputs((float)args.Time);
            ProcessMouseInputs();
        }

        private void ProcessKeyboardInputs(float dt)
        {
            if (Inputs.IsKeyDown(Keys.W))
            {
                Camera.Position += Camera.Front * CameraSpeed * dt; // Forward
            }
            if (Inputs.IsKeyDown(Keys.S))
            {
                Camera.Position -= Camera.Front * CameraSpeed * dt; // Backwards
            }
            if (Inputs.IsKeyDown(Keys.A))
            {
                Camera.Position -= Camera.Right * CameraSpeed * dt; // Left
            }
            if (Inputs.IsKeyDown(Keys.D))
            {
                Camera.Position += Camera.Right * CameraSpeed * dt; // Right
            }
            if (Inputs.IsKeyDown(Keys.Space))
            {
                Camera.Position += Camera.Up * CameraSpeed * dt; // Up
            }
            if (Inputs.IsKeyDown(Keys.LeftShift))
            {
                Camera.Position -= Camera.Up * CameraSpeed * dt; // Down
            }
        }

        private void ProcessMouseInputs()
        {
            if (_firstMove)
            {
                _lastPos = new Vector2(Mouse.X, Mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = Mouse.X - _lastPos.X;
                var deltaY = Mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(Mouse.X, Mouse.Y);

                Camera.Yaw += deltaX * Sensitivity;
                Camera.Pitch -= deltaY * Sensitivity;
            }
        }
    }
}
