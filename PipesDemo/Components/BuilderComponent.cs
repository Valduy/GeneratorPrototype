using System.Collections;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Pipes.Algorithms;
using Pipes.Models;
using Pipes.Models.Utils;
using Pipes.Utils;

namespace PipesDemo.Components
{
    public class BuilderComponent : Component
    {
        private IEnumerator? _currentGenerator;
        private int _index = 0;

        private List<GameObject> _thermometers = new();
        private List<GameObject> _vectors = new();

        private RigidPipesBuilder _rigidPipesBuilder;
        private FlexiblePipesBuilder _flexiblePipesBuilder;

        public Grid? Grid { get; set; }
        public IList<IEnumerator> GenerationSteps { get; set; }

        public override void Start()
        {
            _rigidPipesBuilder = new RigidPipesBuilder(Engine!);
            _flexiblePipesBuilder = new FlexiblePipesBuilder(Engine!);

            CreateWalls();
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (GameObject!.Engine.Window.KeyboardState.IsKeyDown(Keys.Enter))
            {
                if (GenerationSteps.Count == 0) return;

                _currentGenerator ??= GenerationSteps[_index];

                if (_currentGenerator.MoveNext()) return;

                _index = Math.Min(_index + 1, GenerationSteps.Count - 1);
                _currentGenerator = GenerationSteps[_index];
            }
        }

        public IEnumerator GenerateAStarPipe(Vector3i from, Vector3i to)
        {
            ResetField();
            _rigidPipesBuilder.Reset();

            var path = Grid!.GenerateAStarPipe(from, to);

            foreach (var cell in path)
            {
                _rigidPipesBuilder.CreatePipeSegment(cell);
            }
            
            yield return null;
        }

        public IEnumerator GenerateRigidPipe(Vector3i from, Vector3i to)
        {
            ResetField();
            _rigidPipesBuilder.Reset();

            Grid!.CalculateWarmBreathFirst(to);
            //Grid!.CalculateWarm(to);
            var generator = Grid!.GenerateRigidPipe(from, to);

            while (generator.MoveNext())
            {
                _rigidPipesBuilder.CreatePipeSegment(generator.Current);
                yield return null;
            }
        }

        public IEnumerator GenerateFlexiblePipe(Vector3i from, Vector3i to)
        {
            if (Grid![from].IsWallOrPipe()) throw new Exception();
            if (Grid![to].IsWallOrPipe()) throw new Exception();

            ResetField();
            _flexiblePipesBuilder.Reset();

            //Grid!.CalculateWarmBreathFirst(to);
            Grid!.CalculateWarmHeatTransfer(to);
            //Grid!.CalculateWarm(to);
            //VisualizeTemperature();
            //Grid!.CalculateVectors();
            //VisualizeVectors();
            var generator = Grid!.GenerateFlexiblePipe(from, to);

            Vector3 prev = from;
            Vector3 next;

            while (generator.MoveNext())
            {
                next = generator.Current;
                CreateVector(prev, next - prev);
                _flexiblePipesBuilder.CreatePipeSegment(next);
                prev = next;
                yield return null;
            }
        }

        private void ResetField()
        {
            _thermometers.ForEach(t => Engine!.RemoveGameObject(t));
            _thermometers.Clear();

            _vectors.ForEach(v => Engine!.RemoveGameObject(v));
            _vectors.Clear();
        }

        private void CreateWalls()
        {
            foreach (var cell in Grid)
            {
                if (cell.IsWall())
                {
                    var wallGo = Engine!.CreateGameObject();
                    var render = wallGo.Add<MeshRenderComponent>();
                    render.Model = Model.Cube;
                    wallGo.Position = cell.Position;
                }
            }
        }

        private void VisualizeTemperature()
        {
            var minTemperature = Grid!
                .Where(IsTemperatureMeasurable)
                .OrderBy(c => c.Temperature)
                .First().Temperature;

            foreach (var cell in Grid!)
            {
                if (cell.Type is CellType.Empty or CellType.Inside)
                {
                    var percent = GetPercent(FieldAlgorithms.MaxTemperature, minTemperature, cell.Temperature);
                    var color = new Vector3(percent, MathF.Sin(percent * MathF.PI), 1.0f - percent);
                    _thermometers.Add(CreateThermometer(cell, color));
                }
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

        bool IsTemperatureMeasurable(Cell cell) =>
            cell.IsFree() // Isn't empty space.
            && !float.IsNaN(cell.Temperature); // Isn't cut off area.

        private static float GetPercent(float max, float min, float value) 
            => (value - min) / (max - min);

        private void VisualizeVectors()
        {
            foreach (var cell in Grid!)
            {
                if (cell.Type is CellType.Empty or CellType.Inside)
                {
                    CreateVector(cell.Position, cell.Direction!.Value);
                }
            }
        }

        private void CreateVector(Vector3 position, Vector3 direction)
        {
            var vector = Engine!.CreateGameObject();
            var render = vector.Add<MeshRenderComponent>();
            render.Model = Model.Pyramid;
            render.Material.Color = Colors.Blue;
            render.Material.Specular = 0.0f;
            vector.Position = position;
            vector.Scale = new Vector3(0.05f, 0.5f, 0.05f);

            var to = direction.Normalized();

            // crutch for (0, -1, 0) case...
            if (to == -Vector3.UnitY)
            {
                vector.Euler = new Vector3(180, 0, 0);
            }
            else
            {
                vector.Rotation = Mathematics.GetRotation(Vector3.UnitY, to);
            }

            _vectors.Add(vector);
        }
    }
}
