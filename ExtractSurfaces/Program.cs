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
    public class EdgeComparer : IEqualityComparer<(Vector3 A, Vector3 B)>
    {
        public bool Equals((Vector3 A, Vector3 B) x, (Vector3 A, Vector3 B) y)
        {
            return x.A == y.A && x.B == y.B || x.A == y.B && x.B == y.A;
        }

        public int GetHashCode([DisallowNull] (Vector3 A, Vector3 B) obj)
        {
            return obj.A.GetHashCode() ^ obj.B.GetHashCode();
        }
    }

    public class Program
    {
        public static List<List<(Vector3 A, Vector3 B)>> ExtractPolies(MeshTopology.MeshTopology topology)
        {
            var groups = topology.ExtractFacesGroups((reference, node) =>
            {
                return Mathematics.Equal(
                    reference.Face.GetNormal().Normalized(),
                    node.Face.GetNormal().Normalized(),
                    0.1f);
            });
            
            var polies = new List<List<(Vector3 A, Vector3 B)>>();

            foreach (var group in groups)
            {
                var repeates = new HashSet<(Vector3 A, Vector3 B)>(new EdgeComparer());
                var edges = new HashSet<(Vector3 A, Vector3 B)>(new EdgeComparer());

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

                var poly = new List<(Vector3 A, Vector3 B)>() { edges.First() };
                edges.Remove(edges.First());

                while (edges.Count > 0)
                {
                    var next = edges.First(e => e.A == poly.Last().B);
                    poly.Add(next);
                    edges.Remove(next);
                }

                polies.Add(poly);
            }

            return polies;
        }

        public static GameObject CreatePoliesVisualization(Engine engine, List<List<(Vector3 A, Vector3 B)>> polies)
        {
            var go = engine.CreateGameObject();

            foreach (var poly in polies)
            {
                var polyGo = engine.CreateGameObject();

                foreach (var edge in poly)
                {
                    var edgeGo = engine.Line(edge.A, edge.B, Colors.Purple);
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
            var topology = new MeshTopology.MeshTopology(model.Meshes[0], 3);
            var polies = ExtractPolies(topology);

            var poliesGo = CreatePoliesVisualization(engine, polies);
            poliesGo.Position = Vector3.UnitY * 3;

            engine.Run();
        }
    }
}