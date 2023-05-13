using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using GameEngine.Utils;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Drawing;
using TextureUtils;
using UVWfc.Helpers;
using UVWfc.Props;
using UVWfc.Props.Algorithms;
using Mesh = GameEngine.Graphics.Mesh;

namespace PlanetAlgorithms
{
    public class SurfaceGeneratorAlgorithm : ICellAlgorithm
    {
        private struct NodeVertex
        {
            public Color Color;
            public Vector3 Position;

            public NodeVertex(Color color, Vector3 position)
            {
                Color = color;
                Position = position;
            }
        }

        private struct Sequence
        {
            public Color Color = Color.Empty;
            public List<Vector3> Vertices = new();

            public Sequence() {}
        }

        private const int CornersCount = 4;
        private const int SequencesPerNodeMaxCount = 2;

        private static readonly Dictionary<Vector3i, float> ColorsToHeights = new()
        {
            { new Vector3i(112, 136, 227), 0.0f },
            { new Vector3i(0, 215, 46)   , 0.3f },
            { new Vector3i(243, 248, 0)  , 0.3f },
            { new Vector3i(134, 134, 134), 1.5f },
            { new Vector3i(191, 255, 243), 2.0f },
            { new Vector3i(255, 255, 255), 0.3f },
        };

        public bool ProcessCell(Engine engine, LogicalNode node)
        {
            var vertices = ExtractVerticesAndColorsSequences(node);
            var sequences = ExtractSequences(vertices);
            GenerateSurfaces(engine, sequences, Mathematics.GetNormal(node.Corners));

            //foreach (var sequence in sequences)
            //{
            //    var color = sequence.Color.RgbaToVector3();

            //    for (int i = 0; i < sequence.Vertices.Count; i++)
            //    {
            //        engine.Line(sequence.Vertices.GetCircular(i - 1), sequence.Vertices[i], color, 5.0f);
            //    }
            //}

            return true;
        }

        private static void GenerateSurfaces(Engine engine, List<Sequence> sequences, Vector3 normal)
        {
            if (sequences.Count == 1)
            {
                var sequence = sequences[0];
                var mesh = CreateMesh(sequence, normal);
                var model = new Model(mesh);
                CreateSurfaceGameObject(engine, model, sequence.Color);
            }
            else
            {
                for (int i = 0; i < sequences.Count; i++)
                {
                    var thisSequence = sequences[i];
                    var neighbourSequence = sequences.GetCircular(i + 1);
                    var meshes = CreateMeshes(thisSequence, neighbourSequence, normal);
                    var model = new Model(meshes);
                    CreateSurfaceGameObject(engine, model, thisSequence.Color);
                }
            }
        }

        private static bool IsWater(Sequence sequence)
        {
            return sequence.Color.IsSame(Color.FromArgb(112, 136, 227));
        }

        private static void CreateSurfaceGameObject(Engine engine, Model model, Color color)
        {
            var surface = engine.CreateGameObject();
            var renderer = surface.Add<MaterialRenderComponent>();
            renderer.Model = model;
            renderer.Material.Color = color.RgbaToVector3();
        }

        private static Mesh CreateMesh(
            Sequence sequence,
            Vector3 normal)
        {            
            var positions = new List<Vector3>();
            var key = new Vector3i(sequence.Color.R, sequence.Color.G, sequence.Color.B);
            var height = ColorsToHeights[key];

            for (int i = 0; i < sequence.Vertices.Count; i++)
            {
                var temp = sequence.Vertices[i];           
                positions.Add(temp + height * Vector3.Normalize(temp));
            }

            var vertices = positions
                .Select(position => new Vertex(position, normal, Vector2.Zero))
                .ToList();
            var indices = TriangulateConvexPolygon(vertices);

            return new Mesh(vertices, indices);
        }

        private static Mesh CreateMesh(List<Vector3> positions)
        {
            var normal = Mathematics.GetNormal(positions);
            var vertices = positions
                .Select(position => new Vertex(position, normal, Vector2.Zero))
                .ToList();
            var indices = TriangulateConvexPolygon(vertices);

            return new Mesh(vertices, indices);
        }

