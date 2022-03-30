﻿using System.Collections;
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

            var model = new BuildingModel();
            model.Load("Sample/House.bmp");

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            //var cellLogger = operatorGo.Add<CellInfoLoggerComponent>();
            //cellLogger.Model = buildingModel;
            operatorGo.Position = new Vector3(0, 20, 0);

            var builderGo = engine.CreateGameObject();
            var builder = builderGo.Add<BuilderComponent>();
            builder.Model = model;
            builder.GenerationSteps = new List<IEnumerator> 
            {
                builder.GenerateAStarPipe(
                    new Vector3i(1, 1, 0),
                    new Vector3i(model.Width - 1, model.Height - 1, model.Depth - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(1, 3, 0),
                    new Vector3i(model.Width - 1, model.Height - 3, model.Depth - 1)),
                builder.GenerateAStarPipe(
                    new Vector3i(1, 5, 0),
                    new Vector3i(model.Width - 1, model.Height - 5, model.Depth - 1)),
                builder.GenerateRigidPipe(
                    new Vector3i(3, 8, 0),
                    new Vector3i(model.Width - 1, model.Height - 7, model.Depth - 4))
            };

            engine.Run();
        }

        //_pipeGenerator1 = GenerateRigidPipe(
        //new Vector3i(1, 1, 0),
        //new Vector3i(Model!.Width - 1, Model!.Height - 1, Model!.Depth - 1));

        //_buildingModel.GenerateGraphBasePipe(
        //    new Vector3i(1, 1, 0),
        //    new Vector3i(_buildingModel.Width - 1, _buildingModel.Height - 1, _buildingModel.Depth - 1));

        // TODO: this
        //_pipeGenerator1 = Model.GeneratePipes(
        //    new Vector3i(1, 1, 0),
        //    new Vector3i(Model.Width - 1, Model.Height - 1, Model.Depth - 1)
        //    //new Vector3i(_buildingModel.Width - 10, _buildingModel.Height -7, _buildingModel.Depth - 10)
        //    )
        //    .GetEnumerator();

        //_pipeGenerator2 = _buildingModel.GeneratePipes(
        //        new Vector3i(3, 1, 0),
        //        new Vector3i(_buildingModel.Width - 7, _buildingModel.Height - 5, _buildingModel.Depth - 1)
        //    )
        //    .GetEnumerator();

        //_pipeGenerator1 = Model!.GenerateFlexiblePipe(
        //    new Vector3i(1, 1, 0),
        //    new Vector3i(Model!.Width - 7, Model!.Height - 5, Model!.Depth - 1))
        //    .GetEnumerator();

        //_pipeGenerator2 = _buildingModel.GenerateSpline(
        //        new Vector3i(1, 3, 0),
        //        new Vector3i(_buildingModel.Width - 1, _buildingModel.Height - 3, _buildingModel.Depth - 1))
        //    .GetEnumerator();
    }
}