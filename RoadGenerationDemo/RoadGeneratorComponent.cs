using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using Graph;
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

        private bool _isKeyPressed = false;

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
            foreach (var goal in _inputData.Goals.Append(_inputData.Start))
            {
                CreateTriangle(goal, Colors.Lime);
            }

            foreach (var point in _inputData.ImportantPoints)
            {
                CreateTriangle(point, Colors.Red);
            }
        }

        public override void GameUpdate(FrameEventArgs args)
        {
            if (_keyboardState.IsKeyDown(Keys.Space) /*&& !_isKeyPressed*/)
            {
                //_isKeyPressed = true;

                if (!_roadGenerator.Iterate())
                {
                    Console.WriteLine("Generation end.");
                }
            }
            
            //if (_keyboardState.IsKeyReleased(Keys.Space) && _isKeyPressed)
            //{
            //    _isKeyPressed = false;
            //}
        }

        private void OnConnected(object? source, ConnectionEventArgs<Sucessor> args)
        {
            var color = IsMain(args.Node1, args.Node2) ? Colors.Red : Colors.White;

            var roadGo = GameObject!.Engine.CreateGameObject();
            roadGo.Add(() => new Render2DComponent(_renderer)
            {
                Color =  color,
                Shape = Shape2D.Line(args.Node1.Item.Position, args.Node2.Item.Position)
            });
            roadGo.Position = Vector3.UnitZ * -10;
        }

        private bool IsMain(Node<Sucessor> node1, Node<Sucessor> node2) 
            => node1.Item.SucessorType is SucessorType.Main or SucessorType.Pivot 
            && node2.Item.SucessorType is SucessorType.Main or SucessorType.Pivot;        

        private void CreateTriangle(Vector2 position, Vector3 color)
        {
            var triangleGo = GameObject!.Engine.CreateGameObject();
            triangleGo.Add(() => new Render2DComponent(_renderer)
            {
                Shape = Shape2D.Triangle(20),
                Color = color,
            });
            triangleGo.Position = new Vector3(position.X, position.Y, -9.0f);
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
            ImportantPoints = new[]
            {
                new Vector2(500, 300),
                new Vector2(600, 800),
                new Vector2(50, 400),
                new Vector2(250, 650),
            },
        };
    }
}
