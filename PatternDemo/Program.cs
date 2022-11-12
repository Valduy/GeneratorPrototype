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
        public const int LogicalResolution = 4;
        public const int DetailedResolution = 20;

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
            MeshTopology.Topology topology, 
            List<Rule> wallRules, 
            List<Rule> floorRules,
            List<Rule> ceilRules,
            List<Rule?[,]> wallBigTiles,
            List<Rule?[,]> floorBigTiles)
        {
            var possibilities = new Dictionary<TopologyNode, List<Rule>>();
            var forRecalculation = new List<TopologyNode>();
            
            foreach (var node in topology.Where(n => !n.IsDefined()))
            {
                possibilities[node] = new List<Rule>(SelectRuleSet(node, wallRules, floorRules, ceilRules));
            }

            if (wallBigTiles.Any())
            {
                PlaceWallBigTiles(topology, possibilities, wallBigTiles);

                foreach (var defined in topology.Where(n => n.IsDefined()))
                {
                    forRecalculation.AddRange(defined.Neighbours.Where(n => !n.IsDefined()));
                }
            }

            if (floorBigTiles.Any())
            {
                PlaceFloorBigTiles(topology, possibilities, floorBigTiles);

                foreach (var defined in topology.Where(n => n.IsDefined()))
                {
                    forRecalculation.AddRange(defined.Neighbours.Where(n => !n.IsDefined()));
                }
            }

            if (!wallBigTiles.Any() && !floorBigTiles.Any())
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
                                possibilities[n] = new List<Rule>(SelectRuleSet(n, wallRules, floorRules, ceilRules)); 
                            }

                            forRecalculation.Clear();
                            break;
                        }

                        possibilities[node] = new List<Rule>(SelectRuleSet(node, wallRules, floorRules, ceilRules));

                        foreach (var neighbour in node.Neighbours.Where(n => !n.IsDefined()))
                        {
                            possibilities[neighbour] = new List<Rule>(SelectRuleSet(neighbour, wallRules, floorRules, ceilRules));
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

        public static void PlaceWallBigTiles(
            MeshTopology.Topology topology, 
            Dictionary<TopologyNode, List<Rule>> possibilities, 
            List<Rule?[,]> bigTiles)
        {
            int margin = 2;
            var walls = topology.ExtractXyGroups();
            walls.AddRange(topology.ExtractYzGroups());

            PlaceBigTiles(possibilities, walls, bigTiles, margin);
        }

        public static void PlaceFloorBigTiles(
            MeshTopology.Topology topology,
            Dictionary<TopologyNode, List<Rule>> possibilities,
            List<Rule?[,]> bigTiles)
        {
            int margin = 1;
            PlaceBigTiles(possibilities, topology
                .ExtractXzGroups()
                .Where(g => g.Enumerate().First(n => n != null)!.Face.GetNormal().Y < 0)
                .ToList(), bigTiles, margin);
        }

        public static void PlaceBigTiles(
            Dictionary<TopologyNode, List<Rule>> possibilities,
            List<TopologyNode?[,]> groups,
            List<Rule?[,]> bigTiles,            
            int margin)
        {
            foreach (var group in groups)
            {
                var randomTile = bigTiles.GetRandom();

                if (group.GetLength(0) < randomTile.GetLength(0) + margin * 2 ||
                    group.GetLength(1) < randomTile.GetLength(1) + margin * 2)
                {
                    continue;
                }

                var avaliablePlaces = FindAvaliablePlaces(group, randomTile, margin);

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
                            var node = group[randomPlace.X + x, randomPlace.Y + y]!;
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

        public static List<Vector2i> FindAvaliablePlaces(TopologyNode?[,] group, Rule?[,] bigTile, int margin)
        {
            var avaliablePlaces = new List<Vector2i>();

            for (int i = margin; i < group.GetLength(0) - bigTile.GetLength(0) + 1 - margin; i++)
            {
                for (int j = margin; j < group.GetLength(1) - bigTile.GetLength(1) + 1 - margin; j++) 
                {
                    if (IsAllNotNullInWindow(group, i - margin, j - margin, i + bigTile.GetLength(0) - 1 + margin, j + bigTile.GetLength(1) - 1 + margin))
                    {
                        avaliablePlaces.Add(new Vector2i(i, j));
                    }
                }          
            }

            return avaliablePlaces;
        }

        public static List<Rule> SelectRuleSet(
            TopologyNode node, 
            List<Rule> wallRules, 
            List<Rule> floorRules,
            List<Rule> ceilRules)
        {
            switch (node.Face.GetFaceOrientation())
            {
                case FaceOrientation.XY:
                case FaceOrientation.YZ:
                    return wallRules;
                case FaceOrientation.XZ:
                    var normal = node.Face.GetNormal();
                    return (normal.Y > 0) ? ceilRules : floorRules;
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
            CollectionsHelper.UseSeed(seed);

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
            quadModel = new Model(quadModel.Meshes[0].SortVertices());
            var topology = new Topology(quadModel.Meshes[0], 4);

            var wallBigTiles = RulesLoader.ReadBigTiles(
                "Content/Samples/WallBigTilesLogical.png", 
                "Content/Samples/WallBigTilesDetailed.png",
                LogicalResolution, 
                DetailedResolution);

            var floorBigTiles = RulesLoader.ReadBigTiles(
                "Content/Samples/FloorBigTilesLogical.png", 
                "Content/Samples/FloorBigTilesDetailed.png", 
                LogicalResolution, 
                DetailedResolution);

            var wallRules = RulesLoader.CreateRules(
                "Content/Samples/1/WallLogical.png", 
                "Content/Samples/1/WallDetailed.png", 
                LogicalResolution, 
                DetailedResolution);

            var floorRules = RulesLoader.CreateRules(
                "Content/Samples/1/FloorLogical.png", 
                "Content/Samples/1/FloorDetailed.png", 
                LogicalResolution, 
                DetailedResolution);

            var ceilRules = RulesLoader.CreateRules(
                "Content/Samples/1/CeilLogical.png", 
                "Content/Samples/1/CeilDetailed.png", 
                LogicalResolution, 
                DetailedResolution);

            var collapsed = Wfc(topology, wallRules, floorRules, ceilRules, wallBigTiles, floorBigTiles);

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