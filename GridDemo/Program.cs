using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
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
            public Vector3 Color { get; set; }
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
                    node1 = graph.Add(new Node { Color = Colors.Black, Position = vertex1.Position });
                    nodes[vertex1.Position] = node1;
                }
                if (!nodes.TryGetValue(vertex2.Position, out var node2))
                {
                    node2 = graph.Add(new Node { Color = Colors.Black, Position = vertex2.Position });
                    nodes[vertex2.Position] = node2;
                }
                if (!nodes.TryGetValue(vertex3.Position, out var node3))
                {
                    node3 = graph.Add(new Node { Color = Colors.Black, Position = vertex3.Position });
                    nodes[vertex3.Position] = node3;
                }
                if (!nodes.TryGetValue(vertex4.Position, out var node4))
                {
                    node4 = graph.Add(new Node { Color = Colors.Black, Position = vertex4.Position });
                    nodes[vertex4.Position] = node4;
                }

                if (!graph.IsLinked(node1, node2))
                {
                    graph.Link(node1, node2);
                }
                if (!graph.IsLinked(node2, node3))
                {
                    graph.Link(node2, node3);
                }
                if (!graph.IsLinked(node3, node4))
                {
                    graph.Link(node3, node4);
                }
                if (!graph.IsLinked(node4, node1))
                {
                    graph.Link(node4, node1);
                }
            }

            return graph;
        }

        public static void FindColorsWithWfc(UndirectedGraph<Node> graph)
        {
            //var rules = new[]
            //{
            //    new Rule {Color = Colors.Blue, Neighbours = new List<Vector3> {Colors.Green, Colors.Yellow, Colors.White}},
            //    new Rule {Color = Colors.Green, Neighbours = new List<Vector3> {Colors.Blue, Colors.Yellow, Colors.White}},
            //    new Rule {Color = Colors.Yellow, Neighbours = new List<Vector3> {Colors.Green, Colors.Blue, Colors.White}},
            //    new Rule {Color = Colors.White, Neighbours = new List<Vector3> {Colors.Blue, Colors.Green, Colors.Yellow}},
            //};
            var rules = new[]
            {
                new Rule {Color = Colors.Blue, Neighbours = new List<Vector3> {Colors.Green, Colors.Yellow}},
                new Rule {Color = Colors.Green, Neighbours = new List<Vector3> {Colors.Blue, Colors.Yellow}},
                new Rule {Color = Colors.Yellow, Neighbours = new List<Vector3> {Colors.Green, Colors.Blue}},
            };

            var possibilities = new Dictionary<GraphNode<Node>, List<Rule>>();
            var forRecalculation = new List<GraphNode<Node>>();

            foreach (var node in graph.Nodes)
            {
                possibilities[node] = new List<Rule>(rules);
            }

            var nodes = graph.Nodes.ToList();
            var initial = nodes.GetRandom();
            var rule = possibilities[initial].GetRandom();
            possibilities[initial] = new List<Rule> {rule};

            forRecalculation.AddRange(initial.Linked);

            while (true)
            {
                while (forRecalculation.Count > 0)
                {
                    var node = forRecalculation[0];
                    forRecalculation.Remove(node);

                    var possibleHere = possibilities[node];
                    var filtered = FilterPossible(possibilities, possibleHere, node);

                    // Deadlock resolution
                    if (filtered.Count == 0)
                    {
                        possibilities[node] = new List<Rule>(rules);

                        foreach (var linked in node.Linked)
                        {
                            possibilities[linked] = new List<Rule>(rules);
                        }

                        continue;
                    }
                    
                    if (possibleHere.Count > filtered.Count)
                    {
                        forRecalculation.AddRange(node.Linked);
                    }

                    possibilities[node] = filtered;
                }

                var maxNode = possibilities.First().Key;
                int maxPossibilities = possibilities.First().Value.Count;

                foreach (var pair in possibilities)
                {
                    if (pair.Value.Count > maxPossibilities)
                    {
                        maxNode = pair.Key;
                        maxPossibilities = pair.Value.Count;
                    }
                }

                if (maxPossibilities <= 1)
                {
                    break;
                }

                rule = possibilities[maxNode].GetRandom();
                possibilities[maxNode] = new List<Rule> {rule};
                forRecalculation.AddRange(maxNode.Linked);
            }

            foreach (var pair in possibilities)
            {
                pair.Key.Data.Color = pair.Value[0].Color;
            }
        }

        public static List<Rule> FilterPossible(
            Dictionary<GraphNode<Node>, List<Rule>> possibilities, 
            List<Rule> rules, 
            GraphNode<Node> node) 
            => rules.Where(r => IsPossible(possibilities, r, node)).ToList();

        public static bool IsPossible(
            Dictionary<GraphNode<Node>, List<Rule>> possibilities, 
            Rule rule, 
            GraphNode<Node> node)
        {
            foreach (var linked in node.Linked)
            {
                var rules = possibilities[linked];

                if (rules.All(r => !rule.Neighbours.Contains(r.Color)))
                {
                    return false;
                }
            }

            return true;
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
                render.Material.Color = node.Data.Color;
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
            FindColorsWithWfc(graph);
            var visualization = CreateMeshVisualization(engine, graph);
            visualization.Position = new Vector3(0, 1, 0);

            engine.Run();
        }
    }
}