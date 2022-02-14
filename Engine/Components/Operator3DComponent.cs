using GameEngine.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GameEngine.Components
{
    public class Operator3DComponent : Component
    {
        private bool _firstMove;
        private Vector2 _lastPos;

        public float CameraSpeed { get; set; } = 1.5f;
        public float Sensitivity { get; set; } = 0.2f;

        public readonly Game.Game Game;

        public Operator3DComponent(Game.Game game)
        {
            Game = game;
        }

        public override void Start()
        {
            Game.Camera.Position = GameObject!.Position;
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (!Game.Window.IsFocused)
            {
                return;
            }

            ProcessKeyboardInputs((float)args.Time);
            ProcessMouseInputs();
        }

        private void ProcessKeyboardInputs(float dt)
        {
            var input = Game.Window.KeyboardState;

            if (input.IsKeyDown(Keys.W))
            {
                Game.Camera.Position += Game.Camera.Front * CameraSpeed * dt; // Forward
            }
            if (input.IsKeyDown(Keys.S))
            {
                Game.Camera.Position -= Game.Camera.Front * CameraSpeed * dt; // Backwards
            }
            if (input.IsKeyDown(Keys.A))
            {
                Game.Camera.Position -= Game.Camera.Right * CameraSpeed * dt; // Left
            }
            if (input.IsKeyDown(Keys.D))
            {
                Game.Camera.Position += Game.Camera.Right * CameraSpeed * dt; // Right
            }
            if (input.IsKeyDown(Keys.Space))
            {
                Game.Camera.Position += Game.Camera.Up * CameraSpeed * dt; // Up
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                Game.Camera.Position -= Game.Camera.Up * CameraSpeed * dt; // Down
            }
        }

        private void ProcessMouseInputs()
        {
            var mouse = Game.Window.MouseState;

            if (_firstMove)
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastPos.X;
                var deltaY = mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(mouse.X, mouse.Y);

                Game.Camera.Yaw += deltaX * Sensitivity;
                Game.Camera.Pitch -= deltaY * Sensitivity;
            }
        }
    }
}
