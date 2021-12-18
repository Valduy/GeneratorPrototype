using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace GameEngine.Components
{
    public class RenderComponent : Component
    {
        private readonly Renderer _renderer;

        private Shape _shape = new(Enumerable.Empty<Vector2>());
        private int _layer = -10;
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
        public int Layer
        {
            get => _layer;
            set
            {
                if (_layer == value) return;
                _layer = value;
                _renderer.Unregister(_context);
                _context = _renderer.Register(GetVertices());
            }
        }

        public Vector3 Color { get; set; } = new(150);

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
            var result = new float[Shape.Count * 3];

            for (int i = 0; i < Shape.Count; i++)
            {
                int offset = i * 3;
                result[offset] = Shape[i].X;
                result[offset + 1] = Shape[i].Y;
                result[offset + 2] = Layer;
            }

            return result;
        }
    }
}
