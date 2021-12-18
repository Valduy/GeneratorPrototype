using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Math = GameEngine.Mathematics.Math;

namespace GameEngine.Components
{
    public class RenderComponent : Component
    {
        private readonly Renderer _renderer;

        private Shape _shape = new(Enumerable.Empty<Vector2>());
        private RenderContext _context;

        public Shape Shape
        {
            get => _shape;
            set
            {
                if (_shape == value) return;
                _shape = value;
                _renderer.Unregister(_context);
                _context = _renderer.Register(GetVertices());
            }
        }

        /// <summary>
        /// Define render layer. Object on lower layer render behind object on higher layer.
        /// </summary>
        public int Layer { get; set; } = -10;

        public Vector3 Color { get; set; } = Colors.Gray;

        public RenderComponent(Renderer renderer)
        {
            _renderer = renderer;
            _context = _renderer.Register(GetVertices());
        }

        public override void RenderUpdate(FrameEventArgs args)
        {
            _renderer.Render(_context, Color, GameObject!.Scale, GameObject.Rotation, GameObject.Position, Layer);
        }

        private float[] GetVertices()
        {
            if (Shape.Count > 3)
            {
                var triangles = Math.Triangulate(Shape.ToArray());
                var result = new float[triangles.Length * 9];

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var offset = i * 9;
                    result[offset] = Shape[triangles[i]].X;
                    result[offset + 1] = Shape[triangles[i]].Y;
                    result[offset + 2] = 0;
                    result[offset + 3] = Shape[triangles[i + 1]].X;
                    result[offset + 4] = Shape[triangles[i + 1]].Y;
                    result[offset + 5] = 0;
                    result[offset + 6] = Shape[triangles[i + 2]].X;
                    result[offset + 7] = Shape[triangles[i + 2]].Y;
                    result[offset + 8] = 0;
                }

                return result;
            }
            else
            {
                var result = new float[Shape.Count * 3];

                for (int i = 0; i < Shape.Count; i++)
                {
                    int offset = i * 3;
                    result[offset] = Shape[i].X;
                    result[offset + 1] = Shape[i].Y;
                    result[offset + 2] = 0;
                }

                return result;
            }
        }
    }
}
