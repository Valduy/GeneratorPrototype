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
                    int bmpX = x + i;
                    int bmpY = y + j;
                    var color = bmp.GetPixel(bmpX, bmpY);
                    defenition[i, j] = color;
                }
            }

            return defenition;
        }
    }
}
