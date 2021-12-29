using GameEngine.Components;
using GameEngine.Game;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace RoadGenerationDemo
{
    class Program
    {
        public const int MapWidth = 1000;
        public const int MapHeight = 1000;

        public static void Main(string[] args)
        {
            using var game = new Game();

            //var backgroundGo = game.Engine.CreateGameObject();
            //backgroundGo.Add(() => new RenderComponent(game.Window.Renderer)
            //{
            //    Shape = new Shape(new []
            //    {
            //        new Vector2(0, 0),
            //        new Vector2(MapHeight, 0),
            //        new Vector2(MapHeight, MapWidth),
            //        new Vector2(0, MapWidth),
            //    }),
            //    Color = Colors.Gray,
            //    Layer = -99, // Very-very far...
            //});

            var operatorGo = game.Engine.CreateGameObject();
            operatorGo.Add(() => new OperatorComponent(game.Window));

            var generatorGo = game.Engine.CreateGameObject();
            generatorGo.Add(() => new RoadGeneratorComponent(game.Window.Renderer, game.Window.KeyboardState));

            game.Run();
        }
    }
}