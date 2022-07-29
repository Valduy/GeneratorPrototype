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

        public static readonly Vector3i Hearth = new(4);

        private GameObject[,,] _thermometers;

        public Grid? Grid { get; set; }

        public override void Start()
        {
            _thermometers = new GameObject[Grid!.Width, Grid!.Height, Grid!.Depth];

            foreach (var cell in Grid)
            {
                cell.Temperature = MinTemperature;
                var color = GetColor(cell);
                _thermometers[cell.Position.X, cell.Position.Y, cell.Position.Z] = CreateThermometer(cell, color);
                cell.TemperatureChanged += OnTemperatureChanged;
            }

            for (int i = 0; i < 1000; i++)
            {
                Grid!.HeatTransferIteration(Hearth.X, Hearth.Y, Hearth.Z, 0.1f, _ => 0.1f);
            }
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            for (int i = 0; i < 10; i++)
            {
                Grid!.HeatTransferIteration(Hearth.X, Hearth.Y, Hearth.Z, 0.1f, _ => 0.1f);
            }
        }

        private GameObject CreateThermometer(Cell cell, Vector3 color)
        {
            var thermometer = Engine!.CreateGameObject();
            var render = thermometer.Add<MeshRenderComponent>();
            render.Model = Model.Cube;
            render.Material.Color = color;
            thermometer.Position = cell.Position;
            thermometer.Scale = new Vector3(0.05f);
            thermometer.Euler = new Vector3(45);
            return thermometer;
        }

        private static Vector3 GetColor(Cell cell)
        {
            var percent = GetPercent(MaxTemperature, MinTemperature, cell.Temperature);
            return new Vector3(percent, MathF.Sin(percent * MathF.PI), 1.0f - percent);
        }

        private static float GetPercent(float max, float min, float value)
            => (value - min) / (max - min);

        private void OnTemperatureChanged(Cell cell)
        {
            var thermometer = _thermometers[cell.Position.X, cell.Position.Y, cell.Position.Z];
            var render = thermometer.Get<MeshRenderComponent>();
            var color = GetColor(cell);
            render!.Material.Color = color;
        }
    }
}
