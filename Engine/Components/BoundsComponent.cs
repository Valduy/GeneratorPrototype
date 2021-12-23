using GameEngine.Bounds;
using GameEngine.Core;
using OpenTK.Mathematics;
using Math = GameEngine.Mathematics.Math;

namespace GameEngine.Components
{
    public class BoundsComponent : Component
    {
        public RectangleBounds Bounds { get; set; }
        public float Rotation => GameObject!.Rotation;
        public Vector2 Position => GameObject!.Position + Bounds.Offset;

        public float Width => Bounds.Size.X;
        public float Height => Bounds.Size.Y;

        public BoundsComponent(RectangleBounds bounds)
        {
            Bounds = bounds;
        }

        // TODO: true poly intersection...
        public static bool IsIntersects(BoundsComponent bound1, BoundsComponent bound2)
        {
            throw new NotImplementedException();
        }
    }
}
