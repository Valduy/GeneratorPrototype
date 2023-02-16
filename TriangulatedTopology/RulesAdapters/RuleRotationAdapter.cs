using GameEngine.Helpers;
using MeshTopology;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;

namespace TriangulatedTopology.RulesAdapters
{
    public enum RotationDirection
    {
        Positive,
        Negative,
    }

    public class CoordSystem
    {
        public Vector2i Origin;
        public Vector2i XAxis;
        public Vector2i YAxis;

        public CoordSystem(Vector2i origin, Vector2i xAxis, Vector2i yAxis)
        {
            Origin = origin;
            XAxis = xAxis;
            YAxis = yAxis;
        }
    }

    public class RuleRotationAdapter : RuleAdapter
    {
        private Vector2i _origin;
        private Vector2i _xAxis;
        private Vector2i _yAxis;

        public RuleRotationAdapter(Vector2i origin, Vector2i xAxis, Vector2i yAxis, int size) 
            : base(size)
        { 
            _origin = origin;
            _xAxis = xAxis;
            _yAxis = yAxis;
        }

        public override Color Access(Rule rule, int x, int y)
        {
            if (x < 0 || x >= Size)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (y < 0 || y >= Size)
            {
                throw new ArgumentOutOfRangeException(nameof(y));
            }

            var accessor = _origin + x * _xAxis + y * _yAxis;
            return rule.Logical[accessor.X, accessor.Y];
        }
    }
}
