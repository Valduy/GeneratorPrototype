using GameEngine.Helpers;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;

namespace PatternDemo
{
    public static class RulesLoader
    {
        public static List<Rule> CreateRules(string logicalPath, string detailedPath)
        {
            var logicalBmp = new Bitmap(logicalPath);
            var detailedBmp = new Bitmap(detailedPath);

            var logicalDefenitions = ReadDefenitions(logicalBmp, Rule.LogicalResolution);
            var detailedDefenitions = ReadDefenitions(detailedBmp, Rule.DetailedResolution);

            return logicalDefenitions
                .Zip(detailedDefenitions, (l, d) => new Rule(l, d))
                .Where(r => !r.Logical.Enumerate().All(c => c.IsTransparent()))
                .ToList();
        }

        public static List<Rule?[,]> ReadBigTiles(string logicalPath, string detailedPath)
        {
            var logicalBmp = new Bitmap(logicalPath);
            var detailedBmp = new Bitmap(detailedPath);

            if (logicalBmp.Width / Rule.LogicalResolution != detailedBmp.Width / Rule.DetailedResolution ||
                logicalBmp.Height / Rule.LogicalResolution != detailedBmp.Height / Rule.DetailedResolution)
            {
                throw new ArgumentException("Sizes of bitmaps's isn't correct.");
            }

            int columns = logicalBmp.Width / Rule.LogicalResolution;
            int rows = logicalBmp.Height / Rule.LogicalResolution;
            var rules = new Rule?[rows, columns];

            var logicalDefenitions = ReadDefenitions(logicalBmp, Rule.LogicalResolution);
            var detailedDefenitions = ReadDefenitions(detailedBmp, Rule.DetailedResolution);
            var coords = new Dictionary<Rule, Vector2i>();

            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < columns; y++)
                {
                    var rule = new Rule(logicalDefenitions[rows * x + y], detailedDefenitions[rows * x + y]);

                    if (!rule.Logical.Enumerate().All(c => c.IsTransparent()))
                    {
                        rules[x, y] = rule;
                        coords[rule] = new Vector2i(x, y);
                    }
                    else
                    {
                        rules[x, y] = null;
                    }
                }
            }

            var groups = rules.ExtractGroups(coords);
            var result = new List<Rule?[,]>();
            return CreateTiles(rules, groups);
        }

        public static List<Color[,]> ReadDefenitions(Bitmap bmp, int tileSize)
        {
            var defenitions = new List<Color[,]>();

            for (int x = 0; x < bmp.Width; x += tileSize)
            {
                for (int y = 0; y < bmp.Height; y += tileSize)
                {
                    defenitions.Add(ReadRuleDefenition(bmp, x, y, tileSize));
                }
            }

            return defenitions;
        }

        public static Color[,] ReadRuleDefenition(Bitmap bmp, int x, int y, int tileSize)
        {
            var defenition = new Color[tileSize, tileSize];

            for (int i = 0; i < tileSize; i++)
            {
                for (int j = 0; j < tileSize; j++)
                {
                    var color = bmp.GetPixel(x + i, y + j);
                    defenition[i, j] = color;
                }
            }

            return defenition;
        }

        private static List<List<Vector2i>> ExtractGroups(this Rule?[,] rules, Dictionary<Rule, Vector2i> coords)
        {
            var groups = new List<List<Vector2i>>();
            var definedRules = new HashSet<Vector2i>(rules
                .Enumerate()
                .Where(r => r != null)
                .Select(r => coords[r!])
                .ToList());

            while (definedRules.Any()   )
            {
                var initial = definedRules.First();
                var group = rules.ExtractGroup(initial);
                definedRules.ExceptWith(group);
                groups.Add(group);
            }

            return groups;
        }

        private static List<Vector2i> ExtractGroup(this Rule?[,] rules, Vector2i initial)
        {
            if (rules[initial.X, initial.Y] == null)
            {
                throw new ArgumentException();
            }

            var group = new HashSet<Vector2i>();
            var visited = new HashSet<Vector2i> { initial };
            var queue = new Queue<Vector2i>();
            queue.Enqueue(initial);

            while (queue.Count > 0)
            {
                var coords = queue.Dequeue();
                group.Add(coords);

                foreach (var neighbour in rules.GetNeighboursCross(coords))
                {
                    if (!visited.Contains(neighbour) && rules[neighbour.X, neighbour.Y] != null)
                    {
                        queue.Enqueue(neighbour);
                        visited.Add(neighbour);
                    }
                }
            }

            return group
                .OrderBy(c => c.X)
                .ThenBy(c => c.Y)
                .ToList();
        }

        private static List<Rule?[,]> CreateTiles(Rule?[,] rules, List<List<Vector2i>> groups)
        {
            var result = new List<Rule?[,]>();

            foreach (var group in groups)
            {
                var fromX = group.Min(c => c.X);
                var fromY = group.Min(c => c.Y);
                var toX = group.Max(c => c.X);
                var toY = group.Max(c => c.Y);
                var width = toX - fromX + 1;
                var height = toY - fromY + 1;
                var bigTile = new Rule?[width, height];

                foreach (var c in group)
                {
                    bigTile[c.X - fromX, c.Y - fromY] = rules[c.X, c.Y];
                }

                result.Add(bigTile);
            }

            return result;
        }
    }
}
