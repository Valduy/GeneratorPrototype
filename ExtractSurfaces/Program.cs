using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using System.Diagnostics.CodeAnalysis;

namespace ExtractSurfaces
{
    public class EdgeComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge? x, Edge? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is not null && y is not null)
            {
                return x.HasSamePositions(y);
            }

            return false;
        }

        public int GetHashCode([DisallowNull] Edge edge)
        {
            return edge.A.GetHashCode() ^ edge.B.GetHashCode();
        }
    }

    public class Program
    {
        public static List<List<Edge>> ExtractPolies(Topology topology)
        {
            var groups = topology.ExtractFacesGroups((reference, node) =>
            {
                return Mathematics.ApproximatelyEqualEpsilon(
                    reference.Face.GetNormal().Normalized(),
                    node.Face.GetNormal().Normalized(),
                    0.1f);
            });
            
            var polies = new List<List<Edge>>();

            foreach (var group in groups)
            {
                var repeates = new HashSet<Edge>(new EdgeComparer());
                var edges = new HashSet<Edge>(new EdgeComparer());

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

                var poly = new List<Edge>() { edges.First() };
                edges.Remove(edges.First());

                while (edges.Count > 0)
                {
                    var next = edges.First(e => e.A.Position == poly.Last().B.Position);
                    poly.Add(next);
                    edges.Remove(next);
                }

                polies.Add(poly);
            }

            return polies;
        }

        public static GameObject CreatePoliesVisualization(Engine engine, List<List<Edge>> polies)
        {
            var go = engine.CreateGameObject();

            foreach (var poly in polies)
            {
                var polyGo = engine.CreateGameObject();

                foreach (var edge in poly)
                {
                    var edgeGo = engine.Line(edge.A.Position, edge.B.Position, Colors.Purple);
                    polyGo.AddChild(edgeGo);
                } 
                
                go.AddChild(polyGo);
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

            var model = Model.Load("Content/UVExperiments.obj");
            var topology = new Topology(model.Meshes[0], 3);
            var polies = ExtractPolies(topology);

            var poliesGo = CreatePoliesVisualization(engine, polies);
            poliesGo.Position = Vector3.UnitY * 3;

            engine.Run();
        }
    }
}