using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Utils;
using TextureUtils;
using MeshTopology;
using OpenTK.Mathematics;

namespace PatternDemo
{
    public static class DebugVisualizer
    {
        public static GameObject DebugMeshTopologyInvalidNeighboursCount(Engine engine, MeshTopology.Topology topology)
        {
            var go = engine.CreateGameObject();

            foreach (var node in topology)
            {
                foreach (var edge in node.Face.EnumerateEdges())
                {
                    var line = engine.Line(edge.A.Position, edge.B.Position, Colors.Green);
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
                    cube.Scale = new Vector3(0.05f);
                    cube.Position = centroid;
                    go.AddChild(cube);
                }
            }

            return go;
        }

        public static GameObject CreateFacesOrientationVisualization(Engine engine, MeshTopology.Topology topology)
        {
            var go = engine.CreateGameObject();

            foreach (var node in topology)
            {
                var centroid = node.Face
                    .Select(v => v.Position)
                    .Aggregate((p1, p2) => p1 + p2) / node.Face.Count;

                foreach (var edge in node.Face.EnumerateEdges())
                {
                    var line = engine.Line(
                        centroid + (edge.A.Position - centroid).Normalized() * (edge.A.Position - centroid).Length * 0.8f,
                        centroid + (edge.B.Position - centroid).Normalized() * (edge.B.Position - centroid).Length * 0.8f,
                        Colors.Green);

                    go.AddChild(line);
                }

                var first = node.Face.GetEdgeByIndex(0);
                var firstCenter = (first.A.Position + first.B.Position) / 2;
                var up = engine.Line(
                    centroid + (firstCenter - centroid).Normalized() * (firstCenter - centroid).Length * 0.8f,
                    firstCenter + (firstCenter - centroid).Normalized() * 0.1f,
                    Colors.Cyan);

                var second = node.Face.GetEdgeByIndex(1);
                var secondCenter = (second.A.Position + second.B.Position) / 2;
                var right = engine.Line(
                    centroid + (secondCenter - centroid).Normalized() * (secondCenter - centroid).Length * 0.8f,
                    secondCenter + (secondCenter - centroid).Normalized() * 0.1f,
                    Colors.Purple);

                go.AddChild(up);
                go.AddChild(right);
            }

            return go;
        }

        public static GameObject CreateWfcResultDebugVisualization(Engine engine, MeshTopology.Topology topology, Dictionary<TopologyNode, Rule> collapsed)
        {
            var go = engine.CreateGameObject();

            foreach (var node in topology)
            {
                foreach (var edge in node.Face.EnumerateEdges())
                {
                    var line = engine.Line(edge.A.Position, edge.B.Position, Colors.Green);
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
                    var from = (edge.A.Position + edge.B.Position) / 2;
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
