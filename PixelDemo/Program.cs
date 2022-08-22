using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace PixelDemo
{
    public class Rule
    {
        public Color Color { get; set; }
        public List<Color> Colors { get; set; } = new();
    }

    public class RuleComparer : IEqualityComparer<Rule>
    {
        public bool Equals(Rule? x, Rule? y)
        {
            if (!x.Color.IsSame(y.Color))
            {
                return false;
            }

            foreach (var color in x.Colors)
            {
                if (!y.Colors.Any(c => c.IsSame(color)))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode([DisallowNull] Rule obj)
        {
            var hash = obj.Color.GetHashCode();

            foreach (var color in obj.Colors)
            {
                hash ^= color.GetHashCode();
            }

            return hash;
        }
    }

    public static class ColorHelper
    {
        public static bool IsSame(this Color lhs, Color rhs) =>
            lhs.A == rhs.A &&
            lhs.R == rhs.R &&
            lhs.G == rhs.G &&
            lhs.B == rhs.B;
    }

    public static class TextureHelper
    {
        public static void SetColor(this byte[] data, int size, Vector2 position, Color color)
            => data.SetColor(size, (int)position.X, (int)position.Y, color);

        public static void SetColor(this byte[] data, int size, int x, int y, Color color)
        {
            x = Math.Clamp(x, 0, size - 1);
            y = Math.Clamp(y, 0, size - 1);

            data[4 * x + 4 * size * y + 0] = color.R;
            data[4 * x + 4 * size * y + 1] = color.G;
            data[4 * x + 4 * size * y + 2] = color.B;
            data[4 * x + 4 * size * y + 3] = color.A;
        }
    }

    public class Program
    {
        public static List<Rule> CreateRules(string path)
        {
            var rules = new HashSet<Rule>(new RuleComparer());
            var bmp = new Bitmap(path);

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    var rule = new Rule { Color = bmp.GetPixel(x, y) };

                    rule.Colors.Add(x + 1 >= bmp.Width
                        ? bmp.GetPixel(0, y)
                        : bmp.GetPixel(x + 1, y));

                    rule.Colors.Add(y - 1 < 0
                        ? bmp.GetPixel(x, bmp.Height - 1)
                        : bmp.GetPixel(x, y - 1));

                    rule.Colors.Add(x - 1 < 0
                        ? bmp.GetPixel(bmp.Width - 1, y)
                        : bmp.GetPixel(x - 1, y));

                    rule.Colors.Add(y + 1 >= bmp.Height 
                        ? bmp.GetPixel(x, 0) 
                        : bmp.GetPixel(x, y + 1));

                    rules.Add(rule);
                }
            }

            return rules.ToList();
        }

        public static void Wfc(Topology<Rule> topology, List<Rule> rules)
        {
            var possibilities = new Dictionary<TopologyNode<Rule>, List<Rule>>();
            var forRecalculation = new List<TopologyNode<Rule>>();

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

            foreach (var pair in possibilities)
            {
                pair.Key.Data = pair.Value[0];
            }
        }

        public static List<Rule> FilterPossible(
            Dictionary<TopologyNode<Rule>, List<Rule>> possibilities,
            List<Rule> rules,
            TopologyNode<Rule> node)
            => rules.Where(r => IsPossible(possibilities, r, node)).ToList();

        public static bool IsPossible(
            Dictionary<TopologyNode<Rule>, List<Rule>> possibilities,
            Rule rule,
            TopologyNode<Rule> node)
        {
            foreach (var neighbour in node.Neighbours)
            {
                //var nodeIndex = neighbour.Neighbours.IndexOf(node);
                //var neighbourRules = possibilities[neighbour];

                //if (neighbourRules.All(r => !IsSame(r.Colors[nodeIndex], rule.Color)))
                //{
                //    return false;
                //}
                var neighbourRules = possibilities[neighbour];

                if (neighbourRules.All(r => r.Colors.All(c => !c.IsSame(rule.Color))))
                {
                    return false;
                }
            }

            return true;
        }

        public static byte[] GenerateTexture(Topology<Rule> topology, int size)
        {
            var texture = new byte[size * size * 4];
            
            foreach (var node in topology)
            {
                float[] horisontalCoords = node.Face.Vertices
                    .Select(v => v.TextureCoords.X)
                    .OrderBy(x => x)
                    .ToArray();

                float[] verticalCoords = node.Face.Vertices
                    .Select(v => v.TextureCoords.Y)
                    .OrderBy(x => x)
                    .ToArray();

                for (int x = (int)(horisontalCoords.First() * size); x < horisontalCoords.Last() * size; x++)
                {
                    for (int y = (int)(verticalCoords.First() * size); y < verticalCoords.Last() * size; y++)
                    {
                        texture.SetColor(size, x, y, node.Data.Color);
                    }
                }
            }

            return texture;
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
            var topology = new Topology<Rule>(quadModel.Meshes[0]);
            var rules = CreateRules("Content/Sample2.png");
            Wfc(topology, rules);
            var texture = GenerateTexture(topology, 1000);

            var structureGo = engine.CreateGameObject();
            var structureRender = structureGo.Add<MaterialRenderComponent>();
            structureRender.Model = Model.Load("Content/Structure.obj");
            structureRender.Texture = Texture.LoadFromMemory(texture, 1000, 1000);
            structureGo.Position = new Vector3(-5, 1, 0);
            structureGo.AddChild(engine.Axis(5));

            engine.Run();
        }
    }
}