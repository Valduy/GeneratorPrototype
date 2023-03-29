using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using MeshTopology;
using OpenTK.Mathematics;

namespace TriangulatedTopology.Props.Algorithms
{
    public class PanelsGeneratorAlgorithm : ICellAlgorithm
    {
        public bool ProcessCell(Engine engine, LogicalNode node)
        {
            // padding wnutri
            float padding = 0.1f;
            float extrusion = 0.05f;

            var centroid = Mathematics.GetCentroid(node.Corners);
            var normal = Mathematics.GetNormal(node.Corners);
            var circuit = node.Corners.ToList();
            var foundation = circuit.Select(p => p + padding * Vector3.Normalize(centroid - p)).ToList();
            var roof = foundation.Select(p => p + extrusion * normal).ToList();

            var vertices = new List<Vertex>();
            var indices = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                var from = circuit.GetCircular(i);
                var to = circuit.GetCircular(i + 1);

                var foundationFrom = foundation.GetCircular(i);
                var foundationTo = foundation.GetCircular(i + 1);

                var roofFrom = roof.GetCircular(i);
                var roofTo = roof.GetCircular(i + 1);

                var sideDirection = Vector3.Normalize(to - from);
                var sideNormal = Vector3.Normalize(Vector3.Cross(normal, sideDirection));

                vertices.Add(new Vertex(roofFrom, sideNormal, Vector2.Zero));
                vertices.Add(new Vertex(roofTo, sideNormal, Vector2.Zero));
                vertices.Add(new Vertex(foundationTo, sideNormal, Vector2.Zero));
                vertices.Add(new Vertex(foundationFrom, sideNormal, Vector2.Zero));

                indices.Add(4 * i);
                indices.Add(4 * i + 1);
                indices.Add(4 * i + 2);
                indices.Add(4 * i + 2);
                indices.Add(4 * i);
                indices.Add(4 * i + 3);
            }

            vertices.Add(new Vertex(roof[0], normal, Vector2.Zero));
            vertices.Add(new Vertex(roof[1], normal, Vector2.Zero));
            vertices.Add(new Vertex(roof[2], normal, Vector2.Zero));
            vertices.Add(new Vertex(roof[3], normal, Vector2.Zero));

            indices.Add(16);
            indices.Add(17);
            indices.Add(18);
            indices.Add(18);
            indices.Add(16);
            indices.Add(19);

            var mesh = new Mesh(vertices, indices);
            var model = new Model(mesh);
            var go = engine.CreateGameObject();
            var renderer = go.Add<MaterialRenderComponent>();
            renderer.Model = model;
            renderer.Material.Color = new Vector3(0.714f, 0.4284f, 0.18144f);
            renderer.Material.Ambient = 0.15f;
            renderer.Material.Specular = 0.25f;
            renderer.Material.Shininess = 0.2f;

            return true;
        }
    }
}
