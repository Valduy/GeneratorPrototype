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

                var from = node.Face[0].TextureCoords * size;
                var to = node.Face[2].TextureCoords * size;

                var horizontalAxis = (node.Face[1].TextureCoords - node.Face[0].TextureCoords).Normalized();
                var verticalAxis = (node.Face[3].TextureCoords - node.Face[0].TextureCoords).Normalized();

                var direction = to - from;
                var square = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));

                for (int x = 0; x < square.X; x++)
                {
                    var defenition = acessor(rule);
                    int colorX = (int)(x * defenition.GetLength(0) / square.X);

                    for (int y = 0; y < square.Y; y++)
                    {
                        int colorY = (int)(y * defenition.GetLength(1) / square.Y);
                        var color = defenition[defenition.GetLength(0) - 1 - colorX, colorY];
                        var position = from + horizontalAxis * x + verticalAxis * y;
                        texture.SetColor(size, (int)position.X, (int)position.Y, color);
                    }
                }
            }

            return texture;
        }
    }
}
