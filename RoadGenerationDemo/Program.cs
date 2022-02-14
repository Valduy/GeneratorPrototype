using GameEngine.Components;
using GameEngine.Game;
using GameEngine.Graphics;

namespace RoadGenerationDemo
{
    class Program
    {
        public static void Main(string[] args)
        {
            using var game = new Game();
            game.Window.Renderer.Camera.Projection = Projection.Orthographic;

            var operatorGo = game.Engine.CreateGameObject();
            operatorGo.Add(() => new OperatorComponent(game.Window));

            var generatorGo = game.Engine.CreateGameObject();
            generatorGo.Add(() => new RoadGeneratorComponent(game.Window.Renderer, game.Window.KeyboardState));

            game.Run();
        }
    }
}