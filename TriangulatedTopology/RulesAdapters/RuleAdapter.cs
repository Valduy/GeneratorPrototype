using System.Drawing;
using TextureUtils;

namespace TriangulatedTopology.RulesAdapters
{
    public abstract class RuleAdapter
    {
        public readonly int Size;

        public RuleAdapter(int size)
        {
            Size = size;
        }

        public abstract Color Access(Rule rule, int x, int y);

        public Color[] GetSide(Rule rule, int index)
        {
            var result = new Color[Size];

            switch (index)
            {
                case 0:
                    for (int i = 0; i < Size; i++)
                    {
                        result[i] = Access(rule, i, 0);
                    }

                    break;
                case 1:
                    for (int i = 0; i < Size; i++)
                    {
                        result[i] = Access(rule, 0, i);
                    }

                    break;
                case 2:
                    for (int i = 0; i < Size; i++)
                    {
                        result[i] = Access(rule, i, Size - 1);
                    }

                    break;
                case 3:
                    for (int i = 0; i < Size; i++)
                    {
                        result[i] = Access(rule, Size - 1, i);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException("index");
            }

            return result;
        }
    }
}
