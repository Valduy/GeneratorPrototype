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
    public class RenderComponentTests
    {
        [Theory]
        [ClassData(typeof(GameObjectWithRenderComponentsGenerator))]
        public void Points_GameObjectHasSomeScaleRotationAndTranslation_PointsHaveCorrectCoords(GameObject go, Vector2[] expected)
        {
            var result = go.Get<RenderComponent>()!.Points;

            Assert.Equal(result, expected);
        }
    }

    public class GameObjectWithRenderComponentsGenerator : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            var game = new Game();

            var go = game.Engine.CreateGameObject();
            go.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Shape = Shape.Square(2)
            });
            go.Scale = new Vector2(2.0f, 2.0f);
            go.Rotation = 90;
            go.Position = new Vector2(4.0f, 4.0f);

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
