using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using Pipes.Models;
using static System.Single;

namespace Pipes.Utils
{
    public class RigidPipesBuilder
    {
        private static readonly Mesh1 StraightPipeMesh1 = ObjLoader.Load("Content", "IPipe.obj");
        private static readonly Mesh1 AngularPipeMesh1 = ObjLoader.Load("Content", "LPipe.obj");

        private Engine _engine;
        private GameObject? _tail;
        private GameObject? _prev;

        public RigidPipesBuilder(Engine engine)
        {
            _engine = engine;
        }

        public void Reset()
        {
            _tail = null;
            _prev = null;
        }

        public void CreatePipeSegment(Cell cell)
        {
            var pipeGo = _engine.CreateGameObject();
            var render = pipeGo.Add<MeshRenderComponent>();
            render.Shape = StraightPipeMesh1;
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
                        meshRender.Shape = AngularPipeMesh1;
                        _prev.Euler = GetLPipeRotation(_tail.Position, _prev.Position, pipeGo.Position);
                    }
                    if (!MathHelper.ApproximatelyEqualEpsilon(_tail.Position.Z, pipeGo.Position.Z, Epsilon))
                    {
                        var meshRender = _prev!.Get<MeshRenderComponent>()!;
                        meshRender.Shape = AngularPipeMesh1;
                        _prev.Euler = GetLPipeRotation(_tail.Position, _prev.Position, pipeGo.Position);
                    }
                }
                if (!MathHelper.ApproximatelyEqualEpsilon(_tail.Position.Y, pipeGo.Position.Y, Epsilon))
                {
                    if (!MathHelper.ApproximatelyEqualEpsilon(_tail.Position.Z, pipeGo.Position.Z, Epsilon))
                    {
                        var meshRender = _prev!.Get<MeshRenderComponent>()!;
                        meshRender.Shape = AngularPipeMesh1;
                        _prev.Euler = GetLPipeRotation(_tail.Position, _prev.Position, pipeGo.Position);
                    }
                }
            }

            _tail = _prev;
            _prev = pipeGo;
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
                    return new Vector3(90.0f, 0.0f, 270.0f);
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
                    return new Vector3(0.0f, 180.0f, 0.0f);
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
