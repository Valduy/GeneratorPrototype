using OpenTK.Mathematics;
using Math = GameEngine.Mathematics.Math;

namespace RoadNetworkGenerator
{
    public class Vector2Comparer : IEqualityComparer<Vector2>
    {
        public bool Equals(Vector2 x, Vector2 y) 
            => Math.Equal(x, y, Constants.FloatEpsilon);

        public int GetHashCode(Vector2 obj) 
            => obj.GetHashCode();
    }
}
