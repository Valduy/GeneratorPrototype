using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Helpers;
using GameEngine.Utils;
using OpenTK.Mathematics;

namespace TriangulatedTopology
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var random = new Random();
            int seed = random.Next();
            Console.WriteLine(seed);
            Utils.UseSeed(seed);

            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);

            engine.Run();
        }
    }
}