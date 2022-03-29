using System.Collections;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PipesDemo.Models;
using static System.Single;

namespace PipesDemo.Components
{
    public class BuilderComponent : Component
    {
        private readonly Mesh _straightPipeMesh = Utils.ObjLoader.Load("Content", "IPipe.obj");
        private readonly Mesh _angularPipeMesh = Utils.ObjLoader.Load("Content", "LPipe.obj");

        private IEnumerator _pipeGenerator1;
        private IEnumerator _pipeGenerator2;
        private Vector3? _prevPosition = null;
        private Vector3? _prevDirection = null;
        private List<GameObject> _thermometers = new();
        private List<GameObject> _vectors = new();

        #region Pipe generation.

        private GameObject? _tail;
        private GameObject? _prev;

        #endregion

        public BuildingModel? Model { get; set; }

        public override void Start()
        {
            CreateWalls();
            Model!.TemperatureCalculated += OnTemperatureCalculated;
            Model!.VectorsCalculated += OnVectorsCalculate;
            Model!.PipeCreated += OnPipeCreated;
            Model!.SegmentCreated += OnSegmentCreated;
            Model!.GraphPipeGenerated += OnGraphPipeGenerated;

            //_buildingModel.GenerateGraphBasePipe(
            //    new Vector3i(1, 1, 0),
            //    new Vector3i(_buildingModel.Width - 1, _buildingModel.Height - 1, _buildingModel.Depth - 1));

            // TODO: this
            _pipeGenerator1 = Model.GeneratePipes(
                new Vector3i(1, 1, 0),
                new Vector3i(Model.Width - 1, Model.Height - 1, Model.Depth - 1)
                //new Vector3i(_buildingModel.Width - 10, _buildingModel.Height -7, _buildingModel.Depth - 10)
                )
                .GetEnumerator();

            //_pipeGenerator2 = _buildingModel.GeneratePipes(
            //        new Vector3i(3, 1, 0),
            //        new Vector3i(_buildingModel.Width - 7, _buildingModel.Height - 5, _buildingModel.Depth - 1)
            //    )
            //    .GetEnumerator();

            //_pipeGenerator1 = _buildingModel.GenerateSpline(
            //    new Vector3i(1, 1, 0),
            //    new Vector3i(_buildingModel.Width - 7, _buildingModel.Height - 5, _buildingModel.Depth - 1))
            //    .GetEnumerator();

            //_pipeGenerator2 = _buildingModel.GenerateSpline(
            //        new Vector3i(1, 3, 0),
            //        new Vector3i(_buildingModel.Width - 1, _buildingModel.Height - 3, _buildingModel.Depth - 1))
            //    .GetEnumerator();
        }

        private bool _flag = false;

