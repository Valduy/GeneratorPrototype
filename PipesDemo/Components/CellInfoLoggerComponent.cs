using GameEngine.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Pipes.Models;

namespace PipesDemo.Components
{
    public class CellInfoLoggerComponent : Component
    {
        private Vector3i _integerPosition;
        private Vector3 _floatPosition;

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
            //var floatPosition = new Vector3(
            //    Engine!.Camera.Position.X,
            //    Engine!.Camera.Position.Y,
            //    Engine!.Camera.Position.Z);

            //if (_floatPosition != floatPosition
            //    && floatPosition.X >= 0 && floatPosition.X < Grid!.Width
            //    && floatPosition.Y >= 0 && floatPosition.Y < Grid!.Height
            //    && floatPosition.Z >= 0 && floatPosition.Z < Grid!.Depth)
            //{
            //    Console.WriteLine($"Interpolated: ({Grid.GetHeat(floatPosition)})");
            //    Console.WriteLine($"Is greater: ({Grid.GetHeat(floatPosition) > Grid.GetHeat(_floatPosition)})");
            //    _floatPosition = floatPosition;
            //}

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
