using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using OpenTK.Mathematics;
using UVWfc.Props;
using UVWfc.Props.Algorithms;

namespace SciFiAlgorithms
{
    public class PanelsGeneratorAlgorithm : ICellAlgorithm
    {
        private const float FloorTrashold = 45.0f;
        private const float CeilTrashold = 45.0f;

        public readonly float Extrusion;

        private Material WallMaterial = new Material
        {
            Color = new Vector3(0.714f, 0.4284f, 0.18144f),
            Ambient = 0.15f,
            Specular = 0.25f,
            Shininess = 25.6f,
        };

        private Material FloorMaterial = new Material
        {
            Color = new Vector3(0.4f, 0.4f, 0.4f),
            Ambient = 0.25f,
            Specular = 0.774597f,
            Shininess = 76.8f,
        };

        public PanelsGeneratorAlgorithm(float extrusion)
        {
            Extrusion = extrusion;
        }

        public bool ProcessCell(Engine engine, LogicalNode node)
        {
            // padding wnutri
            float padding = 0.1f;

            var centroid = Mathematics.GetCentroid(node.Corners);
            var normal = Mathematics.GetNormal(node.Corners);
            var circuit = node.Corners.ToList();
            var foundation = circuit.Select(p => p + padding * Vector3.Normalize(centroid - p)).ToList();
            var roof = foundation.Select(p => p + Extrusion * normal).ToList();

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

            var cosa = Vector3.Dot(Vector3.UnitY, normal);
            var acos = MathF.Acos(cosa);
            var angle = MathHelper.RadiansToDegrees(acos);

            if (IsFloor(normal) || IsCeil(normal))
            {
                renderer.Material = FloorMaterial;
            }
            else
            {
                renderer.Material = WallMaterial;
            }

            return true;
        }

        private static bool IsFloor(Vector3 normal)
        {
            var cosa = Vector3.Dot(Vector3.UnitY, normal);
            var acos = MathF.Acos(cosa);
            var angle = MathHelper.RadiansToDegrees(acos);
            return angle < FloorTrashold;
        }

        private static bool IsCeil(Vector3 normal)
        {
            var cosa = Vector3.Dot(-Vector3.UnitY, normal);
            var acos = MathF.Acos(cosa);
            var angle = MathHelper.RadiansToDegrees(acos);
            return angle < CeilTrashold;
        }
    }
}
