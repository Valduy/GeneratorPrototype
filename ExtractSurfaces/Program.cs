using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using System.Diagnostics.CodeAnalysis;

namespace ExtractSurfaces
{
    public class EdgeComparer : IEqualityComparer<(Vector3 v1, Vector3 v2)>
    {
        public bool Equals((Vector3 v1, Vector3 v2)x, (Vector3 v1, Vector3 v2)y)
        {
            return x.v1 == y.v1 && x.v2 == y.v2 || x.v1 == y.v2 && x.v2 == y.v1;
        }

        public int GetHashCode([DisallowNull] (Vector3 v1, Vector3 v2) obj)
        {
            return obj.v1.GetHashCode() ^ obj.v2.GetHashCode();
        }
    }

    public class Program
    {
        public static List<List<(Vector3, Vector3)>> ExtractPolies(Topology topology)
        {
            var groups = topology.ExtractFacesGroups((reference, node) =>
            {
                return Mathematics.Equal(
                    reference.Face.GetNormal().Normalized(),
                    node.Face.GetNormal().Normalized(),
                    0.1f);
            });
            
            var polies = new List<List<(Vector3, Vector3)>>();

            foreach (var group in groups)
            {
                var repeates = new HashSet<(Vector3, Vector3)>(new EdgeComparer());
                var edges = new HashSet<(Vector3, Vector3)>(new EdgeComparer());

                foreach (var node in group)
                {
                    foreach (var edge in node.Face.EnumerateEdges())
                    {
                        if (edges.Contains(edge))
                        {
                            repeates.Add(edge);
                        }
                        else
                        {
                            edges.Add(edge);
                        }                        
                    }
                }

                edges.ExceptWith(repeates);
                var poly = new List<(Vector3, Vector3)>() { edges.First() };
                edges.Remove(edges.First());

                while (edges.Count > 0)
                {
                    var next = edges.First(e => e.Item1 == poly.Last().Item2);
                    poly.Add(next);
                    edges.Remove(next);
                }

                polies.Add(poly);
            }

            return polies;
        }

        public static GameObject CreatePoliesVisualization(Engine engine, List<List<(Vector3, Vector3)>> polies)
        {
            var go = engine.CreateGameObject();

            foreach (var poly in polies)
            {
                var polyGo = engine.CreateGameObject();

                foreach (var edge in poly)
                {
                    var edgeGo = engine.Line(edge.Item1, edge.Item2, Colors.Purple);
                    polyGo.AddChild(edgeGo);
                } 
                
                go.AddChild(polyGo);
            }
            
            return go;
        }

        public static void Main(string[] args)
        {
            var random = new Random();
            int seed = random.Next();
            Console.WriteLine(seed);
            Utils.UseSeed(seed);

            using var engine = new Engine();

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);

            var model = Model.Load("Content/UVExperiments.obj");
            var topology = new Topology(model.Meshes[0], 3);
            var polies = ExtractPolies(topology);

            var poliesGo = CreatePoliesVisualization(engine, polies);
            poliesGo.Position = Vector3.UnitY * 3;

            //var structureGo = engine.CreateGameObject();
            //var structureRender = structureGo.Add<MaterialRenderComponent>();
            //structureRender.Model = model;
            //structureGo.Scale = new Vector3(1.0f);
            //structureGo.AddChild(engine.Axis(5));

            engine.Run();
        }
    }
}