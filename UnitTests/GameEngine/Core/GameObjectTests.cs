using System.ComponentModel;
using GameEngine.Core;
using Xunit;
using Component = GameEngine.Core.Component;

namespace UnitTests.GameEngine.Core
{
    public class TestComponent : Component
    {

    }

    public class GameObjectTests
    {
        // Generic Add method call not generic Add inside. That's mean we can test only generic option.

        [Fact]
        public void Add_ParametrizeGenericWithComponentType_ComponentAdded()
        {
            var engine = new Engine();
            var go = engine.CreateGameObject();

            go.Add<TestComponent>();

            Assert.NotNull(go.Get<TestComponent>());
        }


        [Fact]
        public void Remove_RemoveNotExistingComponent_Null()
        {
            var engine = new Engine();
            var go = engine.CreateGameObject();

            var component = go.Remove<TestComponent>();

            Assert.Null(component);
        }

        [Fact]
        public void Remove_RemoveExistingComponent_NotNull()
        {
            var engine = new Engine();
            var go = engine.CreateGameObject();

            go.Add<TestComponent>();
            var component = go.Remove<TestComponent>();

            Assert.NotNull(component);
        }

        [Fact]
        public void Add_ParametrizeGenericWithComponentType_FiredComponentAddedEvent()
        {
            var isFired = false;
            var engine = new Engine();
            var go = engine.CreateGameObject();

            go.ComponentAdded += (g, c) => isFired = true;
            go.Add<TestComponent>();

            Assert.True(isFired);
        }

        [Fact]
        public void Remove_RemoveNotExistingComponent_NotFiredComponentRemovedEvent()
        {
            var isFired = false;
            var engine = new Engine();
            var go = engine.CreateGameObject();

            go.ComponentRemoved += (g, c) => isFired = true;
            go.Remove<TestComponent>();

            Assert.False(isFired);
        }

        [Fact]
        public void Remove_RemoveExistingComponent_FiredComponentRemovedEvent()
        {
            var isFired = false;
            var engine = new Engine();
            var go = engine.CreateGameObject();

            go.ComponentRemoved += (g, c) => isFired = true;
            go.Add<TestComponent>();
            go.Remove<TestComponent>();

            Assert.True(isFired);
        }
    }
}
