using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Utils;
using MeshTopology;

namespace TriangulatedTopology
{
    public static class DebugMeshVisualizer
    {
        public static GameObject CreatePoliesVisualization(Engine engine, Topology topology)
        {
            var go = engine.CreateGameObject();

            foreach (var node in topology)
            {
                var polyGo = engine.CreateGameObject();

                foreach (var edge in node.Face.EnumerateEdges())
                {
                    var edgeGo = engine.Line(edge.A.Position, edge.B.Position, Colors.Green);
                    polyGo.AddChild(edgeGo);
                }

                go.AddChild(polyGo);
            }

            return go;
        }
    }
}
