using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using System.Diagnostics.CodeAnalysis;

namespace TriangulatedTopology
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
        public static List<List<Vector3>> ExtractPolies(MeshTopology.MeshTopology topology)
        {
            var groups = topology.ExtractFacesGroups((reference, node)
                => reference.Face.IsSharedUVEdgeExist(node.Face));

            var polies = new List<List<Vector3>>();

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

                while (edges.Count > 0)
                {
                    var poly = new List<Vector3>() { edges.First().B };
                    edges.Remove(edges.First());

                    while (edges.Any(e => e.A == poly[poly.Count - 1]))
                    {
                        var edge = edges.First(e => e.A == poly[poly.Count - 1]);
                        poly.Add(edge.B);
                        edges.Remove(edge);
                    }

                    polies.Add(poly);
                }            
            }

            return polies;
        }

        public static GameObject CreatePoliesVisualization(Engine engine, List<List<Vector3>> polies)
        {
            var go = engine.CreateGameObject();

            foreach (var poly in polies)
            {
                var polyGo = engine.CreateGameObject();

                for (int i = 0; i < poly.Count; i++)
                {
                    var edgeGo = engine.Line(poly[i], poly.GetCircular(i + 1), Colors.Purple);
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

            var model = Model.Load("Content/Room.obj");

            //var roomGo = engine.CreateGameObject();
            //var roomRenderer = roomGo.Add<MaterialRenderComponent>();
            //roomRenderer.Model = model;

            var topology = new MeshTopology.MeshTopology(model.Meshes[0], 3);
            var polies = ExtractPolies(topology);
            var visualization = CreatePoliesVisualization(engine, polies);
            visualization.Position = 5 * Vector3.UnitY;

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);

            engine.Run();
        }
    }
}