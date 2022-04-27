using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;

namespace RoadGenerationDemo
{
    class Program
    {
        public static void Main(string[] args)
        {
            using var game = new Engine();
            game.Camera.Projection = Projection.Orthographic;

            var operatorGo = game.CreateGameObject();
            operatorGo.Add<Operator2DComponent>();

            var generatorGo = game.CreateGameObject();
            generatorGo.Add<RoadGeneratorComponent>();

            game.Run();
        }
    }
}