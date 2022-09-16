using Assimp;
using GameEngine.Components;
using GameEngine.Core;
using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Utils;
using MeshTopology;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;
using Mesh = GameEngine.Graphics.Mesh;

namespace PatternDemo
{
    public class Program
    {
        public static int DefineTextureSize(Mesh mesh)
        {
            int degree = 0;

            foreach (Vertex v in mesh.Vertices)
            {
                degree = Math.Max(degree, GetDecimalPlaces(v.TextureCoords.X));
                degree = Math.Max(degree, GetDecimalPlaces(v.TextureCoords.Y));
            }

            //return (int)Math.Pow(10, degree);
            return 2048;
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

        public static Dictionary<TopologyNode, Rule> Wfc(Topology topology, List<Rule> wallRules, List<Rule> floorRules)
        {
            var possibilities = new Dictionary<TopologyNode, List<Rule>>();
            var forRecalculation = new List<TopologyNode>();
 
            foreach (var node in topology)
            {
                possibilities[node] = new List<Rule>(SelectRuleSet(node, wallRules, floorRules));
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
                                possibilities[n] = new List<Rule>(SelectRuleSet(n, wallRules, floorRules)); 
                            }

                            forRecalculation.Clear();
                            break;
                        }

                        possibilities[node] = new List<Rule>(SelectRuleSet(node, wallRules, floorRules));

                        foreach (var neighbour in node.Neighbours)
                        {
                            possibilities[neighbour] = new List<Rule>(SelectRuleSet(neighbour, wallRules, floorRules));
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

        public static List<Rule> SelectRuleSet(TopologyNode node, List<Rule> wallRules, List<Rule> floorRules)
        {
            var centroid = node.Face.Centroid();
            var a = (node.Face[0].Position - centroid).Normalized();
            var b = (node.Face[1].Position - centroid).Normalized();
            var c = Vector3.Cross(a, b);
            return new List<Rule>((c.Y != 0) ? floorRules : wallRules);
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

        public static void Main(string[] args)
        {
            var random = new Random();
            int seed = random.Next();
            Console.WriteLine(seed);
            Utils.UseSeed(seed);

            //Utils.UseSeed(96350589);

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

            var wallRules = RulesLoader.CreateRules("Content/Samples/1/WallLogical.png", "Content/Samples/1/WallDetailed.png");
            var floorRules = RulesLoader.CreateRules("Content/Samples/1/FloorLogical.png", "Content/Samples/1/FloorDetailed.png");
            var collapsed = Wfc(topology, wallRules, floorRules);
            
            var textureSize = DefineTextureSize(quadModel.Meshes[0]);
            var detailedTextureData = TextureCreator.CreateDetailedTexture(topology, collapsed, textureSize);
            var logicalTextureData = TextureCreator.CreateLogicalTexture(topology, collapsed, textureSize);
            var detailedTexture = Texture.LoadFromMemory(detailedTextureData, textureSize, textureSize);
            var logicalTexture = Texture.LoadFromMemory(logicalTextureData, textureSize, textureSize);

            var structureGo = engine.CreateGameObject();
            var structureRender = structureGo.Add<MaterialRenderComponent>();
            structureRender.Model = Model.Load("Content/Room.obj");
            //structureRender.Model = Model.Load("Content/Structure.obj");
            structureRender.Texture = detailedTexture;            
            structureGo.Scale = new Vector3(1.0f);
            //structureGo.Position = new Vector3(0.0f, 1.0f, 0.0f);
            structureGo.AddChild(engine.Axis(5));

            var switcher = operatorGo.Add<DebugerComponent>();
            switcher.Model = structureGo;
            switcher.DetailedTexture = detailedTexture;
            switcher.LogicalTexture = logicalTexture;
            switcher.MeshTopology = topology;

            engine.Run();
        }
    }
}