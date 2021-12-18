using GameEngine.Components;
using GameEngine.Game;
using GameEngine.Graphics;
using OpenTK.Mathematics;

namespace GeneratorPrototype
{
    class Program
    {
        public static void Main(string[] args)
        {
            using var game = new Game();

            var operatorGo = game.Engine.CreateGameObject();
            operatorGo.Add(() => new OperatorComponent(game.Window));

            var go = game.Engine.CreateGameObject();
            go.Add(() => new RenderComponent(game.Window.Renderer)
            {
                Shape = new Shape(new[]
                {
                    new Vector2(-200f, -200f),
                    new Vector2(200f, -200f),
                    new Vector2(0f, 200f)
                })
            });
            
            game.Run();
        }
    }
}