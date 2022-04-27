using GameEngine.Mathematics;
using OpenTK.Mathematics;

namespace RoadNetworkGenerator
{
    public class Vector2Comparer : IEqualityComparer<Vector2>
    {
        public bool Equals(Vector2 x, Vector2 y) 
            => Mathematics.Equal(x, y, Constants.FloatEpsilon);

        public int GetHashCode(Vector2 obj) 
            => obj.GetHashCode();
    }
}
