using GameEngine.Mathematics;
using MeshTopology;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;

namespace PatternDemo
{
    public static class TextureCreator
    {
        public static byte[] CreateDetailedTexture(Topology topology, Dictionary<TopologyNode, Rule> collapsed, int size)
        {
            return CreateTexture(topology, collapsed, size, r => r.Detailed);
        }

        public static byte[] CreateLogicalTexture(Topology topology, Dictionary<TopologyNode, Rule> collapsed, int size)
        {
            return CreateTexture(topology, collapsed, size, r => r.Logical);
        }

        private static byte[] CreateTexture(Topology topology, Dictionary<TopologyNode, Rule> collapsed, int size, Func<Rule, Color[,]> acessor)
        {
            var texture = new byte[size * size * 4];

            foreach (var node in topology)
            {
                var rule = collapsed[node];
                var defenition = acessor(rule);

                var from = node.Face[1].TextureCoords * size;
     
                var horizontal = (node.Face[0].TextureCoords - node.Face[1].TextureCoords) * size;
                var horizontalAxis = horizontal.Normalized();

                var vertical = (node.Face[2].TextureCoords - node.Face[1].TextureCoords) * size;
                var verticalAxis = vertical.Normalized();

                var bounds = new Vector2(MathF.Abs(horizontal.SumComponents()), MathF.Abs(vertical.SumComponents()));

                for (int x = 0; x < bounds.X; x++)
                {
                    int colorX = (int)Mathematics.Map(x, 0, bounds.X, 0, defenition.GetLength(0));

                    for (int y = 0; y < bounds.Y; y++)
                    {
                        int colorY = (int)Mathematics.Map(y, 0, bounds.Y, 0, defenition.GetLength(1));
                        var color = defenition[colorX, colorY];
                        var position = from + horizontalAxis * x + verticalAxis * y;
                        texture.SetColor(size, (int)position.X, (int)position.Y, color);
                    }
                }
            }

            return texture;
        }
    }
}
