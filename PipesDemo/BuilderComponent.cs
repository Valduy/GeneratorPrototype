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
        private IEnumerator _pipeGenerator1;
        private IEnumerator _pipeGenerator2;
        private Vector3? _prevPosition = null;
        private Vector3? _prevDirection = null;
        private List<GameObject> _thermometers = new();
        private List<GameObject> _vectors = new();

        public string MapPath { get; set; }

        public override void Start()
        {
            _buildingModel.WallCreated += OnWallCreated;
            _buildingModel.TemperatureCalculated += OnTemperatureCalculated;
            _buildingModel.VectorsCalculated += OnVectorsCalculate;
            _buildingModel.PipeCreated += OnPipeCreated;
            _buildingModel.SegmentCreated += OnSegmentCreated;
            _buildingModel.Load(MapPath);

            //_pipeGenerator1 = _buildingModel.GeneratePipes(
            //    new Vector3i(1, 1, 0),
            //    new Vector3i(_buildingModel.Width - 1, _buildingModel.Height - 1, _buildingModel.Depth - 1)
            //    //new Vector3i(_buildingModel.Width - 10, _buildingModel.Height -7, _buildingModel.Depth - 10)
            //    )
            //    .GetEnumerator();

            //_pipeGenerator1 = _buildingModel.GeneratePipes(
            //        new Vector3i(1, 1, 0),
            //        new Vector3i(_buildingModel.Width - 7, _buildingModel.Height - 5, _buildingModel.Depth - 1)
            //    )
            //    .GetEnumerator();

            _pipeGenerator1 = _buildingModel.GenerateSpline(
                new Vector3i(1, 1, 0),
                new Vector3i(_buildingModel.Width - 7, _buildingModel.Height - 5, _buildingModel.Depth - 1))
                .GetEnumerator();

            //_pipeGenerator2 = _buildingModel.GenerateSpline(
            //        new Vector3i(1, 3, 0),
            //        new Vector3i(_buildingModel.Width - 1, _buildingModel.Height - 3, _buildingModel.Depth - 1))
            //    .GetEnumerator();
        }

        private Vector3i _position;
        private bool _flag = false;

        public override void GameUpdate(FrameEventArgs args)
        {
            var newPosition = new Vector3i(
                (int) Engine!.Camera.Position.X, 
                (int)Engine!.Camera.Position.Y, 
                (int)Engine!.Camera.Position.Z);

            if (_position != newPosition 
                && newPosition.X >= 0 && newPosition.X < _buildingModel.Width
                && newPosition.Y >= 0 && newPosition.Y < _buildingModel.Height
                && newPosition.Z >= 0 && newPosition.Z < _buildingModel.Depth)
            {
                Console.WriteLine($"Temperature: {_buildingModel[newPosition].Temperature}");
                _position = newPosition;
            }

            if (GameObject!.Engine.Window.KeyboardState.IsKeyDown(Keys.Enter))
            {
                if (!_pipeGenerator1.MoveNext())
                {
                    //if (!_flag)
                    //{
                    //    _thermometers.ForEach(t => Engine!.RemoveGameObject(t));
                    //    _thermometers.Clear();

                    //    _vectors.ForEach(v => Engine!.RemoveGameObject(v));
                    //    _vectors.Clear();

                    //    _pipeGenerator2 = _buildingModel.GeneratePipes(
                    //            new Vector3i(1, 3, 0),
                    //            new Vector3i(_buildingModel.Width - 1, _buildingModel.Height - 3, _buildingModel.Depth - 1)
                    //        )
                    //        .GetEnumerator();
                    //    _flag = true;
                    //}

                    //_pipeGenerator2.MoveNext();
                }
            }
        }

        private void OnWallCreated(Cell cell)
        {
            var cellGo = Engine!.CreateGameObject();
            var render = cellGo.Add<MeshRenderComponent>();
            render.Shape = Mesh.Cube;
            //GameObject!.AddChild(cellGo);
            cellGo.Position = cell.Position;
        }

        private void OnTemperatureCalculated()
        {
            var minTemperature = _buildingModel
                .Where(c => c.Type is CellType.Empty or CellType.Inside)
                .OrderBy(c => c.Temperature)
                .First().Temperature;

            foreach (var cell in _buildingModel)
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
                    thermometer.Rotation = new Vector3(45);
                    _thermometers.Add(thermometer);
                }
            }
        }

        private void OnVectorsCalculate()
        {
            foreach (var cell in _buildingModel)
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
                        vector.Rotation = new Vector3(180, 0, 0);
                    }
                    else
                    {
                        vector.Rotation = GetRotation(Vector3.UnitY, new Vector3(cell.Direction!.Value).Normalized()) * 180 / MathHelper.Pi;
                    }

                    _vectors.Add(vector);
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
            cellGo.Position = cell.Position;
        }

        private void OnSegmentCreated(Vector3 position)
        {
            var lineGo = GameObject!.Engine.CreateGameObject();
            var render = lineGo.Add<ShapeRenderComponent>();
            render.IsLinear = true;
            render.Color = Colors.Green;

            Vector3 prevPosition = _prevPosition ?? position;
            Vector3 prevDirection = _prevDirection ?? Vector3.Zero;
            Vector3 currentDirection = position - prevPosition;

            render.Shape = new Shape(GetSegmentPoints(
                prevPosition, prevDirection, position, currentDirection));
            
            _prevDirection = currentDirection;
            _prevPosition = position;
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

        private float[] GetSegmentPoints(
            Vector3 prevPosition, 
            Vector3 prevDirection, 
            Vector3 currentPosition,
            Vector3 currentDirection)
        {
            var result = new List<float>();
            int pointsPerSegment = 10;

            Vector3 p1 = prevPosition;
            Vector3 p2 = currentPosition;
            Vector3 t1 = prevDirection;
            Vector3 t2 = currentDirection;

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
