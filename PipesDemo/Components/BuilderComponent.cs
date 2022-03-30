using System.Collections;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PipesDemo.Models;
using PipesDemo.Utils;

namespace PipesDemo.Components
{
    public class BuilderComponent : Component
    {
        private IEnumerator? currentGenerator;
        private int _index = 0;

        private List<GameObject> _thermometers = new();
        private List<GameObject> _vectors = new();

        private RigidPipesBuilder _rigidPipesBuilder;
        private FlexiblePipesBuilder _flexiblePipesBuilder;

        public BuildingModel? Model { get; set; }
        public IList<IEnumerator> GenerationSteps { get; set; }

        public override void Start()
        {
            _rigidPipesBuilder = new RigidPipesBuilder(Engine!);
            _flexiblePipesBuilder = new FlexiblePipesBuilder(Engine!);

            CreateWalls();
            Model!.TemperatureCalculated += OnTemperatureCalculated;
            Model!.VectorsCalculated += OnVectorsCalculate;
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (GameObject!.Engine.Window.KeyboardState.IsKeyDown(Keys.Enter))
            {
                if (GenerationSteps.Count == 0) return;

                currentGenerator ??= GenerationSteps[_index];

                if (currentGenerator.MoveNext()) return;

                _index = Math.Min(_index + 1, GenerationSteps.Count - 1);
                currentGenerator = GenerationSteps[_index];
            }
        }

        public IEnumerator GenerateAStarPipe(Vector3i from, Vector3i to)
        {
            ResetField();
            _rigidPipesBuilder.Reset();

            var path = Model!.GenerateAStarPipe(from, to);

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

            var generator = Model!.GenerateRigidPipe(from, to);

            while (generator.MoveNext())
            {
                _rigidPipesBuilder.CreatePipeSegment(generator.Current);
                yield return null;
            }
        }

        public IEnumerator GenerateFlexiblePipe(Vector3i from, Vector3i to)
        {
            ResetField();
            _flexiblePipesBuilder.Reset();

            var generator = Model!.GenerateFlexiblePipe(from, to);

            while (generator.MoveNext())
            {
                _flexiblePipesBuilder.CreatePipeSegment(generator.Current);
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
            foreach (var cell in Model)
            {
                if (cell.Type is CellType.Wall)
                {
                    var wallGo = Engine!.CreateGameObject();
                    var render = wallGo.Add<MeshRenderComponent>();
                    render.Shape = Mesh.Cube;
                    wallGo.Position = cell.Position;
                }
            }
        }

        private void OnTemperatureCalculated()
        {
            var minTemperature = Model!
                .Where(IsTemperatureMeasurable)
                .OrderBy(c => c.Temperature)
                .First().Temperature;

            foreach (var cell in Model!)
            {
                if (cell.Type is CellType.Empty or CellType.Inside)
                {
                    var thermometer = Engine!.CreateGameObject();
                    var render = thermometer.Add<MeshRenderComponent>();
                    render.Shape = Mesh.Cube;
                    var percent = GetPercent(BuildingModel.MaxTemperature, minTemperature, cell.Temperature);
                    var color = new Vector3(percent, MathF.Sin(percent * MathF.PI), 1.0f - percent);
                    render.Material.Ambient = color;
                    render.Material.Diffuse = color;
                    thermometer.Position = cell.Position;
                    thermometer.Scale = new Vector3(0.05f);
                    thermometer.Euler = new Vector3(45);
                    _thermometers.Add(thermometer);
                }
            }
        }

        bool IsTemperatureMeasurable(Cell cell) =>
            cell.Type is CellType.Empty or CellType.Inside // Isn't empty space.
            && !float.IsNaN(cell.Temperature); // Isn't cut off area.

        private static float GetPercent(float max, float min, float value) 
            => (value - min) / (max - min);

        private void OnVectorsCalculate()
        {
            foreach (var cell in Model!)
            {
                if (cell.Type is CellType.Empty or CellType.Inside)
                {
                    var vector = Engine!.CreateGameObject();
                    var render = vector.Add<MeshRenderComponent>();
                    render.Shape = Mesh.Pyramid;
                    render.Material.Ambient = Colors.Blue;
                    render.Material.Diffuse = Colors.Blue;
                    render.Material.Specular = new Vector3(0.0f);
                    vector.Position = cell.Position;
                    vector.Scale = new Vector3(0.05f, 0.5f, 0.05f);
                    
                    // crutch for (0, -1, 0) case...
                    if (cell.Direction!.Value == -Vector3i.UnitY)
                    {
                        vector.Euler = new Vector3(180, 0, 0);
                    }
                    else
                    {
                        var to = new Vector3(cell.Direction!.Value).Normalized();
                        vector.Rotation = Mathematics.GetRotation(Vector3.UnitY, to);
                    }

                    _vectors.Add(vector);
                }
            }
        }
    }
}
