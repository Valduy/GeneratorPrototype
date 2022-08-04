using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Utils;
using GridDemo.Graph;
using OpenTK.Mathematics;
using Mesh = GameEngine.Graphics.Mesh;

namespace GridDemo
{
    class Program
    {

        public class Node
        {
            public Vector3 Position { get; set; }
        }

        public static UndirectedGraph<Node> CreateGraph(Mesh mesh)
        {
            var nodes = new Dictionary<Vector3, GraphNode<Node>>();
            var graph = new UndirectedGraph<Node>();

            for (int i = 0; i < mesh.Indices.Count; i += 4)
            {
                var vertex1 = mesh.Vertices[i + 0];
                var vertex2 = mesh.Vertices[i + 1];
                var vertex3 = mesh.Vertices[i + 2];
                var vertex4 = mesh.Vertices[i + 3];

                if (!nodes.TryGetValue(vertex1.Position, out var node1))
                {
                    node1 = graph.Add(new Node { Position = vertex1.Position });
                    nodes[vertex1.Position] = node1;
                }
                if (!nodes.TryGetValue(vertex2.Position, out var node2))
                {
                    node2 = graph.Add(new Node { Position = vertex2.Position });
                    nodes[vertex2.Position] = node2;
                }
                if (!nodes.TryGetValue(vertex3.Position, out var node3))
                {
                    node3 = graph.Add(new Node { Position = vertex3.Position });
                    nodes[vertex3.Position] = node3;
                }
                if (!nodes.TryGetValue(vertex4.Position, out var node4))
                {
                    node4 = graph.Add(new Node { Position = vertex4.Position });
                    nodes[vertex4.Position] = node4;
                }

                graph.Link(node1, node2);
                graph.Link(node2, node3);
                graph.Link(node3, node4);
                graph.Link(node4, node1);
            }

            return graph;
        }

        public static GameObject CreateMeshVisualization(Engine engine, UndirectedGraph<Node> graph)
        {
            var go = engine.CreateGameObject();

            foreach (var (a, b) in graph.GetEdges())
            {
                var line = engine.Line(a.Data.Position, b.Data.Position, Colors.Lime);
                go.AddChild(line);
            }

            foreach (var node in graph.Nodes)
            {
                var cube = engine.CreateGameObject();
                var render = cube.Add<MaterialRenderComponent>();
                render.Model = Model.Cube;
                cube.Scale = new Vector3(0.1f);
                cube.Position = node.Data.Position;
                go.AddChild(cube);
            }

            return go;
        }

        public static void Main(string[] args)
        {
            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);

            var quadModel = Model.Load("Content/Structure.obj", PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);
            var graph = CreateGraph(quadModel.Meshes[0]);
            var visualization = CreateMeshVisualization(engine, graph);
            visualization.Position = new Vector3(0, 1, 0);

            engine.Run();
        }
    }
}