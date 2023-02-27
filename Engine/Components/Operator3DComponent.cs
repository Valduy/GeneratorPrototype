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
        private float _cameraMinSpeed = 1.0f;
        private float _cameraMaxSpeed = 10.0f;
        private float _cameraSpeed = 3.0f;

        private bool _firstMove;
        private Vector2 _lastPos;

        public float CameraMinSpeed 
        { 
            get => _cameraMinSpeed;
            set
            {
                if (value > _cameraMaxSpeed)
                {
                    throw new ArgumentException($"{nameof(CameraMinSpeed)} should be <= then{nameof(CameraMaxSpeed)}.");
                }

                _cameraMinSpeed = value;
                _cameraSpeed = MathF.Max(_cameraSpeed, _cameraMinSpeed);
            } 
        }

        public float CameraMaxSpeed 
        { 
            get => _cameraMaxSpeed;
            set
            {
                if (value < _cameraMinSpeed)
                {
                    throw new ArgumentException($"{nameof(CameraMaxSpeed)} should be >= then {nameof(CameraMinSpeed)}.");
                }

                _cameraMaxSpeed = value;
                _cameraSpeed = MathF.Min(_cameraSpeed, _cameraMaxSpeed);
            } 
        }

        public float CameraSpeed 
        { 
            get => _cameraSpeed;
            set
            {
                _cameraSpeed = Math.Clamp(value, _cameraMinSpeed, _cameraMaxSpeed);
            } 
        }
        
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

            CameraSpeed += Mouse.ScrollDelta.Y;
        }
    }
}
