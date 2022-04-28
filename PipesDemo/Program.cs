using System.Collections;
using GameEngine.Components;
using GameEngine.Core;
using OpenTK.Mathematics;
using Pipes.Models;
using PipesDemo.Components;

namespace PipesDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var engine = new Engine();
            engine.Light.Ambient = new(1);
            engine.Light.Diffuse = new(1);

            var model = new Grid();
            model.Load("Sample/House.bmp");

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            var cellLogger = operatorGo.Add<CellInfoLoggerComponent>();
            cellLogger.Model = model;
            operatorGo.Position = new Vector3(0, 20, 0);

            var builderGo = engine.CreateGameObject();
            var builder = builderGo.Add<BuilderComponent>();
            builder.Model = model;
            builder.GenerationSteps = new List<IEnumerator> 
            {
                builder.GenerateAStarPipe(
                    new Vector3i(
                        Grid.WallSpacing,
                        Grid.WallSpacing,
                        Grid.WallSpacing - 1),
                    new Vector3i(
                        model.Width - Grid.WallSpacing,
                        model.Height - Grid.WallSpacing - 1,
                        model.Depth - Grid.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        Grid.WallSpacing,
                        Grid.WallSpacing + 3,
                        Grid.WallSpacing - 1),
                    new Vector3i(
                        model.Width - Grid.WallSpacing,
                        model.Height - Grid.WallSpacing - 3,
                        model.Depth - Grid.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        Grid.WallSpacing,
                        Grid.WallSpacing + 5,
                        Grid.WallSpacing - 1),
                    new Vector3i(
                        model.Width - Grid.WallSpacing,
                        model.Height - Grid.WallSpacing - 5,
                        model.Depth - Grid.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        Grid.WallSpacing,
                        Grid.WallSpacing + 7,
                        Grid.WallSpacing - 1),
                    new Vector3i(
                        model.Width - Grid.WallSpacing,
                        model.Height - Grid.WallSpacing - 7,
                        model.Depth - Grid.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        Grid.WallSpacing,
                        Grid.WallSpacing + 11,
                        Grid.WallSpacing - 1),
                    new Vector3i(
                        model.Width - Grid.WallSpacing,
                        model.Height - Grid.WallSpacing - 11,
                        model.Depth - Grid.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        Grid.WallSpacing,
                        Grid.WallSpacing + 13,
                        Grid.WallSpacing - 1),
                    new Vector3i(
                        model.Width - Grid.WallSpacing,
                        model.Height - Grid.WallSpacing - 13,
                        model.Depth - Grid.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        Grid.WallSpacing,
                        Grid.WallSpacing + 15,
                        Grid.WallSpacing - 1),
                    new Vector3i(
                        model.Width - Grid.WallSpacing,
                        model.Height - Grid.WallSpacing - 15,
                        model.Depth - Grid.WallSpacing - 1)),
                builder.GenerateFlexiblePipe(
                    new Vector3i(
                        Grid.WallSpacing + 3,
                        Grid.WallSpacing + 7,
                        Grid.WallSpacing - 1),
                    new Vector3i(
                        model.Width - Grid.WallSpacing,
                        model.Height - Grid.WallSpacing - 7,
                        model.Depth - Grid.WallSpacing - 3)),
                builder.GenerateFlexiblePipe(
                    new Vector3i(
                        Grid.WallSpacing + 4,
                        Grid.WallSpacing + 10,
                        Grid.WallSpacing - 1),
                    new Vector3i(
                        model.Width - Grid.WallSpacing,
                        model.Height - Grid.WallSpacing - 9,
                        model.Depth - Grid.WallSpacing - 3)),
                //builder.GenerateFlexiblePipe(
                //    new Vector3i(2, 3, 1),
                //    new Vector3i(model.Width - 2, model.Height - 6, model.Depth - 3)),
                //builder.GenerateFlexiblePipe(
                //    new Vector3i(3, 10, 0),
                //    new Vector3i(model.Width - 1, model.Height - 4, model.Depth - 4)),
                //builder.GenerateFlexiblePipe(
                //    new Vector3i(3, 12, 0),
                //    new Vector3i(model.Width - 1, model.Height - 8, model.Depth - 4))
            };

            engine.Run();
        }
    }
}