using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using System.Drawing;
using System.Drawing.Imaging;
using TextureUtils;
using Mesh = GameEngine.Graphics.Mesh;

namespace PatternDemo
{
    public class Rule
    {
        //public const int LogicalResolution = 3;
        public const int LogicalResolution = 4;
        public const int DetailedResolution = 20;

        public readonly Color[,] Logical = new Color[LogicalResolution, LogicalResolution];
        public readonly Color[,] Detailed = new Color[DetailedResolution, DetailedResolution];

        public Color[] this[int index] 
        { 
            get 
            {
                var result = new Color[LogicalResolution];

                switch (index)
                {
                    case 0:
                        for (int i = 0; i < LogicalResolution; i++)
                        {
                            result[i] = Logical[i, 0];
                        }

                        break;
                    case 1:
                        for (int i = 0; i < LogicalResolution; i++)
                        {
                            result[i] = Logical[0, i];
                        }

                        break;
                    case 2:
                        for (int i = 0; i < LogicalResolution; i++)
                        {
                            result[i] = Logical[i, LogicalResolution - 1];
                        }

                        break;
                    case 3:
                        for (int i = 0; i < LogicalResolution; i++)
                        {
                            result[i] = Logical[LogicalResolution - 1, i];
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException("index");
                }

                return result;
            } 
        }
    }

    public class Program
    {
        public const int TileshealdWidth = 11;
        public const int TileshealdHeight = 6;
        //public const int TileshealdWidth = 5;
        //public const int TileshealdWidth = 6;
        //public const int TileshealdHeight = 3;

        public static List<Rule> CreateRules(string logicalPath, string detailedPath)
        {
            var logicalBmp = new Bitmap(logicalPath);
            var detailedBmp = new Bitmap(detailedPath);
            var rules = new List<Rule>();

            for (int x = 0; x < TileshealdWidth; x ++)
            {
                for (int y = 0; y < TileshealdHeight; y ++)
                {
                    var rule = new Rule();

                    for (int i = 0; i < Rule.LogicalResolution; i++)
                    {
                        for (int j = 0; j < Rule.LogicalResolution; j++)
                        {
                            int bmpX = Rule.LogicalResolution * x + i;
                            int bmpY = Rule.LogicalResolution * y + j;
                            var color = logicalBmp.GetPixel(bmpX, bmpY);
                            rule.Logical[i, j] = color;                            
                        }
                    }

                    for (int i = 0; i < Rule.DetailedResolution; i++)
                    {
                        for (int j = 0; j < Rule.DetailedResolution; j++)
                        {
                            int bmpX = Rule.DetailedResolution * x + i;
                            int bmpY = Rule.DetailedResolution * y + j;
                            var color = detailedBmp.GetPixel(bmpX, bmpY);
                            rule.Detailed[i, j] = color;
                        }
                    }
                                  
                    if (!rule.Logical.Enumerate().All(c => c.IsTransparent()))
                    {
                        rules.Add(rule);
                    }                    
                }
            }

            return rules;
        }

        public static int DefineTextureSize(Mesh mesh)
        {
            int degree = 0;

            foreach (Vertex v in mesh.Vertices)
            {
                degree = Math.Max(degree, GetDecimalPlaces(v.TextureCoords.X));
                degree = Math.Max(degree, GetDecimalPlaces(v.TextureCoords.Y));
            }

            //return (int)Math.Pow(10, degree);
            return 4096;
        }

        public static int GetDecimalPlaces(float n)
        {
            n = Math.Abs(n); 
            n -= (int)n;     
            int decimalPlaces = 0;

            while (n > 0)
            {
                decimalPlaces++;
                n *= 10;
                n -= (int)n;
            }

            return decimalPlaces;
        }

