using GameEngine.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Pipes.Models;

namespace Pipes.Components
{
    public class CellInfoLoggerComponent : Component
    {
        private Vector3i _integerPosition;

        public Grid? Grid { get; set; }

        public override void Start()
        {
            _integerPosition = new Vector3i(
                (int)Engine!.Camera.Position.X,
                (int)Engine!.Camera.Position.Y,
                (int)Engine!.Camera.Position.Z);
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            var integerPosition = new Vector3i(
                (int)MathF.Round(Engine!.Camera.Position.X),
                (int)MathF.Round(Engine!.Camera.Position.Y),
                (int)MathF.Round(Engine!.Camera.Position.Z));

            if (_integerPosition != integerPosition
                && integerPosition.X >= 0 && integerPosition.X < Grid!.Width
                && integerPosition.Y >= 0 && integerPosition.Y < Grid!.Height
                && integerPosition.Z >= 0 && integerPosition.Z < Grid!.Depth)
            {
                Console.WriteLine($"Cell: ({integerPosition})");
                Console.WriteLine($"Temperature: {Grid[integerPosition].Temperature}");
                Console.WriteLine($"Direction: {Grid[integerPosition].Direction}\n");
                _integerPosition = integerPosition;
            }
        }
    }
}
