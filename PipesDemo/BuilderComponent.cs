﻿using System.Collections;
using System.Drawing;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace PipesDemo
{
    public class BuilderComponent : Component
    {
        public const int InWidth = 8;
        public const int InHeight = 8;
        public const int FloorWidth = 16;
        public const int FloorHeight = 16;

        private BuildingModel buildingModel = new();
        private IEnumerator pipeGenerator;

        public string MapPath { get; set; }
        
        public override void Start()
        {
            buildingModel.WallCreated += OnWallCreated;
            buildingModel.PipeCreated += OnPipeCreated;
            buildingModel.Load(MapPath);
            pipeGenerator = buildingModel.GeneratePipes(
                new Vector3i(1, 1, 0), 
                new Vector3i(buildingModel.Width - 1, buildingModel.Height -1, buildingModel.Depth - 1))
                .GetEnumerator();
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (GameObject!.Engine.Window.KeyboardState.IsKeyDown(Keys.Enter))
            {
                pipeGenerator.MoveNext();
            }
        }

        private void OnWallCreated(Cell cell)
        {
            var cellGo = GameObject!.Engine.CreateGameObject();
            var render = cellGo.Add<Render3DComponent>();
            render.Shape = new Mesh(Mesh.Cube);
            GameObject!.AddChild(cellGo);
            cellGo.Position = cell.Position;
        }

        private void OnPipeCreated(Cell cell)
        {
            var cellGo = GameObject!.Engine.CreateGameObject();
            var render = cellGo.Add<Render3DComponent>();
            render.Shape = new Mesh(Mesh.Cube);
            render.Material.Ambient = new Vector3(1.0f, 0.5f, 0.31f);
            render.Material.Diffuse = new Vector3(1.0f, 0.5f, 0.31f);
            render.Material.Specular = new Vector3(0.5f, 0.5f, 0.5f);
            render.Material.Shininess = 32.0f;
            GameObject!.AddChild(cellGo);
            cellGo.Position = cell.Position;
        }
    }
}