        public static Dictionary<TopologyNode, Rule> Wfc(Topology topology, List<Rule> rules)
        {
            var possibilities = new Dictionary<TopologyNode, List<Rule>>();
            var forRecalculation = new List<TopologyNode>();
 
            foreach (var node in topology)
            {
                possibilities[node] = new List<Rule>(rules);
            }

            var initial = topology.GetRandom();
            var rule = possibilities[initial].GetRandom();
            possibilities[initial] = new List<Rule> { rule };
            forRecalculation.AddRange(initial.Neighbours);

            while (true)
            {
                int trashold = 1000;
                int failes = 0;

                while (forRecalculation.Count > 0)
                {
                    var node = forRecalculation[0];
                    forRecalculation.Remove(node);

                    var possibleHere = possibilities[node];
                    var filtered = FilterPossible(possibilities, possibleHere, node);

                    // Deadlock resolution
                    if (filtered.Count == 0)
                    {
                        failes += 1;

                        if (failes >= trashold)
                        {
                            foreach (var n in topology)
                            {
                                possibilities[n] = new List<Rule>(rules);
                            }

                            forRecalculation.Clear();
                            break;
                        }

                        possibilities[node] = new List<Rule>(rules);

                        foreach (var neighbour in node.Neighbours)
                        {
                            possibilities[neighbour] = new List<Rule>(rules);
                        }

                        continue;
                    }

                    if (possibleHere.Count > filtered.Count)
                    {
                        forRecalculation.AddRange(node.Neighbours);
                    }

                    possibilities[node] = filtered;
                }

                var maxNode = possibilities.First().Key;
                int maxPossibilities = possibilities.First().Value.Count;
                int resolved = 0;

                foreach (var pair in possibilities)
                {
                    if (pair.Value.Count > maxPossibilities)
                    {
                        maxNode = pair.Key;
                        maxPossibilities = pair.Value.Count;
                    }

                    if (pair.Value.Count == 1)
                    {
                        resolved += 1;
                    }
                }

                Console.WriteLine(resolved);

                if (maxPossibilities <= 1)
                {
                    break;
                }

                rule = possibilities[maxNode].GetRandom();
                possibilities[maxNode] = new List<Rule> { rule };
                forRecalculation.AddRange(maxNode.Neighbours);
            }

            return possibilities.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[0]);
        }

        public static List<Rule> FilterPossible(
            Dictionary<TopologyNode, List<Rule>> possibilities,
            List<Rule> rules,
            TopologyNode node)
            => rules.Where(r => IsPossible(possibilities, r, node)).ToList();

