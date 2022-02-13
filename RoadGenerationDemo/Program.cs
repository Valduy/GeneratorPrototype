using GameEngine.Components;
using GameEngine.Game;

namespace RoadGenerationDemo
{
    class Program
    {
        public static void Main(string[] args)
        {
            using var game = new Game();

            var operatorGo = game.Engine.CreateGameObject();
            operatorGo.Add(() => new OperatorComponent(game.Window));

            var generatorGo = game.Engine.CreateGameObject();
            generatorGo.Add(() => new RoadGeneratorComponent(game.Window.Renderer, game.Window.KeyboardState));

            game.Run();
        }
    }
}