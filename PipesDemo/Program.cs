using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using PipesDemo.Components;
using PipesDemo.Models;

namespace PipesDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var engine = new Engine();
            engine.Light.Ambient = new(1);
            engine.Light.Diffuse = new(1);

            var buildingModel = new BuildingModel();
            buildingModel.Load("Sample/House.bmp");

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            var cellLogger = operatorGo.Add<CellInfoLoggerComponent>();
            cellLogger.Model = buildingModel;
            operatorGo.Position = new Vector3(0, 20, 0);

            var builderGo = engine.CreateGameObject();
            var builder = builderGo.Add<BuilderComponent>();
            builder.Model = buildingModel;

            engine.Run();
        }
    }
}