        public static bool IsPossible(
            Dictionary<TopologyNode, List<Rule>> possibilities,
            Rule rule,
            TopologyNode node)
        {
            for (int neighbourIndex = 0; neighbourIndex < node.Neighbours.Count; neighbourIndex++)
            {
                var neighbour = node.Neighbours[neighbourIndex];
                var nodeIndex = neighbour.Neighbours.IndexOf(node);
                var rules = possibilities[neighbour];

                if (rules.All(r => !IsSame(r[nodeIndex], rule[neighbourIndex])))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsSame(Color[] lhs, Color[] rhs)
        {
            if (lhs.Length != rhs.Length)
            {
                throw new ArgumentException();
            }

            for (int i = 0; i < lhs.Length; i++)
            {
                if (!lhs[i].IsSame(rhs[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static byte[] CreateTexture(Topology topology, Dictionary<TopologyNode, Rule> collapsed, int size)
        {
            var texture = new byte[size * size * 4];

            foreach (var node in topology)
            {
                var rule = collapsed[node];

                var from = node.Face[0].TextureCoords * size;
                var to = node.Face[2].TextureCoords * size;

                var horizontalAxis = (node.Face[1].TextureCoords - node.Face[0].TextureCoords).Normalized();
                var verticalAxis = (node.Face[3].TextureCoords - node.Face[0].TextureCoords).Normalized();

                var direction = to - from;
                var square = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));

                for (int x = 0; x < square.X; x++)
                {
                    int colorX = (int)(x * rule.Detailed.GetLength(0) / square.X);

                    for (int y = 0; y < square.Y; y++)
                    {
                        int colorY = (int)(y * rule.Detailed.GetLength(1) / square.Y);
                        var color = rule.Detailed[rule.Detailed.GetLength(0) - 1 - colorX, colorY];
                        var position = from + horizontalAxis * x + verticalAxis * y;
                        texture.SetColor(size, (int)position.X, (int)position.Y, color);
                    }
                }

                //for (int x = 0; x < square.X; x++)
                //{
                //    int colorX = (int)(x * rule.Logical.GetLength(0) / square.X);

                //    for (int y = 0; y < square.Y; y++)
                //    {
                //        int colorY = (int)(y * rule.Logical.GetLength(1) / square.Y);
                //        var color = rule.Logical[rule.Logical.GetLength(0) - 1 - colorX, colorY];
                //        var position = from + horizontalAxis * x + verticalAxis * y;
                //        texture.SetColor(size, (int)position.X, (int)position.Y, color);
                //    }
                //}
            }

            return texture;
        }

        public static GameObject CreateTopologyDebugVisualization(Engine engine, Topology topology)
        {
            var go = engine.CreateGameObject();

            foreach (var node in topology)
            {
                foreach (var edge in node.Face.EnumerateEdges())
                {
                    var line = engine.Line(edge.A, edge.B, Colors.Green);
                    go.AddChild(line);
                }
            }

            foreach (var node in topology)
            {
                var centroid = node.Face
                    .Select(v => v.Position)
                    .Aggregate((p1, p2) => p1 + p2) / node.Face.Count;

                if (node.Neighbours.Count > 4)
                {
                    var cube = engine.CreateGameObject();
                    var render = cube.Add<MaterialRenderComponent>();
                    render.Model = Model.Cube;
                    render.Material.Color = Colors.Red;
                    cube.Scale = new Vector3(0.1f);
                    cube.Position = centroid;
                    go.AddChild(cube);
                }
            }

            return go;
        }

        public static GameObject CreateTopologyVisualization(Engine engine, Topology topology, Dictionary<TopologyNode, Rule> collapsed)
        {
            var go = engine.CreateGameObject();

            foreach (var node in topology)
            {
                foreach (var edge in node.Face.EnumerateEdges())
                {
                    var line = engine.Line(edge.A, edge.B, Colors.Green);
                    go.AddChild(line);
                }
            }

            foreach (var node in topology)
            {
                var centroid = node.Face
                    .Select(v => v.Position)
                    .Aggregate((p1, p2) => p1 + p2) / node.Face.Count;
                var rule = collapsed[node];

                for (int i = 0; i < node.Neighbours.Count; i++)
                {
                    var edge = node.Face.GetEdgeByIndex(i);
                    var from = (edge.A + edge.B) / 2;
                    var color = rule[i][1];
                    var line = engine.Line(from, centroid, new Vector3(color.R, color.G, color.B));
                    line.Get<LineRenderComponent>().Width = 10;
                    go.AddChild(line);
                }
            }

            return go;
        }

        public static void SaveTexture(byte[] texture, int size, string path)
        {
            var bmp = new Bitmap(size, size);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    bmp.SetPixel(x, y, texture.GetColor(size, x, y));
                }
            }

            bmp.Save(path, ImageFormat.Bmp);
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

            var quadModel = Model.Load("Content/Room.obj", PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);
            //var quadModel = Model.Load("Content/Structure.obj", PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);
            var topology = new Topology(quadModel.Meshes[0]);

            //var rules = CreateRules("Content/PipesSample.png", "Content/Pipes.png");
            //var rules = CreateRules("Content/NetworkSample.png", "Content/Network.png");
            var rules = CreateRules("Content/SquareSample.png", "Content/Network.png");
            var collapsed = Wfc(topology, rules);
            var textureSize = DefineTextureSize(quadModel.Meshes[0]);
            var texture = CreateTexture(topology, collapsed, textureSize);

            SaveTexture(texture, textureSize, "Vis.bmp");

            var structureGo = engine.CreateGameObject();
            var structureRender = structureGo.Add<MaterialRenderComponent>();
            structureRender.Model = Model.Load("Content/Room.obj");
            //structureRender.Model = Model.Load("Content/Structure.obj");
            structureRender.Texture = Texture.LoadFromMemory(texture, textureSize, textureSize);            
            structureGo.Scale = new Vector3(1.0f);
            structureGo.Position = new Vector3(0.0f, 1.0f, 0.0f);
            structureGo.AddChild(engine.Axis(5));

            //var vis = CreateTopologyVisualization(engine, topology, collapsed);
            //vis.Position = new Vector3(-5, 1, 0);

            engine.Run();
        }
    }
}