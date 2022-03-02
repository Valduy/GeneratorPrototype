using System.Collections;
using System.Drawing;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
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

        private readonly BuildingModel _buildingModel = new();
        private IEnumerator _pipeGenerator;
        private Cell? _prev = null;

        public string MapPath { get; set; }

        public override void Start()
        {
            _buildingModel.WallCreated += OnWallCreated;
            //_buildingModel.TemperatureCalculated += OnTemperatureCalculated;
            _buildingModel.VectorsCalculated += OnVectorsCalculate;
            _buildingModel.PipeCreated += OnPipeCreated;
            _buildingModel.SegmentCreated += OnSegmentCreated;
            _buildingModel.Load(MapPath);

            //_pipeGenerator = _buildingModel.GeneratePipes(
            //    new Vector3i(1, 1, 0), 
            //    new Vector3i(_buildingModel.Width - 1, _buildingModel.Height -1, _buildingModel.Depth - 1)
            //    //new Vector3i(_buildingModel.Width - 10, _buildingModel.Height -7, _buildingModel.Depth - 10)
            //    )
            //    .GetEnumerator();

            _pipeGenerator = _buildingModel.GenerateSpline(
                new Vector3i(1, 1, 0),
                new Vector3i(_buildingModel.Width - 1, _buildingModel.Height - 1, _buildingModel.Depth - 1))
                .GetEnumerator();
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (GameObject!.Engine.Window.KeyboardState.IsKeyDown(Keys.Enter))
            {
                _pipeGenerator.MoveNext();
            }
        }

        private void OnWallCreated(Cell cell)
        {
            var cellGo = GameObject!.Engine.CreateGameObject();
            var render = cellGo.Add<MeshRenderComponent>();
            render.Shape = Mesh.Cube;
            //GameObject!.AddChild(cellGo);
            cellGo.Position = cell.Position;
        }

        private void OnTemperatureCalculated()
        {
            var minTemperature = _buildingModel
                .Where(c => c.Type is CellType.Empty)
                .OrderBy(c => c.Temperature)
                .First().Temperature;

            foreach (var cell in _buildingModel)
            {
                if (cell.Type == CellType.Empty)
                {
                    var cellGo = GameObject!.Engine.CreateGameObject();
                    var render = cellGo.Add<MeshRenderComponent>();
                    render.Shape = Mesh.Cube;
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

            foreach (var cell in _buildingModel)
            {
                if (cell.Type == CellType.Empty)
                {
                    var cellGo = GameObject!.Engine.CreateGameObject();
                    var render = cellGo.Add<MeshRenderComponent>();
                    render.Shape = Mesh.Pyramid;
                    render.Material.Ambient = cell.Direction!.Value;
                    render.Material.Diffuse = cell.Direction!.Value;
                    render.Material.Specular = new Vector3(0.0f);
                    //GameObject!.AddChild(cellGo);
                    cellGo.Position = cell.Position;
                    cellGo.Scale = new Vector3(0.05f, 0.5f, 0.05f);
                    cellGo.Rotation = GetRotation(Vector3.UnitY, new Vector3(cell.Direction.Value).Normalized()) * 180 / MathHelper.Pi;
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
            var render = cellGo.Add<MeshRenderComponent>();
            render.Shape = Mesh.Cube;
            render.Material.Ambient = new Vector3(1.0f, 0.5f, 0.31f);
            render.Material.Diffuse = new Vector3(1.0f, 0.5f, 0.31f);
            render.Material.Specular = new Vector3(0.0f);
            render.Material.Shininess = 32.0f;
            //GameObject!.AddChild(cellGo);
            cellGo.Position = cell.Position;
        }

        private void OnSegmentCreated(Cell cell)
        {
            var lineGo = GameObject!.Engine.CreateGameObject();
            var render = lineGo.Add<ShapeRenderComponent>();
            render.IsLinear = true;
            render.Color = Colors.Green;
            render.Shape = new Shape(GetSegmentPoints(_prev ?? cell, cell));
            _prev = cell;
        }

        private Vector3 GetRotation(Vector3 from, Vector3 to)
        {
            from.Normalize();
            to.Normalize();

            if (from == to) return new Vector3(0);

            float cosa = MathHelper.Clamp(Vector3.Dot(from, to), -1, 1);
            var axis = Vector3.Cross(from, to);
            float angle = MathF.Acos(cosa);
            return Matrix4.CreateFromAxisAngle(axis, angle).ExtractRotation().ToEulerAngles();
        }

        private float[] GetSegmentPoints(Cell prev, Cell current)
        {
            var result = new List<float>();
            int pointsPerSegment = 10;

            Vector3 p1 = current.Position;
            Vector3 p2 = current.Position + current.Direction!.Value;
            Vector3 t1 = prev.Direction!.Value;
            Vector3 t2 = current.Direction!.Value;

            for (int i = 0; i <= pointsPerSegment; i++)
            {
                var t = (float)i / pointsPerSegment;
                var point = Curves.Hermite(p1, p2, t1, t2, t);
                result.Add(point.X);
                result.Add(point.Y);
                result.Add(point.Z);
            }

            return result.ToArray();
        }
    }
}
