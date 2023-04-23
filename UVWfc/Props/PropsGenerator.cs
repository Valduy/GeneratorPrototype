using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Mathematics;
using Graph;
using MeshTopology;
using OpenTK.Mathematics;
using System.Drawing;
using System.Security.Cryptography;
using UVWfc.Helpers;
using UVWfc.LevelGraph;
using UVWfc.Props.Algorithms;

namespace UVWfc.Props
{
    public class PropsGenerator
    {
        public static readonly Color PipesColor = Color.FromArgb(255, 217, 0);
        public static readonly Color WireColor = Color.FromArgb(255, 0, 24);
        public static readonly Color VentilationColor = Color.FromArgb(255, 60, 246);

        private List<ICellAlgorithm> _cellAlgorithms = new();
        private List<INetAlgorithm> _netAlgorithms = new();

        public PropsGenerator() { }

        public PropsGenerator PushCellAlgorithm(ICellAlgorithm cellAlgorithm)
        {
            _cellAlgorithms.Add(cellAlgorithm);
            return this;
        }

        public PropsGenerator PushNetAlgorithm(INetAlgorithm netAlgorithm)
        {
            _netAlgorithms.Add(netAlgorithm);
            return this;
        }

        public void Generate(Engine engine, Topology topology, List<Cell> cells, int size)
        {
            var nets = ExtractNets(topology, cells, size);
            ProcessCells(engine, topology, cells, size);
            ProcessNets(engine, nets);
        }

        #region ForDebug
        // For debug purposes.
        public void VisualizeNets(Engine engine, Topology topology, List<Cell> cells, int size)
        {
            var nets = ExtractNets(topology, cells, size);
            float epsilon = 0.01f;
            float extrusionFactor = 0.3f;

            foreach (var net in nets)
            {
                foreach (var node in net.GetNodes())
                {
                    var centroid = Mathematics.GetCentroid(node.Item.Corners);
                    var normal = Mathematics.GetNormal(node.Item.Corners);
                    var from = centroid + extrusionFactor * normal;
                    var scale = new Vector3(0.3f);

                    foreach (var neighbour in node.Neighbours)
                    {
                        var lineGo = engine.CreateGameObject();
                        var render = lineGo.Add<LineRenderComponent>();
                        render.Color = node.Item.Rule.Logical[1, 1].RgbaToVector3();

                        var neighbourNormal = Mathematics.GetNormal(neighbour.Item.Corners);
                        var extrusionDirection = Vector3.Lerp(normal, neighbourNormal, 0.5f).Normalized();
                        var sharedPoints = Mathematics.GetSharedPoints(node.Item.Corners, neighbour.Item.Corners, epsilon);
                        var to = Mathematics.GetCentroid(sharedPoints) + extrusionFactor * extrusionDirection;

                        render.Line = new Line(from, to);
                        render.Width = 5.0f;
                    }

                    {
                        var lineGo = engine.CreateGameObject();
                        var render = lineGo.Add<LineRenderComponent>();
                        render.Line = new Line(centroid, centroid + normal);
                        render.Color = Colors.Red;
                    }

                    if (node.Neighbours.Count == 1)
                    {
                        var rotation = Mathematics.FromToRotation(Vector3.UnitY, normal);
                        var cube = engine.InstantiateCube(from, rotation, scale, node.Item.Rule.Logical[1, 1]);

                        var lineGo = engine.CreateGameObject();
                        var render = lineGo.Add<LineRenderComponent>();
                        render.Line = new Line(Vector3.Zero, Vector3.UnitY);
                        render.Color = Colors.Blue;
                        cube.AddChild(lineGo);
                    }
                    if (node.Neighbours.Count >= 3)
                    {
                        engine.InstantiateSphere(from, Quaternion.Identity, scale, node.Item.Rule.Logical[1, 1]);
                    }
                }
            }
        }
        #endregion ForDebug

        private List<Net<LogicalNode>> ExtractNets(Topology topology, List<Cell> cells, int size)
        {
            var cellToLogicalNode = CreateLogicalNodes(topology, cells, size);
            var net = ConnectLogicalNodes(cells, cellToLogicalNode);
            return net.GetSubNets().ToList();
        }

