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
    public static class TopologyNodeHepler
    {
        private static HashSet<TopologyNode> _defenitions = new();

        public static bool IsDefined(this TopologyNode node) 
            => _defenitions.Contains(node);

        public static void Define(this TopologyNode node) 
            => _defenitions.Add(node);

        public static void Undefine(this TopologyNode node) 
            => _defenitions.Remove(node);
    }

    public class Program
    {
        static Engine Engine;

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

        public static Dictionary<TopologyNode, Rule> Wfc(
            Topology topology, 
            List<Rule> wallRules, 
            List<Rule> floorRules,
            List<Rule?[,]> bigTiles)
        {
            var possibilities = new Dictionary<TopologyNode, List<Rule>>();
            var forRecalculation = new List<TopologyNode>();
            
            foreach (var node in topology.Where(n => !n.IsDefined()))
            {
                possibilities[node] = new List<Rule>(SelectRuleSet(node, wallRules, floorRules));
            }

            if (bigTiles.Any())
            {
                PlaceBigTiles(topology, possibilities, bigTiles);

                foreach (var defined in topology.Where(n => n.IsDefined()))
                {
                    forRecalculation.AddRange(defined.Neighbours.Where(n => !n.IsDefined()));
                }
            }
            else
            {
                var initial = topology.GetRandom();
                var rule = possibilities[initial].GetRandom();
                possibilities[initial] = new List<Rule> { rule };
                forRecalculation.AddRange(initial.Neighbours);
            }

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
                            foreach (var n in topology.Where(n => !n.IsDefined()))
                            {
                                possibilities[n] = new List<Rule>(SelectRuleSet(n, wallRules, floorRules)); 
                            }

                            forRecalculation.Clear();
                            break;
                        }

                        possibilities[node] = new List<Rule>(SelectRuleSet(node, wallRules, floorRules));

                        foreach (var neighbour in node.Neighbours.Where(n => !n.IsDefined()))
                        {
                            possibilities[neighbour] = new List<Rule>(SelectRuleSet(neighbour, wallRules, floorRules));
                        }

                        continue;
                    }

                    if (possibleHere.Count > filtered.Count)
                    {
                        forRecalculation.AddRange(node.Neighbours.Where(n => !n.IsDefined()));
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

                var rule = possibilities[maxNode].GetRandom();
                possibilities[maxNode] = new List<Rule> { rule };
                forRecalculation.AddRange(maxNode.Neighbours.Where(n => !n.IsDefined()));
            }

            return possibilities.ToDictionary(kvp => kvp.Key, kvp => kvp.Value[0]);
        }

        public static void PlaceBigTiles(
            Topology topology, 
            Dictionary<TopologyNode, List<Rule>> possibilities, 
            List<Rule?[,]> bigTiles)
        {
            int margin = 1;
            var walls = topology.ExtractXyGroups();
            walls.AddRange(topology.ExtractYzGroups());

            foreach (var wall in walls)
            {
                var randomTile = bigTiles.GetRandom();

                if (wall.GetLength(0) < randomTile.GetLength(0) + margin * 2 || 
                    wall.GetLength(1) < randomTile.GetLength(1) + margin * 2)
                {
                    continue;
                }

                var avaliablePlaces = FindAvaliablePlaces(wall, randomTile, margin);

                if (avaliablePlaces.Count == 0)
                {
                    continue;
                }

                var randomPlace = avaliablePlaces.GetRandom();

                for (int x = 0; x < randomTile.GetLength(0); x++)
                {
                    for (int y = 0; y < randomTile.GetLength(1); y++)
                    {
                        if (randomTile[x, y] != null)
                        {
                            var node = wall[randomPlace.X + x, randomPlace.Y + y]!;
                            var rule = randomTile[x, y]!;
                            possibilities[node] = new List<Rule> { rule };
                            node.Define();
                        }
                    }
                }
            }
        }

        public static bool IsAllNotNullInWindow<T>(T?[,] matrix, int fromX, int fromY, int toX, int toY)
        {
            for (int x = fromX; x <= toX; x++)
            {
                for (int y = fromY; y <= toY; y++)
                {
                    if (matrix[x, y] == null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static List<Vector2i> FindAvaliablePlaces(TopologyNode?[,] wall, Rule?[,] bigTile, int margin)
        {
            var avaliablePlaces = new List<Vector2i>();

            for (int i = margin; i < wall.GetLength(0) - bigTile.GetLength(0) - margin; i++)
            {
                for (int j = margin; j < wall.GetLength(1) - bigTile.GetLength(1) - margin; j++) 
                {
                    if (IsAllNotNullInWindow(wall, i - margin, j - margin, i + bigTile.GetLength(0) + margin, j + bigTile.GetLength(1) + margin))
                    {
                        avaliablePlaces.Add(new Vector2i(i, j));
                    }
                }          
            }

            return avaliablePlaces;
        }

        public static List<Rule> SelectRuleSet(TopologyNode node, List<Rule> wallRules, List<Rule> floorRules)
        {
            switch (node.Face.GetFaceOrientation())
            {
                case FaceOrientation.XY:
                case FaceOrientation.YZ:
                    return wallRules;
                case FaceOrientation.XZ:
                    return floorRules;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            Engine = engine;

            var operatorGo = engine.CreateGameObject();
            operatorGo.Add<Operator3DComponent>();
            operatorGo.Add<LightComponent>();
            operatorGo.Position = new Vector3(0, 0, 0);

            var grid = engine.Grid(20);
            var axis = engine.Axis(2);
            axis.Position = new Vector3(-11, 0, -11);

            var quadModel = Model.Load("Content/Room.obj", PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder);
            quadModel = new Model(quadModel.Meshes[0].SortVertices());
            var topology = new Topology(quadModel.Meshes[0]);

            var bigTiles = RulesLoader.ReadBigTiles("Content/Samples/BigTilesLogical.png", "Content/Samples/BigTilesDetailed.png");
            var wallRules = RulesLoader.CreateRules("Content/Samples/1/WallLogical.png", "Content/Samples/1/WallDetailed.png");
            var floorRules = RulesLoader.CreateRules("Content/Samples/1/FloorLogical.png", "Content/Samples/1/FloorDetailed.png");
            var collapsed = Wfc(topology, wallRules, floorRules, bigTiles);

            var textureSize = DefineTextureSize(quadModel.Meshes[0]);
            var detailedTextureData = TextureCreator.CreateDetailedTexture(topology, collapsed, textureSize);
            var logicalTextureData = TextureCreator.CreateLogicalTexture(topology, collapsed, textureSize);
            var detailedTexture = Texture.LoadFromMemory(detailedTextureData, textureSize, textureSize);
            var logicalTexture = Texture.LoadFromMemory(logicalTextureData, textureSize, textureSize);

            var structureGo = engine.CreateGameObject();
            var structureRender = structureGo.Add<MaterialRenderComponent>();
            structureRender.Model = new Model(quadModel.Meshes[0].TriangulateQuadMesh());
            structureRender.Texture = detailedTexture;
            structureGo.Scale = new Vector3(1.0f);
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