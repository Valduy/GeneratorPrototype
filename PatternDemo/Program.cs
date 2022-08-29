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
        public const int RuleResolution = 5;

        public readonly Color[,] Colors = new Color[RuleResolution, RuleResolution];

        public Color[] this[int index] 
        { 
            get 
            {
                var result = new Color[RuleResolution];

                switch (index)
                {
                    case 0:
                        for (int i = 0; i < RuleResolution; i++)
                        {
                            result[i] = Colors[i, 0];
                        }

                        break;
                    case 1:
                        for (int i = 0; i < RuleResolution; i++)
                        {
                            result[i] = Colors[0, i];
                        }

                        break;
                    case 2:
                        for (int i = 0; i < RuleResolution; i++)
                        {
                            result[i] = Colors[i, RuleResolution - 1];
                        }

                        break;
                    case 3:
                        for (int i = 0; i < RuleResolution; i++)
                        {
                            result[i] = Colors[RuleResolution - 1, i];
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
        public static List<Rule> CreateRules(string path)
        {
            var bmp = new Bitmap(path);
            var rules = new List<Rule>();

            for (int x = 0; x < bmp.Width; x += Rule.RuleResolution)
            {
                for (int y = 0; y < bmp.Height; y += Rule.RuleResolution)
                {
                    var rule = new Rule();

                    for (int i = 0; i < Rule.RuleResolution; i++)
                    {
                        for (int j = 0; j < Rule.RuleResolution; j++)
                        {
                            rule.Colors[i, j] = bmp.GetPixel(x + i, y + j);                            
                        }
                    }

                    rules.Add(rule);
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

            return (int)Math.Pow(10, degree);
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
                while (forRecalculation.Count > 0)
                {
                    var node = forRecalculation[0];
                    forRecalculation.Remove(node);

                    var possibleHere = possibilities[node];
                    var filtered = FilterPossible(possibilities, possibleHere, node);

                    // Deadlock resolution
                    if (filtered.Count == 0)
                    {
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

                foreach (var pair in possibilities)
                {
                    if (pair.Value.Count > maxPossibilities)
                    {
                        maxNode = pair.Key;
                        maxPossibilities = pair.Value.Count;
                    }
                }

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
                    int colorX = (int)(x * rule.Colors.GetLength(0) / square.X);

                    for (int y = 0; y < square.Y; y++)
                    {
                        int colorY = (int)(y * rule.Colors.GetLength(1) / square.Y);
                        var color = rule.Colors[rule.Colors.GetLength(0) - 1 - colorX, colorY];
                        var position = from + horizontalAxis * x + verticalAxis * y;
                        texture.SetColor(size, (int)position.X, (int)position.Y, color);
                    }
                }
            }

            return texture;
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

            var quadModel = Model.Load("Content/Structure.obj", PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);
            //var topology = new Topology(SortTextureCoords(quadModel.Meshes[0]));
            var topology = new Topology(quadModel.Meshes[0]);
            var rules = CreateRules("Content/Sample2.png");
            var collapsed = Wfc(topology, rules);
            var textureSize = DefineTextureSize(quadModel.Meshes[0]);
            var texture = CreateTexture(topology, collapsed, textureSize);

            SaveTexture(texture, textureSize, "Vis.bmp");

            var structureGo = engine.CreateGameObject();
            var structureRender = structureGo.Add<MaterialRenderComponent>();
            structureRender.Model = Model.Load("Content/Structure.obj");
            structureRender.Texture = Texture.LoadFromMemory(texture, textureSize, textureSize);            
            structureGo.Scale = new Vector3(0.9f);
            structureGo.Position = new Vector3(-5.25f, 1.15f, 0.25f);
            structureGo.AddChild(engine.Axis(5));

            //var vis = CreateTopologyVisualization(engine, topology, collapsed);
            //vis.Position = new Vector3(-5, 1, 0);

            engine.Run();
        }
    }
}