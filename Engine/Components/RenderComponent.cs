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
                    WriteVector2ToArray(new ArraySegment<float>(result, offset, 3), Shape[triangles[i]]);
                    WriteVector2ToArray(new ArraySegment<float>(result, offset + 3, 3), Shape[triangles[i + 1]]);
                    WriteVector2ToArray(new ArraySegment<float>(result, offset + 6, 3), Shape[triangles[i + 2]]);
                }

                return result;
            }
            else
            {
                var result = new float[Shape.Count * 3];

                for (int i = 0; i < Shape.Count; i++)
                {
                    WriteVector2ToArray(new ArraySegment<float>(result, i * 3, 3), Shape[i]);
                }

                return result;
            }
        }

        private void WriteVector2ToArray(ArraySegment<float> segment, Vector2 vertex)
        {
            segment[0] = vertex.X;
            segment[1] = vertex.Y;
            segment[2] = 0;
        }
    }
}
