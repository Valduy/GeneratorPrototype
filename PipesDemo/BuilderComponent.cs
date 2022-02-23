using System.Collections;
using System.Drawing;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace PipesDemo
{
    public class BuilderComponent : Component
    {
        public const int InWidth = 8;
        public const int InHeight = 8;
        public const int FloorWidth = 16;
        public const int FloorHeight = 16;

        private BuildingModel buildingModel = new();
        private IEnumerator pipeGenerator;

        public string MapPath { get; set; }

        public override void Start()
        {
            buildingModel.WallCreated += OnWallCreated;
            //buildingModel.TemperatureCalculated += OnTemperatureCalculated;
            buildingModel.VectorsCalculated += OnVectorsCalculate;
            buildingModel.PipeCreated += OnPipeCreated;
            buildingModel.Load(MapPath);

            //pipeGenerator = buildingModel.GeneratePipes(
            //    new Vector3i(1, 1, 0), 
            //    new Vector3i(buildingModel.Width - 1, buildingModel.Height -1, buildingModel.Depth - 1)
            //    //new Vector3i(buildingModel.Width - 10, buildingModel.Height -7, buildingModel.Depth - 10)
            //    )
            //    .GetEnumerator();

            pipeGenerator = buildingModel.GenerateSpline(
                new Vector3i(1, 1, 0),
                new Vector3i(buildingModel.Width - 1, buildingModel.Height - 1, buildingModel.Depth - 1))
                .GetEnumerator();
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (GameObject!.Engine.Window.KeyboardState.IsKeyDown(Keys.Enter))
            {
                pipeGenerator.MoveNext();
            }
        }

        private void OnWallCreated(Cell cell)
        {
            var cellGo = GameObject!.Engine.CreateGameObject();
            var render = cellGo.Add<Render3DComponent>();
            render.Shape = new Mesh(Mesh.Cube);
            //GameObject!.AddChild(cellGo);
            cellGo.Position = cell.Position;
        }

        private void OnTemperatureCalculated()
        {
            var minTemperature = buildingModel
                .Where(c => c.Type is CellType.Empty)
                .OrderBy(c => c.Temperature)
                .First().Temperature;

            foreach (var cell in buildingModel)
            {
                if (cell.Type == CellType.Empty)
                {
                    var cellGo = GameObject!.Engine.CreateGameObject();
                    var render = cellGo.Add<Render3DComponent>();
                    render.Shape = new Mesh(Mesh.Cube);
                    var percent = GetPercent(BuildingModel.MaxTemperature, minTemperature, cell.Temperature);
                    render.Material.Ambient = new Vector3(percent, MathF.Sin(percent * MathF.PI), 1.0f - percent);
                    render.Material.Diffuse = new Vector3(percent, MathF.Sin(percent * MathF.PI), 1.0f - percent);
                    //GameObject!.AddChild(cellGo);
                    cellGo.Position = cell.Position;
                    cellGo.Scale = new Vector3(0.05f);
                    cellGo.Rotation = new Vector3(45);
                }
            }
        }

        private void OnVectorsCalculate()
        {
            OnTemperatureCalculated();

            foreach (var cell in buildingModel)
            {
                if (cell.Type == CellType.Empty)
                {
                    var cellGo = GameObject!.Engine.CreateGameObject();
                    var render = cellGo.Add<Render3DComponent>();
                    render.Shape = new Mesh(Mesh.Pyramid);
                    render.Material.Ambient = cell.Direction;
                    render.Material.Diffuse = cell.Direction;
                    render.Material.Specular = new Vector3(0.0f);
                    //GameObject!.AddChild(cellGo);
                    cellGo.Position = cell.Position;
                    cellGo.Scale = new Vector3(0.05f, 0.5f, 0.05f);
                    cellGo.Rotation = GetRotation(Vector3.UnitY, cell.Direction.Normalized()) * 180 / MathHelper.Pi;
                }
            }
        }

        private float GetPercent(float max, float min, float value)
        {
            return (value - min) / (max - min);
        }

        private void OnPipeCreated(Cell cell)
        {
            var cellGo = GameObject!.Engine.CreateGameObject();
            var render = cellGo.Add<Render3DComponent>();
            render.Shape = new Mesh(Mesh.Cube);
            render.Material.Ambient = new Vector3(1.0f, 0.5f, 0.31f);
            render.Material.Diffuse = new Vector3(1.0f, 0.5f, 0.31f);
            render.Material.Specular = new Vector3(0.0f);
            render.Material.Shininess = 32.0f;
            //GameObject!.AddChild(cellGo);
            cellGo.Position = cell.Position;
        }

        private Vector3 GetRotation(Vector3 from, Vector3 to)
        {
            from.Normalize();
            to.Normalize();
            float cosa = MathHelper.Clamp(Vector3.Dot(from, to), -1, 1);
            var axis = Vector3.Cross(from, to);
            float angle = MathF.Acos(cosa);
            return Matrix4.CreateFromAxisAngle(axis, angle).ExtractRotation().ToEulerAngles();
        }
    }
}
