using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Utils;
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

            var field = new Grid(10, 10, 10);

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 10, 0);
            var cellLogger = operatorGo.Add<CellInfoLoggerComponent>();
            cellLogger.Grid = field;

            
            var fieldGo = engine.CreateGameObject();
            var heatComponent = fieldGo.Add<HeatComponent>();
            heatComponent.Grid = field;

            var grid = engine.Grid(10);
            grid.Position = new Vector3(5.0f, 0.0f, 5.0f);
            
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-1.0f, 0.0f, -1.0f);

            engine.Run();
        }
    }
}