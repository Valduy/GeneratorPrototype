using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;

namespace PatternDemo
{
    public static class DebugVisualizer
    {
        public static GameObject CreateMeshTopologyDebugVisualization(Engine engine, Topology topology)
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
                var centroid = node.Face
                    .Select(v => v.Position)
                    .Aggregate((p1, p2) => p1 + p2) / node.Face.Count;

                if (node.Neighbours.Count > 4)
                {
                    var cube = engine.CreateGameObject();
                    var render = cube.Add<MaterialRenderComponent>();
                    render.Model = Model.Cube;
                    render.Material.Color = Colors.Red;
                    cube.Scale = new Vector3(0.1f);
                    cube.Position = centroid;
                    go.AddChild(cube);
                }
            }

            return go;
        }

        public static GameObject CreateWfcResultDebugVisualization(Engine engine, Topology topology, Dictionary<TopologyNode, Rule> collapsed)
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
                var centroid = node.Face
                    .Select(v => v.Position)
                    .Aggregate((p1, p2) => p1 + p2) / node.Face.Count;
                var rule = collapsed[node];

                for (int i = 0; i < node.Neighbours.Count; i++)
                {
                    var edge = node.Face.GetEdgeByIndex(i);
                    var from = (edge.A + edge.B) / 2;
                    var color = rule[i][1];
                    var line = engine.Line(from, centroid, new Vector3(color.R, color.G, color.B));
                    line.Get<LineRenderComponent>().Width = 10;
                    go.AddChild(line);
                }
            }

            return go;
        }
    }
}