        private Dictionary<Cell, LogicalNode> CreateLogicalNodes(Topology topology, List<Cell> cells, int size)
        {
            var cellToLogicalNode = new Dictionary<Cell, LogicalNode>();

            foreach (var cell in cells)
            {
                var rule = cell.Rules[0];

                foreach (var netAlgorithm in _netAlgorithms)
                {
                    if (netAlgorithm.CanProcessRule(rule))
                    {
                        var corners = GetNodeCorners(topology, cell, size);
                        var connections = netAlgorithm.GetRuleConnections(rule);
                        cellToLogicalNode[cell] = new LogicalNode(corners, rule, connections);
                    }
                }
            }

            return cellToLogicalNode;
        }

        private static Net<LogicalNode> ConnectLogicalNodes(List<Cell> cells, Dictionary<Cell, LogicalNode> cellToLogicalNode)
        {
            var cellToNetNode = new Dictionary<Cell, Node<LogicalNode>>();
            var net = new Net<LogicalNode>();

            foreach (var cell in cells)
            {
                if (!cellToLogicalNode.TryGetValue(cell, out var thisLogicalNode))
                {
                    continue;
                }
                if (!cellToNetNode.TryGetValue(cell, out var thisNetNode))
                {
                    thisNetNode = net.CreateNode(thisLogicalNode);
                    cellToNetNode[cell] = thisNetNode;
                }

                for (int i = 0; i < cell.Neighbours.Length; i++)
                {
                    if (!thisLogicalNode.Connections[i])
                    {
                        continue;
                    }

                    var otherCell = cell.Neighbours[i]!.Cell;

                    if (!cellToLogicalNode.TryGetValue(otherCell, out var otherLogicalNode))
                    {
                        continue;
                    }
                    if (!cellToNetNode.TryGetValue(otherCell, out var otherNetNode))
                    {
                        otherNetNode = net.CreateNode(otherLogicalNode);
                        cellToNetNode[otherCell] = otherNetNode;
                    }

                    net.Connect(thisNetNode, otherNetNode);
                }
            }

            return net;
        }

        private static List<Vector3> GetNodeCorners(Topology topology, Cell cell, int size)
        {
            var corners = new List<Vector3>();
            var centroid = Mathematics.GetCentroid(cell);

            foreach (var uv in cell)
            {
                var uvWithError = centroid + (uv - centroid) * 0.9f;
                var point = GetPoint(topology, uvWithError, size);
                //var point = GetPoint(topology, uv, size);
                corners.Add(point);
            }
            return corners;
        }

        private static Vector3 GetPoint(Topology topology, Vector2 uv, int size)
        {
            float epsilon = 0.01f;

            foreach (var node in topology)
            {
                Vertex a = node.Face[0];
                Vertex b = node.Face[1];
                Vertex c = node.Face[2];

                Vector2 barycentric = Mathematics.GetBarycentric(
                    a.TextureCoords * size,
                    b.TextureCoords * size,
                    c.TextureCoords * size,
                    uv);

                float u = barycentric.X;
                float v = barycentric.Y;

                // Face contain this uv
                if (u >= -epsilon && v >= 0 && u + v <= 1 + epsilon)
                {
                    Vector3 point = (1 - u - v) * a.Position + v * b.Position + u * c.Position;
                    return point;
                }
            }

            //foreach (var node in topology)
            //{
            //    var a = node.Face[0].TextureCoords * size;
            //    var b = node.Face[1].TextureCoords * size;
            //    var c = node.Face[2].TextureCoords * size;

            //    if (PointInTriangle(uv, a, b, c))
            //    {
            //        Vector2 barycentric = Mathematics.GetBarycentric(a, b, c, uv);

            //        float u = barycentric.X;
            //        float v = barycentric.Y;

            //        var t = 0;
            //    }
            //}

            throw new ArgumentException();
        }

        private void ProcessCells(Engine engine, Topology topology, List<Cell> cells, int size)
        {
            foreach (var cell in cells)
            {
                var node = new LogicalNode(GetNodeCorners(topology, cell, size), cell.Rules[0]);

                foreach (var cellAlgorithm in _cellAlgorithms)
                {
                    if (cellAlgorithm.ProcessCell(engine, node))
                    {
                        break;
                    }
                }
            }
        }

        private void ProcessNets(Engine engine, List<Net<LogicalNode>> nets)
        {
            foreach (var net in nets)
            {
                var firstNode = net.GetNodes().First();

                foreach (var netAlgorithm in _netAlgorithms)
                {
                    if (netAlgorithm.CanProcessRule(firstNode.Item.Rule))
                    {
                        netAlgorithm.ProcessNet(engine, net);
                    }
                }
            }
        }

        private static bool IsLoop(Net<LogicalNode> net)
        {
            return !net.GetNodes().Any(n => n.Neighbours.Count == 1);
        }
    }
}
