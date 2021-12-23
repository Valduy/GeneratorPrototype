using System.Collections;
using System.Collections.Generic;
using GameEngine.Mathematics;
using OpenTK.Mathematics;
using Xunit;

namespace UnitTests.GameEngine.Mathematics
{
    public class MathTests
    {
        [Theory]
        [ClassData(typeof(NotIntersectedGenerator))]
        public void IsAABBIntersects_NotIntersectedAABBs_False(Vector2 position1, Vector2 size1, Vector2 position2, Vector2 size2)
        {
            var result = Math.IsAABBIntersects(position1, size1.X, size1.Y, position2, size2.X, size2.Y);

            Assert.False(result);
        }

        [Theory]
        [ClassData(typeof(IntersectedGenerator))]
        public void IsAABBIntersects_IntersectedAABBs_True(Vector2 position1, Vector2 size1, Vector2 position2, Vector2 size2)
        {
            var result = Math.IsAABBIntersects(position1, size1.X, size1.Y, position2, size2.X, size2.Y);

            Assert.True(result);
        }
    }

    public class NotIntersectedGenerator : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new Vector2(2.0f, 2.0f),
                new Vector2(4.0f, 2.0f),
                new Vector2(3.0f, 4.0f),
                new Vector2(4.0f, 1.0f),
            };
            yield return new object[]
            {
                new Vector2(4.0f, 2.0f),
                new Vector2(5.0f, 2.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(2.0f, 1.0f),
            };
        }

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }

    public class IntersectedGenerator : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new Vector2(2.0f, 2.0f),
                new Vector2(4.0f, 2.0f),
                new Vector2(3.0f, 2.5f),
                new Vector2(4.0f, 2.0f),
            };
            yield return new object[]
            {
                new Vector2(2.0f, 2.0f),
                new Vector2(4.0f, 2.0f),
                new Vector2(2.0f, 2.0f),
                new Vector2(2.0f, 2.0f),
            };
        }

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }
}