        public override void GameUpdate(FrameEventArgs args)
        {
            if (GameObject!.Engine.Window.KeyboardState.IsKeyDown(Keys.Enter))
            {
                if (!_pipeGenerator1.MoveNext())
                {
                    if (!_flag)
                    {
                        _thermometers.ForEach(t => Engine!.RemoveGameObject(t));
                        _thermometers.Clear();

                        _vectors.ForEach(v => Engine!.RemoveGameObject(v));
                        _vectors.Clear();

                        _tail = null;
                        _prev = null;
                        
                        _pipeGenerator2 = Model.GeneratePipes(
                                new Vector3i(1, 3, 0),
                                new Vector3i(Model.Width - 1, Model.Height - 3, Model.Depth - 1)
                            )
                            .GetEnumerator();
                        _flag = true;
                    }

                    _pipeGenerator2.MoveNext();
                }
            }
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

        private void OnGraphPipeGenerated(List<Cell> cells)
        {
            foreach (var cell in cells)
            {
                OnPipeCreated(cell);
            }
        }

        private void OnTemperatureCalculated()
        {
            var minTemperature = Model!
                .Where(c => c.Type is CellType.Empty or CellType.Inside)
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

        private float GetPercent(float max, float min, float value)
        {
            return (value - min) / (max - min);
        }
        
        private void OnPipeCreated(Cell cell)
        {
            var pipeGo = GameObject!.Engine.CreateGameObject();
            var render = pipeGo.Add<MeshRenderComponent>();
            render.Shape = _straightPipeMesh;
            render.Material.Ambient = new Vector3(1.0f, 0.5f, 0.31f);
            render.Material.Diffuse = new Vector3(1.0f, 0.5f, 0.31f);
            render.Material.Specular = new Vector3(0.0f);
            render.Material.Shininess = 32.0f;
            pipeGo.Position = cell.Position;
            
            // Rotate current segment.
            if (_prev != null)
            {
                if (!MathHelper.ApproximatelyEqualEpsilon(_prev.Position.X, pipeGo.Position.X, Epsilon))
                {
                    pipeGo.Euler = new Vector3(0, 0, 90);
                }
                else if (!MathHelper.ApproximatelyEqualEpsilon(_prev.Position.Z, pipeGo.Position.Z, Epsilon))
                {
                    pipeGo.Euler = new Vector3(-90, 0, 0);
                }
            }

            // Rotate first segment (special case for first pipe segment).
            if (_prev != null && _tail == null)
            {
                _prev.Euler = pipeGo.Euler;
            }

            // Rotate prev segment.
            if (_tail != null)
            {
                if (!MathHelper.ApproximatelyEqualEpsilon(_tail.Position.X, pipeGo.Position.X, Epsilon))
                {
                    if (!MathHelper.ApproximatelyEqualEpsilon(_tail.Position.Y, pipeGo.Position.Y, Epsilon))
                    {
                        var meshRender = _prev!.Get<MeshRenderComponent>()!;
                        meshRender.Shape = _angularPipeMesh;
                        _prev.Euler = GetLPipeRotation(_tail.Position, _prev.Position, pipeGo.Position);
                    }
                    if (!MathHelper.ApproximatelyEqualEpsilon(_tail.Position.Z, pipeGo.Position.Z, Epsilon))
                    {
                        var meshRender = _prev!.Get<MeshRenderComponent>()!;
                        meshRender.Shape = _angularPipeMesh;
                        _prev.Euler = GetLPipeRotation(_tail.Position, _prev.Position, pipeGo.Position);
                    }
                }
                if (!MathHelper.ApproximatelyEqualEpsilon(_tail.Position.Y, pipeGo.Position.Y, Epsilon))
                {
                    if (!MathHelper.ApproximatelyEqualEpsilon(_tail.Position.Z, pipeGo.Position.Z, Epsilon))
                    {
                        var meshRender = _prev!.Get<MeshRenderComponent>()!;
                        meshRender.Shape = _angularPipeMesh;
                        _prev.Euler = GetLPipeRotation(_tail.Position, _prev.Position, pipeGo.Position);
                    }
                }
            }

            _tail = _prev;
            _prev = pipeGo;
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

        private Vector3 GetLPipeRotation(Vector3 from, Vector3 via, Vector3 to)
        {
            if (MathHelper.ApproximatelyEqualEpsilon(from.Y, via.Y, Epsilon) && 
                MathHelper.ApproximatelyEqualEpsilon(via.Y, to.Y, Epsilon))
            {
                // t--v f--v
                //    |    |
                //    f    t
                if (from.Z < via.Z && via.X < to.X ||
                    from.X > via.X && via.Z > to.Z)
                {
                    return new Vector3(90.0f, 0.0f, 0.0f);
                }
                //    f    t
                //    |    |
                // t--v f--v
                if (from.Z > via.Z && via.X < to.X ||
                    from.X > via.X && via.Z < to.Z)
                {
                    return new Vector3(90.0f, 0.0f, 90.0f);
                }
                // f    t
                // |    |
                // v--t v--f
                if (from.Z > via.Z && via.X > to.X ||
                    from.X < via.X && via.Z < to.Z)
                {
                    return new Vector3(90.0f, 0.0f, 180.0f);
                }
                // v--t v--f
                // |    |
                // f    t
                if (from.Z < via.Z && via.X > to.X ||
                    from.X < via.X && via.Z > to.Z)
                {
                    return new Vector3(90.0f, 270.0f, 270.0f);
                }
            }
            else if (MathHelper.ApproximatelyEqualEpsilon(from.Z, via.Z, Epsilon) && 
                     MathHelper.ApproximatelyEqualEpsilon(via.Z, to.Z, Epsilon))
            {
                // t--v f--v
                //    |    |
                //    f    t
                if (from.Y < via.Y && via.X < to.X ||
                    from.X > via.X && via.Y > to.Y)
                {
                    return new Vector3(0.0f, 0.0f, 0.0f);
                }
                //    f    t
                //    |    |
                // t--v f--v
                if (from.Y > via.Y && via.X < to.X ||
                    from.X > via.X && via.Y < to.Y)
                {
                    return new Vector3(0.0f, 0.0f, 90.0f);
                }
                // f    t
                // |    |
                // v--t v--f
                if (from.Y > via.Y && via.X > to.X ||
                    from.X < via.X && via.Y < to.Y)
                {
                    return new Vector3(0.0f, 0.0f, 180.0f);
                }
                // v--t v--f
                // |    |
                // f    t
                if (from.Y < via.Y && via.X > to.X ||
                    from.X < via.X && via.Y > to.Y)
                {
                    return new Vector3(0.0f, 270.0f, 270.0f);
                }
            }
            else if (MathHelper.ApproximatelyEqualEpsilon(from.X, via.X, Epsilon) &&
                     MathHelper.ApproximatelyEqualEpsilon(via.X, to.X, Epsilon))
            {
                // t--v f--v
                //    |    |
                //    f    t
                if (from.Y < via.Y && via.Z < to.Z ||
                    from.Z > via.Z && via.Y > to.Y)
                {
                    return new Vector3(0.0f, -90.0f, 0.0f);
                }
                //    f    t
                //    |    |
                // t--v f--v
                if (from.Y > via.Y && via.Z < to.Z ||
                    from.Z > via.Z && via.Y < to.Y)
                {
                    return new Vector3(0.0f, -90.0f, 90.0f);
                }
                // f    t
                // |    |
                // v--t v--f
                if (from.Y > via.Y && via.Z > to.Z ||
                    from.Z < via.Z && via.Y < to.Y)
                {
                    return new Vector3(0.0f, -90.0f, 180.0f);
                }
                // v--t v--f
                // |    |
                // f    t
                if (from.Y < via.Y && via.Z > to.Z ||
                    from.Z < via.Z && via.Y > to.Y)
                {
                    return new Vector3(0.0f, -90.0f, 270.0f);
                }
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}
