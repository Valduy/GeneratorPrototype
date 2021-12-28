﻿using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using Net;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RoadNetworkGenerator;

namespace RoadGenerationDemo
{
    public class RoadGeneratorComponent : Component
    {
        private readonly Renderer _renderer;
        private readonly KeyboardState _keyboardState;

        private RoadGenerator _roadGenerator;
        private InputData _inputData;

        public RoadGeneratorComponent(Renderer renderer, KeyboardState keyboardState)
        {
            _renderer = renderer;
            _keyboardState = keyboardState;
            _inputData = CreateInputData();
            _roadGenerator = new RoadGenerator(_inputData);
            _roadGenerator.Net.Connected += OnConnected;
        }

        public override void Start()
        {
            CreateTriangle(_inputData.Start);

            foreach (var goal in _inputData.Goals)
            {
                CreateTriangle(goal);
            }
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (!_keyboardState.IsKeyDown(Keys.Space)) return;

            if (!_roadGenerator.Iterate())
            {
                Console.WriteLine("Generation end.");
            }
        }

        private void OnConnected(object? source, ConnectionEventArgs<Sucessor> args)
        {
            var roadGo = GameObject!.Engine.CreateGameObject();
            roadGo.Add(() => new RenderComponent(_renderer)
            {
                Color = Colors.Gray,
                Shape = Shape.Line(args.Node1.Item.Position, args.Node2.Item.Position)
            });
        }

        private void CreateTriangle(Vector2 position)
        {
            var triangleGo = GameObject!.Engine.CreateGameObject();
            triangleGo.Add(() => new RenderComponent(_renderer)
            {
                Shape = Shape.Triangle(20),
                Color = Colors.Lime,
                Layer = -9,
            });
            triangleGo.Position = position;
        }

        private static InputData CreateInputData() => new()
        {
            Start = new Vector2(100.0f, 100.0f),
            Goals = new[]
            {
                new Vector2(900.0f, 200f),
                new Vector2(700.0f, 700.0f),
                new Vector2(150.0f, 800.0f)
            },
            SegmentLength = 50,
        };
    }
}
