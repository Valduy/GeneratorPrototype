using GameEngine.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using PipesDemo.Algorithms;
using PipesDemo.Models;

namespace PipesDemo.Components
{
    public class CellInfoLoggerComponent : Component
    {
        private Vector3i _integerPosition;
        private Vector3 _floatPosition;

        public Grid? Model { get; set; }

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
            //    && floatPosition.X >= 0 && floatPosition.X < Model!.Width
            //    && floatPosition.Y >= 0 && floatPosition.Y < Model!.Height
            //    && floatPosition.Z >= 0 && floatPosition.Z < Model!.Depth)
            //{
            //    Console.WriteLine($"Interpolated: ({Model.GetHeat(floatPosition)})");
            //    Console.WriteLine($"Is greater: ({Model.GetHeat(floatPosition) > Model.GetHeat(_floatPosition)})");
            //    _floatPosition = floatPosition;
            //}

            var integerPosition = new Vector3i(
                (int)MathF.Round(Engine!.Camera.Position.X),
                (int)MathF.Round(Engine!.Camera.Position.Y),
                (int)MathF.Round(Engine!.Camera.Position.Z));

            if (_integerPosition != integerPosition
                && integerPosition.X >= 0 && integerPosition.X < Model!.Width
                && integerPosition.Y >= 0 && integerPosition.Y < Model!.Height
                && integerPosition.Z >= 0 && integerPosition.Z < Model!.Depth)
            {
                Console.WriteLine($"Cell: ({integerPosition})");
                Console.WriteLine($"Temperature: {Model[integerPosition].Temperature}");
                Console.WriteLine($"Direction: {Model[integerPosition].Direction}\n");
                _integerPosition = integerPosition;
            }
        }
    }
}
