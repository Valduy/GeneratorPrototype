using System.Drawing;
using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;

namespace FacesDemo
{
    public class Rule
    {
        public List<int> Indices = new();
    }

    class Program
    {
        public static void Wfc(Topology<Rule> topology)
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

            var possibilities = new Dictionary<TopologyNode<Rule>, List<Rule>>();
            var forRecalculation = new List<TopologyNode<Rule>>();

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
                pair.Key.Data = pair.Value[0];
            }
        }

        public static List<Rule> FilterPossible(
            Dictionary<TopologyNode<Rule>, List<Rule>> possibilities,
            List<Rule> rules,
            TopologyNode<Rule> node)
            => rules.Where(r => IsPossible(possibilities, r, node)).ToList();
        
        public static bool IsPossible(
            Dictionary<TopologyNode<Rule>, List<Rule>> possibilities,
            Rule rule,
            TopologyNode<Rule> node)
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
            Dictionary<TopologyNode<Rule>, List<Rule>> possibilities, 
            TopologyNode<Rule> pivot, 
            TopologyNode<Rule> node)
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

        public static GameObject CreateTopologyVisualization(Engine engine, Topology<Rule> topology)
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

                foreach (var index in node.Data.Indices)
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
            var topology = new Topology<Rule>(quadModel.Meshes[0]);
            Wfc(topology);
            var visualization = CreateTopologyVisualization(engine, topology);
            visualization.Position = Vector3.UnitY;

            engine.Run();
        }
    }
}