using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;

namespace MeshVisualization
{
    class Program
    {
        public static GameObject CreateFacesVisualization(Engine engine, List<MeshTopology.Face> faces)
        {
            var go = engine.CreateGameObject();

            foreach (var face in faces)
            {
                var centroid = face.Vertices
                    .Select(v => v.Position)
                    .Aggregate((p1, p2) => p1 + p2) / face.Vertices.Count;
                
                foreach (var edge in face.EnumerateEdges())
                {
                    var line = engine.Line(
                        centroid + (edge.A - centroid).Normalized() * (edge.A - centroid).Length * 0.8f,
                        centroid + (edge.B - centroid).Normalized() * (edge.B - centroid).Length * 0.8f,
                        Colors.Green);

                    go.AddChild(line);
                }

                var first = face.GetEdgeByIndex(0);
                var firstCenter = (first.A + first.B) / 2;
                var up = engine.Line(
                    centroid + (firstCenter - centroid).Normalized() * (firstCenter - centroid).Length * 0.8f,
                    firstCenter + (firstCenter - centroid).Normalized() * 0.1f,
                    Colors.Cyan);

                var second = face.GetEdgeByIndex(1);
                var secondCenter = (second.A + second.B) / 2;
                var right = engine.Line(
                    centroid + (secondCenter - centroid).Normalized() * (secondCenter - centroid).Length * 0.8f,
                    secondCenter + (secondCenter - centroid).Normalized() * 0.1f,
                    Colors.Purple);

                go.AddChild(up);
                go.AddChild(right);
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
            var faces = quadModel.Meshes[0].ExtractFaces();
            var go = CreateFacesVisualization(engine, faces);
            go.Position = Vector3.UnitY;

            engine.Run();
        }
    }
}