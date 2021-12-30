using System;
using System.Collections;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Xunit;
using Math = GameEngine.Mathematics.Math;

namespace UnitTests.GameEngine.Mathematics
{
    public class MathTests
    {
        public static IEnumerable<object[]> VectorsAndAnglesGenerator
        {
            get
            {
                yield return new object[]
                {
                    new Vector2(2, 0), new Vector2(0, 3), 90
                };
                yield return new object[]
                {
                    new Vector2(2, 4 * MathF.Cos(MathHelper.DegreesToRadians(30))), new Vector2(2, 0), 60
                };
            }
        }

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

        [Theory]
        [ClassData(typeof(PointOutsidePolygonGenerator))]
        public void IsPointInConvexPolygon_PointNotInsidePolygon_False(Vector2 point, Vector2[] polygon)
        {
            var result = Math.IsPointInConvexPolygon(point, polygon);

            Assert.False(result);
        }

        [Theory]
        [ClassData(typeof(PointInsidePolygonGenerator))]
        public void IsPointInConvexPolygon_PointInsidePolygon_True(Vector2 point, Vector2[] polygons)
        {
            var result = Math.IsPointInConvexPolygon(point, polygons);

            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(VectorsAndAnglesGenerator))]
        public void Angle_Vectors_CorrectAngle(Vector2 a, Vector2 b, float angle)
        {
            var result = MathHelper.RadiansToDegrees(Math.Angle(a, b));

            Assert.Equal(result, angle, 2);
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

    public class PointOutsidePolygonGenerator : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new Vector2(6, 5),
                new[]
                {
                    new Vector2(1, 2),
                    new Vector2(6, 3),
                    new Vector2(3, 7),
                    new Vector2(1, 3),
                }
            };
            yield return new object[]
            {
                new Vector2(-2, -1),
                new[]
                {
                    new Vector2(-2, 0),
                    new Vector2(0, -3),
                    new Vector2(3, -2),
                    new Vector2(3, 2),
                    new Vector2(0, 3),
                }
            };
        }

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }

    public class PointInsidePolygonGenerator : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new Vector2(2, 3),
                new[]
                {
                    new Vector2(1, 2),
                    new Vector2(6, 3),
                    new Vector2(3, 7),
                    new Vector2(1, 3),
                }
            };
            yield return new object[]
            {
                new Vector2(-1, -1),
                new[]
                {
                    new Vector2(-2, 0),
                    new Vector2(0, -3),
                    new Vector2(3, -2),
                    new Vector2(3, 2),
                    new Vector2(0, 3),
                }
            };
        }

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }
}
