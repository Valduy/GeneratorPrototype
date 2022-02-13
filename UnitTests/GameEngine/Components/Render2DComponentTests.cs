using System.Collections;
using System.Collections.Generic;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Game;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using Xunit;

namespace UnitTests.GameEngine.Components
{
    public class Render2DComponentTests
    {
        [Theory]
        [ClassData(typeof(GameObjectWithRenderComponentsGenerator))]
        public void Points_GameObjectHasSomeScaleRotationAndTranslation_PointsHaveCorrectCoords(GameObject go, Vector2[] expected)
        {
            var result = go.Get<Render2DComponent>()!.Points;

            Assert.Equal(result, expected);
        }
    }

    public class GameObjectWithRenderComponentsGenerator : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            var game = new Game();

            var go = game.Engine.CreateGameObject();
            go.Add(() => new Render2DComponent(game.Window.Renderer)
            {
                Shape = Shape2D.Square(2)
            });
            go.Scale = new Vector3(2.0f, 2.0f, 1.0f);
            go.Rotation = new Vector3(0.0f, 0.0f, MathHelper.Pi / 2);
            go.Position = new Vector3(4.0f, 4.0f, 0.0f);

            var expected = new[]
            {
                new Vector2(6, 2),
                new Vector2(6, 6),
                new Vector2(2, 6),
                new Vector2(2, 2),
            };

            yield return new object[]
            {
                go,
                expected,
            };
        }

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
    }
}
