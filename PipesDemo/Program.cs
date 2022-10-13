using System.Collections;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Utils;
using OpenTK.Mathematics;
using Pipes.Components;
using Pipes.Models;
using Pipes.Utils;
using PipesDemo.Components;

namespace PipesDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var engine = new Engine();

            var field = new Grid(
                SampleLoader.FloorWidth + SampleLoader.WallSpacing * 2,
                SampleLoader.BuildingHeight + SampleLoader.WallSpacing * 2,
                SampleLoader.FloorDepth + SampleLoader.WallSpacing * 2);
            field.Load("Sample/House.bmp");

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 20, 0);
            var cellLogger = operatorGo.Add<CellInfoLoggerComponent>();
            cellLogger.Grid = field;

            var builderGo = engine.CreateGameObject();
            var builder = builderGo.Add<BuilderComponent>();
            builder.Grid = field;
            builder.GenerationSteps = new List<IEnumerator> 
            {
                builder.GenerateAStarPipe(
                    new Vector3i(
                        SampleLoader.WallSpacing,
                        SampleLoader.WallSpacing,
                        SampleLoader.WallSpacing - 1),
                    new Vector3i(
                        field.Width - SampleLoader.WallSpacing,
                        field.Height - SampleLoader.WallSpacing - 1,
                        field.Depth - SampleLoader.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        SampleLoader.WallSpacing,
                        SampleLoader.WallSpacing + 3,
                        SampleLoader.WallSpacing - 1),
                    new Vector3i(
                        field.Width - SampleLoader.WallSpacing,
                        field.Height - SampleLoader.WallSpacing - 3,
                        field.Depth - SampleLoader.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        SampleLoader.WallSpacing,
                        SampleLoader.WallSpacing + 5,
                        SampleLoader.WallSpacing - 1),
                    new Vector3i(
                        field.Width - SampleLoader.WallSpacing,
                        field.Height - SampleLoader.WallSpacing - 5,
                        field.Depth - SampleLoader.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        SampleLoader.WallSpacing,
                        SampleLoader.WallSpacing + 7,
                        SampleLoader.WallSpacing - 1),
                    new Vector3i(
                        field.Width - SampleLoader.WallSpacing,
                        field.Height - SampleLoader.WallSpacing - 7,
                        field.Depth - SampleLoader.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        SampleLoader.WallSpacing,
                        SampleLoader.WallSpacing + 11,
                        SampleLoader.WallSpacing - 1),
                    new Vector3i(
                        field.Width - SampleLoader.WallSpacing,
                        field.Height - SampleLoader.WallSpacing - 11,
                        field.Depth - SampleLoader.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        SampleLoader.WallSpacing,
                        SampleLoader.WallSpacing + 13,
                        SampleLoader.WallSpacing - 1),
                    new Vector3i(
                        field.Width - SampleLoader.WallSpacing,
                        field.Height - SampleLoader.WallSpacing - 13,
                        field.Depth - SampleLoader.WallSpacing - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(
                        SampleLoader.WallSpacing,
                        SampleLoader.WallSpacing + 15,
                        SampleLoader.WallSpacing - 1),
                    new Vector3i(
                        field.Width - SampleLoader.WallSpacing,
                        field.Height - SampleLoader.WallSpacing - 15,
                        field.Depth - SampleLoader.WallSpacing - 1)),
                builder.GenerateFlexiblePipe(
                    new Vector3i(
                        SampleLoader.WallSpacing + 3,
                        SampleLoader.WallSpacing + 7,
                        SampleLoader.WallSpacing - 1),
                    new Vector3i(
                        field.Width - SampleLoader.WallSpacing,
                        field.Height - SampleLoader.WallSpacing - 7,
                        field.Depth - SampleLoader.WallSpacing - 3)),
                builder.GenerateFlexiblePipe(
                    new Vector3i(
                        SampleLoader.WallSpacing + 4,
                        SampleLoader.WallSpacing + 10,
                        SampleLoader.WallSpacing - 1),
                    new Vector3i(
                        field.Width - SampleLoader.WallSpacing,
                        field.Height - SampleLoader.WallSpacing - 9,
                        field.Depth - SampleLoader.WallSpacing - 3)),
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

            var grid = engine.Grid(30);
            grid.Position = new Vector3(
                (SampleLoader.FloorWidth + SampleLoader.WallSpacing * 2) / 2,
                SampleLoader.WallSpacing - 1.0f, 
                (SampleLoader.FloorDepth + SampleLoader.WallSpacing * 2) / 2);

            var axis = engine.Axis(2);
            axis.Position = new Vector3(-3.0f, SampleLoader.WallSpacing - 1.0f, -3.0f);

            engine.Run();
        }
    }
}