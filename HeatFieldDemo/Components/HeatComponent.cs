using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Pipes.Algorithms;
using Pipes.Models;

namespace HeatFieldDemo.Components
{
    public class HeatComponent : Component
    {
        public const float MaxTemperature = FieldAlgorithms.MaxTemperature;
        public const float MinTemperature = 0;

        public static readonly Vector3i Hearth = new(3, 3, 3);

        private GameObject[,,] _thermometers;

        public Grid? Grid { get; set; }

        public override void Start()
        {
            _thermometers = new GameObject[Grid!.Width, Grid!.Height, Grid!.Depth];

            foreach (var cell in Grid)
            {
                cell.Temperature = MinTemperature;
                var percent = GetPercent(MaxTemperature, MinTemperature, cell.Temperature);
                var color = new Vector3(percent, MathF.Sin(percent * MathF.PI), 1.0f - percent);
                _thermometers[cell.Position.X, cell.Position.Y, cell.Position.Z] = CreateThermometer(cell, color);
            }
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            
        }

        private GameObject CreateThermometer(Cell cell, Vector3 color)
        {
            var thermometer = Engine!.CreateGameObject();
            var render = thermometer.Add<MeshRenderComponent>();
            render.Shape = Mesh1.Cube;
            render.Material.Ambient = color;
            render.Material.Diffuse = color;
            thermometer.Position = cell.Position;
            thermometer.Scale = new Vector3(0.05f);
            thermometer.Euler = new Vector3(45);
            return thermometer;
        }

        private static float GetPercent(float max, float min, float value)
            => (value - min) / (max - min);
    }
}
