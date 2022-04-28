using GameEngine.Components;
using GameEngine.Core;
using HeatFieldDemo.Components;
using OpenTK.Mathematics;
using Pipes.Components;
using Pipes.Models;

namespace HeatFieldDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var engine = new Engine();
            engine.Light.Ambient = new(1);
            engine.Light.Diffuse = new(1);

            var grid = new Grid(10, 10, 10);

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 10, 0);
            var cellLogger = operatorGo.Add<CellInfoLoggerComponent>();
            cellLogger.Grid = grid;

            
            var fieldGo = engine.CreateGameObject();
            var heatComponent = fieldGo.Add<HeatComponent>();
            heatComponent.Grid = grid;

            engine.Run();
        }
    }
}