        private static List<Mesh> CreateMeshes(
            Sequence thisSequence,
            Sequence neighbourSequence,
            Vector3 normal)
        {
            var meshes = new List<Mesh>();
            var thisHeight = ColorsToHeights[thisSequence.Color.RgbaToVector3i()];
            var neighbourHeight = ColorsToHeights[neighbourSequence.Color.RgbaToVector3i()];
            neighbourHeight = neighbourHeight > thisHeight ? thisHeight : neighbourHeight;

            if (thisSequence.Vertices.Count == 3)
            {
                var shore = new List<Vector3>()
                {
                    thisSequence.Vertices[0] + neighbourHeight * Vector3.Normalize(thisSequence.Vertices[0]),
                    thisSequence.Vertices[1] + thisHeight * Vector3.Normalize(thisSequence.Vertices[1]),
                    thisSequence.Vertices[2] + neighbourHeight * Vector3.Normalize(thisSequence.Vertices[2]),
                };

                meshes.Add(CreateMesh(shore));
                return meshes;
            }

            {
                var shore = new List<Vector3>()
                {
                    thisSequence.Vertices[0] + neighbourHeight * Vector3.Normalize(thisSequence.Vertices[0]),
                    thisSequence.Vertices[1] + thisHeight * Vector3.Normalize(thisSequence.Vertices[1]),
                    thisSequence.Vertices[thisSequence.Vertices.Count - 2] + thisHeight * Vector3.Normalize(thisSequence.Vertices[thisSequence.Vertices.Count - 2]),
                    thisSequence.Vertices[thisSequence.Vertices.Count - 1] + neighbourHeight * Vector3.Normalize(thisSequence.Vertices[thisSequence.Vertices.Count - 1]),
                };

                meshes.Add(CreateMesh(shore));
            }

            if (thisSequence.Vertices.Count <= 4)
            {
                return meshes;
            }

            var surface = thisSequence.Vertices
                .Skip(1)
                .SkipLast(1)
                .Select(position => position + thisHeight * Vector3.Normalize(position))
                .ToList();

            meshes.Add(CreateMesh(surface));
            return meshes;
        }

        private static List<int> TriangulateConvexPolygon(List<Vertex> vertices)
        {
            var indices = new List<int>();
            var trianglesCount = vertices.Count - 2;
            int a = 0;
            int b = 1;

            for (int i = 0; i < trianglesCount; i++)
            {
                int c = b + 1;
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                b = c;
            }

            return indices;
        }

        private static List<NodeVertex> ExtractVerticesAndColorsSequences(LogicalNode node)
        {
            var vertices = new List<NodeVertex>();

            var cornerVertices = new[]
            {
                new NodeVertex(node.Rule.Logical[1, 0], node.Corners[0]),
                new NodeVertex(node.Rule.Logical[0, 0], node.Corners[1]),
                new NodeVertex(node.Rule.Logical[0, 1], node.Corners[2]),
                new NodeVertex(node.Rule.Logical[1, 1], node.Corners[3]),
            };

            for (int i = 0; i < CornersCount; i++)
            {
                var prev = cornerVertices.GetCircular(i - 1);
                var next = cornerVertices[i];
                var centroid = (prev.Position + next.Position) / 2;

                vertices.Add(prev);                

                if (prev.Color != next.Color)
                {
                    vertices.Add(new NodeVertex(prev.Color, centroid));
                    vertices.Add(new NodeVertex(next.Color, centroid));
                } 
            }

            return vertices;
        }

        private static List<Sequence> ExtractSequences(List<NodeVertex> vertices)
        {
            int initial = 0;
            var sequences = new List<Sequence>();

            for (int i = 0; i < vertices.Count; i++)
            {
                var prev = vertices.GetCircular(i - 1);
                var next = vertices.GetCircular(i);

                if (prev.Color != next.Color)
                {
                    initial = i;
                    break;
                }
            }

            for (int i = 0; i < SequencesPerNodeMaxCount; i++)
            {
                var sequence = new Sequence();
                sequence.Color = vertices[initial].Color;
                sequences.Add(sequence);

                var from = vertices.GetCircular(initial);

                for (var counter = 0; counter < vertices.Count; counter++)
                {
                    var temp = vertices.GetCircular(initial + counter);

                    if (from.Color != temp.Color)
                    {
                        initial += counter;
                        break;
                    }

                    sequence.Vertices.Add(temp.Position);
                }

                if (sequence.Vertices.Count == vertices.Count)
                {
                    break;
                }
            }

            return sequences;
        }
    }
}