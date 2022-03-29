using GameEngine.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using PipesDemo.Models;

namespace PipesDemo.Components
{
    public class CellInfoLoggerComponent : Component
    {
        private Vector3i _position;

        public BuildingModel? Model { get; set; }

        public override void Start()
        {
            _position = new Vector3i(
                (int)Engine!.Camera.Position.X,
                (int)Engine!.Camera.Position.Y,
                (int)Engine!.Camera.Position.Z);
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            var newPosition = new Vector3i(
                (int)Engine!.Camera.Position.X,
                (int)Engine!.Camera.Position.Y,
                (int)Engine!.Camera.Position.Z);

            if (_position != newPosition
                && newPosition.X >= 0 && newPosition.X < Model!.Width
                && newPosition.Y >= 0 && newPosition.Y < Model!.Height
                && newPosition.Z >= 0 && newPosition.Z < Model!.Depth)
            {
                Console.WriteLine($"Cell: ({newPosition})");
                Console.WriteLine($"Temperature: {Model[newPosition].Temperature}");
                Console.WriteLine($"Direction: {Model[newPosition].Direction}\n");
                _position = newPosition;
            }
        }
    }
}
