using System.Drawing;
using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Utils;
using OpenTK.Mathematics;
using Mesh = GameEngine.Graphics.Mesh;

namespace FacesDemo
{
    public class TopologyNode
    {
        public Rule Rule { get; set; }
        public Face Face { get; set; }
        public List<TopologyNode> Neighbours { get; set; }

        public TopologyNode(Face face)
        {
            Rule = new Rule();
            Face = face;
            Neighbours = new List<TopologyNode>();
        }
    }

    public class Rule
    {
        public List<int> Indices = new();
    }

    class Program
    {
        public static List<Face> ExtractFaces(Mesh mesh)
        {
            var result = new List<Face>();

            for (int i = 0; i < mesh.Indices.Count; i += 4)
            {
                result.Add(new Face(new List<Vertex>
                {
                    mesh.Vertices[i + 0],
                    mesh.Vertices[i + 1],
                    mesh.Vertices[i + 2],
                    mesh.Vertices[i + 3]
                }));
            }

            return result;
        }

        public static List<TopologyNode> CreateTopology(List<Face> faces)
        {
            var result = new List<TopologyNode>();

            // Initialize topology nodes.
            foreach (var face in faces)
            {
                result.Add(new TopologyNode(face));
            }

            // Link neighbours.
            foreach (var node in result)
            {
                foreach (var neighbour in result)
                {
                    if (node == neighbour)
                    {
                        continue;
                    }

                    if (node.Face.IsSharedEdgeExist(neighbour.Face))
                    {
                        node.Neighbours.Add(neighbour);
                    }
                }
            }

            // Sort neighbours.
            foreach (var node in result)
            {
                var sortedEdges = node.Face.EnumerateEdges().ToList();
                node.Neighbours.Sort((n1, n2) =>
                {
                    int i1 = node.Face.GetEdgeIndex(n1.Face.GetSharedEdge(node.Face));
                    int i2 = node.Face.GetEdgeIndex(n2.Face.GetSharedEdge(node.Face));
                    return i1.CompareTo(i2);
                });
            }

            return result;
        }

        public static void Wfc(List<TopologyNode> topology)
        {
            var rules = new[]
            {
                new Rule {Indices = {0}},
                new Rule {Indices = {1}},
                new Rule {Indices = {2}},
                new Rule {Indices = {3}},
                new Rule {Indices = {0, 1}},
                new Rule {Indices = {1, 2}},
                new Rule {Indices = {2, 3}},
                new Rule {Indices = {3, 0}},
                new Rule {Indices = {0, 2}},
                new Rule {Indices = {1, 3}},
                new Rule {Indices = {0, 1, 2}},
                new Rule {Indices = {1, 2, 3}},
                new Rule {Indices = {2, 3, 0}},
                new Rule {Indices = {3, 0, 1}},
                new Rule {Indices = {0, 1, 2, 3}},
            };

            var possibilities = new Dictionary<TopologyNode, List<Rule>>();
            var forRecalculation = new List<TopologyNode>();

            foreach (var node in topology)
            {
                possibilities[node] = new List<Rule>(rules);
            }

            var initial = topology.GetRandom();
            var rule = possibilities[initial].GetRandom();
            possibilities[initial] = new List<Rule> {rule};
            forRecalculation.AddRange(initial.Neighbours);

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

                        foreach (var neighbour in node.Neighbours)
                        {
                            possibilities[neighbour] = new List<Rule>(rules);
                        }

                        continue;
                    }

                    if (possibleHere.Count > filtered.Count)
                    {
                        forRecalculation.AddRange(node.Neighbours);
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
                possibilities[maxNode] = new List<Rule> { rule };
                forRecalculation.AddRange(maxNode.Neighbours);
            }

            foreach (var pair in possibilities)
            {
                pair.Key.Rule = pair.Value[0];
            }
        }

        public static List<Rule> FilterPossible(
            Dictionary<TopologyNode, List<Rule>> possibilities,
            List<Rule> rules,
            TopologyNode node)
            => rules.Where(r => IsPossible(possibilities, r, node)).ToList();
        
        public static bool IsPossible(
            Dictionary<TopologyNode, List<Rule>> possibilities,
            Rule rule,
            TopologyNode node)
        {
            for (int i = 0; i < node.Neighbours.Count; i++)
            {
                State state = GetState(possibilities, node.Neighbours[i], node);

                if (state == State.Connected && !rule.Indices.Contains(i))
                {
                    return false;
                }
                if (state == State.Blocked && rule.Indices.Contains(i))
                {
                    return false;
                }
            }

            return true;
        }

        public enum State
        {
            Connected,
            Blocked,
            Undefined,
        }

        public static State GetState(
            Dictionary<TopologyNode, List<Rule>> possibilities, 
            TopologyNode pivot, 
            TopologyNode node)
        {
            var index = pivot.Neighbours.IndexOf(node);
            var rules = possibilities[pivot];
            State state = State.Undefined;

            if (rules.All(r => r.Indices.Contains(index)))
            {
                state = State.Connected;
            }
            if (rules.All(r => !r.Indices.Contains(index)))
            {
                state = State.Blocked;
            }

            return state;
        }

        public static GameObject CreateTopologyVisualization(Engine engine, List<TopologyNode> topology)
        {
            var go = engine.CreateGameObject();

            foreach (var node in topology)
            {
                foreach (var edge in node.Face.EnumerateEdges())
                {
                    var line = engine.Line(edge.A, edge.B, Colors.Green);
                    go.AddChild(line);
                }
            }

            foreach (var node in topology)
            {
                var centroid = node.Face.Vertices
                    .Select(v => v.Position)
                    .Aggregate((p1, p2) => p1 + p2) / node.Face.Vertices.Count;

                foreach (var index in node.Rule.Indices)
                {
                    var edge = node.Face.GetEdgeByIndex(index);
                    var from = (edge.A + edge.B) / 2;
                    var line = engine.Line(from, centroid, Colors.Blue);
                    go.AddChild(line);
                }
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
            var faces = ExtractFaces(quadModel.Meshes[0]);
            var topology = CreateTopology(faces);
            Wfc(topology);
            var visualization = CreateTopologyVisualization(engine, topology);
            visualization.Position = Vector3.UnitY;

            engine.Run();
        }
    }
}