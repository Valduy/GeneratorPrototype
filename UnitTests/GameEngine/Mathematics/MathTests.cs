using GameEngine.Mathematics;
using OpenTK.Mathematics;
using Xunit;

namespace UnitTests.GameEngine.Mathematics
{
    public class MathTests
    {
        [Fact]
        public void IsAABBIntersects_NotIntersectedAABBs_False()
        {
            var position1 = new Vector2(2.0f, 2.0f);
            var size1 = new Vector2(4.0f, 2.0f);

            var position2 = new Vector2(3.0f, 4.0f);
            var size2 = new Vector2(4.0f, 1.0f);

            var result = Math.IsAABBIntersects(position1, size1.X, size1.Y, position2, size2.X, size2.Y);

            Assert.False(result);
        }

        [Fact]
        public void IsAABBIntersects_IntersectedAABBs_True()
        {
            var position1 = new Vector2(2.0f, 2.0f);
            var size1 = new Vector2(4.0f, 2.0f);

            var position2 = new Vector2(3.0f, 2.5f);
            var size2 = new Vector2(4.0f, 2.0f);

            var result = Math.IsAABBIntersects(position1, size1.X, size1.Y, position2, size2.X, size2.Y);

            Assert.True(result);
        }
    }
}
